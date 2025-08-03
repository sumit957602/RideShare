using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.Models.AdminManagement
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Admin")]
        public int AdminId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }  // Example: "BanUser", "UpdateSettings", "RefundIssued"

        [StringLength(1000)]
        public string LogDescription { get; set; }  // Detailed description of the action

        [ForeignKey("User")]
        public int? UserId { get; set; }  // Optional: affected user, if any (e.g., banned user)

        public virtual Admin Admin { get; set; }
        public virtual User User { get; set; }
    }
}
