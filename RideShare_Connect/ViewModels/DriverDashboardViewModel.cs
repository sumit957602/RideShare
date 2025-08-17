using System.Collections.Generic;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.RideManagement;

namespace RideShare_Connect.ViewModels
{
    public class DriverDashboardViewModel
    {
        public Driver Driver { get; set; }
        public DriverProfile DriverProfile { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public IEnumerable<Ride> CreatedRides { get; set; } = new List<Ride>();
        public IEnumerable<RideBooking> AcceptedRideBookings { get; set; } = new List<RideBooking>();
        public double AverageRating { get; set; }
        public IEnumerable<DriverRating> RecentRatings { get; set; } = new List<DriverRating>();
    }
}
