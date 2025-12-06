using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    // Sepetteki ürün DTO
    public class CartItemDto
    {
        public int CartItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
        public int Count { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal Subtotal { get; set; } // Price * Count
    }

    // Sepet özeti DTO
    public class CartSummaryDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // Sepete ürün ekleme DTO
    public class AddToCartDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Count { get; set; }
    }

    // Sepet ürününü güncelleme DTO
    public class UpdateCartItemDto
    {
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Count { get; set; }
    }
}
