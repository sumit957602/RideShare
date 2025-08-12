using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect.ViewModels
{
    public class EditRideViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Origin { get; set; }

        [Required]
        public string Destination { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DepartureDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan DepartureTime { get; set; }

        [Required]
        [Range(1, 20)]
        public int Seats { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal PricePerSeat { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal DistanceKm { get; set; }

        public bool IsRecurring { get; set; }

        public List<Vehicle> Vehicles { get; set; } = new();
    }
}
