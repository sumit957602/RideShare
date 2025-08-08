using RideShare_Connect.Models.UserManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.UserManagement
{
    public class LoginHistory
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string IpAddress { get; set; }

        [Required]
        public DateTime LoginTime { get; set; }

        [StringLength(200)]
        public string DeviceInfo { get; set; }

        public virtual User User { get; set; }
    }
}
