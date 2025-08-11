namespace RideShare_Connect.Api.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string CarMaker { get; set; }
        public string Model { get; set; }
        public string VehicleType { get; set; }
        public string LicensePlate { get; set; }
        public int Year { get; set; }
        public string VerificationStatus { get; set; }
    }
}
