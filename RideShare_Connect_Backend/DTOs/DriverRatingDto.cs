using System;

namespace RideShare_Connect.Api.DTOs
{
    public class DriverRatingDto
    {
        public int Id { get; set; }
        public int PassengerId { get; set; }
        public int RideId { get; set; }
        public int Rating { get; set; }
        public string Review { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
