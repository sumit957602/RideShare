using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class RideBookingRequestDto
    {
        [Required]
        public int RideId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 10)]
        public int Seats { get; set; }

        [Required]
        [StringLength(100)]
        public string PickupLocation { get; set; }

        [Required]
        [StringLength(100)]
        public string DropLocation { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal DistanceKm { get; set; }
    }
}
