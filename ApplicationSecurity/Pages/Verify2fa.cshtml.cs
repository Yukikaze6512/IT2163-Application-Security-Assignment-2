using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Data;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    public class Verify2faModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public Verify2faModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLogService,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Verification code is required.")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
            [Display(Name = "Verification Code")]
            public string Code { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return RedirectToPage("/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return RedirectToPage("/Login");

            var authenticatorCode = Input.Code.Replace(" ", "").Replace("-", "");
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode, isPersistent: false, rememberClient: false);

            if (result.Succeeded)
            {
                // Create new UserSession
                var sessionId = Guid.NewGuid().ToString();
                var userSession = new UserSession
                {
                    UserId = user.Id,
                    SessionId = sessionId,
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.UserSessions.Add(userSession);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("SessionId", sessionId);

                await _auditLogService.LogAsync(user.Id, "Login", "User logged in with 2FA verification.");
                return RedirectToPage("/Index");
            }

            if (result.IsLockedOut)
            {
                await _auditLogService.LogAsync(user.Id, "AccountLocked",
                    "Account locked after multiple failed 2FA attempts.");
                ModelState.AddModelError(string.Empty,
                    "Account locked due to multiple failed attempts. Please try again later.");
                return Page();
            }

            await _auditLogService.LogAsync(user.Id, "2FAFailed", "Invalid 2FA verification code.");
            ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
            return Page();
        }
    }
}
