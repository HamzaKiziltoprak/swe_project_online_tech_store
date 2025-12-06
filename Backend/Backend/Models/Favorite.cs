using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    /// <summary>
    /// Kullanıcı favori ürünleri
    /// </summary>
    public class Favorite
    {
        [Key]
        public int FavoriteID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [ValidateNever]
        public virtual User User { get; set; } = null!;

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        [ValidateNever]
        public virtual Product Product { get; set; } = null!;

        /// <summary>
        /// Favori ekleme tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
