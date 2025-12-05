namespace Backend.DTOs
{
    // CategoryDto - Kategorileri listelerken kullanılan DTO
    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = null!;
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // CategoryDetailDto - Kategori detayları ve alt kategoriler
    public class CategoryDetailDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = null!;
        public int? ParentCategoryID { get; set; }
        public string? ParentCategoryName { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryDto> SubCategories { get; set; } = new List<CategoryDto>();
        public DateTime CreatedAt { get; set; }
    }

    // CreateCategoryDto - Yeni kategori oluşturma DTO
    public class CreateCategoryDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Kategori adı gereklidir")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2, 
            ErrorMessage = "Kategori adı 2-100 karakter arasında olmalıdır")]
        public string CategoryName { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, 
            ErrorMessage = "Geçerli bir ana kategori seçiniz")]
        public int? ParentCategoryID { get; set; }
    }

    // UpdateCategoryDto - Kategori güncelleme DTO
    public class UpdateCategoryDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Kategori adı gereklidir")]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2,
            ErrorMessage = "Kategori adı 2-100 karakter arasında olmalıdır")]
        public string CategoryName { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue,
            ErrorMessage = "Geçerli bir ana kategori seçiniz")]
        public int? ParentCategoryID { get; set; }
    }
}
