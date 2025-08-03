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

        [StringLength(16)]
        public string? CardNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
