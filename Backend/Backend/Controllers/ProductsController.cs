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
    public class ProductsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(DataContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products
        // Tüm ürünleri getir (Filtreleme, Arama, Sıralama ve Sayfalama ile)
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductListDto>>>> GetProducts([FromQuery] ProductFilterParams filterParams)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .AsQueryable();

                // 1. Arama (Ürün adı veya açıklama)
                if (!string.IsNullOrWhiteSpace(filterParams.SearchTerm))
                {
                    var searchTerm = filterParams.SearchTerm.ToLower().Trim();
                    query = query.Where(p => 
                        p.ProductName.ToLower().Contains(searchTerm) || 
                        p.Description.ToLower().Contains(searchTerm) ||
                        p.Brand.ToLower().Contains(searchTerm));
                }

                // 2. Marka filtresi
                if (!string.IsNullOrWhiteSpace(filterParams.Brand))
                {
                    query = query.Where(p => p.Brand.ToLower() == filterParams.Brand.ToLower());
                }

                // 3. Kategori filtresi
                if (filterParams.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryID == filterParams.CategoryId.Value);
                }

                // 4. Fiyat aralığı
                if (filterParams.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filterParams.MinPrice.Value);
                }

                if (filterParams.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filterParams.MaxPrice.Value);
                }

                // 5. Stok durumu
                if (filterParams.InStock.HasValue && filterParams.InStock.Value)
                {
                    query = query.Where(p => p.Stock > 0);
                }

                // 6. Sıralama
                query = filterParams.SortBy?.ToLower() switch
                {
                    "name_asc" => query.OrderBy(p => p.ProductName),
                    "name_desc" => query.OrderByDescending(p => p.ProductName),
                    "price_asc" => query.OrderBy(p => p.Price),
                    "price_desc" => query.OrderByDescending(p => p.Price),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                // Toplam kayıt sayısı
                var totalCount = await query.CountAsync();

                // 7. Sayfalama
                var products = await query
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .Select(p => new ProductListDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category.CategoryName,
                        Stock = p.Stock,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                var pagedResult = new PagedResult<ProductListDto>
                {
                    Items = products,
                    TotalCount = totalCount,
                    PageNumber = filterParams.PageNumber,
                    PageSize = filterParams.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                };

                return Ok(ApiResponse<PagedResult<ProductListDto>>.SuccessResponse(
                    pagedResult, 
                    $"Retrieved {products.Count} products successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, ApiResponse<PagedResult<ProductListDto>>.FailureResponse(
                    "An error occurred while retrieving products"));
            }
        }

        // GET: api/products/{id}
        // Tek bir ürünün detaylarını getir
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductReviews)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<ProductDetailDto>.FailureResponse(
                        $"Product with ID {id} not found"));
                }

                var productDto = new ProductDetailDto
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Brand = product.Brand,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    CategoryName = product.Category.CategoryName,
                    CategoryID = product.CategoryID,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    AverageRating = product.ProductReviews.Any() 
                        ? Math.Round(product.ProductReviews.Average(r => r.Rating), 1)
                        : 0,
                    ReviewCount = product.ProductReviews.Count
                };

                return Ok(ApiResponse<ProductDetailDto>.SuccessResponse(
                    productDto, 
                    "Product details retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
                return StatusCode(500, ApiResponse<ProductDetailDto>.FailureResponse(
                    "An error occurred while retrieving product details"));
            }
        }

        // GET: api/products/category/{categoryId}
        // Belirli bir kategoriye ait ürünleri getir
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetProductsByCategory(int categoryId)
        {
            try
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == categoryId);
                if (!categoryExists)
                {
                    return NotFound(ApiResponse<List<ProductListDto>>.FailureResponse(
                        $"Category with ID {categoryId} not found"));
                }

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryID == categoryId && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductListDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category.CategoryName,
                        Stock = p.Stock,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<ProductListDto>>.SuccessResponse(
                    products, 
                    $"Retrieved {products.Count} products for category"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {CategoryId}", categoryId);
                return StatusCode(500, ApiResponse<List<ProductListDto>>.FailureResponse(
                    "An error occurred while retrieving products"));
            }
        }

        // GET: api/products/featured
        // Öne çıkan/popüler ürünleri getir (Son eklenen 8 ürün)
        [HttpGet("featured")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetFeaturedProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.Stock > 0)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .Select(p => new ProductListDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category.CategoryName,
                        Stock = p.Stock,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<ProductListDto>>.SuccessResponse(
                    products, 
                    "Featured products retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured products");
                return StatusCode(500, ApiResponse<List<ProductListDto>>.FailureResponse(
                    "An error occurred while retrieving featured products"));
            }
        }

        // GET: api/products/brands
        // Tüm marka isimlerini getir (Filtreleme için)
        [HttpGet("brands")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetBrands()
        {
            try
            {
                var brands = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Brand)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();

                return Ok(ApiResponse<List<string>>.SuccessResponse(
                    brands, 
                    "Brands retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands");
                return StatusCode(500, ApiResponse<List<string>>.FailureResponse(
                    "An error occurred while retrieving brands"));
            }
        }

        // POST: api/products
        // Yeni ürün ekle (Sadece Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductDetailDto>>> CreateProduct([FromBody] CreateProductDto productDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                    "Validation failed", errors));
            }

            try
            {
                // Kategori kontrolü
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == productDto.CategoryID);
                if (!categoryExists)
                {
                    return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                        $"Category with ID {productDto.CategoryID} does not exist"));
                }

                // Aynı isimde ürün var mı kontrol et
                var productExists = await _context.Products
                    .AnyAsync(p => p.ProductName.ToLower() == productDto.ProductName.ToLower());
                if (productExists)
                {
                    return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                        "A product with this name already exists"));
                }

                var product = new Product
                {
                    ProductName = productDto.ProductName,
                    Brand = productDto.Brand,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    Stock = productDto.Stock,
                    CategoryID = productDto.CategoryID,
                    ImageUrl = productDto.ImageUrl,
                    IsActive = productDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Eklenen ürünü detaylarıyla birlikte döndür
                var createdProduct = await _context.Products
                    .Include(p => p.Category)
                    .FirstAsync(p => p.ProductID == product.ProductID);

                var resultDto = new ProductDetailDto
                {
                    ProductID = createdProduct.ProductID,
                    ProductName = createdProduct.ProductName,
                    Brand = createdProduct.Brand,
                    Description = createdProduct.Description,
                    Price = createdProduct.Price,
                    Stock = createdProduct.Stock,
                    ImageUrl = createdProduct.ImageUrl,
                    CategoryName = createdProduct.Category.CategoryName,
                    CategoryID = createdProduct.CategoryID,
                    IsActive = createdProduct.IsActive,
                    CreatedAt = createdProduct.CreatedAt,
                    AverageRating = 0,
                    ReviewCount = 0
                };

                _logger.LogInformation("Product created successfully with ID {ProductId}", product.ProductID);

                return CreatedAtAction(
                    nameof(GetProductById), 
                    new { id = product.ProductID }, 
                    ApiResponse<ProductDetailDto>.SuccessResponse(
                        resultDto, 
                        "Product created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, ApiResponse<ProductDetailDto>.FailureResponse(
                    "An error occurred while creating the product"));
            }
        }

        // PUT: api/products/{id}
        // Ürün güncelle (Sadece Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductDetailDto>>> UpdateProduct(int id, [FromBody] UpdateProductDto productDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                    "Validation failed", errors));
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<ProductDetailDto>.FailureResponse(
                        $"Product with ID {id} not found"));
                }

                // Kategori kontrolü
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == productDto.CategoryID);
                if (!categoryExists)
                {
                    return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                        $"Category with ID {productDto.CategoryID} does not exist"));
                }

                // Aynı isimde başka bir ürün var mı kontrol et (kendisi hariç)
                var duplicateProduct = await _context.Products
                    .AnyAsync(p => p.ProductName.ToLower() == productDto.ProductName.ToLower() 
                               && p.ProductID != id);
                if (duplicateProduct)
                {
                    return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                        "Another product with this name already exists"));
                }

                // Güncelle
                product.ProductName = productDto.ProductName;
                product.Brand = productDto.Brand;
                product.Description = productDto.Description;
                product.Price = productDto.Price;
                product.Stock = productDto.Stock;
                product.CategoryID = productDto.CategoryID;
                product.ImageUrl = productDto.ImageUrl;
                product.IsActive = productDto.IsActive;

                await _context.SaveChangesAsync();

                // Güncellenmiş ürünü döndür
                var updatedProduct = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductReviews)
                    .FirstAsync(p => p.ProductID == id);

                var resultDto = new ProductDetailDto
                {
                    ProductID = updatedProduct.ProductID,
                    ProductName = updatedProduct.ProductName,
                    Brand = updatedProduct.Brand,
                    Description = updatedProduct.Description,
                    Price = updatedProduct.Price,
                    Stock = updatedProduct.Stock,
                    ImageUrl = updatedProduct.ImageUrl,
                    CategoryName = updatedProduct.Category.CategoryName,
                    CategoryID = updatedProduct.CategoryID,
                    IsActive = updatedProduct.IsActive,
                    CreatedAt = updatedProduct.CreatedAt,
                    AverageRating = updatedProduct.ProductReviews.Any() 
                        ? Math.Round(updatedProduct.ProductReviews.Average(r => r.Rating), 1)
                        : 0,
                    ReviewCount = updatedProduct.ProductReviews.Count
                };

                _logger.LogInformation("Product with ID {ProductId} updated successfully", id);

                return Ok(ApiResponse<ProductDetailDto>.SuccessResponse(
                    resultDto, 
                    "Product updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
                return StatusCode(500, ApiResponse<ProductDetailDto>.FailureResponse(
                    "An error occurred while updating the product"));
            }
        }

        // DELETE: api/products/{id}
        // Ürün sil (Soft Delete - Sadece Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse(
                        $"Product with ID {id} not found"));
                }

                // Soft Delete (Recommended: Pasife çek, veritabanından silme)
                product.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product with ID {ProductId} marked as inactive", id);

                return Ok(ApiResponse<object>.SuccessResponse(
                    new { ProductID = id, IsActive = false }, 
                    "Product deactivated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    "An error occurred while deleting the product"));
            }
        }

        // DELETE: api/products/{id}/permanent
        // Ürünü kalıcı olarak sil (Hard Delete - Sadece Admin - Dikkatli kullan!)
        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> PermanentDeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductReviews)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductSpecifications)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse(
                        $"Product with ID {id} not found"));
                }

                // Sipariş ile ilişkili mi kontrol et
                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductID == id);
                if (hasOrders)
                {
                    return BadRequest(ApiResponse<object>.FailureResponse(
                        "Cannot permanently delete product. It is associated with existing orders. Use soft delete instead."));
                }

                // İlişkili kayıtları sil
                _context.ProductReviews.RemoveRange(product.ProductReviews);
                _context.ProductImages.RemoveRange(product.ProductImages);
                _context.ProductSpecifications.RemoveRange(product.ProductSpecifications);

                // Ürünü sil
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Product with ID {ProductId} permanently deleted", id);

                return Ok(ApiResponse<object>.SuccessResponse(
                    null, 
                    "Product permanently deleted"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting product with ID {ProductId}", id);
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    "An error occurred while permanently deleting the product"));
            }
        }

        // PATCH: api/products/{id}/stock
        // Stok güncelle (Admin)
        [HttpPatch("{id}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStock(int id, [FromBody] int newStock)
        {
            try
            {
                if (newStock < 0)
                {
                    return BadRequest(ApiResponse<object>.FailureResponse(
                        "Stock cannot be negative"));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse(
                        $"Product with ID {id} not found"));
                }

                var oldStock = product.Stock;
                product.Stock = newStock;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stock updated for product {ProductId}: {OldStock} -> {NewStock}", 
                    id, oldStock, newStock);

                return Ok(ApiResponse<object>.SuccessResponse(
                    new { ProductID = id, OldStock = oldStock, NewStock = newStock }, 
                    "Stock updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    "An error occurred while updating stock"));
            }
        }
    }
}