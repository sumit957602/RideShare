using RideShare_Connect.Models.UserManagement;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.AdminManagement
{
    public class UserReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportedUserId { get; set; } // The user being reported

        [Required]
        public int ReportingUserId { get; set; } // The user who reported

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } // Reason for report

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // e.g., Pending, Resolved, Dismissed

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? HandledByAdminId { get; set; } // Admin who handled it

        public DateTime? HandledAt { get; set; } // When it was handled

        [StringLength(1000)]
        public string ResolutionNote { get; set; } // Admin notes

        // Optional navigation properties
        [ForeignKey("ReportedUserId")]
        public User ReportedUser { get; set; }

        [ForeignKey("ReportingUserId")]
        public User ReportingUser { get; set; }

        [ForeignKey("HandledByAdminId")]
        public Admin HandledByAdmin { get; set; }
    }
}
