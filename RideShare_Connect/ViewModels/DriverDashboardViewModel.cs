using System.Collections.Generic;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.ViewModels
{
    public class DriverDashboardViewModel
    {
        public UserProfile Profile { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public double AverageRating { get; set; }
        public IEnumerable<DriverRating> RecentRatings { get; set; } = new List<DriverRating>();
    }
}
