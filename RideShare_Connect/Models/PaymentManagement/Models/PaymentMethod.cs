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
        public string MethodType { get; set; }  // e.g., "Credit Card", "Debit Card", "UPI", "NetBanking"

        [Required]
        [StringLength(100)]
        public string? CardholderName { get; set; }

        [Required]
        [StringLength(16)]
        public string? CardNumber { get; set; }

        [Required]
        [StringLength(5)]
        public string? Expiry { get; set; }

        [Required]
        [StringLength(4)]
        public string? Cvc { get; set; }

        [StringLength(100)]
        public string? PaymentToken { get; set; }

        [StringLength(4)]
        public string? Last4 { get; set; }

        [StringLength(20)]
        public string? Brand { get; set; }

        // Navigation Property
        public virtual User User { get; set; }
    }
}

