using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurity.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string EncryptedNRIC { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        public string? ResumePath { get; set; }

        public string? ResumeFileName { get; set; }

        [Required]
        public string WhoAmI { get; set; } = string.Empty;

        public string? SessionId { get; set; }

        public DateTime? LastPasswordChangeDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
