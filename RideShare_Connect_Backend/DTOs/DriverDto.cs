using System;

namespace RideShare_Connect.Api.DTOs
{
    public class DriverDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string LicenseNumber { get; set; }
        public string BackgroundCheckStatus { get; set; }
        public int DrivingExperienceYears { get; set; }
        public DateOnly DOB { get; set; }
    }
}
