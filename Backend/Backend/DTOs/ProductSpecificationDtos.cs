namespace Backend.DTOs
{
    /// <summary>
    /// Ürün özelliği/spesifikasyonu
    /// </summary>
    public class ProductSpecificationDto
    {
        public int SpecID { get; set; }
        public int ProductID { get; set; }
        public string SpecName { get; set; } = null!;
        public string SpecValue { get; set; } = null!;
    }

    /// <summary>
    /// Yeni spesifikasyon oluşturma
    /// </summary>
    public class CreateProductSpecificationDto
    {
        public string SpecName { get; set; } = null!;
        public string SpecValue { get; set; } = null!;
    }

    /// <summary>
    /// Spesifikasyon güncelleme
    /// </summary>
    public class UpdateProductSpecificationDto
    {
        public string? SpecName { get; set; }
        public string? SpecValue { get; set; }
    }
}
