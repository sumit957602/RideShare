namespace RideShare_Connect.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; } // From UserProfile

        public string ProfilePicture { get; set; }
        public string UserType { get; set; }
        // other user details to return with the token
    }
}
