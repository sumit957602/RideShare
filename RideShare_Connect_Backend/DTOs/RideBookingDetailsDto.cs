using System;

namespace RideShare_Connect.DTOs
{
    public class RideBookingDetailsDto
    {
        public int BookingId { get; set; }
        public int RideId { get; set; }
        public int PassengerId { get; set; }
        public int BookedSeats { get; set; }
        public string PickupLocation { get; set; }
        public string DropLocation { get; set; }
        public decimal DistanceKm { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime DepartureTime { get; set; }
        public decimal PricePerSeat { get; set; }
    }
}
