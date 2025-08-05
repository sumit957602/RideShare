using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class AdminRegisterDto
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "Admin";
    }
}
