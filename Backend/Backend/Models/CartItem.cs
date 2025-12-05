using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemID { get; set; }

        [Range(1, 100)]
        public int Count { get; set; }

        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        [ValidateNever]
        public virtual Product Product { get; set; } = null!;

        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [ValidateNever]
        public virtual User User { get; set; } = null!;

        [NotMapped]
        public decimal TotalPrice
        {
            get
            {
                return Product != null ? Product.Price * Count : 0;
            }
        }
    }
}