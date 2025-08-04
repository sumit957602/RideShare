using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.RideManagement
{
    public class RideRequest
    {
        public int Id { get; set; }

        [Required]
        public int PassengerId { get; set; } 

        [Required]
        [ForeignKey("Ride")]
        public int RideId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } 

        [Required]
        public DateTime RequestTime { get; set; }

        public virtual Ride Ride { get; set; }
    }
}
