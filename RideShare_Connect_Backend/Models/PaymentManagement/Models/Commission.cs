using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.RideManagement;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class Commission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PlatformFee { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal Percentage { get; set; } 

        [Required]
        public bool PaidOut { get; set; }

        public DateTime? PayoutDate { get; set; }

        // Navigation property
        public virtual RideBooking Booking { get; set; }
    }
}
