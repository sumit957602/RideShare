using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RideShare_Connect.ViewModels
{
    public class EditDriverViewModel
    {
        public int DriverId { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        public string? ExistingProfilePicture { get; set; }

        [Display(Name = "Driving Experience Years")]
        public int DrivingExperienceYears { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }
    }
}
