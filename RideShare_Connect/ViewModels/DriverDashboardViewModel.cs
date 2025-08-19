using System.Collections.Generic;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.DTOs;

namespace RideShare_Connect.ViewModels
{
    public class DriverDashboardViewModel
    {
        public Driver Driver { get; set; }
        public DriverProfile DriverProfile { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public IEnumerable<Ride> CreatedRides { get; set; } = new List<Ride>();
        public IEnumerable<RideBookingDetailsDto> AcceptedRideBookings { get; set; } = new List<RideBookingDetailsDto>();
        public double AverageRating { get; set; }
        public IEnumerable<DriverRating> RecentRatings { get; set; } = new List<DriverRating>();
        public decimal WalletBalance { get; set; }
        public decimal TotalEarnings { get; set; }

        // Overview statistics
        public int VehicleCount { get; set; }
        public int CreatedRidesCount { get; set; }
        public int AcceptedRidesCount { get; set; }
        public int ActiveRidesCount { get; set; }
        public int CancelledRidesCount { get; set; }
        public int TotalRidesCount { get; set; }
    }
}
