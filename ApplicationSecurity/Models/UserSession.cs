using System.ComponentModel.DataAnnotations;

namespace ApplicationSecurity.Models
{
    public class UserSession
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string SessionId { get; set; } = string.Empty;

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}
