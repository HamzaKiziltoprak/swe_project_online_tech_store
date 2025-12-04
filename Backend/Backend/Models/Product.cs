using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{

    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = null!;

        public string Description { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; } = 0;

        public string ImageUrl { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public bool IsHomeParams { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        [ValidateNever]
        public virtual Category Category { get; set; } = null!;

        [ValidateNever]
        public ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();

        [ValidateNever]
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        [ValidateNever]
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

        [ValidateNever]
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}