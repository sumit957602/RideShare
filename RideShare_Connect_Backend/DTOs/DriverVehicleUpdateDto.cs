namespace RideShare_Connect.Api.DTOs
{
    public class DriverVehicleUpdateDto
    {
        public string CarMaker { get; set; }
        public string Model { get; set; }
        public string VehicleType { get; set; }
        public string LicensePlate { get; set; }
        public int? Year { get; set; }
    }
}
