using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.ViewModels
{
    public class AddVehicleViewModel
    {
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

        [Range(1990, 2100)]
        public int Year { get; set; }
    }
}
