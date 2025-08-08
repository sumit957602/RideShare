using System;

namespace RideShare_Connect.DTOs
{
    public class RideDto
    {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime DepartureTime { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal PricePerSeat { get; set; }
        public string Status { get; set; }
        public bool IsRecurring { get; set; }
    }
}

