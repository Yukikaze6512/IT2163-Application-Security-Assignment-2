using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    [Authorize]
    public class Enable2faModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogService;

        public Enable2faModel(
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLogService)
        {
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string SharedKey { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public byte[]? QrCodeImage { get; set; }
        public bool Is2faEnabled { get; set; }
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Verification code is required.")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
            [Display(Name = "Verification Code")]
            public string Code { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

            if (!Is2faEnabled)
            {
                await LoadSharedKeyAndQrCodeAsync(user);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostEnableAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeAsync(user);
                return Page();
            }

            var verificationCode = Input.Code.Replace(" ", "").Replace("-", "");
            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Invalid verification code.");
                await LoadSharedKeyAndQrCodeAsync(user);
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _auditLogService.LogAsync(user.Id, "2FAEnabled", "Two-factor authentication enabled.");

            Is2faEnabled = true;
            StatusMessage = "Two-factor authentication has been enabled successfully!";
            return Page();
        }

        public async Task<IActionResult> OnPostDisableAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _auditLogService.LogAsync(user.Id, "2FADisabled", "Two-factor authentication disabled.");

            Is2faEnabled = false;
            StatusMessage = "Two-factor authentication has been disabled.";
            return Page();
        }

        private async Task LoadSharedKeyAndQrCodeAsync(ApplicationUser user)
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(key!);
            QrCodeUri = GenerateQrCodeUri(user.Email!, key!);

            // Generate QR code image
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(QrCodeUri, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            QrCodeImage = qrCode.GetGraphic(4);
        }

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
                result.Append(unformattedKey.AsSpan(currentPosition));

            return result.ToString().ToLower();
        }

        private static string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return $"otpauth://totp/AceJobAgency:{email}?secret={unformattedKey}&issuer=AceJobAgency&digits=6";
        }
    }
}
