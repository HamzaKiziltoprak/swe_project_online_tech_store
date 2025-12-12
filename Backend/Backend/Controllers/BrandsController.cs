using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(DataContext context, ILogger<BrandsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all brands
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetBrands(
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.Brands.AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(b => b.IsActive == isActive.Value);
                }

                var brands = await query
                    .Select(b => new BrandDto
                    {
                        BrandID = b.BrandID,
                        BrandName = b.BrandName,
                        Description = b.Description,
                        LogoUrl = b.LogoUrl,
                        IsActive = b.IsActive,
                        ProductCount = b.Products.Count(p => p.IsActive)
                    })
                    .OrderBy(b => b.BrandName)
                    .ToListAsync();

                return Ok(ApiResponse<List<BrandDto>>.SuccessResponse(
                    brands,
                    $"{brands.Count} brands retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands");
                return StatusCode(500, ApiResponse<List<BrandDto>>.FailureResponse(
                    "Failed to retrieve brands"));
            }
        }

        /// <summary>
        /// Get brand by ID with products
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BrandDetailDto>>> GetBrand(int id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products.Where(p => p.IsActive))
                        .ThenInclude(p => p.Category)
                    .Include(b => b.Products)
                        .ThenInclude(p => p.ProductReviews)
                    .FirstOrDefaultAsync(b => b.BrandID == id);

                if (brand == null)
                {
                    return NotFound(ApiResponse<BrandDetailDto>.FailureResponse("Brand not found"));
                }

                var brandDetail = new BrandDetailDto
                {
                    BrandID = brand.BrandID,
                    BrandName = brand.BrandName,
                    Description = brand.Description,
                    LogoUrl = brand.LogoUrl,
                    IsActive = brand.IsActive,
                    CreatedAt = brand.CreatedAt,
                    ProductCount = brand.Products.Count(p => p.IsActive),
                    Products = brand.Products
                        .Where(p => p.IsActive)
                        .Select(p => new ProductListDto
                        {
                            ProductID = p.ProductID,
                            ProductName = p.ProductName,
                            Brand = brand.BrandName,
                            Price = p.Price,
                            Stock = p.Stock,
                            ImageUrl = p.ImageUrl,
                            CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                            IsActive = p.IsActive,
                            AverageRating = p.ProductReviews.Any() ? p.ProductReviews.Average(r => r.Rating) : 0
                        })
                        .ToList()
                };

                return Ok(ApiResponse<BrandDetailDto>.SuccessResponse(brandDetail));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving brand {id}");
                return StatusCode(500, ApiResponse<BrandDetailDto>.FailureResponse(
                    "Failed to retrieve brand"));
            }
        }

        /// <summary>
        /// Create new brand (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<BrandDto>>> CreateBrand(
            [FromBody] CreateBrandDto dto)
        {
            try
            {
                // Check if brand already exists
                var existingBrand = await _context.Brands
                    .FirstOrDefaultAsync(b => b.BrandName.ToLower() == dto.BrandName.ToLower());

                if (existingBrand != null)
                {
                    return BadRequest(ApiResponse<BrandDto>.FailureResponse(
                        "A brand with this name already exists"));
                }

                var brand = new Brand
                {
                    BrandName = dto.BrandName,
                    Description = dto.Description,
                    LogoUrl = dto.LogoUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Brand created: {brand.BrandName} (ID: {brand.BrandID})");

                var result = new BrandDto
                {
                    BrandID = brand.BrandID,
                    BrandName = brand.BrandName,
                    Description = brand.Description,
                    LogoUrl = brand.LogoUrl,
                    IsActive = brand.IsActive,
                    ProductCount = 0
                };

                return CreatedAtAction(nameof(GetBrand), new { id = brand.BrandID },
                    ApiResponse<BrandDto>.SuccessResponse(result, "Brand created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                return StatusCode(500, ApiResponse<BrandDto>.FailureResponse(
                    "Failed to create brand"));
            }
        }

        /// <summary>
        /// Update brand (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<BrandDto>>> UpdateBrand(
            int id,
            [FromBody] UpdateBrandDto dto)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return NotFound(ApiResponse<BrandDto>.FailureResponse("Brand not found"));
                }

                // Check if new name conflicts with existing brand
                if (brand.BrandName.ToLower() != dto.BrandName.ToLower())
                {
                    var existingBrand = await _context.Brands
                        .FirstOrDefaultAsync(b => b.BrandName.ToLower() == dto.BrandName.ToLower());

                    if (existingBrand != null)
                    {
                        return BadRequest(ApiResponse<BrandDto>.FailureResponse(
                            "A brand with this name already exists"));
                    }
                }

                brand.BrandName = dto.BrandName;
                brand.Description = dto.Description;
                brand.LogoUrl = dto.LogoUrl;
                brand.IsActive = dto.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Brand updated: {brand.BrandName} (ID: {brand.BrandID})");

                var result = new BrandDto
                {
                    BrandID = brand.BrandID,
                    BrandName = brand.BrandName,
                    Description = brand.Description,
                    LogoUrl = brand.LogoUrl,
                    IsActive = brand.IsActive,
                    ProductCount = await _context.Products.CountAsync(p => p.BrandID == id && p.IsActive)
                };

                return Ok(ApiResponse<BrandDto>.SuccessResponse(result, "Brand updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating brand {id}");
                return StatusCode(500, ApiResponse<BrandDto>.FailureResponse(
                    "Failed to update brand"));
            }
        }

        /// <summary>
        /// Delete brand (Admin only) - Soft delete
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteBrand(int id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BrandID == id);

                if (brand == null)
                {
                    return NotFound(ApiResponse<string>.FailureResponse("Brand not found"));
                }

                // Check if brand has active products
                var activeProductCount = brand.Products.Count(p => p.IsActive);
                if (activeProductCount > 0)
                {
                    return BadRequest(ApiResponse<string>.FailureResponse(
                        $"Cannot delete brand with {activeProductCount} active products. Deactivate products first."));
                }

                // Soft delete
                brand.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Brand deactivated: {brand.BrandName} (ID: {brand.BrandID})");

                return Ok(ApiResponse<string>.SuccessResponse(
                    "Brand deactivated successfully",
                    "Brand has been marked as inactive"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting brand {id}");
                return StatusCode(500, ApiResponse<string>.FailureResponse(
                    "Failed to delete brand"));
            }
        }

        /// <summary>
        /// Get brands with product counts for filtering
        /// </summary>
        [HttpGet("with-counts")]
        public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetBrandsWithCounts()
        {
            try
            {
                var brands = await _context.Brands
                    .Where(b => b.IsActive)
                    .Select(b => new BrandDto
                    {
                        BrandID = b.BrandID,
                        BrandName = b.BrandName,
                        Description = b.Description,
                        LogoUrl = b.LogoUrl,
                        IsActive = b.IsActive,
                        ProductCount = b.Products.Count(p => p.IsActive)
                    })
                    .Where(b => b.ProductCount > 0)
                    .OrderByDescending(b => b.ProductCount)
                    .ToListAsync();

                return Ok(ApiResponse<List<BrandDto>>.SuccessResponse(brands));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands with counts");
                return StatusCode(500, ApiResponse<List<BrandDto>>.FailureResponse(
                    "Failed to retrieve brands"));
            }
        }
    }
}
