using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace OnlineTechStore.Server.Models
{

    public class User
    {   
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public string Role { get; set; } = "Customer";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [ValidateNever]
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        [ValidateNever]
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}