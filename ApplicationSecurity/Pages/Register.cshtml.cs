using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationSecurity.Data;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

namespace ApplicationSecurity.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EncryptionService _encryptionService;
        private readonly AuditLogService _auditLogService;
        private readonly ReCaptchaService _reCaptchaService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            EncryptionService encryptionService,
            AuditLogService auditLogService,
            ReCaptchaService reCaptchaService,
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _encryptionService = encryptionService;
            _auditLogService = auditLogService;
            _reCaptchaService = reCaptchaService;
            _environment = environment;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
            [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "First name can only contain letters, spaces, and hyphens.")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
            [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "Last name can only contain letters, spaces, and hyphens.")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Gender is required.")]
            [Display(Name = "Gender")]
            public string Gender { get; set; } = string.Empty;

            [Required(ErrorMessage = "NRIC is required.")]
            [RegularExpression(@"^[STFGM]\d{7}[A-Z]$", ErrorMessage = "Invalid NRIC format (e.g., S1234567A).")]
            [Display(Name = "NRIC")]
            public string NRIC { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
                ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Date of birth is required.")]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime DateOfBirth { get; set; }

            [Required(ErrorMessage = "Resume is required.")]
            [Display(Name = "Resume")]
            public IFormFile? Resume { get; set; }

            [Required(ErrorMessage = "Who Am I is required.")]
            [Display(Name = "Who Am I")]
            public string WhoAmI { get; set; } = string.Empty;

            public string? RecaptchaToken { get; set; }
        }

        public void OnGet()
        {
            Input.DateOfBirth = DateTime.Today;
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

            // Validate resume file type (.docx or .pdf only)
            if (Input.Resume != null)
            {
                var allowedExtensions = new[] { ".docx", ".pdf" };
                var fileExtension = Path.GetExtension(Input.Resume.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Input.Resume", "Only .docx and .pdf files are allowed.");
                    return Page();
                }

                // Validate MIME type
                var allowedMimeTypes = new[]
                {
                    "application/pdf",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };
                if (!allowedMimeTypes.Contains(Input.Resume.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Input.Resume", "Invalid file type. Only .docx and .pdf files are allowed.");
                    return Page();
                }

                // Validate file size (max 10MB)
                if (Input.Resume.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("Input.Resume", "File size cannot exceed 10MB.");
                    return Page();
                }
            }

            // Check for duplicate email
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "This email address is already registered.");
                return Page();
            }

            // Save resume file to secure directory
            string? resumePath = null;
            string? resumeFileName = null;
            if (Input.Resume != null)
            {
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads");
                Directory.CreateDirectory(uploadsPath);
                var fileExtension = Path.GetExtension(Input.Resume.FileName).ToLower();
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                resumePath = Path.Combine(uploadsPath, uniqueFileName);
                resumeFileName = Input.Resume.FileName;

                using var stream = new FileStream(resumePath, FileMode.Create);
                await Input.Resume.CopyToAsync(stream);
            }

            // HTML encode text fields before saving (prevent stored XSS)
            var encodedFirstName = WebUtility.HtmlEncode(Input.FirstName);
            var encodedLastName = WebUtility.HtmlEncode(Input.LastName);
            var encodedWhoAmI = WebUtility.HtmlEncode(Input.WhoAmI);

            // Encrypt NRIC
            var encryptedNRIC = _encryptionService.Encrypt(Input.NRIC);

            // Create user
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = encodedFirstName,
                LastName = encodedLastName,
                Gender = Input.Gender,
                EncryptedNRIC = encryptedNRIC,
                DateOfBirth = Input.DateOfBirth,
                ResumePath = resumePath,
                ResumeFileName = resumeFileName,
                WhoAmI = encodedWhoAmI,
                LastPasswordChangeDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Save initial password to history
                _context.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash!,
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // Audit log
                await _auditLogService.LogAsync(user.Id, "Registration", "User registered successfully.");

                return RedirectToPage("/Login", new { message = "registered" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
