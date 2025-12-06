namespace Backend.DTOs
{
    /// <summary>
    /// Favori ürün DTOsu
    /// </summary>
    public class FavoriteDto
    {
        public int FavoriteID { get; set; }
        public int UserID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int Stock { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Favori ekleme/silme response
    /// </summary>
    public class FavoriteActionDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int ProductID { get; set; }
    }

    /// <summary>
    /// Favorileri listeleme (sayfalanmış)
    /// </summary>
    public class PagedFavoriteResult
    {
        public List<FavoriteDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    /// <summary>
    /// Favoride olup olmadığını kontrol etme
    /// </summary>
    public class IsFavoriteDto
    {
        public int ProductID { get; set; }
        public bool IsFavorite { get; set; }
    }
}
