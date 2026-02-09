using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ApplicationSecurity.Data;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogService;
        private readonly ApplicationDbContext _context;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLogService,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _context = context;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Mark session as inactive
                    var userSession = await _context.UserSessions
                        .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == user.Id);
                    
                    if (userSession != null)
                    {
                        userSession.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                }

                await _auditLogService.LogAsync(user.Id, "Logout", "User logged out.");
            }

            // Sign out and clear session
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();

            return RedirectToPage("/Login", new { message = "logged_out" });
        }
    }
}
