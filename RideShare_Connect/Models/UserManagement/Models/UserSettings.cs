using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.UserManagement
{
    public class UserSettings
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string PrivacyLevel { get; set; }

        [StringLength(250)]
        public string NotificationPreferences { get; set; }

        [StringLength(10)]
        public string Language { get; set; }

        public virtual User User { get; set; }
    }
}
