using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class MaintenanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceType { get; set; }  

        [Required]
        public DateTime ServiceDate { get; set; }

        [StringLength(1000)]
        public string Details { get; set; } 
        public virtual Vehicle Vehicle { get; set; }
    }
}
