using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers;
using Backend.Data;
using Backend.Models;
using Backend.DTOs; // PaymentResponse burada
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Tests.Controllers
{
    public class OrdersControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<OrdersController>> _mockLogger;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);

            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockLogger = new Mock<ILogger<OrdersController>>();
            _mockPaymentService = new Mock<IPaymentService>();

            _controller = new OrdersController(_context, _mockUserManager.Object, _mockLogger.Object, _mockPaymentService.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void MockUser(string userId, string email, string role = "Customer")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(new User 
                { 
                    Id = int.Parse(userId), 
                    Email = email, 
                    UserName = email,
                    FirstName = "Test",
                    LastName = "User"
                });
        }

        private async Task SeedData()
        {
            var user = new User 
            { 
                Id = 1, 
                Email = "test@test.com", 
                UserName = "testuser",
                FirstName = "Test",
                LastName = "User"
            };

            var product = new Product 
            { 
                ProductID = 1, 
                ProductName = "Laptop", 
                Price = 1000, 
                Stock = 10, 
                IsActive = true,
                Description = "Test Description", 
                ImageUrl = "test-image.jpg"
            };

            var cartItem = new CartItem { CartItemID = 1, UserID = 1, ProductID = 1, Count = 2, Product = product };

            _context.Users.Add(user);
            _context.Products.Add(product);
            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateOrder_ShouldCreateOrder_WhenCartIsValid()
        {
            // Arrange
            await SeedData();
            MockUser("1", "test@test.com");
            var dto = new CreateOrderDto { ShippingAddress = "Test Address" };

            // Act
            var result = await _controller.CreateOrder(dto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            
            var order = await _context.Orders.FirstOrDefaultAsync();
            Assert.NotNull(order);
            Assert.Equal(2000, order.TotalAmount);
            Assert.Equal("Pending", order.Status);
            
            var product = await _context.Products.FindAsync(1);
            Assert.NotNull(product);
            Assert.Equal(8, product.Stock);
            
            Assert.Empty(_context.CartItems);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnBadRequest_WhenCartIsEmpty()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            MockUser("1", "test@test.com");
            var dto = new CreateOrderDto { ShippingAddress = "Test Address" };

            // Act
            var result = await _controller.CreateOrder(dto);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Contains("Sepetiniz boş", apiResponse.Message);
        }

        [Fact]
        public async Task GetMyOrders_ShouldReturnOrders_ForLoggedUser()
        {
            // Arrange
            await SeedData();
            MockUser("1", "test@test.com");

            var order = new Order 
            { 
                OrderID = 1, 
                UserID = 1, 
                TotalAmount = 500, 
                Status = "Pending", 
                OrderDate = DateTime.UtcNow, 
                ShippingAddress = "Test Address"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedOrderResult>>(actionResult.Value);
            
            Assert.NotNull(apiResponse);
            Assert.NotNull(apiResponse.Data);
            Assert.Single(apiResponse.Data.Orders);
            Assert.Equal(1, apiResponse.Data.Orders[0].OrderID);
        }

        [Fact]
        public async Task CancelOrder_ShouldCancel_WhenStatusIsPending()
        {
            // Arrange
            await SeedData(); 
            MockUser("1", "test@test.com");

            var order = new Order 
            { 
                OrderID = 1, 
                UserID = 1, 
                TotalAmount = 100, 
                Status = "Pending", 
                ShippingAddress = "Test Address",
                OrderItems = new List<OrderItem> 
                { 
                    new OrderItem { ProductID = 1, Quantity = 1, UnitPrice = 100 } 
                } 
            };
            _context.Orders.Add(order);
            
            var product = await _context.Products.FindAsync(1);
            Assert.NotNull(product);
            product.Stock = 9; 
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CancelOrder(1);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            
            var dbOrder = await _context.Orders.FindAsync(1);
            Assert.NotNull(dbOrder);
            Assert.Equal("Cancelled", dbOrder.Status);
            
            var dbProduct = await _context.Products.FindAsync(1);
            Assert.NotNull(dbProduct);
            Assert.Equal(10, dbProduct.Stock); 
        }

        [Fact]
        public async Task CancelOrder_ShouldReturnBadRequest_WhenStatusIsNotPending()
        {
            // Arrange
            MockUser("1", "test@test.com");
            var order = new Order 
            { 
                OrderID = 1, 
                UserID = 1, 
                Status = "Shipped",
                ShippingAddress = "Test Address"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CancelOrder(1);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse);
            Assert.Contains("Sadece 'Pending'", apiResponse.Message);
        }

        [Fact]
        public async Task RequestReturn_ShouldCreateReturn_WhenEligible()
        {
            // Arrange
            MockUser("1", "test@test.com");
            var order = new Order 
            { 
                OrderID = 1, 
                UserID = 1, 
                Status = "Delivered",
                ShippingAddress = "Test Address"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var dto = new CreateReturnDto { ReturnReason = "Defective", ReturnDescription = "Broken screen" };

            // Act
            var result = await _controller.RequestReturn(1, dto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReturnDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse);
            var dbReturn = await _context.OrderReturns.FirstOrDefaultAsync();
            Assert.NotNull(dbReturn);
            Assert.Equal(1, dbReturn.OrderID);
            Assert.Equal("Pending", dbReturn.Status);
        }

        [Fact]
        public async Task ApproveReturn_ShouldUpdateStatusAndStock_WhenAdmin()
        {
            // Arrange
            await SeedData(); 
            MockUser("2", "admin@test.com", "Admin");

            var product = await _context.Products.FindAsync(1);
            
            var order = new Order 
            { 
                OrderID = 1, 
                UserID = 1, 
                Status = "Delivered",
                ShippingAddress = "Test Address",
                OrderItems = new List<OrderItem> 
                { 
                    new OrderItem { ProductID = 1, Quantity = 1, UnitPrice = 1000, Product = product! } 
                }
            };
            
            // DÜZELTME: ReturnReason Eklendi
            var orderReturn = new OrderReturn 
            { 
                ReturnID = 1, 
                OrderID = 1, 
                UserID = 1, 
                Status = "Pending", 
                Order = order,
                ReturnReason = "Defective Product" 
            };

            _context.Orders.Add(order);
            _context.OrderReturns.Add(orderReturn);
            await _context.SaveChangesAsync();

            var dto = new ApproveReturnDto { RefundAmount = 1000, AdminNote = "Approved" };

            // Act
            var result = await _controller.ApproveReturn(1, dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            
            var dbReturn = await _context.OrderReturns.FindAsync(1);
            Assert.NotNull(dbReturn);
            Assert.Equal("Approved", dbReturn.Status);
            Assert.Equal(1000, dbReturn.RefundAmount);

            var dbOrder = await _context.Orders.FindAsync(1);
            Assert.NotNull(dbOrder);
            Assert.Equal("Returned", dbOrder.Status);

            var dbProduct = await _context.Products.FindAsync(1);
            Assert.NotNull(dbProduct);
            Assert.Equal(11, dbProduct.Stock);
        }

        [Fact]
        public async Task OneClickBuy_ShouldSucceed_WhenPaymentIsSuccessful()
        {
            // Arrange
            await SeedData(); 
            MockUser("1", "test@test.com");
            var dto = new OneClickBuyDto { PaymentMethod = "CreditCard", ShippingAddress = "Home" };

            // Payment Service Mocking
            var mockPaymentResponse = new Backend.DTOs.PaymentResponse 
            { 
                Success = true, 
                TransactionId = "TXN123", 
                Status = "Completed", 
                Message = "Success"
            };

            _mockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockPaymentResponse);

            // Act
            var result = await _controller.OneClickBuy(dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<OneClickBuyResponse>(actionResult.Value);
            Assert.True(response.Success);
            Assert.Equal("TXN123", response.TransactionId);

            var order = await _context.Orders.FirstOrDefaultAsync();
            Assert.NotNull(order);
            Assert.Equal(2000, order.TotalAmount);
            Assert.Equal("Processing", order.Status);

            Assert.Empty(_context.CartItems);
        }

        [Fact]
        public async Task OneClickBuy_ShouldFail_WhenPaymentFails()
        {
            // Arrange
            await SeedData();
            MockUser("1", "test@test.com");
            var dto = new OneClickBuyDto { PaymentMethod = "CreditCard", ShippingAddress = "Home" };

            var mockPaymentResponse = new Backend.DTOs.PaymentResponse
            {
                Success = false,
                Message = "Insufficient funds",
                Status = "Failed"
            };

            _mockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockPaymentResponse);

            // Act
            var result = await _controller.OneClickBuy(dto);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<OneClickBuyResponse>(actionResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Insufficient funds", response.Message);

            Assert.Empty(_context.Orders);
            Assert.Single(_context.CartItems);
        }

        [Fact]
        public async Task PurchaseSingleItem_ShouldCreateOrder_ForOneItem()
        {
            // Arrange
            await SeedData(); 
            MockUser("1", "test@test.com");
            
            var dto = new PurchaseSingleItemDto { CartItemID = 1, ShippingAddress = "Home" };

            // Act
            var result = await _controller.PurchaseSingleItem(dto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);

            var order = await _context.Orders.FirstOrDefaultAsync();
            Assert.NotNull(order);
            Assert.Equal(2000, order.TotalAmount); 

            Assert.Empty(_context.CartItems);
        }
    }
}