using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class FavoritesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(DataContext context, ILogger<FavoritesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcının tüm favori ürünlerini getir
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedFavoriteResult>>> GetMyFavorites(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<PagedFavoriteResult>.FailureResponse("Kullanıcı tanınmadı"));
                }

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 12;

                var totalCount = await _context.Favorites
                    .Where(f => f.UserID == userIdInt)
                    .CountAsync();

                var favorites = await _context.Favorites
                    .Where(f => f.UserID == userIdInt)
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Category)
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new FavoriteDto
                    {
                        FavoriteID = f.FavoriteID,
                        UserID = f.UserID,
                        ProductID = f.ProductID,
                        ProductName = f.Product.ProductName,
                        Brand = f.Product.Brand.BrandName,
                        Price = f.Product.Price,
                        ImageUrl = f.Product.ImageUrl,
                        Stock = f.Product.Stock,
                        CategoryName = f.Product.Category != null ? f.Product.Category.CategoryName : "Unknown",
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();

                var result = new PagedFavoriteResult
                {
                    Data = favorites,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(ApiResponse<PagedFavoriteResult>.SuccessResponse(result, "Favori ürünler getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Favori ürünler getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<PagedFavoriteResult>.FailureResponse(
                    "Favori ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Ürün favori listesine ekle veya çıkar
        /// </summary>
        [HttpPost("{productId}")]
        public async Task<ActionResult<ApiResponse<FavoriteActionDto>>> AddRemoveFavorite([FromRoute] int productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<FavoriteActionDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                // Ürün var mı kontrol et
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(ApiResponse<FavoriteActionDto>.FailureResponse("Ürün bulunamadı"));
                }

                // Favoriye zaten ekli mi kontrol et
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserID == userIdInt && f.ProductID == productId);

                if (existingFavorite != null)
                {
                    // Varsa kaldır
                    _context.Favorites.Remove(existingFavorite);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Ürün favorilerden çıkarıldı: ProductID={productId}, UserID={userIdInt}");

                    var removeDto = new FavoriteActionDto
                    {
                        Success = true,
                        Message = "Ürün favorilerden çıkarıldı",
                        ProductID = productId
                    };

                    return Ok(ApiResponse<FavoriteActionDto>.SuccessResponse(removeDto, "Ürün favorilerden çıkarıldı"));
                }
                else
                {
                    // Yoksa ekle
                    var favorite = new Favorite
                    {
                        UserID = userIdInt,
                        ProductID = productId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Favorites.Add(favorite);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Ürün favorilere eklendi: ProductID={productId}, UserID={userIdInt}");

                    var addDto = new FavoriteActionDto
                    {
                        Success = true,
                        Message = "Ürün favorilere eklendi",
                        ProductID = productId
                    };

                    return CreatedAtAction(nameof(GetMyFavorites), 
                        ApiResponse<FavoriteActionDto>.SuccessResponse(addDto, "Ürün favorilere eklendi"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Favori işlemi sırasında hata oluştu");
                return StatusCode(500, ApiResponse<FavoriteActionDto>.FailureResponse(
                    "Favori işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Ürünün favoride olup olmadığını kontrol et
        /// </summary>
        [HttpGet("{productId}/check")]
        public async Task<ActionResult<ApiResponse<IsFavoriteDto>>> IsFavorite([FromRoute] int productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<IsFavoriteDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                var isFav = await _context.Favorites
                    .AnyAsync(f => f.UserID == userIdInt && f.ProductID == productId);

                var result = new IsFavoriteDto
                {
                    ProductID = productId,
                    IsFavorite = isFav
                };

                return Ok(ApiResponse<IsFavoriteDto>.SuccessResponse(result, "Favori durumu kontrol edildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Favori kontrol sırasında hata oluştu");
                return StatusCode(500, ApiResponse<IsFavoriteDto>.FailureResponse(
                    "Favori kontrol sırasında bir hata oluştu"));
            }
        }
    }
}
