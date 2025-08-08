using RideShare_Connect.Models.VehicleManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.RideManagement
{
    public class Ride
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(100)]
        public string Origin { get; set; }

        [Required]
        [StringLength(100)]
        public string Destination { get; set; }

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        [Range(1, 20)]
        public int TotalSeats { get; set; }

        [Required]
        [Range(0, 20)]
        public int AvailableSeats { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal PricePerSeat { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal DistanceKm { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } 

        public bool IsRecurring { get; set; }

        // Navigation Properties
        public virtual Driver Driver { get; set; }
        public virtual Vehicle Vehicle { get; set; }

    }
}
