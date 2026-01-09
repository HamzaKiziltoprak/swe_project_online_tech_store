using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class FavoritesControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<FavoritesController>> _mockLogger;
        private readonly FavoritesController _controller;

        public FavoritesControllerTests()
        {
            // 1. InMemory DB Setup
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(options);

            // 2. Mocks
            _mockLogger = new Mock<ILogger<FavoritesController>>();

            // 3. Controller Init
            _controller = new FavoritesController(_context, _mockLogger.Object);

            // 4. Seed Data
            SeedDatabase();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            var brand = new Brand { BrandID = 1, BrandName = "BrandX" };
            var category = new Category { CategoryID = 1, CategoryName = "CatX" };

            var products = new List<Product>
            {
                new Product { ProductID = 1, ProductName = "P1", Price = 100, Stock = 10, IsActive = true, BrandID = 1, Brand = brand, CategoryID = 1, Category = category, ImageUrl = "img1.jpg", Description = "Desc1" },
                new Product { ProductID = 2, ProductName = "P2", Price = 200, Stock = 5, IsActive = true, BrandID = 1, Brand = brand, CategoryID = 1, Category = category, ImageUrl = "img2.jpg", Description = "Desc2" }
            };

            // User 1 favorileri
            var fav1 = new Favorite { FavoriteID = 1, UserID = 1, ProductID = 1, Product = products[0], CreatedAt = DateTime.UtcNow };
            
            _context.Brands.Add(brand);
            _context.Categories.Add(category);
            _context.Products.AddRange(products);
            _context.Favorites.Add(fav1);
            
            _context.SaveChanges();
        }

        private void SetupHttpContextWithUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region GetMyFavorites Tests

        [Fact]
        public async Task GetMyFavorites_ShouldReturnPagedResults_ForUser()
        {
            // Arrange
            SetupHttpContextWithUser(1); // User 1'in 1 favorisi var

            // Act
            var result = await _controller.GetMyFavorites(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<PagedFavoriteResult>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal(1, response.Data!.TotalCount);
            Assert.Single(response.Data.Data);
            Assert.Equal("P1", response.Data.Data[0].ProductName);
        }

        [Fact]
        public async Task GetMyFavorites_ShouldReturnEmpty_ForUserWithNoFavorites()
        {
            // Arrange
            SetupHttpContextWithUser(99); // Favorisi olmayan kullanıcı

            // Act
            var result = await _controller.GetMyFavorites(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<PagedFavoriteResult>>(okResult.Value);
            
            Assert.Empty(response!.Data!.Data);
            Assert.Equal(0, response.Data.TotalCount);
        }

        #endregion

        #region AddRemoveFavorite Tests

        [Fact]
        public async Task AddRemoveFavorite_ShouldAddFavorite_WhenNotExists()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var productId = 2; // P2 favoride değil

            // Act
            var result = await _controller.AddRemoveFavorite(productId);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<FavoriteActionDto>>(createdResult.Value);
            
            Assert.True(response!.Success);
            Assert.Contains("eklendi", response.Message); // "Ürün favorilere eklendi"

            // DB Check
            var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserID == 1 && f.ProductID == productId);
            Assert.NotNull(fav);
        }

        [Fact]
        public async Task AddRemoveFavorite_ShouldRemoveFavorite_WhenExists()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var productId = 1; // P1 zaten favoride

            // Act
            var result = await _controller.AddRemoveFavorite(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<FavoriteActionDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Contains("çıkarıldı", response.Message); // "Ürün favorilerden çıkarıldı"

            // DB Check
            var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserID == 1 && f.ProductID == productId);
            Assert.Null(fav);
        }

        [Fact]
        public async Task AddRemoveFavorite_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var productId = 999; 

            // Act
            var result = await _controller.AddRemoveFavorite(productId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<FavoriteActionDto>>(notFoundResult.Value);
            Assert.Equal("Ürün bulunamadı", response!.Message);
        }

        #endregion

        #region IsFavorite Tests

        [Fact]
        public async Task IsFavorite_ShouldReturnTrue_WhenProductIsFavorited()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var productId = 1; // Favoride

            // Act
            var result = await _controller.IsFavorite(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IsFavoriteDto>>(okResult.Value);
            
            Assert.True(response!.Data!.IsFavorite);
        }

        [Fact]
        public async Task IsFavorite_ShouldReturnFalse_WhenProductIsNotFavorited()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var productId = 2; // Favoride değil

            // Act
            var result = await _controller.IsFavorite(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IsFavoriteDto>>(okResult.Value);
            
            Assert.False(response!.Data!.IsFavorite);
        }

        #endregion
    }
}