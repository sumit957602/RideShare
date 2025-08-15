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
        public string MethodType { get; set; }

        [Required]
        [StringLength(100)]
        public string CardholderName { get; set; }

        [Required]
        [StringLength(16)]
        public string CardNumber { get; set; }

        [Required]
        [StringLength(7)]
        [RegularExpression(@"^(0[1-9]|1[0-2])/\d{4}$", ErrorMessage = "Expiry must be in MM/YYYY format")]
        public string Expiry { get; set; }

        [Required]
        [StringLength(4)]
        public string Cvc { get; set; }

        [StringLength(4)]
        public string? Last4 { get; set; }

        [StringLength(20)]
        public string? Brand { get; set; }
    }
}
