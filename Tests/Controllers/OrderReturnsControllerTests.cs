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
    public class OrderReturnsControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<OrderReturnsController>> _mockLogger;
        private readonly OrderReturnsController _controller;

        public OrderReturnsControllerTests()
        {
            // 1. InMemory Database Setup
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(options);

            // 2. Mocks
            _mockLogger = new Mock<ILogger<OrderReturnsController>>();
            
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // 3. Controller Init
            _controller = new OrderReturnsController(_context, _mockUserManager.Object, _mockLogger.Object);

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
            var user1 = new User { Id = 1, FirstName = "Customer", LastName = "One", Email = "cust@test.com" };
            var user2 = new User { Id = 2, FirstName = "Admin", LastName = "User", Email = "admin@test.com" };

            // Siparişler
            var orders = new List<Order>
            {
                // İade edilebilir sipariş (Tamamlanmış)
                new Order { OrderID = 1, UserID = 1, User = user1, Status = "Completed", TotalAmount = 100, ShippingAddress = "Addr1" },
                // İade edilemez sipariş (Henüz kargolanmamış)
                new Order { OrderID = 2, UserID = 1, User = user1, Status = "Pending", TotalAmount = 200, ShippingAddress = "Addr2" },
                // Zaten iade talebi olan sipariş
                new Order { OrderID = 3, UserID = 1, User = user1, Status = "Completed", TotalAmount = 300, ShippingAddress = "Addr3" }
            };

            // Mevcut İade Talepleri
            var returns = new List<OrderReturn>
            {
                // Bekleyen talep (Order 3 için)
                new OrderReturn 
                { 
                    ReturnID = 1, 
                    OrderID = 3, 
                    Order = orders[2], 
                    UserID = 1, 
                    User = user1, 
                    Status = "Pending", 
                    ReturnReason = "Defective", 
                    CreatedAt = DateTime.UtcNow 
                }
            };

            _context.Users.AddRange(user1, user2);
            _context.Orders.AddRange(orders);
            _context.OrderReturns.AddRange(returns);
            
            _context.SaveChanges();
        }

        private void SetupHttpContextWithUser(int userId, string role = "Customer")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region CreateReturn Tests

        [Fact]
        public async Task CreateReturn_ShouldCreate_WhenOrderEligible()
        {
            // Arrange
            SetupHttpContextWithUser(1); // Customer
            var dto = new CreateReturnDto { ReturnReason = "Damaged", ReturnDescription = "Box broken" };

            // Act: Order 1 (Completed) için talep oluştur
            var result = await _controller.CreateReturn(1, dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(createdResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("Pending", response.Data!.Status);
            
            // DB Check
            var dbReturn = await _context.OrderReturns.FirstOrDefaultAsync(r => r.OrderID == 1);
            Assert.NotNull(dbReturn);
            Assert.Equal("Damaged", dbReturn!.ReturnReason);
        }

        [Fact]
        public async Task CreateReturn_ShouldFail_WhenOrderNotCompleted()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new CreateReturnDto { ReturnReason = "Bad", ReturnDescription = "Desc" };

            // Act: Order 2 (Pending status)
            var result = await _controller.CreateReturn(2, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(badRequestResult.Value);
            Assert.Contains("Only completed or delivered", response!.Message);
        }

        [Fact]
        public async Task CreateReturn_ShouldFail_WhenReturnAlreadyExists()
        {
            // Arrange
            SetupHttpContextWithUser(1);
            var dto = new CreateReturnDto { ReturnReason = "Bad", ReturnDescription = "Desc" };

            // Act: Order 3 (Zaten ReturnID=1 var)
            var result = await _controller.CreateReturn(3, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(badRequestResult.Value);
            Assert.Contains("already exists", response!.Message);
        }

        [Fact]
        public async Task CreateReturn_ShouldReturnNotFound_WhenOrderBelongsToAnotherUser()
        {
            // Arrange
            SetupHttpContextWithUser(2); // Farklı kullanıcı (Admin/User 2)
            var dto = new CreateReturnDto { ReturnReason = "Bad" };

            // Act: User 2, User 1'in siparişini (Order 1) iade etmeye çalışıyor
            var result = await _controller.CreateReturn(1, dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            // "Order not found" döner çünkü query'de UserID filtresi var
        }

        #endregion

        #region GetReturn Tests

        [Fact]
        public async Task GetReturn_ShouldReturnData_WhenUserIsOwner()
        {
            // Arrange
            SetupHttpContextWithUser(1);

            // Act: Return 1
            var result = await _controller.GetReturn(3, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(okResult.Value);
            Assert.Equal(1, response!.Data!.ReturnID);
        }

        [Fact]
        public async Task GetReturn_ShouldForbid_WhenUserIsNotOwner()
        {
            // Arrange
            SetupHttpContextWithUser(2); // User 2

            // Act: Return 1 (User 1'e ait)
            var result = await _controller.GetReturn(3, 1);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        #endregion

        #region GetMyReturns Tests

        [Fact]
        public async Task GetMyReturns_ShouldReturnList_ForAuthenticatedUser()
        {
            // Arrange
            SetupHttpContextWithUser(1);

            // Act
            var result = await _controller.GetMyReturns();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<ReturnDto>>>(okResult.Value);
            
            Assert.Single(response!.Data!); // Return 1
            Assert.Equal(1, response.Data![0].UserID);
        }

        #endregion

        #region GetAllReturns (Admin) Tests

        [Fact]
        public async Task GetAllReturns_ShouldReturnAll_ForAdmin()
        {
            // Arrange
            SetupHttpContextWithUser(2, "Admin");
            var filter = new ReturnFilterParams { PageSize = 10, PageNumber = 1 };

            // Act
            var result = await _controller.GetAllReturns(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<PagedReturnResult>>(okResult.Value);
            
            Assert.NotNull(response!.Data);
            Assert.Equal(1, response.Data!.TotalCount);
        }

        #endregion

        #region ApproveReturn Tests

        [Fact]
        public async Task ApproveReturn_ShouldProcessRefundAndComplete_WhenPending()
        {
            // Arrange
            SetupHttpContextWithUser(2, "Admin");
            var dto = new ApproveReturnDto { RefundAmount = 50, AdminNote = "Approved OK" };

            // Act: Return 1 (Pending)
            var result = await _controller.ApproveReturn(3, 1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("Completed", response.Data!.Status); // Approve sonrası Completed olur
            Assert.Equal(50, response.Data.RefundAmount);

            // Transaction Oluştu mu?
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionType == "Refund" && t.OrderID == 3);
            Assert.NotNull(transaction);
            Assert.Equal(50, transaction!.Amount);
        }

        [Fact]
        public async Task ApproveReturn_ShouldFail_WhenAlreadyProcessed()
        {
            // Arrange
            SetupHttpContextWithUser(2, "Admin");
            // Return 1'i önce Rejected yapalım
            var ret = await _context.OrderReturns.FindAsync(1);
            ret!.Status = "Rejected";
            await _context.SaveChangesAsync();

            var dto = new ApproveReturnDto { RefundAmount = 50 };

            // Act
            var result = await _controller.ApproveReturn(3, 1, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(badRequestResult.Value);
            Assert.Contains("Only pending", response!.Message);
        }

        #endregion

        #region RejectReturn Tests

        [Fact]
        public async Task RejectReturn_ShouldUpdateStatusToRejected()
        {
            // Arrange
            SetupHttpContextWithUser(2, "Admin");
            var dto = new RejectReturnDto { AdminNote = "Invalid reason" };

            // Act: Return 1 (Pending)
            var result = await _controller.RejectReturn(3, 1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ReturnDto>>(okResult.Value);
            
            Assert.Equal("Rejected", response!.Data!.Status);
            Assert.Equal("Invalid reason", response.Data.AdminNote);

            // DB Check
            var dbReturn = await _context.OrderReturns.FindAsync(1);
            Assert.Equal("Rejected", dbReturn!.Status);
        }

        #endregion
    }
}