// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; } // Add this!
        public DbSet<UserVerification> UserVerifications { get; set; }
        // ... other DbSets

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the one-to-one relationship between User and UserProfile
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId); // This specifies the foreign key

            // Optional: Ensure email is unique
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<UserVerification>()
                .HasOne(v => v.User)
                .WithMany(u => u.Verifications)
                .HasForeignKey(v => v.UserId);
        }
    }
}