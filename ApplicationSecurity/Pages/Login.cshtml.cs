using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogService;
        private readonly ReCaptchaService _reCaptchaService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLogService,
            ReCaptchaService reCaptchaService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _reCaptchaService = reCaptchaService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? Message { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            public string? RecaptchaToken { get; set; }
        }

        public void OnGet(string? message)
        {
            Message = message switch
            {
                "registered" => "Registration successful! Please log in.",
                "session_expired" => "Your session has expired. Please log in again.",
                "another_login" => "You have been logged out because another login was detected on a different device.",
                "logged_out" => "You have been logged out successfully.",
                "password_changed" => "Password changed successfully. Please log in with your new password.",
                "password_reset" => "Password reset successfully. Please log in with your new password.",
                _ => null
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Verify reCAPTCHA v3
            if (!await _reCaptchaService.VerifyToken(Input.RecaptchaToken))
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                return Page();
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                await _auditLogService.LogAsync(user.Id, "LoginAttempt_LockedOut",
                    $"Login attempt while account is locked until {lockoutEnd?.UtcDateTime:HH:mm:ss UTC}.");
                ModelState.AddModelError(string.Empty,
                    $"Account is locked due to multiple failed attempts. Try again after {lockoutEnd?.LocalDateTime:hh:mm tt}.");
                return Page();
            }

            // Attempt sign-in
            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Set session ID for multi-device login detection
                var sessionId = Guid.NewGuid().ToString();
                user.SessionId = sessionId;
                await _userManager.UpdateAsync(user);
                HttpContext.Session.SetString("SessionId", sessionId);

                await _auditLogService.LogAsync(user.Id, "Login", "User logged in successfully.");
                return RedirectToPage("/Index");
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("/Verify2fa");
            }

            if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync(user.Id, "AccountLocked",
                    "Account locked due to multiple failed login attempts.");
                ModelState.AddModelError(string.Empty,
                    "Account locked due to multiple failed login attempts. Please try again in 15 minutes.");
                return Page();
            }

            // Failed login
            await _auditLogService.LogAsync(user.Id, "LoginFailed", "Invalid password attempt.");
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }
    }
}
