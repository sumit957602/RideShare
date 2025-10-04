namespace RideShare_Connect.DTOs
{
    public class RideBookingResultDto
    {
        public int BookingId { get; set; }
        public int PaymentId { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalFare { get; set; }
    }
}
