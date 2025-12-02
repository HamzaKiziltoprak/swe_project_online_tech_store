using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineTechStore.Server.Models
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
        public virtual Product Product { get; set; }

        public string UserId { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; }

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