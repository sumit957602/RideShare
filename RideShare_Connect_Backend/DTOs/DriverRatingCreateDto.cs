using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class DriverRatingCreateDto
    {
        [Required]
        public int RideId { get; set; }

        [Required]
        public int DriverId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Review { get; set; }
    }
}
