namespace Backend.DTOs
{
    /// <summary>
    /// Product comparison request
    /// </summary>
    public class CompareProductsDto
    {
        public List<int> ProductIds { get; set; } = new();
    }

    /// <summary>
    /// Single product comparison data
    /// </summary>
    public class ProductComparisonDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        public int Stock { get; set; }
        public string CategoryName { get; set; } = null!;
        public Dictionary<string, string> Specifications { get; set; } = new();
    }

    /// <summary>
    /// Comparison attribute (for matrix display)
    /// </summary>
    public class ComparisonAttribute
    {
        public string AttributeName { get; set; } = null!;
        public Dictionary<int, string> ProductValues { get; set; } = new(); // Key: ProductID, Value: Attribute value
        public bool HasDifference { get; set; } // True if products have different values
    }

    /// <summary>
    /// Complete comparison result with matrix
    /// </summary>
    public class ProductComparisonResult
    {
        public List<ProductComparisonDto> Products { get; set; } = new();
        public List<ComparisonAttribute> Attributes { get; set; } = new();
        public List<string> CommonCategories { get; set; } = new();
        public string? ComparisonSummary { get; set; }
    }

    /// <summary>
    /// Add product to comparison list
    /// </summary>
    public class AddToComparisonDto
    {
        public int ProductId { get; set; }
    }

    /// <summary>
    /// Comparison list item (for user's saved comparison list)
    /// </summary>
    public class ComparisonListItemDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public DateTime AddedAt { get; set; }
    }
}
