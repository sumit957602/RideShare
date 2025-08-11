using RideShare_Connect.Models.UserManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Driver")]
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

        [Required]
        [StringLength(20)]
        public string VerificationStatus { get; set; }

        [Range(1990, 2100)]
        public int Year { get; set; }

        // Navigation Properties
        public virtual Driver Driver { get; set; }
    }
}
