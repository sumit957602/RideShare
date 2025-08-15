using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class VehicleRegisterDto
    {
        [Required]
        [StringLength(50)]
        public required string CarMaker { get; set; }

        [Required]
        [StringLength(50)]
        public required string CarModel { get; set; }

        [Required]
        [StringLength(30)]
        public required string VehicleType { get; set; }

        [Required]
        [StringLength(20)]
        public required string LicensePlate { get; set; }

        [Range(1990, 2100)]
        public required int Year { get; set; }
    }
}
