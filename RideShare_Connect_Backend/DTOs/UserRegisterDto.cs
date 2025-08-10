// DTOs/UserRegisterDto.cs (for user registration input)
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class UserRegisterDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 6,7,8, or 9.")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,}$", ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required] // FullName is now part of the registration
        [StringLength(100)]
        public string FullName { get; set; }

        public string UserType { get; set; } = "Rider";

    }
}