using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// İade isteği oluşturmak için
    /// </summary>
    public class CreateReturnDto
    {
        [Required(ErrorMessage = "İade nedeni zorunludur")]
        [StringLength(50, MinimumLength = 1)]
        public string ReturnReason { get; set; } = null!;

        [StringLength(1000)]
        public string? ReturnDescription { get; set; }
    }

    /// <summary>
    /// İade döndürme DTOsu (API yanıtı)
    /// </summary>
    public class ReturnDto
    {
        public int ReturnID { get; set; }
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public string ReturnReason { get; set; } = null!;
        public string? ReturnDescription { get; set; }
        public string Status { get; set; } = null!;
        public decimal? RefundAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? AdminNote { get; set; }
    }

    /// <summary>
    /// İade onaylama / reddetme için Admin DTOsu
    /// </summary>
    public class ApproveReturnDto
    {
        [Required(ErrorMessage = "Geri ödeme tutarı zorunludur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Geri ödeme tutarı 0'dan büyük olmalıdır")]
        public decimal RefundAmount { get; set; }

        [StringLength(500)]
        public string? AdminNote { get; set; }
    }

    /// <summary>
    /// İade reddetme için Admin DTOsu
    /// </summary>
    public class RejectReturnDto
    {
        [Required(ErrorMessage = "Reddetme nedeni zorunludur")]
        [StringLength(500, MinimumLength = 1)]
        public string AdminNote { get; set; } = null!;
    }

    /// <summary>
    /// İade filtreleme (Admin için)
    /// </summary>
    public class ReturnFilterParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Status { get; set; } // "Pending", "Approved", "Rejected", "Completed"
        public string? ReturnReason { get; set; } // Filtreleme
        public int? OrderID { get; set; } // Specific order için
        public int? UserID { get; set; } // Specific user için
        public DateTime? StartDate { get; set; } // Tarih aralığı
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Sayfalanmış İade sonuçları
    /// </summary>
    public class PagedReturnResult
    {
        public List<ReturnDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
