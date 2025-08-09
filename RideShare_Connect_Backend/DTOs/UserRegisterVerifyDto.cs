using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class UserRegisterVerifyDto
    {
        [Required]
        public string RegistrationId { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}
