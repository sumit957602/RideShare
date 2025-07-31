using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class VehicleDocument
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; }

        [Required]
        public byte[] FileData { get; set; }  // 👈 Binary file storage

        [Required]
        [StringLength(100)]
        public string FileName { get; set; }  // e.g., insurance.pdf

        [Required]
        [StringLength(50)]
        public string ContentType { get; set; } // e.g., image/jpeg, application/pdf

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidTo { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        // Navigation Property
        public virtual Vehicle Vehicle { get; set; }
    }
}
