using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; }
    }
}
