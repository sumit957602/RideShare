using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.DTOs
{
    public class DocumentUploadDto
    {
        [Required]
        public string DocumentType { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}
