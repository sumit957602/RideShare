using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class RideCreateDto
    {
        [Required]
        public int DriverId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public string Origin { get; set; }

        [Required]
        public string Destination { get; set; }

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public int TotalSeats { get; set; }

        [Required]
        public decimal DistanceKm { get; set; }

        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }

        public List<RoutePointDto> RoutePoints { get; set; } = new();
    }
}
