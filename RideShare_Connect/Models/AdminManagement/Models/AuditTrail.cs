using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.AdminManagement
{
    public class AuditTrail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Admin")]
        public int AdminId { get; set; }

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } 

        [Required]
        public int EntityId { get; set; } 

        [Required]
        [StringLength(2048)]
        public string ChangeSummary { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        // Navigation Property
        public virtual Admin Admin { get; set; }
    }
}
