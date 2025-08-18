using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class DriverWalletTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("DriverWallet")]
        public int DriverWalletId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string TxnType { get; set; }

        [Required]
        public DateTime TxnDate { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public virtual DriverWallet DriverWallet { get; set; }
    }
}
