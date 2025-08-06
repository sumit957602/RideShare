using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        public string UserType { get; set; }

        public string AccountStatus { get; set; }
    }
}
