namespace RideShare_Connect.Api.DTOs
{
    public class UserUpdateDto
    {
        public string Email { get; set; } // Can be nullable if not always updated
        public string PhoneNumber { get; set; } // Can be nullable
        public string FullName { get; set; } // For updating UserProfile.FullName
        public string UserType { get; set; }
        public string AccountStatus { get; set; }
    }
}