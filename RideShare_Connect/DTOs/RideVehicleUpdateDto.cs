using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class RideVehicleUpdateDto
    {
        [Required]
        public int VehicleId { get; set; }
    }
}
