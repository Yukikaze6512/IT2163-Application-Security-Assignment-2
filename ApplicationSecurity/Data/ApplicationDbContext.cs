using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ApplicationSecurity.Models;

namespace ApplicationSecurity.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
            });

            builder.Entity<PasswordHistory>(entity =>
            {
                entity.HasIndex(e => e.UserId);
            });

            builder.Entity<UserSession>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SessionId);
            });
        }
    }
}
