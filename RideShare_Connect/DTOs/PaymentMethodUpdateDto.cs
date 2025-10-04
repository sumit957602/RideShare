using System;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class PaymentMethodUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [StringLength(50)]
        public string? CardType { get; set; }

        [StringLength(16)]
        public string? CardNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
