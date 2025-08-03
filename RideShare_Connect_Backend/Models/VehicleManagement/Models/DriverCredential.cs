using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.Models.VehicleManagement
{
    /// <summary>
    /// Represents credentials used by a driver to sign up and log in.
    /// This table links to the core <see cref="User"/> entity to keep driver
    /// authentication details within the vehicle management domain.
    /// </summary>
    public class DriverCredential
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Reference to the underlying user account for this driver.
        /// </summary>
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        /// <summary>
        /// Hashed password used for authentication.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual User User { get; set; }
    }
}
