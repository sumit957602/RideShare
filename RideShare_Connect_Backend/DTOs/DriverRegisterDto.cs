using System;
using System.ComponentModel.DataAnnotations;
using RideShare_Connect_Backend.Validators;

namespace RideShare_Connect.Api.DTOs
{
    public class DriverRegisterDto
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

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Range(0, 50)]
        public int DrivingExperienceYears { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [MinimumAge(18)]
        public DateTime DOB { get; set; }
    }
}
