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
using System.Security.Claims;
using Xunit;

namespace Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<TransactionsController>> _mockLogger;
        private readonly TransactionsController _controller;

        public TransactionsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Her test için izole DB
                .EnableSensitiveDataLogging()
                .Options;

            _context = new DataContext(options);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _mockLogger = new Mock<ILogger<TransactionsController>>();
            _controller = new TransactionsController(_context, _mockUserManager.Object, _mockLogger.Object);
        }

        // --- HELPERS ---

        private void MockUserLogin(int userId, string role = "Customer")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, $"User{userId}")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Mock UserManager setup (DB'ye dokunmaz, sadece UserManager davranışını taklit eder)
            var userMock = CreateValidUser(userId);
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(userMock);
        }

        private User CreateValidUser(int id)
        {
            return new User
            {
                Id = id,
                UserName = $"user{id}",
                Email = $"user{id}@test.com",
                FirstName = "Test",
                LastName = "User", 
                EmailConfirmed = true
            };
        }

        private Order CreateValidOrder(int orderId, int userId)
        {
            return new Order
            {
                OrderID = orderId,
                UserID = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Completed",
                TotalAmount = 100,
                ShippingAddress = "Test Address 123"
            };
        }

        private Transaction CreateValidTransaction(int id, int orderId, int userId, string type = "Purchase", decimal amount = 100, string status = "Completed")
        {
            return new Transaction
            {
                TransactionID = id,
                OrderID = orderId,
                UserID = userId,
                TransactionType = type,
                Amount = amount,
                Status = status,
                TransactionDate = DateTime.UtcNow,
                Description = "Test Transaction"
            };
        }

        // --- TESTLER ---

        [Fact]
        public async Task GetAllTransactions_ShouldReturnFilteredResult_WhenAdmin()
        {
            // Arrange
            var adminId = 1;
            var userId = 10;
            
            MockUserLogin(adminId, "Admin");
            
            // 1. User ve Order'ı ekle ve kaydet
            _context.Users.Add(CreateValidUser(userId));
            _context.Orders.Add(CreateValidOrder(100, userId));
            await _context.SaveChangesAsync();
            
            // Context'i temizle ki User/Order referansları cache'de kalmasın
            _context.ChangeTracker.Clear();

            // 2. Transaction'ları SADECE ID REFERANSLARI ile ekle (Navigation Property atamadan)
            var t1 = CreateValidTransaction(1, 100, userId, "Purchase", 100, "Completed");
            var t2 = CreateValidTransaction(2, 100, userId, "Refund", 50, "Completed");

            _context.Transactions.AddRange(t1, t2);
            await _context.SaveChangesAsync();
            
            // Context'i tekrar temizle (Controller taze veri çeksin)
            _context.ChangeTracker.Clear();

            var filter = new TransactionFilterParams 
            { 
                TransactionType = "Purchase", 
                Page = 1, 
                PageSize = 10,
                SortBy = "TransactionDate",
                SortOrder = "Desc"
            };

            // Act
            var result = await _controller.GetAllTransactions(filter);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedTransactionResult>>(actionResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(1, apiResponse.Data!.TotalCount); 
            Assert.Equal("Purchase", apiResponse.Data!.Data.First().TransactionType);
        }

        [Fact]
        public async Task GetMyTransactions_ShouldReturnOnlyUserTransactions()
        {
            // Arrange
            var userId = 10;
            var otherUserId = 11;
            
            MockUserLogin(userId); 

            // User ve Order ekle
            _context.Users.AddRange(CreateValidUser(userId), CreateValidUser(otherUserId));
            _context.Orders.AddRange(CreateValidOrder(100, userId), CreateValidOrder(101, otherUserId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Transaction ekle (Sadece FK ile)
            _context.Transactions.AddRange(
                CreateValidTransaction(1, 100, userId, "Purchase", 100),
                CreateValidTransaction(2, 101, otherUserId, "Purchase", 200) 
            );
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.GetMyTransactions();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedTransactionResult>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(1, apiResponse.Data!.TotalCount);
            Assert.Equal(userId, apiResponse.Data!.Data.First().UserID);
        }

        [Fact]
        public async Task GetTransaction_ShouldReturnDetails_WhenUserIsOwner()
        {
            // Arrange
            var userId = 10;
            var transId = 1;
            
            MockUserLogin(userId); 

            _context.Users.Add(CreateValidUser(userId));
            _context.Orders.Add(CreateValidOrder(100, userId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _context.Transactions.Add(CreateValidTransaction(transId, 100, userId)); 
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.GetTransaction(transId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<TransactionDetailDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(transId, apiResponse.Data!.TransactionID);
        }

        [Fact]
        public async Task GetTransaction_ShouldReturnForbid_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = 10; 
            var ownerId = 20; 
            var transId = 1;
            
            MockUserLogin(userId); 

            _context.Users.Add(CreateValidUser(ownerId));
            _context.Orders.Add(CreateValidOrder(100, ownerId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _context.Transactions.Add(CreateValidTransaction(transId, 100, ownerId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.GetTransaction(transId);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetTransaction_ShouldReturnSuccess_WhenAdminAccessingOthers()
        {
            // Arrange
            var adminId = 1;
            var ownerId = 20;
            var transId = 1;
            
            MockUserLogin(adminId, "Admin"); 

            _context.Users.Add(CreateValidUser(ownerId));
            _context.Orders.Add(CreateValidOrder(100, ownerId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _context.Transactions.Add(CreateValidTransaction(transId, 100, ownerId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.GetTransaction(transId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<TransactionDetailDto>>(actionResult.Value);
            Assert.True(apiResponse.Success);
        }

        [Fact]
        public async Task GetStatistics_ShouldReturnCorrectCalculations()
        {
            // Arrange
            MockUserLogin(1, "Admin");
            var userId = 10;
            
            _context.Users.Add(CreateValidUser(userId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _context.Transactions.AddRange(
                CreateValidTransaction(1, 100, userId, "Purchase", 100, "Completed"),
                CreateValidTransaction(2, 100, userId, "Purchase", 200, "Completed"),
                CreateValidTransaction(3, 100, userId, "Refund", 50, "Completed"),
                CreateValidTransaction(4, 100, userId, "Purchase", 500, "Failed") 
            );
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _controller.GetStatistics();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<TransactionStatisticsDto>>(actionResult.Value);
            
            var stats = apiResponse.Data!;
            Assert.Equal(300, stats.TotalRevenue); 
            Assert.Equal(50, stats.TotalRefunds);
            Assert.Equal(250, stats.NetRevenue); 
            Assert.Equal(4, stats.TotalTransactions);
            Assert.Equal(3, stats.SuccessfulTransactions);
            Assert.Equal(1, stats.FailedTransactions);
        }

        [Fact]
        public async Task CreateTransaction_ShouldCreate_WhenDataIsValid()
        {
            // Arrange
            MockUserLogin(1, "Admin");
            var userId = 10;
            var orderId = 100;

            _context.Users.Add(CreateValidUser(userId));
            _context.Orders.Add(CreateValidOrder(orderId, userId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var dto = new CreateTransactionDto
            {
                UserID = userId,
                OrderID = orderId,
                Amount = 150,
                TransactionType = "Adjustment",
                Description = "Manual fix"
            };

            // UserManager'ın FindByIdAsync çağrısını karşılaması için (Controller içinde kullanılıyor)
            // MockUserLogin içinde 1 için ayarlanmıştı, burada 10 için de ayarlıyoruz.
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(CreateValidUser(userId));

            // Act
            var result = await _controller.CreateTransaction(dto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<TransactionDto>>(actionResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal(150, apiResponse.Data!.Amount);
            
            var dbTrans = await _context.Transactions.LastOrDefaultAsync();
            Assert.NotNull(dbTrans);
            Assert.Equal("Manual fix", dbTrans!.Description);
        }

        [Fact]
        public async Task CreateTransaction_ShouldReturnNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            MockUserLogin(1, "Admin");
            var userId = 10;
            
            _context.Users.Add(CreateValidUser(userId));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var dto = new CreateTransactionDto
            {
                UserID = userId,
                OrderID = 999, // Yok
                Amount = 100,
                TransactionType = "Purchase",
                Description = "Test"
            };

            // Act
            var result = await _controller.CreateTransaction(dto);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<TransactionDto>>(actionResult.Value);
            Assert.Equal("Order not found", apiResponse.Message);
        }
    }
}