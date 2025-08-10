using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; }

        public string ProfilePicture { get; set; }

        public string FullName { get; set; }
    }
}
