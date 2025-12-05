using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;
        
        [StringLength(500)]
        public string? Address { get; set; }
    }
}
