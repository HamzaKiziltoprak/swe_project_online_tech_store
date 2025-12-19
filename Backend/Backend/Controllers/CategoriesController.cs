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
    public class CategoriesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(DataContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/categories
        // Tüm kategorileri hiyerarşik yapı ile getir
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryDetailDto>>>> GetCategories()
        {
            try
            {
                // Sadece ana kategorileri getir (ParentCategoryID = null)
                var categories = await _context.Categories
                    .Where(c => c.ParentCategoryID == null)
                    .Include(c => c.SubCategories)
                    .ThenInclude(c => c.Products)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = new List<CategoryDetailDto>();

                foreach (var category in categories)
                {
                    var categoryDto = new CategoryDetailDto
                    {
                        CategoryID = category.CategoryID,
                        CategoryName = category.CategoryName,
                        ParentCategoryID = null,
                        ParentCategoryName = "",
                        ProductCount = category.Products.Count,
                        CreatedAt = DateTime.Now,
                        SubCategories = category.SubCategories.Select(sc => new CategoryDto
                        {
                            CategoryID = sc.CategoryID,
                            CategoryName = sc.CategoryName,
                            ParentCategoryID = sc.ParentCategoryID,
                            ParentCategoryName = category.CategoryName,
                            ProductCount = sc.Products.Count,
                            CreatedAt = DateTime.Now
                        }).OrderBy(x => x.CategoryName).ToList()
                    };

                    categoryDtos.Add(categoryDto);
                }

                _logger.LogInformation($"Retrieved {categoryDtos.Count} categories");

                return Ok(ApiResponse<List<CategoryDetailDto>>.SuccessResponse(
                    categoryDtos,
                    $"Retrieved {categoryDtos.Count} categories"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, ApiResponse<List<CategoryDetailDto>>.FailureResponse(
                    "Kategoriler getirilirken bir hata oluştu"
                ));
            }
        }

        // GET: api/categories/{id}
        // Belirli bir kategorinin detaylarını getir
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> GetCategoryById(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .ThenInclude(c => c.Products)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound(ApiResponse<CategoryDetailDto>.FailureResponse(
                        $"ID {id} ile kategori bulunamadı"
                    ));
                }

                var categoryDto = new CategoryDetailDto
                {
                    CategoryID = category.CategoryID,
                    CategoryName = category.CategoryName,
                    ParentCategoryID = category.ParentCategoryID,
                    ParentCategoryName = category.ParentCategory?.CategoryName ?? "",
                    ProductCount = category.Products.Count,
                    CreatedAt = DateTime.Now,
                    SubCategories = category.SubCategories.Select(sc => new CategoryDto
                    {
                        CategoryID = sc.CategoryID,
                        CategoryName = sc.CategoryName,
                        ParentCategoryID = sc.ParentCategoryID,
                        ParentCategoryName = category.CategoryName,
                        ProductCount = sc.Products.Count,
                        CreatedAt = DateTime.Now
                    }).OrderBy(x => x.CategoryName).ToList()
                };

                _logger.LogInformation($"Retrieved category with ID {id}");

                return Ok(ApiResponse<CategoryDetailDto>.SuccessResponse(
                    categoryDto,
                    "Kategori başarıyla getirildi"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving category with ID {id}");
                return StatusCode(500, ApiResponse<CategoryDetailDto>.FailureResponse(
                    "Kategori getirilirken bir hata oluştu"
                ));
            }
        }

        // GET: api/categories/parent/{parentId}
        // Belirli bir ana kategoriye ait alt kategorileri getir
        [HttpGet("parent/{parentId}")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetSubCategories(int parentId)
        {
            try
            {
                // Ana kategorinin var olduğunu kontrol et
                var parentCategory = await _context.Categories.FindAsync(parentId);
                if (parentCategory == null)
                {
                    return NotFound(ApiResponse<List<CategoryDto>>.FailureResponse(
                        $"ID {parentId} ile ana kategori bulunamadı"
                    ));
                }

                var subCategories = await _context.Categories
                    .Where(c => c.ParentCategoryID == parentId)
                    .Include(c => c.Products)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var subCategoryDtos = subCategories.Select(sc => new CategoryDto
                {
                    CategoryID = sc.CategoryID,
                    CategoryName = sc.CategoryName,
                    ParentCategoryID = sc.ParentCategoryID,
                    ParentCategoryName = parentCategory.CategoryName,
                    ProductCount = sc.Products.Count,
                    CreatedAt = DateTime.Now
                }).ToList();

                _logger.LogInformation($"Retrieved {subCategoryDtos.Count} sub-categories for parent {parentId}");

                return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(
                    subCategoryDtos,
                    $"Retrieved {subCategoryDtos.Count} sub-categories"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sub-categories for parent {parentId}");
                return StatusCode(500, ApiResponse<List<CategoryDto>>.FailureResponse(
                    "Alt kategoriler getirilirken bir hata oluştu"
                ));
            }
        }

        // POST: api/categories
        // Yeni kategori oluştur (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                    "Validasyon başarısız",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            try
            {
                // Aynı isimde kategori var mı kontrol et
                var existingCategory = await _context.Categories
                    .AnyAsync(c => c.CategoryName.ToLower() == createCategoryDto.CategoryName.ToLower());

                if (existingCategory)
                {
                    return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                        "Bu isimde kategori zaten mevcut"
                    ));
                }

                // Eğer ParentCategoryID varsa, kontrol et
                if (createCategoryDto.ParentCategoryID.HasValue && createCategoryDto.ParentCategoryID.Value > 0)
                {
                    var parentCategoryExists = await _context.Categories
                        .AnyAsync(c => c.CategoryID == createCategoryDto.ParentCategoryID.Value);

                    if (!parentCategoryExists)
                    {
                        return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                            $"ID {createCategoryDto.ParentCategoryID} ile ana kategori bulunamadı"
                        ));
                    }
                }

                var category = new Category
                {
                    CategoryName = createCategoryDto.CategoryName,
                    ParentCategoryID = createCategoryDto.ParentCategoryID.HasValue && 
                        createCategoryDto.ParentCategoryID.Value > 0 
                        ? createCategoryDto.ParentCategoryID.Value 
                        : null
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var parentCategory = category.ParentCategoryID.HasValue
                    ? await _context.Categories.FindAsync(category.ParentCategoryID.Value)
                    : null;

                var categoryDto = new CategoryDetailDto
                {
                    CategoryID = category.CategoryID,
                    CategoryName = category.CategoryName,
                    ParentCategoryID = category.ParentCategoryID,
                    ParentCategoryName = parentCategory?.CategoryName ?? "",
                    ProductCount = 0,
                    SubCategories = new List<CategoryDto>(),
                    CreatedAt = DateTime.Now
                };

                _logger.LogInformation($"Category created successfully with ID {category.CategoryID}");

                return CreatedAtAction(
                    nameof(GetCategoryById),
                    new { id = category.CategoryID },
                    ApiResponse<CategoryDetailDto>.SuccessResponse(
                        categoryDto,
                        "Kategori başarıyla oluşturuldu"
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, ApiResponse<CategoryDetailDto>.FailureResponse(
                    "Kategori oluşturulurken bir hata oluştu"
                ));
            }
        }

        // PUT: api/categories/{id}
        // Kategori güncelle (Admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateCategoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                    "Validasyon başarısız",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound(ApiResponse<CategoryDetailDto>.FailureResponse(
                        $"ID {id} ile kategori bulunamadı"
                    ));
                }

                // Aynı isimde başka kategori var mı kontrol et
                var duplicateCategory = await _context.Categories
                    .AnyAsync(c => c.CategoryName.ToLower() == updateCategoryDto.CategoryName.ToLower() 
                        && c.CategoryID != id);

                if (duplicateCategory)
                {
                    return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                        "Bu isimde başka kategori zaten mevcut"
                    ));
                }

                // Eğer ParentCategoryID varsa, kontrol et
                if (updateCategoryDto.ParentCategoryID.HasValue && updateCategoryDto.ParentCategoryID.Value > 0)
                {
                    // Kendisini ana kategori yapmaya çalışıp çalışmadığını kontrol et
                    if (updateCategoryDto.ParentCategoryID.Value == id)
                    {
                        return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                            "Bir kategori kendisinin ana kategorisi olamaz"
                        ));
                    }

                    var parentCategoryExists = await _context.Categories
                        .AnyAsync(c => c.CategoryID == updateCategoryDto.ParentCategoryID.Value);

                    if (!parentCategoryExists)
                    {
                        return BadRequest(ApiResponse<CategoryDetailDto>.FailureResponse(
                            $"ID {updateCategoryDto.ParentCategoryID} ile ana kategori bulunamadı"
                        ));
                    }
                }

                category.CategoryName = updateCategoryDto.CategoryName;
                category.ParentCategoryID = updateCategoryDto.ParentCategoryID.HasValue && 
                    updateCategoryDto.ParentCategoryID.Value > 0 
                    ? updateCategoryDto.ParentCategoryID.Value 
                    : null;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                var parentCategory = category.ParentCategoryID.HasValue
                    ? await _context.Categories.FindAsync(category.ParentCategoryID.Value)
                    : null;

                var categoryDto = new CategoryDetailDto
                {
                    CategoryID = category.CategoryID,
                    CategoryName = category.CategoryName,
                    ParentCategoryID = category.ParentCategoryID,
                    ParentCategoryName = parentCategory?.CategoryName ?? "",
                    ProductCount = 0,
                    SubCategories = category.SubCategories.Select(sc => new CategoryDto
                    {
                        CategoryID = sc.CategoryID,
                        CategoryName = sc.CategoryName,
                        ParentCategoryID = sc.ParentCategoryID,
                        ParentCategoryName = category.CategoryName,
                        ProductCount = 0,
                        CreatedAt = DateTime.Now
                    }).ToList(),
                    CreatedAt = DateTime.Now
                };

                _logger.LogInformation($"Category with ID {id} updated successfully");

                return Ok(ApiResponse<CategoryDetailDto>.SuccessResponse(
                    categoryDto,
                    "Kategori başarıyla güncellendi"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category with ID {id}");
                return StatusCode(500, ApiResponse<CategoryDetailDto>.FailureResponse(
                    "Kategori güncellenirken bir hata oluştu"
                ));
            }
        }

        // DELETE: api/categories/{id}
        // Kategori sil (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse(
                        $"ID {id} ile kategori bulunamadı"
                    ));
                }

                // Kategoriye ait ürün var mı kontrol et
                if (category.Products.Any())
                {
                    return BadRequest(ApiResponse<object>.FailureResponse(
                        $"Bu kategori {category.Products.Count} ürüne sahip olduğu için silinemez. " +
                        "Lütfen önce bu ürünleri başka kategoriye taşıyın veya silin."
                    ));
                }

                // Alt kategorileri kontrol et
                if (category.SubCategories.Any())
                {
                    return BadRequest(ApiResponse<object>.FailureResponse(
                        $"Bu kategori {category.SubCategories.Count} alt kategoriye sahip olduğu için silinemez. " +
                        "Lütfen önce alt kategorileri silin."
                    ));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogWarning($"Category with ID {id} deleted successfully");

                return Ok(ApiResponse<object>.SuccessResponse(
                    new object(),
                    "Kategori başarıyla silindi"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category with ID {id}");
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    "Kategori silinirken bir hata oluştu"
                ));
            }
        }

        // DELETE: api/categories/{id}/permanent
        // ⚠️ DANGEROUS: Kategorisi ve tüm ilişkileri sil (Hard Delete - Sadece Admin)
        // ⚠️ Bu işlem GERİ ALINAMAZ! Kategori, alt kategoriler ve ürünler silinir.
        // ⚠️ Soft delete (normal DELETE) kullanımı ŞİDDETLE önerilir.
        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> PermanentDeleteCategory(int id)
        {
            try
            {
                _logger.LogWarning("⚠️ PERMANENT DELETE ATTEMPT: Category ID {CategoryId} - This will delete all subcategories and products!", id);
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .ThenInclude(c => c.Products)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse(
                        $"ID {id} ile kategori bulunamadı"
                    ));
                }

                // Alt kategoriler varsa ve ürünleri varsa, onları da sil
                if (category.SubCategories.Any())
                {
                    foreach (var subCategory in category.SubCategories)
                    {
                        if (subCategory.Products.Any())
                        {
                            _context.Products.RemoveRange(subCategory.Products);
                        }
                        _context.Categories.Remove(subCategory);
                    }
                }

                // Ana kategoriyi sil
                if (category.Products.Any())
                {
                    _context.Products.RemoveRange(category.Products);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogWarning($"Category with ID {id} and all its relationships permanently deleted");

                return Ok(ApiResponse<object>.SuccessResponse(
                    new object(),
                    "Kategori ve tüm ilişkileri kalıcı olarak silindi"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error permanently deleting category with ID {id}");
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    "Kategori kalıcı olarak silinirken bir hata oluştu"
                ));
            }
        }

        // GET: api/categories/search/{searchTerm}
        // Kategori arama
        [HttpGet("search/{searchTerm}")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> SearchCategories(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(ApiResponse<List<CategoryDto>>.FailureResponse(
                        "Arama terimi boş olamaz"
                    ));
                }

                var categories = await _context.Categories
                    .Where(c => c.CategoryName.ToLower().Contains(searchTerm.ToLower()))
                    .Include(c => c.ParentCategory)
                    .Include(c => c.Products)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = categories.Select(c => new CategoryDto
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    ParentCategoryID = c.ParentCategoryID,
                    ParentCategoryName = c.ParentCategory?.CategoryName ?? "",
                    ProductCount = c.Products.Count,
                    CreatedAt = DateTime.Now
                }).ToList();

                _logger.LogInformation($"Search for categories with term '{searchTerm}' returned {categoryDtos.Count} results");

                return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(
                    categoryDtos,
                    $"'{searchTerm}' için {categoryDtos.Count} kategori bulundu"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching categories with term {searchTerm}");
                return StatusCode(500, ApiResponse<List<CategoryDto>>.FailureResponse(
                    "Kategori aranırken bir hata oluştu"
                ));
            }
        }
    }
}
