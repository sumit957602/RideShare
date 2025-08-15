using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class AddMoneyDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Successful|Failed|Pending)$", ErrorMessage = "transactionStatus should be Successful, Failed or Pending")]
        public string TransactionStatus { get; set; } = string.Empty;
    }
}
