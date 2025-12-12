using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    /// <summary>
    /// Sipariş iadesi/döndürme istekleri
    /// </summary>
    public class OrderReturn
    {
        [Key]
        public int ReturnID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        [ValidateNever]
        public virtual Order Order { get; set; } = null!;

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [ValidateNever]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// İade nedeni: "DefectiveProduct", "NotAsDescribed", "Damaged", "ChangeOfMind", "Other"
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ReturnReason { get; set; } = null!;

        /// <summary>
        /// İade açıklaması
        /// </summary>
        [StringLength(1000)]
        public string? ReturnDescription { get; set; }

        /// <summary>
        /// İade Durumu: "Pending" (Bekleniyor), "Approved" (Onaylandı), "Rejected" (Reddedildi), "Completed" (Tamamlandı)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Geri ödeme tutarı (Admin tarafından set edilir)
        /// </summary>
        public decimal? RefundAmount { get; set; }

        /// <summary>
        /// İade istemi tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Admin tarafından eklenmiş not
        /// </summary>
        [StringLength(500)]
        public string? AdminNote { get; set; }

        /// <summary>
        /// Refund transaction ID (when status becomes "Completed")
        /// </summary>
        public int? RefundTransactionID { get; set; }

        [ForeignKey("RefundTransactionID")]
        [ValidateNever]
        public virtual Transaction? RefundTransaction { get; set; }
    }
}
