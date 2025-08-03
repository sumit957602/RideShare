using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class AddMoneyDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}
