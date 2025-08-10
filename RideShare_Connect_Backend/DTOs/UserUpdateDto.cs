using System.ComponentModel.DataAnnotations;
﻿namespace RideShare_Connect.Api.DTOs
{
    public class UserUpdateDto
    {
        public string Email { get; set; } // Can be nullable if not always updated
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 6,7,8, or 9.")]
        public string PhoneNumber { get; set; } // Can be nullable
        public string FullName { get; set; } // For updating UserProfile.FullName
        public string UserType { get; set; }
        public string AccountStatus { get; set; }
    }
}