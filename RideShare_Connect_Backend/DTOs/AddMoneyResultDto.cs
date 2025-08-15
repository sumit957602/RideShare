using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class AddMoneyResultDto
    {
        public int UserId { get; set; }
        public decimal Balance { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
    }
}
