using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = null!; // Purchase, Refund, Adjustment

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Pending, Completed, Failed

        // Foreign Keys
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        [ValidateNever]
        public virtual Order Order { get; set; } = null!;

        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [ValidateNever]
        public virtual User User { get; set; } = null!;
    }
}
