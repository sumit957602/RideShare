using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}
