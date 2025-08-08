using System;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Models.AdminManagement
{
    public class Admin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(512)]
        public string PasswordHash { get; set; }  // Hashed password for security

        [Required]
        [StringLength(20)]
        public string Role { get; set; }  // e.g., SuperAdmin, Moderator

        [Required]
        [StringLength(20)]
        public string Status { get; set; }  // e.g., Active, Inactive, Suspended
    }
}
