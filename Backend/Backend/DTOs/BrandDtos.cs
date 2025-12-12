using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Brand list item DTO
    /// </summary>
    public class BrandDto
    {
        public int BrandID { get; set; }
        public string BrandName { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }

    /// <summary>
    /// Create brand DTO
    /// </summary>
    public class CreateBrandDto
    {
        [Required(ErrorMessage = "Brand name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Brand name must be between 2 and 100 characters")]
        public string BrandName { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "Logo URL cannot exceed 200 characters")]
        public string? LogoUrl { get; set; }
    }

    /// <summary>
    /// Update brand DTO
    /// </summary>
    public class UpdateBrandDto
    {
        [Required(ErrorMessage = "Brand name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Brand name must be between 2 and 100 characters")]
        public string BrandName { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "Logo URL cannot exceed 200 characters")]
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Brand with products DTO
    /// </summary>
    public class BrandDetailDto
    {
        public int BrandID { get; set; }
        public string BrandName { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductCount { get; set; }
        public List<ProductListDto>? Products { get; set; }
    }
}
