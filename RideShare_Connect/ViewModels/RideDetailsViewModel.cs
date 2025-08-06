using System.Collections.Generic;
using RideShare_Connect.Models.RideManagement;

namespace RideShare_Connect.ViewModels
{
    public class RideDetailsViewModel
    {
        public Ride Ride { get; set; }
        public List<string> Riders { get; set; } = new();
    }
}
