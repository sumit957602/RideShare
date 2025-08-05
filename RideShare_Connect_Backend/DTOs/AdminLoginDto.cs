using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class AdminLoginDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
