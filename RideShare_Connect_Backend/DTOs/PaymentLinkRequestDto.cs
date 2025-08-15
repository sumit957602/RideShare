using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class PaymentLinkRequestDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? Description { get; set; }
    }
}
