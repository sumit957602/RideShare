using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class RoutePointDto
    {
        [Required]
        public string Location { get; set; }

        [Required]
        public int SequenceNumber { get; set; }
    }
}
