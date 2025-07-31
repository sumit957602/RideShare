using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.RideManagement
{
    public class BookingHistory
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } 

        [Required]
        public DateTime ActionTimestamp { get; set; }

        public virtual RideBooking Booking { get; set; }
    }
}
