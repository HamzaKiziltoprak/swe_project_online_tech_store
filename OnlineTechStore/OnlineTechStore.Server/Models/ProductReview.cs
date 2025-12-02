using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineTechStore.Server.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewID { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;

        public int ProductID { get; set; }
        [ForeignKey("ProductID")]
        [ValidateNever]
        public virtual Product Product { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual ApplicationUser User { get; set; }
    }
}