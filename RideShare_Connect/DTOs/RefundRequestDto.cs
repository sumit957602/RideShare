using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class RefundRequestDto
    {
        [Required]
        public int PaymentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? Reason { get; set; }
    }
}
