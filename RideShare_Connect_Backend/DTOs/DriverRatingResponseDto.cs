namespace RideShare_Connect_Backend.DTOs
{
    public class DriverRatingResponseDto
    {
       
            public int Id { get; set; }
            public int RideId { get; set; }
            public int DriverId { get; set; }
            public int PassengerId { get; set; }
            public int Rating { get; set; }
            public string? Review { get; set; }
            public DateTime Timestamp { get; set; }
        
    }
}
