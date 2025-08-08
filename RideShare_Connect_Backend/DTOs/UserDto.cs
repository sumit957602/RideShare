// DTOs/UserDto.cs (for responses)
namespace RideShare_Connect.Api.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; } // Added for UserProfile.FullName
        public string UserType { get; set; }
        public string AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}