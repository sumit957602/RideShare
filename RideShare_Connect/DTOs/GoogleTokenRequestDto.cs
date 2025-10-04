using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class GoogleTokenRequestDto
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string RedirectUri { get; set; }
    }
}
