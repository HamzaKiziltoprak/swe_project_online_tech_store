namespace Backend.DTOs
{
    /// <summary>
    /// Transaction list item for display
    /// </summary>
    public class TransactionDto
    {
        public int TransactionID { get; set; }
        public string TransactionType { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public string? UserName { get; set; }
    }

    /// <summary>
    /// Detailed transaction information
    /// </summary>
    public class TransactionDetailDto
    {
        public int TransactionID { get; set; }
        public string TransactionType { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        
        // Order information
        public int OrderID { get; set; }
        public string OrderStatus { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        
        // User information
        public int UserID { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
    }

    /// <summary>
    /// Create manual transaction (for admin adjustments)
    /// </summary>
    public class CreateTransactionDto
    {
        public string TransactionType { get; set; } = null!; // Purchase, Refund, Adjustment
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int OrderID { get; set; }
        public int UserID { get; set; }
    }

    /// <summary>
    /// Transaction filter parameters
    /// </summary>
    public class TransactionFilterParams
    {
        public string? TransactionType { get; set; }
        public string? Status { get; set; }
        public int? UserID { get; set; }
        public int? OrderID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Sorting
        public string SortBy { get; set; } = "TransactionDate";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Paged transaction result
    /// </summary>
    public class PagedTransactionResult
    {
        public List<TransactionDto> Data { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Transaction statistics for analytics
    /// </summary>
    public class TransactionStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        // Breakdown by type
        public Dictionary<string, decimal> RevenueByType { get; set; } = new();
        public Dictionary<string, int> CountByType { get; set; } = new();
    }
}
