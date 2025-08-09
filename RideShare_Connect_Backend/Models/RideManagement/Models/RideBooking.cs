using RideShare_Connect.Models.UserManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.RideManagement
{
    public class RideBooking
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Ride")]
        public int RideId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int PassengerId { get; set; }

        [Required]
        [Range(1, 10)]
        public int BookedSeats { get; set; }

        [Required]
        [StringLength(100)]
        public string PickupLocation { get; set; }

        [Required]
        [StringLength(100)]
        public string DropLocation { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal DistanceKm { get; set; }

        [Required]
        public DateTime BookingTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        // Navigation Properties
        public virtual Ride Ride { get; set; }
        public virtual User User { get; set; }

        public virtual ICollection<BookingHistory> BookingHistories { get; set; }
    }
}
