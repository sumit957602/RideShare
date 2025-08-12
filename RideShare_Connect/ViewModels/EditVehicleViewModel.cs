using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.ViewModels
{
    public class EditVehicleViewModel
    {
        public int Id { get; set; }

        public int DriverId { get; set; }

        [Required]
        [StringLength(50)]
        public string CarMaker { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; }

        [Required]
        [StringLength(30)]
        public string VehicleType { get; set; }

        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; }

        [StringLength(20)]
        public string? VerificationStatus { get; set; }

        [Range(1990, 2100)]
        public int Year { get; set; }
    }
}
