using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string CardType { get; set; }  // e.g., "Credit Card", "Debit Card", "UPI", "NetBanking"

        [Required]
        [StringLength(100)]
        public string? CardholderName { get; set; }

        [Required]
        [StringLength(16)]
        public string? CardNumber { get; set; }

        [Required]
        [StringLength(7)]
        [Column(TypeName = "nvarchar(7)")]
        public string? ExpiryDate { get; set; }

        [Required]
        [StringLength(4)]
        public string? CVV { get; set; }

        [StringLength(100)]
        public string? CardTokenNo { get; set; }

        [StringLength(4)]
        public string? CardLast4Digit { get; set; }

        [StringLength(20)]
        public string? CardBrand { get; set; }

        // Navigation Property
        public virtual User User { get; set; }
    }
}
