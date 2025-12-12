using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Controllers
{
    public class ProductComparisonTests
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly ProductsController _controller;

        public ProductComparisonTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);
            _mockLogger = new Mock<ILogger<ProductsController>>();
            _controller = new ProductsController(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnComparisonMatrix_WhenProductsExist()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Laptops" };
            _context.Categories.Add(category);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Dell XPS 15",
                Brand = "Dell",
                Price = 1500m,
                Stock = 10,
                ImageUrl = "dell.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "High-end laptop"
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "HP Spectre",
                Brand = "HP",
                Price = 1400m,
                Stock = 5,
                ImageUrl = "hp.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Premium laptop"
            };

            _context.Products.AddRange(product1, product2);

            // Add specifications
            _context.ProductSpecifications.AddRange(
                new ProductSpecification { SpecID = 1, ProductID = 1, SpecName = "RAM", SpecValue = "16GB" },
                new ProductSpecification { SpecID = 2, ProductID = 1, SpecName = "CPU", SpecValue = "Intel i7" },
                new ProductSpecification { SpecID = 3, ProductID = 2, SpecName = "RAM", SpecValue = "32GB" },
                new ProductSpecification { SpecID = 4, ProductID = 2, SpecName = "CPU", SpecValue = "Intel i9" }
            );

            await _context.SaveChangesAsync();

            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ProductComparisonResult>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Products.Count);
            Assert.NotEmpty(response.Data.Attributes);

            // Verify brand comparison
            var brandAttribute = response.Data.Attributes.FirstOrDefault(a => a.AttributeName == "Brand");
            Assert.NotNull(brandAttribute);
            Assert.True(brandAttribute!.HasDifference);
            Assert.Equal("Dell", brandAttribute.ProductValues[1]);
            Assert.Equal("HP", brandAttribute.ProductValues[2]);

            // Verify RAM comparison
            var ramAttribute = response.Data.Attributes.FirstOrDefault(a => a.AttributeName == "RAM");
            Assert.NotNull(ramAttribute);
            Assert.True(ramAttribute!.HasDifference);
            Assert.Equal("16GB", ramAttribute.ProductValues[1]);
            Assert.Equal("32GB", ramAttribute.ProductValues[2]);
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnError_WhenLessThan2Products()
        {
            // Arrange
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ProductComparisonResult>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("At least 2 products", response.Message);
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnError_WhenMoreThan5Products()
        {
            // Arrange
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2, 3, 4, 5, 6 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ProductComparisonResult>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Maximum 5 products", response.Message);
        }

        [Fact]
        public async Task CompareProducts_ShouldHandleMissingSpecifications()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Product 1",
                Brand = "Brand A",
                Price = 100m,
                Stock = 10,
                ImageUrl = "p1.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Product 1"
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Product 2",
                Brand = "Brand B",
                Price = 200m,
                Stock = 5,
                ImageUrl = "p2.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Product 2"
            };

            _context.Products.AddRange(product1, product2);

            // Only product1 has RAM specification
            _context.ProductSpecifications.Add(
                new ProductSpecification { SpecID = 1, ProductID = 1, SpecName = "RAM", SpecValue = "8GB" }
            );

            await _context.SaveChangesAsync();

            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ProductComparisonResult>>(okResult.Value);
            Assert.True(response.Success);

            var ramAttribute = response.Data!.Attributes.FirstOrDefault(a => a.AttributeName == "RAM");
            Assert.NotNull(ramAttribute);
            Assert.Equal("8GB", ramAttribute!.ProductValues[1]);
            Assert.Equal("N/A", ramAttribute.ProductValues[2]);
        }

        [Fact]
        public async Task GetSimilarProducts_ShouldReturnProductsInSameCategory()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Monitors" };
            _context.Categories.Add(category);

            var mainProduct = new Product
            {
                ProductID = 1,
                ProductName = "Monitor A",
                Brand = "Samsung",
                Price = 300m,
                Stock = 10,
                ImageUrl = "monitor.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Main monitor"
            };

            var similar1 = new Product
            {
                ProductID = 2,
                ProductName = "Monitor B",
                Brand = "LG",
                Price = 320m,
                Stock = 5,
                ImageUrl = "monitor2.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Similar monitor"
            };

            var similar2 = new Product
            {
                ProductID = 3,
                ProductName = "Monitor C",
                Brand = "Dell",
                Price = 280m,
                Stock = 8,
                ImageUrl = "monitor3.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Similar monitor 2"
            };

            _context.Products.AddRange(mainProduct, similar1, similar2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSimilarProducts(1, 5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<ComparisonListItemDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
            Assert.DoesNotContain(response.Data, p => p.ProductID == 1); // Main product not included
        }

        [Fact]
        public async Task GetSimilarProducts_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            // Act
            var result = await _controller.GetSimilarProducts(999, 5);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<ComparisonListItemDto>>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task GetComparisonDetails_ShouldReturnProductDetails()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Keyboards" };
            _context.Categories.Add(category);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Keyboard 1",
                Brand = "Logitech",
                Price = 50m,
                Stock = 20,
                ImageUrl = "kb1.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Mechanical keyboard"
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Keyboard 2",
                Brand = "Corsair",
                Price = 80m,
                Stock = 15,
                ImageUrl = "kb2.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "RGB keyboard"
            };

            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetComparisonDetails(new List<int> { 1, 2 });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<ComparisonListItemDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
            Assert.Contains(response.Data, p => p.ProductName == "Keyboard 1");
            Assert.Contains(response.Data, p => p.ProductName == "Keyboard 2");
        }

        [Fact]
        public async Task CompareProducts_ShouldIdentifyDifferences()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Graphics Cards" };
            _context.Categories.Add(category);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "RTX 3060",
                Brand = "NVIDIA",
                Price = 400m,
                Stock = 5,
                ImageUrl = "rtx3060.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Graphics card"
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "RTX 3070",
                Brand = "NVIDIA", // Same brand
                Price = 600m, // Different price
                Stock = 3,
                ImageUrl = "rtx3070.jpg",
                IsActive = true,
                CategoryID = 1,
                Description = "Graphics card"
            };

            _context.Products.AddRange(product1, product2);

            _context.ProductSpecifications.AddRange(
                new ProductSpecification { SpecID = 1, ProductID = 1, SpecName = "VRAM", SpecValue = "12GB" },
                new ProductSpecification { SpecID = 2, ProductID = 2, SpecName = "VRAM", SpecValue = "8GB" }
            );

            await _context.SaveChangesAsync();

            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ProductComparisonResult>>(okResult.Value);

            var brandAttribute = response.Data!.Attributes.FirstOrDefault(a => a.AttributeName == "Brand");
            Assert.False(brandAttribute!.HasDifference); // Both NVIDIA

            var priceAttribute = response.Data.Attributes.FirstOrDefault(a => a.AttributeName == "Price");
            Assert.True(priceAttribute!.HasDifference); // Different prices

            var vramAttribute = response.Data.Attributes.FirstOrDefault(a => a.AttributeName == "VRAM");
            Assert.True(vramAttribute!.HasDifference); // Different VRAM
        }
    }
}
