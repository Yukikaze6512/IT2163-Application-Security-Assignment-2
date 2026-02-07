using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EncryptionService _encryptionService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            EncryptionService encryptionService,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string NRIC { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? ResumeFileName { get; set; }
        public string WhoAmI { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            // Decode HTML-encoded fields for display
            FirstName = WebUtility.HtmlDecode(user.FirstName);
            LastName = WebUtility.HtmlDecode(user.LastName);
            Gender = user.Gender;
            Email = user.Email!;
            DateOfBirth = user.DateOfBirth;
            ResumeFileName = user.ResumeFileName;
            WhoAmI = WebUtility.HtmlDecode(user.WhoAmI);

            // Decrypt NRIC for display
            try
            {
                NRIC = _encryptionService.Decrypt(user.EncryptedNRIC);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt NRIC for user {UserId}", user.Id);
                NRIC = "[Decryption Error]";
            }

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadResumeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.ResumePath))
                return NotFound();

            if (!System.IO.File.Exists(user.ResumePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(user.ResumePath);
            var extension = Path.GetExtension(user.ResumePath).ToLower();
            var contentType = extension == ".pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(fileBytes, contentType, user.ResumeFileName ?? "resume" + extension);
        }
    }
}
