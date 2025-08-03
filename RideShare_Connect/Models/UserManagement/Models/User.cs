using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideShare_Connect.Models.UserManagement
{
    public class User
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string UserType { get; set; } 

        public string AccountStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string VerificationBadge { get; set; } = string.Empty;

        public virtual UserProfile UserProfile { get; set; }

        public virtual UserSettings UserSettings { get; set; }

        public virtual ICollection<UserVerification> Verifications { get; set; } = new HashSet<UserVerification>();
        public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new HashSet<LoginHistory>();
        public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new HashSet<PasswordResetToken>();
    }
}
