using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Balance { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
}
