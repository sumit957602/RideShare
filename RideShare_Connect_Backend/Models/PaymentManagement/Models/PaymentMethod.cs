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
        public string MethodType { get; set; }  // e.g., "Credit Card", "Debit Card", "UPI", "NetBanking"

        [StringLength(16)]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }  // Optional for UPI/NetBanking

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }  // Nullable for UPI/NetBanking

        // Navigation Property
        public virtual User User { get; set; }
    }
}
