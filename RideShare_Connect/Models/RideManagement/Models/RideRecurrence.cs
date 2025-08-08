using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.RideManagement
{
    public class RideRecurrence
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Ride")]
        public int RideId { get; set; }

        [Required]
        [StringLength(50)]
        public string RecurrencePattern { get; set; } 

        [Required]
        public DateTime EndDate { get; set; }

        // Navigation property
        public virtual Ride Ride { get; set; }
    }
}
