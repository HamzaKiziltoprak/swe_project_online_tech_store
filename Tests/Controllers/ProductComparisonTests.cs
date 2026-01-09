using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    /// <summary>
    /// Tests for Product Comparison feature
    /// Note: This feature is not yet implemented in the frontend, so some tests may fail.
    /// </summary>
    public class ProductComparisonTests
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly ProductsController _controller;

        public ProductComparisonTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            _mockLogger = new Mock<ILogger<ProductsController>>();
            _controller = new ProductsController(_context, _mockLogger.Object);
        }

        private async Task SeedTestData()
        {
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            var brand1 = new Brand { BrandID = 1, BrandName = "Apple" };
            var brand2 = new Brand { BrandID = 2, BrandName = "Samsung" };

            _context.Categories.Add(category);
            _context.Brands.AddRange(brand1, brand2);
            await _context.SaveChangesAsync();

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "iPhone 15",
                BrandID = 1,
                Description = "Apple smartphone",
                Price = 999.99m,
                Stock = 50,
                ImageUrl = "iphone15.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Galaxy S24",
                BrandID = 2,
                Description = "Samsung smartphone",
                Price = 899.99m,
                Stock = 30,
                ImageUrl = "galaxys24.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var product3 = new Product
            {
                ProductID = 3,
                ProductName = "iPhone 14",
                BrandID = 1,
                Description = "Apple smartphone (older model)",
                Price = 799.99m,
                Stock = 20,
                ImageUrl = "iphone14.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.AddRange(product1, product2, product3);
            await _context.SaveChangesAsync();

            // Add specifications
            var specs = new List<ProductSpecification>
            {
                new ProductSpecification { SpecID = 1, ProductID = 1, SpecName = "Screen Size", SpecValue = "6.1 inch" },
                new ProductSpecification { SpecID = 2, ProductID = 1, SpecName = "RAM", SpecValue = "8GB" },
                new ProductSpecification { SpecID = 3, ProductID = 1, SpecName = "Storage", SpecValue = "256GB" },
                new ProductSpecification { SpecID = 4, ProductID = 2, SpecName = "Screen Size", SpecValue = "6.2 inch" },
                new ProductSpecification { SpecID = 5, ProductID = 2, SpecName = "RAM", SpecValue = "12GB" },
                new ProductSpecification { SpecID = 6, ProductID = 2, SpecName = "Storage", SpecValue = "256GB" },
                new ProductSpecification { SpecID = 7, ProductID = 3, SpecName = "Screen Size", SpecValue = "6.1 inch" },
                new ProductSpecification { SpecID = 8, ProductID = 3, SpecName = "RAM", SpecValue = "6GB" },
                new ProductSpecification { SpecID = 9, ProductID = 3, SpecName = "Storage", SpecValue = "128GB" }
            };

            _context.ProductSpecifications.AddRange(specs);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnComparison_WhenTwoProductsProvided()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data!.Products.Count);
            Assert.Contains(apiResponse.Data.Attributes, a => a.AttributeName == "Brand");
            Assert.Contains(apiResponse.Data.Attributes, a => a.AttributeName == "Price");
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnBadRequest_WhenLessThanTwoProducts()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("At least 2 products are required", apiResponse.Message);
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnBadRequest_WhenMoreThanFiveProducts()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2, 3, 4, 5, 6 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Maximum 5 products", apiResponse.Message);
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 999 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.False(apiResponse.Success);
        }

        [Fact]
        public async Task CompareProducts_ShouldShowSpecificationDifferences()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2, 3 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            
            // RAM should have differences (8GB, 12GB, 6GB)
            var ramAttribute = apiResponse.Data!.Attributes.FirstOrDefault(a => a.AttributeName == "RAM");
            Assert.NotNull(ramAttribute);
            Assert.True(ramAttribute!.HasDifference);

            // Screen Size should not have major differences (6.1, 6.2, 6.1)
            var screenAttribute = apiResponse.Data.Attributes.FirstOrDefault(a => a.AttributeName == "Screen Size");
            Assert.NotNull(screenAttribute);
        }

        [Fact]
        public async Task CompareProducts_ShouldIncludeAllSpecifications()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.True(apiResponse.Success);

            // Should include Screen Size, RAM, Storage specifications
            Assert.Contains(apiResponse.Data!.Attributes, a => a.AttributeName == "Screen Size");
            Assert.Contains(apiResponse.Data.Attributes, a => a.AttributeName == "RAM");
            Assert.Contains(apiResponse.Data.Attributes, a => a.AttributeName == "Storage");
        }

        [Fact]
        public async Task CompareProducts_ShouldExcludeInactiveProducts()
        {
            // Arrange
            await SeedTestData();
            
            // Deactivate product 2
            var product2 = await _context.Products.FindAsync(2);
            product2!.IsActive = false;
            await _context.SaveChangesAsync();

            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("not found or inactive", apiResponse.Message);
        }

        [Fact]
        public async Task CompareProducts_ShouldReturnComparisonSummary()
        {
            // Arrange
            await SeedTestData();
            var dto = new CompareProductsDto { ProductIds = new List<int> { 1, 2 } };

            // Act
            var result = await _controller.CompareProducts(dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResult>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data!.ComparisonSummary);
            Assert.Contains("Comparing", apiResponse.Data.ComparisonSummary);
        }
    }
}
