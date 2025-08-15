using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.ViewModels
{
    public class AddVehicleViewModel
    {
        [Required]
        public string CarMaker { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string VehicleType { get; set; }

        [Required]
        public string LicensePlate { get; set; }

        [Range(1990, 2100)]
        public int Year { get; set; }
    }
}
