using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(DataContext context, UserManager<User> userManager, ILogger<ReviewsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all reviews for a product with pagination and filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedReviewResult>>> GetProductReviews(
            int productId,
            [FromQuery] ReviewFilterParams filterParams)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return NotFound(ApiResponse<PagedReviewResult>.FailureResponse("Product not found", null));

                var query = _context.ProductReviews
                    .Where(r => r.ProductID == productId)
                    .Include(r => r.User)
                    .AsQueryable();

                if (filterParams.Rating.HasValue && filterParams.Rating >= 1 && filterParams.Rating <= 5)
                    query = query.Where(r => r.Rating == filterParams.Rating.Value);

                if (filterParams.SortBy?.ToLower() == "rating")
                {
                    query = filterParams.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(r => r.Rating)
                        : query.OrderByDescending(r => r.Rating);
                }
                else
                {
                    query = filterParams.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(r => r.ReviewDate)
                        : query.OrderByDescending(r => r.ReviewDate);
                }

                var totalCount = await query.CountAsync();
                var reviews = await query
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .ToListAsync();

                var reviewDtos = reviews.Select(r => new ReviewDto
                {
                    ProductReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserName = r.User?.UserName ?? "Anonymous",
                    Rating = r.Rating,
                    ReviewText = r.Comment,
                    ReviewDate = r.ReviewDate,
                    IsVerifiedPurchase = r.IsApproved
                }).ToList();

                var result = new PagedReviewResult
                {
                    Reviews = reviewDtos,
                    TotalCount = totalCount,
                    PageNumber = filterParams.PageNumber,
                    PageSize = filterParams.PageSize
                };

                _logger.LogInformation($"Retrieved {reviews.Count} reviews for product {productId}");
                return Ok(ApiResponse<PagedReviewResult>.SuccessResponse(result, "Reviews retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving reviews: {ex.Message}");
                return StatusCode(500, ApiResponse<PagedReviewResult>.FailureResponse("An error occurred", null));
            }
        }

        /// <summary>
        /// Get review statistics for a product
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<ProductReviewSummaryDto>>> GetProductReviewSummary(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.ProductID == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                    return NotFound(ApiResponse<ProductReviewSummaryDto>.FailureResponse("Product not found", null));

                var reviews = await _context.ProductReviews
                    .Where(r => r.ProductID == productId)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.ReviewDate)
                    .ToListAsync();

                var totalReviews = reviews.Count;
                var averageRating = totalReviews > 0 ? (decimal)reviews.Average(r => r.Rating) : 0;

                var ratingDistribution = new Dictionary<int, int>
                {
                    { 5, reviews.Count(r => r.Rating == 5) },
                    { 4, reviews.Count(r => r.Rating == 4) },
                    { 3, reviews.Count(r => r.Rating == 3) },
                    { 2, reviews.Count(r => r.Rating == 2) },
                    { 1, reviews.Count(r => r.Rating == 1) }
                };

                var reviewDtos = reviews.Take(5).Select(r => new ReviewDto
                {
                    ProductReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserName = r.User?.UserName ?? "Anonymous",
                    Rating = r.Rating,
                    ReviewText = r.Comment,
                    ReviewDate = r.ReviewDate,
                    IsVerifiedPurchase = r.IsApproved
                }).ToList();

                var summary = new ProductReviewSummaryDto
                {
                    ProductID = productId,
                    ProductName = product.ProductName,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalReviews = totalReviews,
                    RatingDistribution = ratingDistribution,
                    Reviews = reviewDtos
                };

                _logger.LogInformation($"Review summary retrieved for product {productId}: {averageRating:F2} stars");
                return Ok(ApiResponse<ProductReviewSummaryDto>.SuccessResponse(summary, "Review summary retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving review summary: {ex.Message}");
                return StatusCode(500, ApiResponse<ProductReviewSummaryDto>.FailureResponse("An error occurred", null));
            }
        }

        /// <summary>
        /// Create a new review for a product
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview(
            int productId,
            [FromBody] CreateReviewDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ReviewDto>.FailureResponse("Invalid request data", errors));
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return NotFound(ApiResponse<ReviewDto>.FailureResponse("Product not found", null));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                    return Unauthorized(ApiResponse<ReviewDto>.FailureResponse("User not found", null));

                var existingReview = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ProductID == productId && r.UserID == userId);

                if (existingReview != null)
                    return BadRequest(ApiResponse<ReviewDto>.FailureResponse("You have already reviewed this product", null));

                var hasPurchased = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserID == userId && o.Status == "Delivered")
                    .SelectMany(o => o.OrderItems)
                    .AnyAsync(oi => oi.ProductID == productId);

                var review = new ProductReview
                {
                    ProductID = productId,
                    UserID = userId,
                    Rating = request.Rating,
                    Comment = request.ReviewText ?? "",
                    ReviewDate = DateTime.UtcNow,
                    IsApproved = hasPurchased
                };

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                var reviewDto = new ReviewDto
                {
                    ProductReviewID = review.ReviewID,
                    ProductID = review.ProductID,
                    UserName = user.UserName ?? "Anonymous",
                    Rating = review.Rating,
                    ReviewText = review.Comment,
                    ReviewDate = review.ReviewDate,
                    IsVerifiedPurchase = review.IsApproved
                };

                _logger.LogInformation($"User {userId} created review for product {productId} with rating {request.Rating}");
                return CreatedAtAction(nameof(GetProductReviews), new { productId }, ApiResponse<ReviewDto>.SuccessResponse(reviewDto, "Review created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating review: {ex.Message}");
                return StatusCode(500, ApiResponse<ReviewDto>.FailureResponse("An error occurred", null));
            }
        }

        /// <summary>
        /// Update user's review
        /// </summary>
        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> UpdateReview(
            int productId,
            int reviewId,
            [FromBody] UpdateReviewDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ReviewDto>.FailureResponse("Invalid request data", errors));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var review = await _context.ProductReviews
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.ReviewID == reviewId && r.ProductID == productId);

                if (review == null)
                    return NotFound(ApiResponse<ReviewDto>.FailureResponse("Review not found", null));

                if (review.UserID != userId)
                    return Forbid();

                review.Rating = request.Rating;
                review.Comment = request.ReviewText ?? "";
                review.ReviewDate = DateTime.UtcNow;

                _context.ProductReviews.Update(review);
                await _context.SaveChangesAsync();

                var reviewDto = new ReviewDto
                {
                    ProductReviewID = review.ReviewID,
                    ProductID = review.ProductID,
                    UserName = review.User?.UserName ?? "Anonymous",
                    Rating = review.Rating,
                    ReviewText = review.Comment,
                    ReviewDate = review.ReviewDate,
                    IsVerifiedPurchase = review.IsApproved
                };

                _logger.LogInformation($"User {userId} updated review {reviewId}");
                return Ok(ApiResponse<ReviewDto>.SuccessResponse(reviewDto, "Review updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating review: {ex.Message}");
                return StatusCode(500, ApiResponse<ReviewDto>.FailureResponse("An error occurred", null));
            }
        }

        /// <summary>
        /// Delete review (user or admin only)
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteReview(int productId, int reviewId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var isAdmin = User.IsInRole("Admin");

                var review = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ReviewID == reviewId && r.ProductID == productId);

                if (review == null)
                    return NotFound(ApiResponse<object>.FailureResponse("Review not found", null));

                if (review.UserID != userId && !isAdmin)
                    return Forbid();

                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Review {reviewId} deleted by user {userId}");
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Review deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting review: {ex.Message}");
                return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred", null));
            }
        }

        /// <summary>
        /// Get user's reviews
        /// </summary>
        [HttpGet("~/api/reviews/my-reviews")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedReviewResult>>> GetMyReviews([FromQuery] ReviewFilterParams filterParams)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var query = _context.ProductReviews
                    .Where(r => r.UserID == userId)
                    .Include(r => r.Product)
                    .Include(r => r.User)
                    .AsQueryable();

                if (filterParams.Rating.HasValue && filterParams.Rating >= 1 && filterParams.Rating <= 5)
                    query = query.Where(r => r.Rating == filterParams.Rating.Value);

                if (filterParams.SortBy?.ToLower() == "rating")
                {
                    query = filterParams.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(r => r.Rating)
                        : query.OrderByDescending(r => r.Rating);
                }
                else
                {
                    query = filterParams.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(r => r.ReviewDate)
                        : query.OrderByDescending(r => r.ReviewDate);
                }

                var totalCount = await query.CountAsync();
                var reviews = await query
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .ToListAsync();

                var reviewDtos = reviews.Select(r => new ReviewDto
                {
                    ProductReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserName = r.User?.UserName ?? "Anonymous",
                    Rating = r.Rating,
                    ReviewText = r.Comment,
                    ReviewDate = r.ReviewDate,
                    IsVerifiedPurchase = r.IsApproved
                }).ToList();

                var result = new PagedReviewResult
                {
                    Reviews = reviewDtos,
                    TotalCount = totalCount,
                    PageNumber = filterParams.PageNumber,
                    PageSize = filterParams.PageSize
                };

                _logger.LogInformation($"User {userId} retrieved {reviews.Count} reviews");
                return Ok(ApiResponse<PagedReviewResult>.SuccessResponse(result, "Your reviews retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user reviews: {ex.Message}");
                return StatusCode(500, ApiResponse<PagedReviewResult>.FailureResponse("An error occurred", null));
            }
        }

        /// <summary>
        /// İnsan kaynakları/Çalışan tarafından review onayı
        /// </summary>
        [HttpPut("{reviewId}/approve")]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> ApproveReview(
            [FromRoute] int productId,
            [FromRoute] int reviewId)
        {
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ReviewID == reviewId && r.ProductID == productId);

                if (review == null)
                {
                    return NotFound(ApiResponse<ReviewDto>.FailureResponse("Review bulunamadı"));
                }

                // Zaten onaylanmış mı kontrol et
                if (review.IsApproved)
                {
                    return BadRequest(ApiResponse<ReviewDto>.FailureResponse("Bu review zaten onaylanmış"));
                }

                // Review'ı onayla
                review.IsApproved = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Review onaylandı: ReviewID={reviewId}, ProductID={productId}");

                var reviewDto = new ReviewDto
                {
                    ProductReviewID = review.ReviewID,
                    ProductID = review.ProductID,
                    UserName = review.User?.UserName ?? "Anonymous",
                    Rating = review.Rating,
                    ReviewText = review.Comment,
                    ReviewDate = review.ReviewDate,
                    IsVerifiedPurchase = review.IsApproved
                };

                return Ok(ApiResponse<ReviewDto>.SuccessResponse(reviewDto, "Review başarıyla onaylandı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Review onaylanırken hata oluştu");
                return StatusCode(500, ApiResponse<ReviewDto>.FailureResponse("Review onaylanırken bir hata oluştu"));
            }
        }
    }
}
