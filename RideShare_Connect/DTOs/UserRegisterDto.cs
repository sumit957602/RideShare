// DTOs/UserRegisterDto.cs (for user registration input)
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class UserRegisterDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        [Required] // FullName is now part of the registration
        [StringLength(100)]
        public string FullName { get; set; }

        public string UserType { get; set; } = "Rider";

        public string? Otp { get; set; }
    }
}