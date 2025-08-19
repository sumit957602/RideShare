using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.ViewModels
{
    public class SubmitDriverRatingVm
    {
        [Required]
        public int RideId { get; set; }

        [Required]
        public int DriverId { get; set; }

        public string DriverName { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime? DepartureTime { get; set; }

        [Required, Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review must be at most 1000 characters.")]
        public string? Review { get; set; }
    }
}
