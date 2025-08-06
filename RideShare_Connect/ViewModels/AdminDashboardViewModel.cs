using System.Collections.Generic;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.AdminManagement;

namespace RideShare_Connect.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public int RidesCompleted { get; set; }
        public int OpenReports { get; set; }

        public List<User> Users { get; set; } = new();
        public List<Driver> Drivers { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();

        public List<Ride> Rides { get; set; } = new();
        public List<RideBooking> RideBookings { get; set; } = new();
    }
}
