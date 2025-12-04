using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class ProductSpecification
    {
        [Key]
        public int SpecID { get; set; }

        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        [ValidateNever]
        public virtual Product Product { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string SpecName { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string SpecValue { get; set; } = null!;
    }
}