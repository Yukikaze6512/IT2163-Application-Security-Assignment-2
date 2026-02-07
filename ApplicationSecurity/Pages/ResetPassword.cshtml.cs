using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Data;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogService;
        private readonly ApplicationDbContext _context;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLogService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _auditLogService = auditLogService;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string Token { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required.")]
            [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToPage("/Login");

            Input = new InputModel { Token = token, Email = email };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that user doesn't exist
                return RedirectToPage("/Login", new { message = "password_reset" });
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

            var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);

            if (result.Succeeded)
            {
                // Save to password history
                _context.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash!,
                    CreatedDate = DateTime.UtcNow
                });

                // Keep only last 2 entries
                var oldHistory = _context.PasswordHistories
                    .Where(p => p.UserId == user.Id)
                    .OrderByDescending(p => p.CreatedDate)
                    .Skip(2)
                    .ToList();
                _context.PasswordHistories.RemoveRange(oldHistory);

                // Update last password change date
                user.LastPasswordChangeDate = DateTime.UtcNow;

                // Reset lockout
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(user.Id, "PasswordReset", "Password reset successfully via email link.");

                return RedirectToPage("/Login", new { message = "password_reset" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
