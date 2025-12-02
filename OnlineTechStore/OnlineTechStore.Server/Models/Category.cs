using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineTechStore.Server.Models
{

    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        public int? ParentCategoryID { get; set; }
        [ForeignKey("ParentCategoryID")]
        [ValidateNever]
        public virtual Category? ParentCategory { get; set; }

        [ValidateNever]
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        [ValidateNever]
        public ICollection<Product> Products { get; set; } = new List<Product>(); 
    }
}