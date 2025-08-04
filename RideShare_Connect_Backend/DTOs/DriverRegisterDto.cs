using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class DriverRegisterDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Range(0, 50)]
        public int DrivingExperienceYears { get; set; }

        [StringLength(500)]
        public string ReferenceContacts { get; set; }
    }
}
