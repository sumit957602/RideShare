using RideShare_Connect.Models.UserManagement;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.UserManagement
{
    public class UserProfile
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        public string ProfilePicture { get; set; }

        [Phone]
        public string EmergencyContact { get; set; }

        [StringLength(300)]
        public string Bio { get; set; }

        public virtual User User { get; set; }
    }
}
