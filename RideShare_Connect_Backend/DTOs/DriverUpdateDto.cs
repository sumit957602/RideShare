namespace RideShare_Connect.Api.DTOs
{
    public class DriverUpdateDto
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string AccountStatus { get; set; }
        public string LicenseNumber { get; set; }
        public string BackgroundCheckStatus { get; set; }
        public int? DrivingExperienceYears { get; set; }
        public string ReferenceContacts { get; set; }
    }
}
