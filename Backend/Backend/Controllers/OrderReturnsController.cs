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
    [Route("api/orders/{orderId}/returns")]
    [ApiController]
    [Authorize]
    public class OrderReturnsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<OrderReturnsController> _logger;

        public OrderReturnsController(
            DataContext context,
            UserManager<User> userManager,
            ILogger<OrderReturnsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Create a return request for an order (Sadece Customer)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> CreateReturn(
            int orderId,
            [FromBody] CreateReturnDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<ReturnDto>.FailureResponse("User not authenticated"));
                }

                var userIdInt = int.Parse(userId);

                // Check if order exists and belongs to user
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userIdInt);

                if (order == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("Order not found"));
                }

                // Check if order is completed
                if (order.Status != "Completed" && order.Status != "Delivered")
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Only completed or delivered orders can be returned"));
                }

                // Check if return already exists
                var existingReturn = await _context.OrderReturns
                    .FirstOrDefaultAsync(r => r.OrderID == orderId);

                if (existingReturn != null)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "A return request already exists for this order"));
                }

                // Create return request
                var orderReturn = new OrderReturn
                {
                    OrderID = orderId,
                    UserID = userIdInt,
                    ReturnReason = dto.ReturnReason,
                    ReturnDescription = dto.ReturnDescription,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderReturns.Add(orderReturn);
                await _context.SaveChangesAsync();

                var returnDto = new ReturnDto
                {
                    ReturnID = orderReturn.ReturnID,
                    OrderID = orderReturn.OrderID,
                    UserID = orderReturn.UserID,
                    ReturnReason = orderReturn.ReturnReason,
                    ReturnDescription = orderReturn.ReturnDescription,
                    Status = orderReturn.Status,
                    RefundAmount = orderReturn.RefundAmount,
                    CreatedAt = orderReturn.CreatedAt,
                    UpdatedAt = orderReturn.UpdatedAt,
                    AdminNote = orderReturn.AdminNote
                };

                _logger.LogInformation($"Return request created: ReturnID={orderReturn.ReturnID}, OrderID={orderId}");

                return CreatedAtAction(nameof(GetReturn), new { orderId, id = orderReturn.ReturnID },
                    ApiResponse<ReturnDto>.SuccessResponse(returnDto, "Return request created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating return request");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse(
                    "An error occurred while creating return request"));
            }
        }

        /// <summary>
        /// Get return request for an order (Sadece Customer kendi iade talebini görebilir)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> GetReturn(int orderId, int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var orderReturn = await _context.OrderReturns
                    .FirstOrDefaultAsync(r => r.ReturnID == id && r.OrderID == orderId);

                if (orderReturn == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("Return request not found"));
                }

                // Check authorization - Customer sadece kendi iade talebini görebilir
                if (orderReturn.UserID.ToString() != userId)
                {
                    return Forbid();
                }

                var returnDto = new ReturnDto
                {
                    ReturnID = orderReturn.ReturnID,
                    OrderID = orderReturn.OrderID,
                    UserID = orderReturn.UserID,
                    ReturnReason = orderReturn.ReturnReason,
                    ReturnDescription = orderReturn.ReturnDescription,
                    Status = orderReturn.Status,
                    RefundAmount = orderReturn.RefundAmount,
                    CreatedAt = orderReturn.CreatedAt,
                    UpdatedAt = orderReturn.UpdatedAt,
                    AdminNote = orderReturn.AdminNote
                };

                return Ok(ApiResponse<ReturnDto>.SuccessResponse(returnDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching return request");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse(
                    "An error occurred while fetching return request"));
            }
        }

        /// <summary>
        /// Get all return requests with filtering (Admin only)
        /// </summary>
        [HttpGet("/api/returns")]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<ApiResponse<PagedReturnResult>>> GetAllReturns(
            [FromQuery] ReturnFilterParams filterParams)
        {
            try
            {
                var query = _context.OrderReturns
                    .Include(r => r.Order)
                    .Include(r => r.User)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filterParams.Status))
                {
                    query = query.Where(r => r.Status == filterParams.Status);
                }

                if (!string.IsNullOrWhiteSpace(filterParams.ReturnReason))
                {
                    query = query.Where(r => r.ReturnReason == filterParams.ReturnReason);
                }

                if (filterParams.OrderID.HasValue)
                {
                    query = query.Where(r => r.OrderID == filterParams.OrderID.Value);
                }

                if (filterParams.UserID.HasValue)
                {
                    query = query.Where(r => r.UserID == filterParams.UserID.Value);
                }

                if (filterParams.StartDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt >= filterParams.StartDate.Value);
                }

                if (filterParams.EndDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt <= filterParams.EndDate.Value);
                }

                var totalCount = await query.CountAsync();

                var returns = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .Select(r => new ReturnDto
                    {
                        ReturnID = r.ReturnID,
                        OrderID = r.OrderID,
                        UserID = r.UserID,
                        ReturnReason = r.ReturnReason,
                        ReturnDescription = r.ReturnDescription,
                        Status = r.Status,
                        RefundAmount = r.RefundAmount,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        AdminNote = r.AdminNote
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize);

                var result = new PagedReturnResult
                {
                    Data = returns,
                    TotalCount = totalCount,
                    PageNumber = filterParams.PageNumber,
                    PageSize = filterParams.PageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = filterParams.PageNumber > 1,
                    HasNextPage = filterParams.PageNumber < totalPages
                };

                return Ok(ApiResponse<PagedReturnResult>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching return requests");
                return StatusCode(500, ApiResponse<PagedReturnResult>.FailureResponse(
                    "An error occurred while fetching return requests"));
            }
        }

        /// <summary>
        /// Get user's return requests
        /// </summary>
        [HttpGet("/api/my-returns")]
        public async Task<ActionResult<ApiResponse<List<ReturnDto>>>> GetMyReturns()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<List<ReturnDto>>.FailureResponse("User not authenticated"));
                }

                var userIdInt = int.Parse(userId);

                var returns = await _context.OrderReturns
                    .Where(r => r.UserID == userIdInt)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReturnDto
                    {
                        ReturnID = r.ReturnID,
                        OrderID = r.OrderID,
                        UserID = r.UserID,
                        ReturnReason = r.ReturnReason,
                        ReturnDescription = r.ReturnDescription,
                        Status = r.Status,
                        RefundAmount = r.RefundAmount,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        AdminNote = r.AdminNote
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<ReturnDto>>.SuccessResponse(returns));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user returns");
                return StatusCode(500, ApiResponse<List<ReturnDto>>.FailureResponse(
                    "An error occurred while fetching your returns"));
            }
        }

        /// <summary>
        /// Approve return request and issue refund (Admin only)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> ApproveReturn(
            int orderId,
            int id,
            [FromBody] ApproveReturnDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var orderReturn = await _context.OrderReturns
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.ReturnID == id && r.OrderID == orderId);

                if (orderReturn == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("Return request not found"));
                }

                if (orderReturn.Status != "Pending")
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Only pending return requests can be approved"));
                }

                // Update return status
                orderReturn.Status = "Approved";
                orderReturn.RefundAmount = dto.RefundAmount;
                orderReturn.AdminNote = dto.AdminNote;
                orderReturn.UpdatedAt = DateTime.UtcNow;

                // Create refund transaction
                var refundTransaction = new Transaction
                {
                    TransactionType = "Refund",
                    Amount = dto.RefundAmount,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Refund for Order #{orderId} - {orderReturn.ReturnReason}",
                    Status = "Completed",
                    OrderID = orderId,
                    UserID = orderReturn.UserID
                };
                _context.Transactions.Add(refundTransaction);
                await _context.SaveChangesAsync();

                // Link refund transaction to return
                orderReturn.RefundTransactionID = refundTransaction.TransactionID;
                orderReturn.Status = "Completed"; // Mark as completed after refund
                await _context.SaveChangesAsync();

                var returnDto = new ReturnDto
                {
                    ReturnID = orderReturn.ReturnID,
                    OrderID = orderReturn.OrderID,
                    UserID = orderReturn.UserID,
                    ReturnReason = orderReturn.ReturnReason,
                    ReturnDescription = orderReturn.ReturnDescription,
                    Status = orderReturn.Status,
                    RefundAmount = orderReturn.RefundAmount,
                    CreatedAt = orderReturn.CreatedAt,
                    UpdatedAt = orderReturn.UpdatedAt,
                    AdminNote = orderReturn.AdminNote
                };

                _logger.LogInformation($"Return approved and refunded: ReturnID={id}, Amount={dto.RefundAmount}");

                return Ok(ApiResponse<ReturnDto>.SuccessResponse(returnDto, "Return approved and refund processed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving return request");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse(
                    "An error occurred while approving return request"));
            }
        }

        /// <summary>
        /// Reject return request (Admin only)
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> RejectReturn(
            int orderId,
            int id,
            [FromBody] RejectReturnDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var orderReturn = await _context.OrderReturns
                    .FirstOrDefaultAsync(r => r.ReturnID == id && r.OrderID == orderId);

                if (orderReturn == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("Return request not found"));
                }

                if (orderReturn.Status != "Pending")
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Only pending return requests can be rejected"));
                }

                orderReturn.Status = "Rejected";
                orderReturn.AdminNote = dto.AdminNote;
                orderReturn.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var returnDto = new ReturnDto
                {
                    ReturnID = orderReturn.ReturnID,
                    OrderID = orderReturn.OrderID,
                    UserID = orderReturn.UserID,
                    ReturnReason = orderReturn.ReturnReason,
                    ReturnDescription = orderReturn.ReturnDescription,
                    Status = orderReturn.Status,
                    RefundAmount = orderReturn.RefundAmount,
                    CreatedAt = orderReturn.CreatedAt,
                    UpdatedAt = orderReturn.UpdatedAt,
                    AdminNote = orderReturn.AdminNote
                };

                _logger.LogInformation($"Return rejected: ReturnID={id}");

                return Ok(ApiResponse<ReturnDto>.SuccessResponse(returnDto, "Return request rejected"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting return request");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse(
                    "An error occurred while rejecting return request"));
            }
        }
    }
}
