using System;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class MaintenanceRecordDto
    {
        [Required]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceType { get; set; }

        [Required]
        public DateTime ServiceDate { get; set; }

        public string Details { get; set; }
    }
}
