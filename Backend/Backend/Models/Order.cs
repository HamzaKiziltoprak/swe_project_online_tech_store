using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Order
    {   
        [Key]
        public int OrderID { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(500)]
        public string ShippingAddress { get; set; } = null!;

        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [ValidateNever]
        public virtual User User { get; set; } = null!;

        [ValidateNever]
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [ValidateNever]
        public virtual Transaction? Transaction { get; set; }

        [ValidateNever]
        public virtual OrderReturn? OrderReturn { get; set; }
    }
}