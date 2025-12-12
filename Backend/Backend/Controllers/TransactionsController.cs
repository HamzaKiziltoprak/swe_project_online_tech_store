using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            DataContext context,
            UserManager<User> userManager,
            ILogger<TransactionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all transactions with filtering and pagination (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<ApiResponse<PagedTransactionResult>>> GetAllTransactions(
            [FromQuery] TransactionFilterParams filterParams)
        {
            try
            {
                var query = _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.Order)
                    .AsQueryable();

                // Filters
                if (!string.IsNullOrWhiteSpace(filterParams.TransactionType))
                {
                    query = query.Where(t => t.TransactionType == filterParams.TransactionType);
                }

                if (!string.IsNullOrWhiteSpace(filterParams.Status))
                {
                    query = query.Where(t => t.Status == filterParams.Status);
                }

                if (filterParams.UserID.HasValue)
                {
                    query = query.Where(t => t.UserID == filterParams.UserID.Value);
                }

                if (filterParams.OrderID.HasValue)
                {
                    query = query.Where(t => t.OrderID == filterParams.OrderID.Value);
                }

                if (filterParams.StartDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= filterParams.StartDate.Value);
                }

                if (filterParams.EndDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= filterParams.EndDate.Value);
                }

                // Total count before pagination
                var totalCount = await query.CountAsync();

                // Sorting
                query = filterParams.SortBy.ToLower() switch
                {
                    "amount" => filterParams.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(t => t.Amount)
                        : query.OrderByDescending(t => t.Amount),
                    "transactiondate" => filterParams.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(t => t.TransactionDate)
                        : query.OrderByDescending(t => t.TransactionDate),
                    _ => query.OrderByDescending(t => t.TransactionDate)
                };

                // Pagination
                var transactions = await query
                    .Skip((filterParams.Page - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .Select(t => new TransactionDto
                    {
                        TransactionID = t.TransactionID,
                        TransactionType = t.TransactionType,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        Description = t.Description,
                        Status = t.Status,
                        OrderID = t.OrderID,
                        UserID = t.UserID,
                        UserName = t.User != null ? $"{t.User.FirstName} {t.User.LastName}" : "N/A"
                    })
                    .ToListAsync();

                var result = new PagedTransactionResult
                {
                    Data = transactions,
                    CurrentPage = filterParams.Page,
                    PageSize = filterParams.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                };

                return Ok(ApiResponse<PagedTransactionResult>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transactions");
                return StatusCode(500, ApiResponse<PagedTransactionResult>.FailureResponse(
                    "An error occurred while fetching transactions"));
            }
        }

        /// <summary>
        /// Get current user's transactions
        /// </summary>
        [HttpGet("my-transactions")]
        public async Task<ActionResult<ApiResponse<PagedTransactionResult>>> GetMyTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<PagedTransactionResult>.FailureResponse("User not authenticated"));
                }

                var userIdInt = int.Parse(userId);

                var query = _context.Transactions
                    .Where(t => t.UserID == userIdInt)
                    .Include(t => t.Order)
                    .OrderByDescending(t => t.TransactionDate);

                var totalCount = await query.CountAsync();

                var transactions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TransactionDto
                    {
                        TransactionID = t.TransactionID,
                        TransactionType = t.TransactionType,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        Description = t.Description,
                        Status = t.Status,
                        OrderID = t.OrderID,
                        UserID = t.UserID
                    })
                    .ToListAsync();

                var result = new PagedTransactionResult
                {
                    Data = transactions,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(ApiResponse<PagedTransactionResult>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user transactions");
                return StatusCode(500, ApiResponse<PagedTransactionResult>.FailureResponse(
                    "An error occurred while fetching your transactions"));
            }
        }

        /// <summary>
        /// Get transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TransactionDetailDto>>> GetTransaction(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("ProductManager");

                var transaction = await _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.Order)
                    .Where(t => t.TransactionID == id)
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    return NotFound(ApiResponse<TransactionDetailDto>.FailureResponse("Transaction not found"));
                }

                // Authorization check: User can only see their own transactions unless admin
                if (!isAdmin && transaction.UserID.ToString() != userId)
                {
                    return Forbid();
                }

                var result = new TransactionDetailDto
                {
                    TransactionID = transaction.TransactionID,
                    TransactionType = transaction.TransactionType,
                    Amount = transaction.Amount,
                    TransactionDate = transaction.TransactionDate,
                    Description = transaction.Description,
                    Status = transaction.Status,
                    OrderID = transaction.OrderID,
                    OrderStatus = transaction.Order.Status,
                    OrderDate = transaction.Order.OrderDate,
                    UserID = transaction.UserID,
                    UserName = $"{transaction.User.FirstName} {transaction.User.LastName}",
                    UserEmail = transaction.User.Email!
                };

                return Ok(ApiResponse<TransactionDetailDto>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction {TransactionId}", id);
                return StatusCode(500, ApiResponse<TransactionDetailDto>.FailureResponse(
                    "An error occurred while fetching the transaction"));
            }
        }

        /// <summary>
        /// Get transaction statistics (Admin only)
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,CompanyOwner")]
        public async Task<ActionResult<ApiResponse<TransactionStatisticsDto>>> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var query = _context.Transactions.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= endDate.Value);
                }

                var transactions = await query.ToListAsync();

                var statistics = new TransactionStatisticsDto
                {
                    TotalRevenue = transactions
                        .Where(t => t.TransactionType == "Purchase" && t.Status == "Completed")
                        .Sum(t => t.Amount),
                    TotalRefunds = transactions
                        .Where(t => t.TransactionType == "Refund" && t.Status == "Completed")
                        .Sum(t => t.Amount),
                    TotalTransactions = transactions.Count,
                    SuccessfulTransactions = transactions.Count(t => t.Status == "Completed"),
                    FailedTransactions = transactions.Count(t => t.Status == "Failed"),
                    RevenueByType = transactions
                        .Where(t => t.Status == "Completed")
                        .GroupBy(t => t.TransactionType)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)),
                    CountByType = transactions
                        .GroupBy(t => t.TransactionType)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                statistics.NetRevenue = statistics.TotalRevenue - statistics.TotalRefunds;
                statistics.AverageOrderValue = statistics.SuccessfulTransactions > 0
                    ? statistics.TotalRevenue / statistics.SuccessfulTransactions
                    : 0;

                return Ok(ApiResponse<TransactionStatisticsDto>.SuccessResponse(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating transaction statistics");
                return StatusCode(500, ApiResponse<TransactionStatisticsDto>.FailureResponse(
                    "An error occurred while calculating statistics"));
            }
        }

        /// <summary>
        /// Create manual transaction (Admin only - for adjustments)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<TransactionDto>>> CreateTransaction(
            [FromBody] CreateTransactionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<TransactionDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                // Verify order exists
                var order = await _context.Orders.FindAsync(dto.OrderID);
                if (order == null)
                {
                    return NotFound(ApiResponse<TransactionDto>.FailureResponse("Order not found"));
                }

                // Verify user exists
                var user = await _userManager.FindByIdAsync(dto.UserID.ToString());
                if (user == null)
                {
                    return NotFound(ApiResponse<TransactionDto>.FailureResponse("User not found"));
                }

                var transaction = new Transaction
                {
                    TransactionType = dto.TransactionType,
                    Amount = dto.Amount,
                    Description = dto.Description,
                    OrderID = dto.OrderID,
                    UserID = dto.UserID,
                    TransactionDate = DateTime.UtcNow,
                    Status = "Completed"
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                var result = new TransactionDto
                {
                    TransactionID = transaction.TransactionID,
                    TransactionType = transaction.TransactionType,
                    Amount = transaction.Amount,
                    TransactionDate = transaction.TransactionDate,
                    Description = transaction.Description,
                    Status = transaction.Status,
                    OrderID = transaction.OrderID,
                    UserID = transaction.UserID,
                    UserName = $"{user.FirstName} {user.LastName}"
                };

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.TransactionID },
                    ApiResponse<TransactionDto>.SuccessResponse(result, "Transaction created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return StatusCode(500, ApiResponse<TransactionDto>.FailureResponse(
                    "An error occurred while creating the transaction"));
            }
        }
    }
}
