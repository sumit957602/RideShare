using System.ComponentModel.DataAnnotations;

namespace RideShare_Connect.Models.AdminManagement
{
    public class SystemConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; }  

        [Required]
        [StringLength(512)]
        public string Value { get; set; }  

        [StringLength(1024)]
        public string Description { get; set; }  
    }
}
