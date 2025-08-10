using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class DriverProfile
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Required]
        [StringLength(30)]
        public string BackgroundCheckStatus { get; set; } 

        [Range(0, 50)]
        public int DrivingExperienceYears { get; set; }

        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        public virtual Driver Driver { get; set; }
    }
}
