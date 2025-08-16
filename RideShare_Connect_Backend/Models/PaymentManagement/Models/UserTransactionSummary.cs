using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect.Models.PaymentManagement
{
    public class UserTransactionSummary
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        [ForeignKey("Ride")]
        public int RideId { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTransactionAmount { get; set; }

        // Navigation properties
        public virtual Ride Ride { get; set; }
        public virtual Driver Driver { get; set; }
        public virtual User User { get; set; }
    }
}
