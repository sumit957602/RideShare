using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.UserManagement;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMode { get; set; }

        [ForeignKey("PaymentMethod")]
        public int? PaymentMethodId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // e.g., "Completed", "Pending", "Failed"

        [StringLength(100)]
        public string? ProviderTransactionId { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual RideBooking Booking { get; set; }
        public virtual PaymentMethod? PaymentMethod { get; set; }
    }
}
