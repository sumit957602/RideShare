using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
