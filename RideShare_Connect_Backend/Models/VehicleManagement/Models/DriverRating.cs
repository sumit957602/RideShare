using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.RideManagement;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class DriverRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("DriverId")]
        public int DriverId { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int PassengerId { get; set; }

        [Required]
        [ForeignKey("Ride")]
        public int RideId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Review { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User Driver { get; set; }
        public virtual User User { get; set; }
        public virtual Ride Ride { get; set; }
    }
}
