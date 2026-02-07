using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Data;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuditLogService _auditLogService;
        private readonly ApplicationDbContext _context;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AuditLogService auditLogService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditLogService = auditLogService;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? Message { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Current password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required.")]
            [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your new password.")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            public string ConfirmNewPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? message)
        {
            Message = message switch
            {
                "password_expired" => "Your password has expired. Please change it now.",
                _ => null
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            // Check minimum password age (cannot change within 5 minutes of last change)
            if (user.LastPasswordChangeDate.HasValue)
            {
                var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangeDate.Value;
                if (timeSinceLastChange.TotalMinutes < 5)
                {
                    ModelState.AddModelError(string.Empty,
                        "You cannot change your password within 5 minutes of the last change.");
                    return Page();
                }
            }

            // Check password history (cannot reuse last 2 passwords)
            var passwordHistories = _context.PasswordHistories
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedDate)
                .Take(2)
                .ToList();

            var passwordHasher = new PasswordHasher<ApplicationUser>();
            foreach (var history in passwordHistories)
            {
                var verifyResult = passwordHasher.VerifyHashedPassword(user, history.PasswordHash, Input.NewPassword);
                if (verifyResult == PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty,
                        "You cannot reuse your last 2 passwords. Please choose a different password.");
                    return Page();
                }
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

            if (result.Succeeded)
            {
                // Save to password history
                _context.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash!,
                    CreatedDate = DateTime.UtcNow
                });

                // Keep only last 2 entries in history
                var oldHistory = _context.PasswordHistories
                    .Where(p => p.UserId == user.Id)
                    .OrderByDescending(p => p.CreatedDate)
                    .Skip(2)
                    .ToList();
                _context.PasswordHistories.RemoveRange(oldHistory);

                // Update last password change date
                user.LastPasswordChangeDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(user.Id, "PasswordChanged", "Password changed successfully.");

                // Sign out and redirect to login
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();

                return RedirectToPage("/Login", new { message = "password_changed" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
