using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly AuditLogService _auditLogService;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            AuditLogService auditLogService)
        {
            _userManager = userManager;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool EmailSent { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Always show success message to prevent email enumeration
            EmailSent = true;

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Page("/ResetPassword", null,
                    new { token, email = user.Email }, Request.Scheme);

                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email!,
                        "Reset Password - Ace Job Agency",
                        $@"<h2>Password Reset Request</h2>
                        <p>You requested a password reset for your Ace Job Agency account.</p>
                        <p>Click the link below to reset your password:</p>
                        <p><a href='{resetLink}'>Reset Password</a></p>
                        <p>If you did not request this, please ignore this email.</p>
                        <p>This link will expire in 1 hour.</p>");
                }
                catch (Exception)
                {
                    // Log but don't expose error to user
                }

                await _auditLogService.LogAsync(user.Id, "PasswordResetRequested",
                    "Password reset email requested.");
            }

            return Page();
        }
    }
}
