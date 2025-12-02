using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace OnlineTechStore.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [ValidateNever]
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        [ValidateNever]
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}
