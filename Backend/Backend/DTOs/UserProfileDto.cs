using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;
        
        public string? Address { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public List<string> Roles { get; set; } = new List<string>();
    }
}
