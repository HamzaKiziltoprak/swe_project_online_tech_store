using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
    public class CartControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<CartController>> _mockLogger;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            // Bellekte çalışan test veritabanı oluşturuyoruz.
            // Her test çalıştığında yeni ve temiz bir DB oluşacak.
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(options);

            // UserManager için standart mock oluşturuluyor.
            // Sadece kimlik kontrolü gereken noktalarda iş görüyor.
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Controller loglaması için mock logger hazırlanıyor.
            _mockLogger = new Mock<ILogger<CartController>>();

            // Test edilecek controller örneği oluşturuluyor.
            _controller = new CartController(
                _context,
                _mockUserManager.Object,
                _mockLogger.Object
            );
        }

        // Kullanıcının sepetinde ürün varsa özet bilgilerin doğru dönmesini test ediyor.
        [Fact]
        public async Task GetCart_ShouldReturnCartSummary_WhenCartIsNotEmpty()
        {
            // Kullanıcıyı teste tanıtıyoruz.
            var userId = 1;
            SetupHttpContextWithUser(userId);

            // Ürün verisini test veritabanına ekliyoruz.
            var product = new Product { ProductID = 1, ProductName = "Laptop", Price = 1000, Stock = 10, BrandID=1, Description="Desc", ImageUrl="img.jpg" };
            _context.Products.Add(product);

            // Kullanıcının sepetine ürün ekliyoruz.
            _context.CartItems.Add(new CartItem { CartItemID = 1, UserID = userId, ProductID = 1, Count = 2 });
            await _context.SaveChangesAsync();

            // Controller çağrılıyor.
            var result = await _controller.GetCart();

            // Dönen cevabı kontrol ediyoruz.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CartSummaryDto>>(actionResult.Value);
            var summary = apiResponse.Data;

            // Hesaplamaların doğru olduğunu doğruluyoruz.
            Assert.True(apiResponse.Success);
            Assert.Equal(2, summary.TotalItems);
            Assert.Equal(2000, summary.TotalPrice);
            Assert.Single(summary.Items);
        }

        // Sepet boşsa boş özet dönülmesini test eder.
        [Fact]
        public async Task GetCart_ShouldReturnEmptySummary_WhenCartIsEmpty()
        {
            var userId = 2;
            SetupHttpContextWithUser(userId);

            var result = await _controller.GetCart();
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CartSummaryDto>>(actionResult.Value);

            Assert.Equal(0, apiResponse.Data.TotalItems);
            Assert.Empty(apiResponse.Data.Items);
        }

        // Sepete ilk defa ürün eklendiğinde başarılı şekilde Created dönmesini test eder.
        [Fact]
        public async Task AddToCart_ShouldReturnCreated_WhenItemIsAddedNew()
        {
            var userId = 3;
            SetupHttpContextWithUser(userId);

            // Sepete eklenecek ürün
            var product = new Product { ProductID = 10, ProductName = "Mouse", Price = 50, Stock = 100, BrandID=1, Description="Desc", ImageUrl="img.jpg" };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // İstek nesnesi
            var request = new AddToCartDto { ProductID = 10, Count = 1 };

            var result = await _controller.AddToCart(request);

            // Dönen cevabı kontrol ediyoruz.
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var cartItem = Assert.IsType<CartItemDto>(actionResult.Value);

            // Ürün bilgileri doğru dönmüş mü kontrolü
            Assert.Equal("Mouse", cartItem.ProductName);

            // Veritabanına gerçekten yazılmış mı kontrolü
            var dbItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == 10);
            Assert.NotNull(dbItem);
            Assert.Equal(1, dbItem.Count);
        }

        // Sepette zaten aynı ürün varsa miktarın güncellenmesini test eder.
        [Fact]
        public async Task AddToCart_ShouldUpdateQuantity_WhenItemAlreadyExists()
        {
            var userId = 4;
            SetupHttpContextWithUser(userId);

            var product = new Product { ProductID = 20, ProductName = "Keyboard", Price = 100, Stock = 50, BrandID=1, Description="Desc", ImageUrl="img.jpg" };
            _context.Products.Add(product);

            // Kullanıcının sepetinde ürün zaten var.
            _context.CartItems.Add(new CartItem { UserID = userId, ProductID = 20, Count = 1 });
            await _context.SaveChangesAsync();

            var request = new AddToCartDto { ProductID = 20, Count = 2 };

            var result = await _controller.AddToCart(request);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var cartItem = Assert.IsType<CartItemDto>(actionResult.Value);

            // Miktarın doğru şekilde güncellendiğini kontrol ediyoruz.
            Assert.Equal(3, cartItem.Count);
        }

        // Stok yetmezse BadRequest dönmesini test eder.
        [Fact]
        public async Task AddToCart_ShouldReturnBadRequest_WhenStockIsInsufficient()
        {
            var userId = 5;
            SetupHttpContextWithUser(userId);

            var product = new Product { ProductID = 30, ProductName = "Rare Item", Price = 500, Stock = 5, BrandID=1, Description="Desc", ImageUrl="img.jpg" };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var request = new AddToCartDto { ProductID = 30, Count = 10 };

            var result = await _controller.AddToCart(request);
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CartItemDto>>(actionResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Contains("Not enough stock", apiResponse.Message);
        }

        // Ürün yoksa NotFound dönmeli.
        [Fact]
        public async Task AddToCart_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            var userId = 6;
            SetupHttpContextWithUser(userId);
            var request = new AddToCartDto { ProductID = 999, Count = 1 };

            var result = await _controller.AddToCart(request);
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // Sepet ürününün miktarını güncellerken başarılı güncellenmesini test eder.
        [Fact]
        public async Task UpdateCartItem_ShouldReturnOk_WhenUpdateIsValid()
        {
            var userId = 7;
            SetupHttpContextWithUser(userId);

            var product = new Product { ProductID = 40, ProductName = "Monitor", Price = 200, Stock = 20, BrandID=1, Description="Desc", ImageUrl="img.jpg" };
            _context.Products.Add(product);

            var item = new CartItem { CartItemID = 100, UserID = userId, ProductID = 40, Count = 1 };
            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();

            var request = new UpdateCartItemDto { Count = 5 };

            var result = await _controller.UpdateCartItem(100, request);
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CartItemDto>>(actionResult.Value);

            Assert.Equal(5, apiResponse.Data.Count);
        }

        // Kullanıcı kendine ait olmayan ürünü güncelleyememeli.
        [Fact]
        public async Task UpdateCartItem_ShouldReturnNotFound_WhenItemDoesNotBelongToUser()
        {
            var userId = 8;
            var otherUserId = 9;
            SetupHttpContextWithUser(userId);

            var product = new Product { ProductID = 50, ProductName = "Webcam", Price = 50, Stock = 10, BrandID=1, Description="Desc", ImageUrl="img.jpg" };
            _context.Products.Add(product);

            _context.CartItems.Add(new CartItem { CartItemID = 200, UserID = otherUserId, ProductID = 50, Count = 1 });
            await _context.SaveChangesAsync();

            var request = new UpdateCartItemDto { Count = 2 };

            var result = await _controller.UpdateCartItem(200, request);
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // Sepetten ürün silme işleminin başarılı gerçekleşmesini test eder.
        [Fact]
        public async Task RemoveFromCart_ShouldReturnOk_WhenItemExists()
        {
            var userId = 10;
            SetupHttpContextWithUser(userId);

            _context.CartItems.Add(new CartItem { CartItemID = 300, UserID = userId, Count = 1, ProductID = 1 });
            await _context.SaveChangesAsync();

            var result = await _controller.RemoveFromCart(300);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(actionResult.Value);

            Assert.True(apiResponse.Success);

            // Ürünün gerçekten silindiğini kontrol ediyoruz.
            var dbItem = await _context.CartItems.FindAsync(300);
            Assert.Null(dbItem);
        }

        // Kullanıcının tüm sepetinin temizlenmesini test eder.
        [Fact]
        public async Task ClearCart_ShouldRemoveAllItemsForUser()
        {
            var userId = 11;
            SetupHttpContextWithUser(userId);

            _context.CartItems.Add(new CartItem { CartItemID = 401, UserID = userId, Count = 1, ProductID=1 });
            _context.CartItems.Add(new CartItem { CartItemID = 402, UserID = userId, Count = 2, ProductID=2 });
            _context.CartItems.Add(new CartItem { CartItemID = 403, UserID = 99, Count = 1, ProductID=1 });
            await _context.SaveChangesAsync();

            var result = await _controller.ClearCart();
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);

            // Sadece giriş yapan kullanıcıya ait ürünler silinmeli.
            var userItems = await _context.CartItems.Where(c => c.UserID == userId).ToListAsync();
            Assert.Empty(userItems);

            var otherUserItems = await _context.CartItems.Where(c => c.UserID == 99).ToListAsync();
            Assert.Single(otherUserItems);
        }

        // Controller içinde kullanıcı kimliğini taklit etmek için kullanılan yardımcı fonksiyon.
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
