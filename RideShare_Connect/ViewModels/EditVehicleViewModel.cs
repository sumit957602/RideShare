using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.ViewModels
{
    public class EditVehicleViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Driver Id")]
        public int DriverId { get; set; }

        [Required]
        [Display(Name = "Car Maker")]
        public string CarMaker { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; }

        [Required]
        [Display(Name = "License Plate")]
        public string LicensePlate { get; set; }

        [Required]
        [Display(Name = "Verification Status")]
        public string VerificationStatus { get; set; }

        [Range(1990, 2100)]
        public int Year { get; set; }
    }
}
