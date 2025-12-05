using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = null!;
        
        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
