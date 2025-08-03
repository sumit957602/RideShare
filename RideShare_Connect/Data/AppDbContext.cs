using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.AdminManagement;

namespace RideShareConnect.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // User Management
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserVerification> UserVerifications { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        // Ride Management
        public DbSet<Ride> Rides { get; set; }
        public DbSet<RideRecurrence> RideRecurrences { get; set; }
        public DbSet<RoutePoint> RoutePoints { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }
        public DbSet<RideBooking> RideBookings { get; set; }
        public DbSet<BookingHistory> BookingHistories { get; set; }

        // Vehicle Management
        public DbSet<Driver> Drivers { get; set; } // ✅ Added
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<DriverRating> DriverRatings { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<DocumentReminder> DocumentReminders { get; set; }

        // Payment Management
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }

        // Admin & System Management
        public DbSet<Admin> Admins { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Analytics> Analytics { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ One-to-One: Driver <-> DriverProfile (shared primary key and foreign key)
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.DriverProfile)
                .WithOne(dp => dp.Driver)
                .HasForeignKey<DriverProfile>(dp => dp.DriverId);
        }
    }
}
