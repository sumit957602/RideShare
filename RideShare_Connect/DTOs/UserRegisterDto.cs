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

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        [Required] // FullName is now part of the registration
        [StringLength(100)]
        public string FullName { get; set; }

        public string UserType { get; set; } = "Rider";
    }
}