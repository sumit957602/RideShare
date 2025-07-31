using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.RideManagement
{
    public class RoutePoint
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Ride")]
        public int RideId { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } 

        [Required]
        public int SequenceNumber { get; set; } 

        public virtual Ride Ride { get; set; }
    }
}
