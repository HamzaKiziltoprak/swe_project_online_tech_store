using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class AssignRoleDto
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string RoleName { get; set; } = null!;
    }
}
