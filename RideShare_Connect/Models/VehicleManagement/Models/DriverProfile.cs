using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class DriverProfile
    {
        [Key, ForeignKey("Driver")] // shared PK and FK
        public int DriverId { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Required]
        [StringLength(30)]
        public string BackgroundCheckStatus { get; set; }

        [Range(0, 50)]
        public int DrivingExperienceYears { get; set; }

        [StringLength(500)]
        public string ReferenceContacts { get; set; }

        // Navigation
        public virtual Driver Driver { get; set; }
    }
}
