using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
        [Required]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9]).{8,}$", ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
