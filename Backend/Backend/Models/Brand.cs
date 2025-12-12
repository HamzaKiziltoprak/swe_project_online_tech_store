using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    /// <summary>
    /// Brand/Marka modeli - Ürün markalarını normalize etmek için
    /// </summary>
    public class Brand
    {
        [Key]
        public int BrandID { get; set; }

        [Required]
        [StringLength(100)]
        public string BrandName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property - Bu markaya ait ürünler
        [ValidateNever]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
