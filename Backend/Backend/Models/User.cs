using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class User : IdentityUser<int>
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;

        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [ValidateNever]
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        [ValidateNever]
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

        [ValidateNever]
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        [ValidateNever]
        public ICollection<OrderReturn> OrderReturns { get; set; } = new List<OrderReturn>();
    }
}