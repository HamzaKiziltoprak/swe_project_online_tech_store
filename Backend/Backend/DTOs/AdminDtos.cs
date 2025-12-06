using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Kullanıcıya rol atama isteği
    /// </summary>
    public class AssignRoleRequestDto
    {
        [Required(ErrorMessage = "Kullanıcı ID zorunludur")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Rol zorunludur")]
        [StringLength(50, MinimumLength = 1)]
        public string Role { get; set; } = null!;
    }

    /// <summary>
    /// Rol atama yanıtı
    /// </summary>
    public class AssignRoleResponseDto
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string AssignedRole { get; set; } = null!;
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
    }

    /// <summary>
    /// Admin istatistikleri
    /// </summary>
    public class AdminStatsDto
    {
        /// <summary>
        /// Toplam ürün sayısı
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// Aktif ürün sayısı
        /// </summary>
        public int ActiveProducts { get; set; }

        /// <summary>
        /// Stoğu tamamlanmış ürün sayısı
        /// </summary>
        public int OutOfStockProducts { get; set; }

        /// <summary>
        /// Toplam kategori sayısı
        /// </summary>
        public int TotalCategories { get; set; }

        /// <summary>
        /// Toplam sipariş sayısı
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Beklemede olan sipariş sayısı
        /// </summary>
        public int PendingOrders { get; set; }

        /// <summary>
        /// Tamamlanmış sipariş sayısı
        /// </summary>
        public int CompletedOrders { get; set; }

        /// <summary>
        /// İptal edilmiş sipariş sayısı
        /// </summary>
        public int CancelledOrders { get; set; }

        /// <summary>
        /// Toplam sipariş cirosu (TL)
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Tamamlanmış siparişlerin cirosu (TL)
        /// </summary>
        public decimal CompletedRevenue { get; set; }

        /// <summary>
        /// Toplam kullanıcı sayısı
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Admin kullanıcı sayısı
        /// </summary>
        public int AdminUsers { get; set; }

        /// <summary>
        /// Çalışan sayısı (Employee)
        /// </summary>
        public int EmployeeUsers { get; set; }

        /// <summary>
        /// Müşteri sayısı (Customer)
        /// </summary>
        public int CustomerUsers { get; set; }

        /// <summary>
        /// Toplam review sayısı
        /// </summary>
        public int TotalReviews { get; set; }

        /// <summary>
        /// Onaylanan review sayısı
        /// </summary>
        public int ApprovedReviews { get; set; }

        /// <summary>
        /// Beklemede olan review sayısı
        /// </summary>
        public int PendingReviews { get; set; }

        /// <summary>
        /// Toplam iade talebi sayısı
        /// </summary>
        public int TotalReturns { get; set; }

        /// <summary>
        /// Beklemede olan iade talebi sayısı
        /// </summary>
        public int PendingReturns { get; set; }

        /// <summary>
        /// Onaylanan iade talebi sayısı
        /// </summary>
        public int ApprovedReturns { get; set; }

        /// <summary>
        /// Reddedilen iade talebi sayısı
        /// </summary>
        public int RejectedReturns { get; set; }

        /// <summary>
        /// Toplam iade cirosu (TL)
        /// </summary>
        public decimal TotalRefundAmount { get; set; }
    }
}
