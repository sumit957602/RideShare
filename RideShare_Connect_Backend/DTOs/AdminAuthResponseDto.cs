namespace RideShare_Connect.Api.DTOs
{
    public class AdminAuthResponseDto
    {
        public string Token { get; set; }
        public int AdminId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }
}
