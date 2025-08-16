using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class PaymentProcessDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        [Required]
        [RegularExpression("Wallet|Razor Pay|Cash", ErrorMessage = "Invalid payment mode")]
        public string PaymentMode { get; set; }
    }
}
