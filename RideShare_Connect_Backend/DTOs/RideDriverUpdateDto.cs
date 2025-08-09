using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class RideDriverUpdateDto
    {
        [Required]
        public int DriverId { get; set; }
    }
}
