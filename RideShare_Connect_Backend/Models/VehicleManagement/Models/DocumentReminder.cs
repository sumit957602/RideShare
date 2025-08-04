using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class DocumentReminder
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("VehicleDocument")]
        public int DocumentId { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        public DateTime ReminderDate { get; set; }

        [Required]
        [StringLength(30)]
        public string ReminderType { get; set; }  // e.g., "Email", "SMS", etc.

        // Navigation Properties
        public virtual VehicleDocument VehicleDocument { get; set; }
        public virtual Driver Driver { get; set; }
    }
}
