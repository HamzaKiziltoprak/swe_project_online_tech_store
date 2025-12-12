using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    // Ana Order DTO - Sipariş detaylarını görüntülemek için
    public class OrderDto
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public string UserEmail { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled
        public DateTime OrderDate { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    // Sipariş oluşturmak için DTO
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Teslimat adresi gereklidir")]
        [MinLength(10, ErrorMessage = "Adres en az 10 karakter olmalıdır")]
        [MaxLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
        public string ShippingAddress { get; set; } = null!;
    }

    // Sipariş durumunu güncellemek için DTO (Admin)
    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Durum gereklidir")]
        [RegularExpression("^(Pending|Processing|Shipped|Delivered|Cancelled)$", 
            ErrorMessage = "Geçersiz durum. İzin verilen değerler: Pending, Processing, Shipped, Delivered, Cancelled")]
        public string Status { get; set; } = null!;

        [MaxLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir")]
        public string? ReasonOrNotes { get; set; }
    }

    // Sipariş içindeki her ürün için DTO
    public class OrderItemDto
    {
        public int OrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; } // Sipariş anındaki fiyat
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; } // UnitPrice * Quantity
    }

    // Sayfalı sipariş listesi için
    public class PagedOrderResult
    {
        public List<OrderDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // Sipariş filtreleme parametreleri
    public class OrderFilterParams
    {
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UserID { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "OrderDate"; // OrderDate, TotalAmount
        public bool SortDescending { get; set; } = true;
    }

    // One-Click Buy için DTO (Hızlı satın alma)
    public class OneClickBuyDto
    {
        [Required(ErrorMessage = "Teslimat adresi gereklidir")]
        [MinLength(10, ErrorMessage = "Adres en az 10 karakter olmalıdır")]
        [MaxLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
        public string ShippingAddress { get; set; } = null!;

        [StringLength(50)]
        public string? PaymentMethod { get; set; } = "Default"; // Default, CreditCard, DebitCard
    }

    // One-Click Buy Response
    public class OneClickBuyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public OrderDto? Order { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TransactionId { get; set; }
        public List<string>? Errors { get; set; }
    }

    // Payment Service Response (Mock)
    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Status { get; set; } = null!; // Authorized, Failed, InsufficientFunds
    }
}
