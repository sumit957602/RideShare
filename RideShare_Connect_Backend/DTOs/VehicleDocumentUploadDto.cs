using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Api.DTOs
{
    public class VehicleDocumentUploadDto
    {
        [Required]
        public int VehicleId { get; set; }

        [Required]
        public string DocumentType { get; set; }

        [Required]
        public IFormFile File { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidTo { get; set; }
    }
}
