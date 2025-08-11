namespace RideShare_Connect.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
        public string EmergencyContact { get; set; }
        public string Bio { get; set; }
        public string PhoneNumber { get; set; }
    }
}
