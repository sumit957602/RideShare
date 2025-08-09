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
    }
}
