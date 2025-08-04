using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.AdminManagement
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdminId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }  // Example: "BanUser", "UpdateSettings", "RefundIssued"

        [StringLength(1000)]
        public string LogDescription { get; set; }  // Detailed description of the action

        public int? UserId { get; set; }  // Optional: affected user, if any (e.
    }
}
