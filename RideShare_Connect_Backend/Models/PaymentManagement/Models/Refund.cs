using RideShare_Connect.Models.PaymentManagement;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class Refund
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Payment")]
        public int PaymentId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(250)]
        public string Reason { get; set; }

        [Required]
        [StringLength(20)]
        public string RefundStatus { get; set; }  

        [DataType(DataType.DateTime)]
        public DateTime ProcessedAt { get; set; }

        // Navigation property
        public virtual Payment Payment { get; set; }
    }
}
