using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.UserManagement
{
    public class UserVerification
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string DocUrl { get; set; }

        [Required]
        [StringLength(50)]
        public string VerificationType { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public DateTime IssuedOn { get; set; }

        public DateTime ExpiresOn { get; set; }

        public virtual User User { get; set; }
    }
}
