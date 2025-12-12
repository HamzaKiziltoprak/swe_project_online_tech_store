using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,ProductManager,CompanyOwner")]
    public class AnalyticsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(DataContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard overview with key metrics
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetDashboardOverview()
        {
            try
            {
                var totalRevenue = await _context.Transactions
                    .Where(t => t.TransactionType == "Purchase" && t.Status == "Completed")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var totalOrders = await _context.Orders.CountAsync();

                var totalCustomers = await _context.Users
                    .Where(u => u.Orders.Any())
                    .CountAsync();

                var totalProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .CountAsync();

                var lowStockCount = await _context.Products
                    .Where(p => p.Stock <= p.CriticalStockLevel && p.IsActive)
                    .CountAsync();

                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == "Pending")
                    .CountAsync();

                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                var overview = new DashboardOverviewDto
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    TotalCustomers = totalCustomers,
                    TotalProducts = totalProducts,
                    LowStockProductsCount = lowStockCount,
                    PendingOrdersCount = pendingOrders,
                    AverageOrderValue = averageOrderValue
                };

                _logger.LogInformation("Dashboard overview fetched successfully");

                return Ok(ApiResponse<DashboardOverviewDto>.SuccessResponse(
                    overview,
                    "Dashboard overview retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard overview");
                return StatusCode(500, ApiResponse<DashboardOverviewDto>.FailureResponse(
                    "Failed to fetch dashboard overview"));
            }
        }

        /// <summary>
        /// Get top selling products
        /// </summary>
        [HttpGet("top-products")]
        public async Task<ActionResult<ApiResponse<List<TopSellingProductDto>>>> GetTopSellingProducts(
            [FromQuery] int limit = 10)
        {
            try
            {
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                    .GroupBy(oi => new
                    {
                        oi.ProductID,
                        oi.Product.ProductName,
                        BrandName = oi.Product.Brand.BrandName,
                        oi.Product.ImageUrl
                    })
                    .Select(g => new TopSellingProductDto
                    {
                        ProductID = g.Key.ProductID,
                        ProductName = g.Key.ProductName,
                        Brand = g.Key.BrandName,
                        ImageUrl = g.Key.ImageUrl,
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                        OrderCount = g.Select(oi => oi.OrderID).Distinct().Count()
                    })
                    .OrderByDescending(p => p.TotalQuantitySold)
                    .Take(limit)
                    .ToListAsync();

                _logger.LogInformation($"Top {topProducts.Count} selling products fetched");

                return Ok(ApiResponse<List<TopSellingProductDto>>.SuccessResponse(
                    topProducts,
                    $"Top {topProducts.Count} products retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top selling products");
                return StatusCode(500, ApiResponse<List<TopSellingProductDto>>.FailureResponse(
                    "Failed to fetch top selling products"));
            }
        }

        /// <summary>
        /// Get category-based sales distribution
        /// </summary>
        [HttpGet("category-sales")]
        public async Task<ActionResult<ApiResponse<List<CategorySalesDto>>>> GetCategorySales()
        {
            try
            {
                var categorySales = await _context.OrderItems
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                    .GroupBy(oi => new
                    {
                        oi.Product.CategoryID,
                        oi.Product.Category.CategoryName
                    })
                    .Select(g => new CategorySalesDto
                    {
                        CategoryID = g.Key.CategoryID,
                        CategoryName = g.Key.CategoryName,
                        ProductCount = g.Select(oi => oi.ProductID).Distinct().Count(),
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                        OrderCount = g.Select(oi => oi.OrderID).Distinct().Count()
                    })
                    .OrderByDescending(c => c.TotalRevenue)
                    .ToListAsync();

                _logger.LogInformation($"Category sales data fetched: {categorySales.Count} categories");

                return Ok(ApiResponse<List<CategorySalesDto>>.SuccessResponse(
                    categorySales,
                    "Category sales data retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category sales");
                return StatusCode(500, ApiResponse<List<CategorySalesDto>>.FailureResponse(
                    "Failed to fetch category sales"));
            }
        }

        /// <summary>
        /// Get sales trend over time period
        /// </summary>
        [HttpGet("sales-trend")]
        public async Task<ActionResult<ApiResponse<List<SalesTrendDto>>>> GetSalesTrend(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var salesTrend = await _context.Orders
                    .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new SalesTrendDto
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount),
                        ProductsSold = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                    })
                    .OrderBy(t => t.Date)
                    .ToListAsync();

                _logger.LogInformation($"Sales trend fetched: {salesTrend.Count} data points");

                return Ok(ApiResponse<List<SalesTrendDto>>.SuccessResponse(
                    salesTrend,
                    $"Sales trend from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales trend");
                return StatusCode(500, ApiResponse<List<SalesTrendDto>>.FailureResponse(
                    "Failed to fetch sales trend"));
            }
        }

        /// <summary>
        /// Get revenue analytics with detailed breakdown
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<ApiResponse<RevenueAnalyticsDto>>> GetRevenueAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var totalRevenue = await _context.Transactions
                    .Where(t => t.TransactionType == "Purchase" 
                        && t.Status == "Completed"
                        && t.TransactionDate >= start 
                        && t.TransactionDate <= end)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var refundedAmount = await _context.Transactions
                    .Where(t => t.TransactionType == "Refund"
                        && t.Status == "Completed"
                        && t.TransactionDate >= start
                        && t.TransactionDate <= end)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var transactionCount = await _context.Transactions
                    .Where(t => t.TransactionDate >= start && t.TransactionDate <= end)
                    .CountAsync();

                var dailyTrend = await _context.Orders
                    .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new SalesTrendDto
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount),
                        ProductsSold = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity)
                    })
                    .OrderBy(t => t.Date)
                    .ToListAsync();

                var analytics = new RevenueAnalyticsDto
                {
                    TotalRevenue = totalRevenue,
                    RefundedAmount = refundedAmount,
                    NetRevenue = totalRevenue - refundedAmount,
                    TotalTransactions = transactionCount,
                    StartDate = start,
                    EndDate = end,
                    DailyTrend = dailyTrend
                };

                _logger.LogInformation($"Revenue analytics fetched: Net revenue = {analytics.NetRevenue}");

                return Ok(ApiResponse<RevenueAnalyticsDto>.SuccessResponse(
                    analytics,
                    "Revenue analytics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching revenue analytics");
                return StatusCode(500, ApiResponse<RevenueAnalyticsDto>.FailureResponse(
                    "Failed to fetch revenue analytics"));
            }
        }
    }
}
