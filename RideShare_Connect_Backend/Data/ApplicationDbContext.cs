using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.AdminManagement;

namespace RideShare_Connect.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; } 
        public DbSet<UserVerification> UserVerifications { get; set; }
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<DriverPasswordResetToken> DriverPasswordResetTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        // ... other DbSets

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<DriverRating> DriverRatings { get; set; }
        public DbSet<Driver> Driver { get; set; }

        public DbSet<Ride> Rides { get; set; }
        public DbSet<RoutePoint> RoutePoints { get; set; }
        public DbSet<RideRecurrence> RideRecurrences { get; set; }
        public DbSet<RideBooking> RideBookings { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<DriverWallet> DriverWallets { get; set; }
        public DbSet<DriverWalletTransaction> DriverWalletTransactions { get; set; }
        public DbSet<DriverEarning> DriverEarnings { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        public DbSet<UserTransactionSummary> UserTransactionSummaries { get; set; }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Analytics> Analytics { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Driver)
                .WithMany()
                .HasForeignKey(v => v.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserReport>()
                .HasOne(ur => ur.ReportedUser)
                .WithMany()
                .HasForeignKey(ur => ur.ReportedUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserReport>()
                .HasOne(ur => ur.ReportingUser)
                .WithMany()
                .HasForeignKey(ur => ur.ReportingUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserReport>()
                .HasOne(ur => ur.HandledByAdmin)
                .WithMany()
                .HasForeignKey(ur => ur.HandledByAdminId)
                .OnDelete(DeleteBehavior.SetNull); 

            modelBuilder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithMany()
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentMethod)
                .WithMany()
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserTransactionSummary>()
                .HasOne(t => t.Ride)
                .WithMany()
                .HasForeignKey(t => t.RideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserTransactionSummary>()
                .HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserTransactionSummary>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}