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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class CartControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<CartController>> _mockLogger;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            // 1. InMemory Database Setup
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(options);

            // 2. Mocks
            _mockLogger = new Mock<ILogger<CartController>>();
            
            // UserManager Mock
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // 3. Controller Init
            _controller = new CartController(_context, _mockUserManager.Object, _mockLogger.Object);

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
            var product1 = new Product 
            { 
                ProductID = 1, 
                ProductName = "Laptop", 
                Price = 1000, 
                Stock = 10, 
                IsActive = true, 
                ImageUrl = "img1.jpg",
                Description = "Powerful Laptop"
            };
            
            var product2 = new Product 
            { 
                ProductID = 2, 
                ProductName = "Mouse", 
                Price = 50, 
                Stock = 5, 
                IsActive = true, 
                ImageUrl = "img2.jpg",
                Description = "Wireless Mouse" 
            };
            
            _context.Products.AddRange(product1, product2);

            // User 1 için sepet
            _context.CartItems.Add(new CartItem { CartItemID = 1, UserID = 1, ProductID = 1, Product = product1, Count = 2 }); // 2 Laptop (2000)

            // User 2 için sepet (User 1 görmemeli)
            _context.CartItems.Add(new CartItem { CartItemID = 2, UserID = 2, ProductID = 2, Product = product2, Count = 1 });

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

        #region GetCart Tests

        [Fact]
        public async Task GetCart_ShouldReturnCorrectItemsAndTotals_ForUser1()
        {
            // Arrange
            SetupHttpContextWithUser(1);

            // Act
            var result = await _controller.GetCart();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CartSummaryDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            var cart = response.Data!;

            Assert.Single(cart.Items); 
            Assert.Equal(2, cart.TotalItems); 
            Assert.Equal(2000, cart.TotalPrice); 
            Assert.Equal("Laptop", cart.Items[0].ProductName);
        }

        [Fact]
        public async Task GetCart_ShouldReturnEmpty_ForUserWithNoItems()
        {
            // Arrange
            SetupHttpContextWithUser(99); 

            // Act
            var result = await _controller.GetCart();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CartSummaryDto>>(okResult.Value);
            
            Assert.Empty(response!.Data!.Items);
            Assert.Equal(0, response.Data.TotalPrice);
        }

        #endregion

        #region AddToCart Tests

        [Fact]
        public async Task AddToCart_ShouldAddNewItem_WhenItemDoesNotExist()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new AddToCartDto { ProductID = 2, Count = 1 }; // Mouse ekle

            // Act
            var result = await _controller.AddToCart(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            
            // DÜZELTME: Controller CreatedAtAction içinde direkt DTO dönüyor, ApiResponse wrapper kullanmıyor.
            var response = Assert.IsType<CartItemDto>(createdResult.Value);
            
            Assert.Equal("Mouse", response.ProductName);
            Assert.Equal(1, response.Count);
            
            // DB kontrolü
            var dbItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserID == 1 && c.ProductID == 2);
            Assert.NotNull(dbItem);
            Assert.Equal(1, dbItem!.Count);
        }

        [Fact]
        public async Task AddToCart_ShouldIncreaseQuantity_WhenItemAlreadyExists()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new AddToCartDto { ProductID = 1, Count = 1 }; 

            // Act
            var result = await _controller.AddToCart(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            
            // DÜZELTME: Controller direkt DTO dönüyor
            var response = Assert.IsType<CartItemDto>(createdResult.Value);
            
            Assert.Equal(3, response.Count); // 2 + 1 = 3

            // DB kontrolü
            var dbItem = await _context.CartItems.FirstAsync(c => c.CartItemID == 1);
            Assert.Equal(3, dbItem.Count);
        }

        [Fact]
        public async Task AddToCart_ShouldReturnBadRequest_WhenStockInsufficient()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new AddToCartDto { ProductID = 1, Count = 9 }; 

            // Act
            var result = await _controller.AddToCart(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CartItemDto>>(badRequestResult.Value);
            
            Assert.False(response!.Success);
            Assert.Contains("Not enough stock", response.Message);
        }

        [Fact]
        public async Task AddToCart_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new AddToCartDto { ProductID = 999, Count = 1 };

            // Act
            var result = await _controller.AddToCart(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region UpdateCartItem Tests

        [Fact]
        public async Task UpdateCartItem_ShouldUpdateCount_WhenStockIsSufficient()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new UpdateCartItemDto { Count = 5 };

            // Act
            var result = await _controller.UpdateCartItem(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CartItemDto>>(okResult.Value);
            
            Assert.Equal(5, response!.Data!.Count);
            
            // DB Check
            var dbItem = await _context.CartItems.FindAsync(1);
            Assert.Equal(5, dbItem!.Count);
        }

        [Fact]
        public async Task UpdateCartItem_ShouldReturnBadRequest_WhenStockInsufficient()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new UpdateCartItemDto { Count = 15 };

            // Act
            var result = await _controller.UpdateCartItem(1, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CartItemDto>>(badRequestResult.Value);
            
            Assert.Contains("Not enough stock", response!.Message);
        }

        [Fact]
        public async Task UpdateCartItem_ShouldReturnNotFound_WhenItemBelongsToAnotherUser()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new UpdateCartItemDto { Count = 1 };

            // Act
            var result = await _controller.UpdateCartItem(2, dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CartItemDto>>(notFoundResult.Value);
            Assert.Equal("Cart item not found", response!.Message);
        }

        #endregion

        #region RemoveFromCart Tests

        [Fact]
        public async Task RemoveFromCart_ShouldRemoveItem_WhenItemExists()
        {
            // Arrange
            SetupHttpContextWithUser(1);

            // Act
            var result = await _controller.RemoveFromCart(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            
            Assert.True(response!.Success);

            // DB Check
            var item = await _context.CartItems.FindAsync(1);
            Assert.Null(item);
        }

        [Fact]
        public async Task RemoveFromCart_ShouldReturnNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            SetupHttpContextWithUser(1);

            // Act
            var result = await _controller.RemoveFromCart(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region ClearCart Tests

        [Fact]
        public async Task ClearCart_ShouldRemoveAllItemsForUser()
        {
            // Arrange
            SetupHttpContextWithUser(1); 

            // Act
            var result = await _controller.ClearCart();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response!.Success);

            // DB Check: User 1'in sepeti boş olmalı
            var user1Items = await _context.CartItems.Where(c => c.UserID == 1).ToListAsync();
            Assert.Empty(user1Items);

            // DB Check: User 2'nin sepeti durmalı
            var user2Items = await _context.CartItems.Where(c => c.UserID == 2).ToListAsync();
            Assert.Single(user2Items);
        }

        [Fact]
        public async Task ClearCart_ShouldReturnOk_WhenCartAlreadyEmpty()
        {
            // Arrange
            SetupHttpContextWithUser(99); 

            // Act
            var result = await _controller.ClearCart();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.Contains("already empty", response!.Message);
        }

        #endregion
    }
}