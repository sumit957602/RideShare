using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class UserProfileUpdateDto
    {
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 6,7,8, or 9.")]
        public string EmergencyContact { get; set; }
        public string Bio { get; set; }
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 6,7,8, or 9.")]
        public string PhoneNumber { get; set; }
    }
}
