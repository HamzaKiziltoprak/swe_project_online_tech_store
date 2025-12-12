namespace Backend.DTOs
{
    /// <summary>
    /// Dashboard overview with key metrics
    /// </summary>
    public class DashboardOverviewDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProductsCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    /// <summary>
    /// Top selling product statistics
    /// </summary>
    public class TopSellingProductDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Category-based sales distribution
    /// </summary>
    public class CategorySalesDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = null!;
        public int ProductCount { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Sales trend over time
    /// </summary>
    public class SalesTrendDto
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public int ProductsSold { get; set; }
    }

    /// <summary>
    /// Revenue analytics with time period
    /// </summary>
    public class RevenueAnalyticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<SalesTrendDto> DailyTrend { get; set; } = new();
    }

    /// <summary>
    /// Time period filter for analytics
    /// </summary>
    public enum TimePeriod
    {
        Today,
        Yesterday,
        Last7Days,
        Last30Days,
        ThisMonth,
        LastMonth,
        Custom
    }
}
