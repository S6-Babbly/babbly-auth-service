using babbly_auth_service.Models;
using Microsoft.EntityFrameworkCore;

namespace babbly_auth_service.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Auth0Id)
                .IsUnique();

            // Configure serialization for string arrays (roles)
            modelBuilder.Entity<User>()
                .Property(u => u.Roles)
                .HasConversion(
                    v => string.Join(',', v ?? Array.Empty<string>()),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }
} 