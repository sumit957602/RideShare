using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.VehicleManagement
{
    public class DriverPasswordResetToken
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        [StringLength(100)]
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public virtual Driver Driver { get; set; }
    }
}
