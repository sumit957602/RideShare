// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.PaymentManagement;

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
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        // ... other DbSets

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<DriverRating> DriverRatings { get; set; }

        public DbSet<Ride> Rides { get; set; }
        public DbSet<RoutePoint> RoutePoints { get; set; }
        public DbSet<RideRecurrence> RideRecurrences { get; set; }
        public DbSet<RideBooking> RideBookings { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Commission> Commissions { get; set; }

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

            modelBuilder.Entity<DriverProfile>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(dp => dp.DriverId);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId);

            modelBuilder.Entity<Ride>()
                .HasMany(r => r.RoutePoints)
                .WithOne(rp => rp.Ride)
                .HasForeignKey(rp => rp.RideId);

            modelBuilder.Entity<Ride>()
                .HasMany(r => r.Recurrences)
                .WithOne(rr => rr.Ride)
                .HasForeignKey(rr => rr.RideId);

            modelBuilder.Entity<Ride>()
                .HasMany(r => r.Bookings)
                .WithOne(rb => rb.Ride)
                .HasForeignKey(rb => rb.RideId);

            modelBuilder.Entity<Ride>()
                .HasMany(r => r.Requests)
                .WithOne(rr => rr.Ride)
                .HasForeignKey(rr => rr.RideId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentMethod)
                .WithMany()
                .HasForeignKey(p => p.PaymentMethodId);

            modelBuilder.Entity<PaymentMethod>()
                .HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId);

            modelBuilder.Entity<Refund>()
                .HasOne(r => r.Payment)
                .WithMany()
                .HasForeignKey(r => r.PaymentId);

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany()
                .HasForeignKey(wt => wt.WalletId);

            modelBuilder.Entity<Commission>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId);
        }
    }
}