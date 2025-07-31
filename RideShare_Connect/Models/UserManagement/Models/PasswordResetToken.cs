using RideShare_Connect.Models.UserManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.UserManagement
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public virtual User User { get; set; }
    }
}
