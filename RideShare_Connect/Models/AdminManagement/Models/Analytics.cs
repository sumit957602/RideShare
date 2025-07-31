using System;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Models.AdminManagement
{
    public class Analytics
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MetricName { get; set; }  // e.g., TotalRides, Revenue, ActiveDrivers

        [Required]
        public decimal Value { get; set; }  // Stores the numeric value of the metric

        [Required]
        public DateTime CapturedAt { get; set; }  // When the metric was recorded
    }
}
