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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class FavoritesControllerTests
    {
        private readonly DataContext _context; // Testlerde kullanılacak InMemory veritabanı
        private readonly Mock<ILogger<FavoritesController>> _mockLogger; // Controller için sahte logger
        private readonly FavoritesController _controller; // Test edilecek controller

        public FavoritesControllerTests()
        {
            // InMemory veritabanını kurarak testlerin birbirinden izole çalışmasını sağlıyoruz.
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            // Logger mock oluşturuluyor.
            _mockLogger = new Mock<ILogger<FavoritesController>>();

            // Controller örneği oluşturuluyor.
            _controller = new FavoritesController(_context, _mockLogger.Object);
        }

        // Kullanıcının favori listesi varsa doğru şekilde dönüp dönmediğini test eden senaryo.
        [Fact]
        public async Task GetMyFavorites_ShouldReturnFavorites_WhenUserHasFavorites()
        {
            var userId = 777; // Test için kullanıcı ID'si

            // Test kullanıcısının kimlik bilgilerini HTTP context'e ekliyoruz.
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

            // Veritabanına ürün ekliyoruz.
            var product1 = new Product { ProductID = 88, ProductName = "P1", Price = 10, Stock = 1, Brand = "B", Description = "D", ImageUrl = "I", IsActive = true };
            var product2 = new Product { ProductID = 99, ProductName = "P2", Price = 20, Stock = 1, Brand = "B", Description = "D", ImageUrl = "I", IsActive = true };

            _context.Products.Add(product1);
            _context.Products.Add(product2);

            // Kullanıcıya ait favori kayıtlarını ekliyoruz.
            var fav1 = new Favorite { FavoriteID = 101, UserID = userId, ProductID = 88, Product = product1, CreatedAt = DateTime.UtcNow };
            var fav2 = new Favorite { FavoriteID = 102, UserID = userId, ProductID = 99, Product = product2, CreatedAt = DateTime.UtcNow };

            _context.Favorites.Add(fav1);
            _context.Favorites.Add(fav2);
            await _context.SaveChangesAsync();

            // ChangeTracker temizlenerek gerçek bir istek davranışı simüle ediliyor.
            _context.ChangeTracker.Clear();

            // Controller metodunu çalıştırıyoruz.
            var result = await _controller.GetMyFavorites(pageNumber: 1, pageSize: 10);

            // Dönen cevabın OK olup olmadığını kontrol ediyoruz.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedFavoriteResult>>(actionResult.Value);

            // Favori sayısının doğru dönmesini bekliyoruz.
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.TotalCount);
        }

        // Kullanıcının favorisi yoksa boş liste dönmesini test eden senaryo.
        [Fact]
        public async Task GetMyFavorites_ShouldReturnEmptyList_WhenUserHasNoFavorites()
        {
            var userId = 2;
            SetupHttpContextWithUser(userId);

            var result = await _controller.GetMyFavorites();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedFavoriteResult>>(actionResult.Value);

            Assert.Equal(0, apiResponse.Data.TotalCount);
            Assert.Empty(apiResponse.Data.Data);
        }

        // Favori yoksa ekleme işleminin doğru çalışıp çalışmadığını test ediyor.
        [Fact]
        public async Task AddRemoveFavorite_ShouldAddFavorite_WhenItDoesNotExist()
        {
            var userId = 3;
            SetupHttpContextWithUser(userId);
            var productId = 30;

            // Ürün ekleniyor.
            _context.Products.Add(new Product { ProductID = productId, ProductName = "Laptop", Price = 2000, Stock = 5, Brand = "Dell", Description = "Desc", ImageUrl = "img.jpg", IsActive = true });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var result = await _controller.AddRemoveFavorite(productId);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<FavoriteActionDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Ürün favorilere eklendi", apiResponse.Message);

            // Veritabanına gerçekten eklenip eklenmediğini kontrol ediyoruz.
            var favInDb = await _context.Favorites.FirstOrDefaultAsync(f => f.UserID == userId && f.ProductID == productId);
            Assert.NotNull(favInDb);
        }

        // Favori varsa silinme işleminin doğru çalıştığını test eder.
        [Fact]
        public async Task AddRemoveFavorite_ShouldRemoveFavorite_WhenItAlreadyExists()
        {
            var userId = 4;
            SetupHttpContextWithUser(userId);
            var productId = 40;

            _context.Products.Add(new Product { ProductID = productId, ProductName = "Tablet", Price = 500, Stock = 10, Brand = "Samsung", Description = "Desc", ImageUrl = "img.jpg", IsActive = true });
            _context.Favorites.Add(new Favorite { UserID = userId, ProductID = productId });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var result = await _controller.AddRemoveFavorite(productId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<FavoriteActionDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Ürün favorilerden çıkarıldı", apiResponse.Message);

            // Favori kaydının silindiğini kontrol ediyoruz.
            var favInDb = await _context.Favorites.FirstOrDefaultAsync(f => f.UserID == userId && f.ProductID == productId);
            Assert.Null(favInDb);
        }

        // Ürün bulunamazsa doğru hata mesajının dönüp dönmediğini test eder.
        [Fact]
        public async Task AddRemoveFavorite_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            var userId = 5;
            SetupHttpContextWithUser(userId);
            var productId = 999;

            var result = await _controller.AddRemoveFavorite(productId);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<FavoriteActionDto>>(actionResult.Value);

            Assert.Equal("Ürün bulunamadı", apiResponse.Message);
        }

        // Ürün favoriye ekli ise true dönmeli.
        [Fact]
        public async Task IsFavorite_ShouldReturnTrue_WhenProductIsFavorited()
        {
            var userId = 6;
            SetupHttpContextWithUser(userId);
            var productId = 60;

            _context.Favorites.Add(new Favorite { UserID = userId, ProductID = productId });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var result = await _controller.IsFavorite(productId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<IsFavoriteDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.True(apiResponse.Data.IsFavorite);
        }

        // Ürün favoride değilse false dönmeli.
        [Fact]
        public async Task IsFavorite_ShouldReturnFalse_WhenProductIsNotFavorited()
        {
            var userId = 7;
            SetupHttpContextWithUser(userId);
            var productId = 70;

            var result = await _controller.IsFavorite(productId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<IsFavoriteDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.False(apiResponse.Data.IsFavorite);
        }

        // Testlerde tekrar eden kullanıcı bağlama işlemini kolaylaştırmak için yardımcı metot.
        private void SetupHttpContextWithUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }
    }
}
