using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class DriverWallet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Balance { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        public virtual Driver Driver { get; set; }
    }
}
