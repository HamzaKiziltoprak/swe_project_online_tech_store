using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }

        [Required]
        public string ImageUrl { get; set; } = null!;

        public bool IsMainImage { get; set; } = false;

        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        [ValidateNever]
        public virtual Product Product { get; set; } = null!;
    }
}