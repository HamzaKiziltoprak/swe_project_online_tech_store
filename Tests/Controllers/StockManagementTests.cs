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
    public class StockManagementTests
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly ProductsController _controller;

        public StockManagementTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            _mockLogger = new Mock<ILogger<ProductsController>>();

            _controller = new ProductsController(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task GetLowStockProducts_ShouldReturnProductsBelowCriticalLevel()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var lowStockProduct1 = new Product
            {
                ProductID = 1,
                ProductName = "Low Stock Product 1",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 5,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var lowStockProduct2 = new Product
            {
                ProductID = 2,
                ProductName = "Low Stock Product 2",
                Brand = "Brand B",
                Description = "Description",
                Price = 200,
                Stock = 8,
                CriticalStockLevel = 15,
                ImageUrl = "test2.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var normalStockProduct = new Product
            {
                ProductID = 3,
                ProductName = "Normal Stock Product",
                Brand = "Brand C",
                Description = "Description",
                Price = 150,
                Stock = 50,
                CriticalStockLevel = 10,
                ImageUrl = "test3.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.AddRange(lowStockProduct1, lowStockProduct2, normalStockProduct);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLowStockProducts();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ProductListDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count); // Only 2 products with low stock
            Assert.All(apiResponse.Data, p => Assert.True(p.Stock <= p.CriticalStockLevel));
        }

        [Fact]
        public async Task GetLowStockProducts_ShouldReturnEmptyList_WhenNoLowStockProducts()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var normalStockProduct = new Product
            {
                ProductID = 1,
                ProductName = "Normal Stock Product",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 50,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.Add(normalStockProduct);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLowStockProducts();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ProductListDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Empty(apiResponse.Data);
        }

        [Fact]
        public async Task GetLowStockProducts_ShouldOrderByStock()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Product with 8 stock",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 8,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Product with 3 stock",
                Brand = "Brand B",
                Description = "Description",
                Price = 200,
                Stock = 3,
                CriticalStockLevel = 10,
                ImageUrl = "test2.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLowStockProducts();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ProductListDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count);
            Assert.Equal(3, apiResponse.Data[0].Stock); // First product should have lowest stock
            Assert.Equal(8, apiResponse.Data[1].Stock);
        }

        [Fact]
        public async Task UpdateCriticalStockLevel_ShouldUpdateSuccessfully()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var product = new Product
            {
                ProductID = 1,
                ProductName = "Test Product",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 50,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateCriticalStockLevel(1, 20);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDetailDto>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(20, apiResponse.Data.CriticalStockLevel);

            // Verify in database
            var dbProduct = await _context.Products.FindAsync(1);
            Assert.Equal(20, dbProduct!.CriticalStockLevel);
        }

        [Fact]
        public async Task UpdateCriticalStockLevel_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            // Act
            var result = await _controller.UpdateCriticalStockLevel(999, 20);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDetailDto>>(actionResult.Value);
            Assert.False(apiResponse.Success);
        }

        [Fact]
        public async Task UpdateCriticalStockLevel_ShouldReturnBadRequest_WhenNegativeValue()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var product = new Product
            {
                ProductID = 1,
                ProductName = "Test Product",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 50,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateCriticalStockLevel(1, -5);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDetailDto>>(actionResult.Value);
            Assert.False(apiResponse.Success);
        }

        [Fact]
        public async Task GetLowStockProducts_ShouldExcludeInactiveProducts()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var activeProduct = new Product
            {
                ProductID = 1,
                ProductName = "Active Low Stock",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 5,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var inactiveProduct = new Product
            {
                ProductID = 2,
                ProductName = "Inactive Low Stock",
                Brand = "Brand B",
                Description = "Description",
                Price = 200,
                Stock = 3,
                CriticalStockLevel = 10,
                ImageUrl = "test2.jpg",
                CategoryID = 1,
                IsActive = false
            };

            _context.Products.AddRange(activeProduct, inactiveProduct);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLowStockProducts();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ProductListDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Single(apiResponse.Data); // Only active product
            Assert.True(apiResponse.Data[0].IsActive);
        }
    }
}
