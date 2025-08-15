using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class VehicleRegisterDto
    {
        [Required]
        [StringLength(50)]
        public string CarMaker { get; set; }

        [Required]
        [StringLength(50)]
        public string CarModel { get; set; }

        [Required]
        [StringLength(30)]
        public string VehicleType { get; set; }

        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; }

        [Range(1990, 2100)]
        public int Year { get; set; }
    }
}
