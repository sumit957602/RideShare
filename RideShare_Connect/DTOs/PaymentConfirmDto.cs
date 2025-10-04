using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class PaymentConfirmDto
    {
        [Required]
        public int PaymentId { get; set; }

        public int? PaymentMethodId { get; set; }
    }
}
