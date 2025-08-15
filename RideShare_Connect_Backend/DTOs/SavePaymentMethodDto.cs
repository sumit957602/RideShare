using System;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class SavePaymentMethodDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string CardType { get; set; }

        [Required]
        [StringLength(100)]
        public string CardholderName { get; set; }

        [Required]
        [StringLength(16)]
        public string CardNumber { get; set; }

        [Required]
        [StringLength(7)]
        [RegularExpression(@"^(0[1-9]|1[0-2])/\d{4}$", ErrorMessage = "Expiry date must be in MM/YYYY format")]
        public string ExpiryDate { get; set; }

        [Required]
        [StringLength(4)]
        public string CVV { get; set; }

        [StringLength(4)]
        public string? CardLast4Digit { get; set; }

        [StringLength(20)]
        public string? CardBrand { get; set; }
    }
}
