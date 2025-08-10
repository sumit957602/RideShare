using System;
using System.ComponentModel.DataAnnotations;
namespace RideShare_Connect.Api.DTOs
{
    public class DriverUpdateDto
    {
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 6,7,8, or 9.")]
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string LicenseNumber { get; set; }
        public int? DrivingExperienceYears { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }
    }
}
