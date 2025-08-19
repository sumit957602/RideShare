using System.Collections.Generic;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect.ViewModels
{
    public class UserDashboardViewModel
    {
        public UserProfile Profile { get; set; }
        public int RidesBooked { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal WalletBalance { get; set; }
        public double Rating { get; set; }
        public List<RideBooking> Bookings { get; set; } = new();
        public List<RideBooking> RecentBookings { get; set; } = new();
        public List<Ride> AvailableRides { get; set; } = new();
        public Dictionary<int, string> PaymentModes { get; set; } = new();
        public int ActiveRidesCount { get; set; }
        public int CancelledRidesCount { get; set; }
        public int CompletedRidesCount { get; set; }
        public int TotalBookingsCount { get; set; }
    }
}
