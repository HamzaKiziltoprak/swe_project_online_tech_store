using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    // Ürün Listeleme İçin (Card View - Ana Sayfa/Kategori Sayfaları)
    public class ProductListDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public int BrandID { get; set; }
        public string Brand { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int Stock { get; set; }
        public int? CriticalStockLevel { get; set; }
        public bool IsActive { get; set; }
        public double? AverageRating { get; set; }
    }

    // Ürün Detay Sayfası İçin (Tam Bilgi)
    public class ProductDetailDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public int BrandID { get; set; }
        public string Brand { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CriticalStockLevel { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int CategoryID { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Review bilgileri (varsa)
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<ProductSpecificationDto>? Specifications { get; set; }
        public List<ProductImageDto>? Images { get; set; }
    }

    // Yeni Ürün Ekleme İçin (Admin)
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string ProductName { get; set; } = null!;

        [Required(ErrorMessage = "Brand is required")]
        public int BrandID { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        [Url(ErrorMessage = "Invalid image URL format")]
        public string ImageUrl { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }

    // Ürün Güncelleme İçin (Admin)
    public class UpdateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string ProductName { get; set; } = null!;

        [Required(ErrorMessage = "Brand is required")]
        public int BrandID { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        [Url(ErrorMessage = "Invalid image URL format")]
        public string ImageUrl { get; set; } = null!;

        public bool IsActive { get; set; }
    }

    // Ürün Filtreleme ve Arama İçin
    public class ProductFilterParams
    {
        // Arama terimi (ürün adı veya açıklamada ara)
        public string? SearchTerm { get; set; }

        // Marka filtresi (tekil - geriye dönük uyumluluk için)
        public int? BrandID { get; set; }
        
        // Marka filtreleri (çoğul - virgülle ayrılmış: "1,2,3")
        public string? BrandIds { get; set; }

        // Kategori filtresi
        public int? CategoryId { get; set; }

        // Fiyat aralığı
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Stok durumu
        public bool? InStock { get; set; }

        // EXCLUDE FILTERS - "Bu özellikleri istemiyor" filtreleri
        // Hariç tutulacak markalar (virgülle ayrılmış: "Apple,Samsung")
        public string? ExcludeBrands { get; set; }

        // Hariç tutulacak kategoriler (virgülle ayrılmış: "1,2,3")
        public string? ExcludeCategoryIds { get; set; }

        // Maksimum fiyattan daha pahalı ürünleri hariç tut
        public decimal? ExcludeAbovePrice { get; set; }

        // Minimuma fiyattan daha ucuz ürünleri hariç tut
        public decimal? ExcludeBelowPrice { get; set; }

        // Sıralama: "name_asc", "name_desc", "price_asc", "price_desc", "newest"
        public string? SortBy { get; set; } = "newest";

        // Sayfalama
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 12;
    }

    // Product Image DTO
    public class ProductImageDto
    {
        public int ImageID { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsMainImage { get; set; }
    }

    // Sayfalama Sonucu İçin
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}