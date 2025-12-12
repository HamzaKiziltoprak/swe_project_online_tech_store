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

                // 6. EXCLUDE FILTERS - "Bu özellikleri istemiyor" filtreleri
                
                // Hariç tutulacak markalar
                if (!string.IsNullOrWhiteSpace(filterParams.ExcludeBrands))
                {
                    var excludedBrands = filterParams.ExcludeBrands
                        .Split(',')
                        .Select(b => b.Trim().ToLower())
                        .ToList();
                    
                    query = query.Where(p => !excludedBrands.Contains(p.Brand.ToLower()));
                    _logger.LogInformation($"Excluded brands: {string.Join(", ", excludedBrands)}");
                }

                // Hariç tutulacak kategoriler
                if (!string.IsNullOrWhiteSpace(filterParams.ExcludeCategoryIds))
                {
                    var excludedCategoryIds = filterParams.ExcludeCategoryIds
                        .Split(',')
                        .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : -1)
                        .Where(id => id > 0)
                        .ToList();
                    
                    if (excludedCategoryIds.Count > 0)
                    {
                        query = query.Where(p => !excludedCategoryIds.Contains(p.CategoryID));
                        _logger.LogInformation($"Excluded category IDs: {string.Join(", ", excludedCategoryIds)}");
                    }
                }

                // Maksimum fiyattan daha pahalı ürünleri hariç tut
                if (filterParams.ExcludeAbovePrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filterParams.ExcludeAbovePrice.Value);
                    _logger.LogInformation($"Excluded products above price: {filterParams.ExcludeAbovePrice}");
                }

                // Minimuma fiyattan daha ucuz ürünleri hariç tut
                if (filterParams.ExcludeBelowPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filterParams.ExcludeBelowPrice.Value);
                    _logger.LogInformation($"Excluded products below price: {filterParams.ExcludeBelowPrice}");
                }

                // 7. Sıralama
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

                // 8. Sayfalama
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
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Unknown",
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
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Unknown",
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

                return Ok(ApiResponse<string>.SuccessResponse(
                    "Deleted", 
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

        // GET: api/products/{id}/related
        // Aynı kategoriden benzer ürünleri getir (4 ürün - "Bunu da beğenebilirsiniz" için)
        [HttpGet("{id}/related")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetRelatedProducts(int id)
        {
            try
            {
                // Orijinal ürünü bul
                var product = await _context.Products
                    .Where(p => p.ProductID == id && p.IsActive)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(ApiResponse<List<ProductListDto>>.FailureResponse(
                        "Product not found"));
                }

                // Aynı kategoriden başka ürünleri bul (max 4) + rastgele seç
                var relatedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => 
                        p.CategoryID == product.CategoryID && 
                        p.ProductID != id && 
                        p.IsActive &&
                        p.Stock > 0) // Stokta olan ürünleri göster
                    .OrderBy(p => Guid.NewGuid()) // Rastgele sırala
                    .Take(4)
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

                _logger.LogInformation(
                    "Retrieved {Count} related products for product {ProductId} in category {CategoryId}",
                    relatedProducts.Count, id, product.CategoryID);

                return Ok(ApiResponse<List<ProductListDto>>.SuccessResponse(
                    relatedProducts,
                    $"Retrieved {relatedProducts.Count} related products"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving related products for product {ProductId}", id);
                return StatusCode(500, ApiResponse<List<ProductListDto>>.FailureResponse(
                    "An error occurred while retrieving related products"));
            }
        }

        /// <summary>
        /// Ürünün tüm spesifikasyonlarını getir
        /// </summary>
        [HttpGet("{id}/specifications")]
        public async Task<ActionResult<ApiResponse<List<ProductSpecificationDto>>>> GetProductSpecifications(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<List<ProductSpecificationDto>>.FailureResponse(
                        "Ürün bulunamadı"));
                }

                var specifications = await _context.ProductSpecifications
                    .Where(s => s.ProductID == id)
                    .Select(s => new ProductSpecificationDto
                    {
                        SpecID = s.SpecID,
                        ProductID = s.ProductID,
                        SpecName = s.SpecName,
                        SpecValue = s.SpecValue
                    })
                    .ToListAsync();

                _logger.LogInformation($"Ürün spesifikasyonları getirildi: ProductID={id}, Count={specifications.Count}");

                return Ok(ApiResponse<List<ProductSpecificationDto>>.SuccessResponse(
                    specifications,
                    $"Ürün spesifikasyonları başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün spesifikasyonları getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<List<ProductSpecificationDto>>.FailureResponse(
                    "Ürün spesifikasyonları getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Ürüne yeni spesifikasyon ekle (Admin Only)
        /// </summary>
        [HttpPost("{id}/specifications")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductSpecificationDto>>> AddProductSpecification(
            [FromRoute] int id,
            [FromBody] CreateProductSpecificationDto specDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ProductSpecificationDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<ProductSpecificationDto>.FailureResponse(
                        "Ürün bulunamadı"));
                }

                var specification = new ProductSpecification
                {
                    ProductID = id,
                    SpecName = specDto.SpecName,
                    SpecValue = specDto.SpecValue
                };

                _context.ProductSpecifications.Add(specification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Yeni spesifikasyon eklendi: ProductID={id}, SpecName={specDto.SpecName}");

                var resultDto = new ProductSpecificationDto
                {
                    SpecID = specification.SpecID,
                    ProductID = specification.ProductID,
                    SpecName = specification.SpecName,
                    SpecValue = specification.SpecValue
                };

                return CreatedAtAction(nameof(GetProductSpecifications), new { id = id },
                    ApiResponse<ProductSpecificationDto>.SuccessResponse(
                        resultDto,
                        "Spesifikasyon başarıyla eklendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Spesifikasyon eklenirken hata oluştu");
                return StatusCode(500, ApiResponse<ProductSpecificationDto>.FailureResponse(
                    "Spesifikasyon eklenirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Ürün spesifikasyonunu güncelle (Admin Only)
        /// </summary>
        [HttpPut("{id}/specifications/{specId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductSpecificationDto>>> UpdateProductSpecification(
            [FromRoute] int id,
            [FromRoute] int specId,
            [FromBody] UpdateProductSpecificationDto specDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<ProductSpecificationDto>.FailureResponse(
                        "Ürün bulunamadı"));
                }

                var specification = await _context.ProductSpecifications
                    .FirstOrDefaultAsync(s => s.SpecID == specId && s.ProductID == id);

                if (specification == null)
                {
                    return NotFound(ApiResponse<ProductSpecificationDto>.FailureResponse(
                        "Spesifikasyon bulunamadı"));
                }

                if (!string.IsNullOrWhiteSpace(specDto.SpecName))
                {
                    specification.SpecName = specDto.SpecName;
                }

                if (!string.IsNullOrWhiteSpace(specDto.SpecValue))
                {
                    specification.SpecValue = specDto.SpecValue;
                }

                _context.ProductSpecifications.Update(specification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Spesifikasyon güncellendi: SpecID={specId}, ProductID={id}");

                var resultDto = new ProductSpecificationDto
                {
                    SpecID = specification.SpecID,
                    ProductID = specification.ProductID,
                    SpecName = specification.SpecName,
                    SpecValue = specification.SpecValue
                };

                return Ok(ApiResponse<ProductSpecificationDto>.SuccessResponse(
                    resultDto,
                    "Spesifikasyon başarıyla güncellendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Spesifikasyon güncellenirken hata oluştu");
                return StatusCode(500, ApiResponse<ProductSpecificationDto>.FailureResponse(
                    "Spesifikasyon güncellenirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Ürün spesifikasyonunu sil (Admin Only)
        /// </summary>
        [HttpDelete("{id}/specifications/{specId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteProductSpecification(
            [FromRoute] int id,
            [FromRoute] int specId)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<string>.FailureResponse("Ürün bulunamadı"));
                }

                var specification = await _context.ProductSpecifications
                    .FirstOrDefaultAsync(s => s.SpecID == specId && s.ProductID == id);

                if (specification == null)
                {
                    return NotFound(ApiResponse<string>.FailureResponse("Spesifikasyon bulunamadı"));
                }

                _context.ProductSpecifications.Remove(specification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Spesifikasyon silindi: SpecID={specId}, ProductID={id}");

                return Ok(ApiResponse<string>.SuccessResponse(
                    "Silindi",
                    "Spesifikasyon başarıyla silindi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Spesifikasyon silinirken hata oluştu");
                return StatusCode(500, ApiResponse<string>.FailureResponse(
                    "Spesifikasyon silinirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Compare multiple products by their specifications
        /// </summary>
        [HttpPost("compare")]
        public async Task<ActionResult<ApiResponse<ProductComparisonResult>>> CompareProducts(
            [FromBody] CompareProductsDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ProductComparisonResult>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                if (dto.ProductIds == null || dto.ProductIds.Count < 2)
                {
                    return BadRequest(ApiResponse<ProductComparisonResult>.FailureResponse(
                        "At least 2 products are required for comparison"));
                }

                if (dto.ProductIds.Count > 5)
                {
                    return BadRequest(ApiResponse<ProductComparisonResult>.FailureResponse(
                        "Maximum 5 products can be compared at once"));
                }

                // Fetch products with specifications
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductSpecifications)
                    .Where(p => dto.ProductIds.Contains(p.ProductID) && p.IsActive)
                    .ToListAsync();

                if (products.Count != dto.ProductIds.Count)
                {
                    return NotFound(ApiResponse<ProductComparisonResult>.FailureResponse(
                        "One or more products not found or inactive"));
                }

                // Build comparison DTOs
                var comparisonProducts = products.Select(p => new ProductComparisonDto
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Brand = p.Brand,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    CategoryName = p.Category?.CategoryName ?? "Uncategorized",
                    Specifications = p.ProductSpecifications.ToDictionary(
                        ps => ps.SpecName,
                        ps => ps.SpecValue
                    )
                }).ToList();

                // Get all unique specification keys
                var allSpecKeys = products
                    .SelectMany(p => p.ProductSpecifications.Select(ps => ps.SpecName))
                    .Distinct()
                    .OrderBy(k => k)
                    .ToList();

                // Build comparison matrix
                var comparisonAttributes = new List<ComparisonAttribute>();

                // Add basic attributes
                comparisonAttributes.Add(new ComparisonAttribute
                {
                    AttributeName = "Brand",
                    ProductValues = products.ToDictionary(p => p.ProductID, p => p.Brand),
                    HasDifference = products.Select(p => p.Brand).Distinct().Count() > 1
                });

                comparisonAttributes.Add(new ComparisonAttribute
                {
                    AttributeName = "Price",
                    ProductValues = products.ToDictionary(p => p.ProductID, p => $"${p.Price:N2}"),
                    HasDifference = products.Select(p => p.Price).Distinct().Count() > 1
                });

                comparisonAttributes.Add(new ComparisonAttribute
                {
                    AttributeName = "Stock Status",
                    ProductValues = products.ToDictionary(p => p.ProductID, p => p.Stock > 0 ? "In Stock" : "Out of Stock"),
                    HasDifference = products.Select(p => p.Stock > 0).Distinct().Count() > 1
                });

                // Add specifications
                foreach (var specKey in allSpecKeys)
                {
                    var productValues = new Dictionary<int, string>();
                    var values = new List<string>();

                    foreach (var product in products)
                    {
                        var spec = product.ProductSpecifications.FirstOrDefault(ps => ps.SpecName == specKey);
                        var value = spec?.SpecValue ?? "N/A";
                        productValues[product.ProductID] = value;
                        values.Add(value);
                    }

                    comparisonAttributes.Add(new ComparisonAttribute
                    {
                        AttributeName = specKey,
                        ProductValues = productValues,
                        HasDifference = values.Where(v => v != "N/A").Distinct().Count() > 1
                    });
                }

                // Get common categories
                var categories = products
                    .Select(p => p.Category?.CategoryName)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();

                var result = new ProductComparisonResult
                {
                    Products = comparisonProducts,
                    Attributes = comparisonAttributes,
                    CommonCategories = categories!,
                    ComparisonSummary = $"Comparing {products.Count} products across {allSpecKeys.Count} specifications"
                };

                _logger.LogInformation($"Product comparison: {products.Count} products, {allSpecKeys.Count} specs");

                return Ok(ApiResponse<ProductComparisonResult>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing products");
                return StatusCode(500, ApiResponse<ProductComparisonResult>.FailureResponse(
                    "An error occurred while comparing products"));
            }
        }

        /// <summary>
        /// Get products in the same category for comparison suggestions
        /// </summary>
        [HttpGet("{id}/similar")]
        public async Task<ActionResult<ApiResponse<List<ComparisonListItemDto>>>> GetSimilarProducts(
            int id,
            [FromQuery] int limit = 5)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound(ApiResponse<List<ComparisonListItemDto>>.FailureResponse("Product not found"));
                }

                var similarProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryID == product.CategoryID 
                             && p.ProductID != id 
                             && p.IsActive)
                    .OrderBy(p => Math.Abs(p.Price - product.Price)) // Sort by price similarity
                    .Take(limit)
                    .Select(p => new ComparisonListItemDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                        AddedAt = DateTime.UtcNow
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<ComparisonListItemDto>>.SuccessResponse(similarProducts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching similar products");
                return StatusCode(500, ApiResponse<List<ComparisonListItemDto>>.FailureResponse(
                    "An error occurred while fetching similar products"));
            }
        }

        /// <summary>
        /// Get comparison-ready product details (minimal info for comparison list)
        /// </summary>
        [HttpGet("comparison-details")]
        public async Task<ActionResult<ApiResponse<List<ComparisonListItemDto>>>> GetComparisonDetails(
            [FromQuery] List<int> productIds)
        {
            try
            {
                if (productIds == null || !productIds.Any())
                {
                    return BadRequest(ApiResponse<List<ComparisonListItemDto>>.FailureResponse(
                        "Product IDs are required"));
                }

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => productIds.Contains(p.ProductID) && p.IsActive)
                    .Select(p => new ComparisonListItemDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                        AddedAt = DateTime.UtcNow
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<ComparisonListItemDto>>.SuccessResponse(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comparison details");
                return StatusCode(500, ApiResponse<List<ComparisonListItemDto>>.FailureResponse(
                    "An error occurred while fetching comparison details"));
            }
        }

        /// <summary>
        /// Get products with low stock (Admin only)
        /// </summary>
        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetLowStockProducts()
        {
            try
            {
                var lowStockProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Stock <= p.CriticalStockLevel && p.IsActive)
                    .OrderBy(p => p.Stock)
                    .Select(p => new ProductListDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Price = p.Price,
                        Stock = p.Stock,
                        CriticalStockLevel = p.CriticalStockLevel,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                        IsActive = p.IsActive,
                        AverageRating = p.ProductReviews.Any() ? p.ProductReviews.Average(r => r.Rating) : 0
                    })
                    .ToListAsync();

                _logger.LogInformation($"Low stock products fetched: Count={lowStockProducts.Count}");

                return Ok(ApiResponse<List<ProductListDto>>.SuccessResponse(
                    lowStockProducts,
                    $"{lowStockProducts.Count} ürün kritik stok seviyesinde veya altında"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching low stock products");
                return StatusCode(500, ApiResponse<List<ProductListDto>>.FailureResponse(
                    "Low stock products could not be fetched"));
            }
        }

        /// <summary>
        /// Update critical stock level for a product (Admin only)
        /// </summary>
        [HttpPut("{id}/critical-level")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ProductDetailDto>>> UpdateCriticalStockLevel(
            [FromRoute] int id,
            [FromBody] int criticalLevel)
        {
            try
            {
                if (criticalLevel < 0)
                {
                    return BadRequest(ApiResponse<ProductDetailDto>.FailureResponse(
                        "Critical stock level cannot be negative"));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ApiResponse<ProductDetailDto>.FailureResponse("Product not found"));
                }

                product.CriticalStockLevel = criticalLevel;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Critical stock level updated: ProductID={id}, NewLevel={criticalLevel}");

                var productDto = await _context.Products
                    .Where(p => p.ProductID == id)
                    .Include(p => p.Category)
                    .Include(p => p.ProductSpecifications)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductReviews)
                    .Select(p => new ProductDetailDto
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Brand = p.Brand,
                        Description = p.Description,
                        Price = p.Price,
                        Stock = p.Stock,
                        CriticalStockLevel = p.CriticalStockLevel,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        CategoryID = p.CategoryID,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized",
                        CreatedAt = p.CreatedAt,
                        AverageRating = p.ProductReviews.Any() ? p.ProductReviews.Average(r => r.Rating) : 0,
                        ReviewCount = p.ProductReviews.Count,
                        Specifications = p.ProductSpecifications.Select(ps => new ProductSpecificationDto
                        {
                            SpecID = ps.SpecID,
                            ProductID = ps.ProductID,
                            SpecName = ps.SpecName,
                            SpecValue = ps.SpecValue
                        }).ToList(),
                        Images = p.ProductImages.Select(pi => new ProductImageDto
                        {
                            ImageID = pi.ImageID,
                            ImageUrl = pi.ImageUrl,
                            IsMainImage = pi.IsMainImage
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                return Ok(ApiResponse<ProductDetailDto>.SuccessResponse(
                    productDto!,
                    "Critical stock level updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating critical stock level");
                return StatusCode(500, ApiResponse<ProductDetailDto>.FailureResponse(
                    "An error occurred while updating critical stock level"));
            }
        }
    }
}