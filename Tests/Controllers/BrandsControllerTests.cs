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
    public class BrandsControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<BrandsController>> _mockLogger;
        private readonly BrandsController _controller;

        public BrandsControllerTests()
        {
            // 1. InMemory DB Setup
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);
            _mockLogger = new Mock<ILogger<BrandsController>>();

            // 2. Seed Data
            SeedDatabase();

            // 3. Controller Setup
            _controller = new BrandsController(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            // Brands
            var brands = new List<Brand>
            {
                new Brand { BrandID = 1, BrandName = "Apple", Description = "Tech Giant", IsActive = true },
                new Brand { BrandID = 2, BrandName = "Samsung", Description = "Electronics", IsActive = true },
                new Brand { BrandID = 3, BrandName = "Nokia", Description = "Old phones", IsActive = false }
            };

            // Products associated with brands
            var products = new List<Product>
            {
                new Product 
                { 
                    ProductID = 1, ProductName = "iPhone", BrandID = 1, IsActive = true, Stock = 10, Price = 1000, 
                    Description = "Smartphone", ImageUrl = "img1.jpg"
                },
                new Product 
                { 
                    ProductID = 2, ProductName = "Galaxy", BrandID = 2, IsActive = true, Stock = 5, Price = 900, 
                    Description = "Android Phone", ImageUrl = "img2.jpg"
                },
                // Inactive product for Samsung
                new Product 
                { 
                    ProductID = 3, ProductName = "Old Galaxy", BrandID = 2, IsActive = false, Stock = 0, Price = 500, 
                    Description = "Old Android", ImageUrl = "img3.jpg"
                }
            };

            _context.Brands.AddRange(brands);
            _context.Products.AddRange(products);
            _context.SaveChanges();
        }

        #region GetBrands Tests

        [Fact]
        public async Task GetBrands_ShouldReturnAllBrands_WhenNoFilterApplied()
        {
            // Act
            var result = await _controller.GetBrands(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<BrandDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal(3, response.Data!.Count); // Apple, Samsung, Nokia
        }

        [Fact]
        public async Task GetBrands_ShouldReturnOnlyActiveBrands_WhenIsActiveTrue()
        {
            // Act
            var result = await _controller.GetBrands(true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<BrandDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal(2, response.Data!.Count); // Apple, Samsung
            Assert.DoesNotContain(response.Data, b => b.BrandName == "Nokia");
        }

        [Fact]
        public async Task GetBrands_ShouldReturnCorrectProductCounts()
        {
            // Act
            var result = await _controller.GetBrands(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<BrandDto>>>(okResult.Value);
            var brands = response!.Data!;

            var samsung = brands.First(b => b.BrandName == "Samsung");
            
            // Samsung has 2 products total, but only 1 is active.
            // Controller counts only active products: b.Products.Count(p => p.IsActive)
            Assert.Equal(1, samsung.ProductCount);
        }

        #endregion

        #region GetBrand Tests

        [Fact]
        public async Task GetBrand_ShouldReturnBrandDetail_WhenBrandExists()
        {
            // Act
            var result = await _controller.GetBrand(1); // Apple

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<BrandDetailDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Products);
            
            Assert.Equal("Apple", response.Data!.BrandName);
            Assert.Single(response.Data.Products); // 1 active product (iPhone)
        }

        [Fact]
        public async Task GetBrand_ShouldReturnNotFound_WhenBrandDoesNotExist()
        {
            // Act
            var result = await _controller.GetBrand(99);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<BrandDetailDto>>(notFoundResult.Value);
            
            Assert.False(response!.Success);
            Assert.Equal("Brand not found", response.Message);
        }

        #endregion

        #region CreateBrand Tests

        [Fact]
        public async Task CreateBrand_ShouldReturnCreated_WhenValid()
        {
            // Arrange
            var dto = new CreateBrandDto 
            { 
                BrandName = "Xiaomi", 
                Description = "New Brand", 
                LogoUrl = "logo.png" 
            };

            // Act
            var result = await _controller.CreateBrand(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<BrandDto>>(createdResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("Xiaomi", response.Data!.BrandName);
            
            // Verify DB
            var dbBrand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandName == "Xiaomi");
            Assert.NotNull(dbBrand);
        }

        [Fact]
        public async Task CreateBrand_ShouldReturnBadRequest_WhenBrandNameExists()
        {
            // Arrange
            var dto = new CreateBrandDto 
            { 
                BrandName = "Apple", // Already exists
                Description = "Duplicate" 
            };

            // Act
            var result = await _controller.CreateBrand(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<BrandDto>>(badRequestResult.Value);
            
            Assert.False(response!.Success);
            Assert.Contains("already exists", response.Message);
        }

        #endregion

        #region UpdateBrand Tests

        [Fact]
        public async Task UpdateBrand_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            // Arrange
            var dto = new UpdateBrandDto 
            { 
                BrandName = "Apple Inc.", 
                Description = "Updated Desc", 
                IsActive = true 
            };

            // Act
            var result = await _controller.UpdateBrand(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<BrandDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("Apple Inc.", response.Data!.BrandName);

            // Verify DB
            var dbBrand = await _context.Brands.FindAsync(1);
            Assert.Equal("Apple Inc.", dbBrand!.BrandName);
        }

        [Fact]
        public async Task UpdateBrand_ShouldReturnBadRequest_WhenNewNameConflicts()
        {
            // Arrange: Try to rename "Apple" (ID 1) to "Samsung" (ID 2)
            var dto = new UpdateBrandDto 
            { 
                BrandName = "Samsung", 
                Description = "Updated Desc", 
                IsActive = true 
            };

            // Act
            var result = await _controller.UpdateBrand(1, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<BrandDto>>(badRequestResult.Value);
            
            Assert.Contains("already exists", response!.Message);
        }

        [Fact]
        public async Task UpdateBrand_ShouldReturnNotFound_WhenBrandDoesNotExist()
        {
            // Arrange
            var dto = new UpdateBrandDto { BrandName = "Ghost" };

            // Act
            var result = await _controller.UpdateBrand(99, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region DeleteBrand Tests

        [Fact]
        public async Task DeleteBrand_ShouldReturnOk_WhenBrandHasNoActiveProducts()
        {
            // Arrange
            // Nokia (ID 3) has no products in seed data
            var brandId = 3; 

            // Act
            var result = await _controller.DeleteBrand(brandId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(response!.Success);
            
            // Verify Soft Delete
            var dbBrand = await _context.Brands.FindAsync(brandId);
            Assert.False(dbBrand!.IsActive);
        }

        [Fact]
        public async Task DeleteBrand_ShouldReturnBadRequest_WhenBrandHasActiveProducts()
        {
            // Arrange
            // Apple (ID 1) has 1 active product
            var brandId = 1;

            // Act
            var result = await _controller.DeleteBrand(brandId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(response!.Success);
            Assert.Contains("active products", response.Message);
        }

        [Fact]
        public async Task DeleteBrand_ShouldReturnNotFound_WhenBrandDoesNotExist()
        {
            // Act
            var result = await _controller.DeleteBrand(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region GetBrandsWithCounts Tests

        [Fact]
        public async Task GetBrandsWithCounts_ShouldReturnOnlyActiveBrandsWithProducts()
        {
            // Act
            var result = await _controller.GetBrandsWithCounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<BrandDto>>>(okResult.Value);
            var list = response!.Data!;

            // Apple: Active, Has 1 active product -> Should be returned
            // Samsung: Active, Has 1 active product -> Should be returned
            // Nokia: Inactive -> Should NOT be returned
            
            Assert.Equal(2, list.Count);
            Assert.Contains(list, b => b.BrandName == "Apple");
            Assert.Contains(list, b => b.BrandName == "Samsung");
            Assert.DoesNotContain(list, b => b.BrandName == "Nokia");
        }

        #endregion
    }
}