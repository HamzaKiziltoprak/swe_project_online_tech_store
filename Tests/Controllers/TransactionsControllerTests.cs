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

namespace Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<TransactionsController>> _mockLogger;
        private readonly DataContext _context;
        private readonly TransactionsController _controller;

        public TransactionsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(options);

            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockLogger = new Mock<ILogger<TransactionsController>>();

            _controller = new TransactionsController(_context, _mockUserManager.Object, _mockLogger.Object);
        }

        private void SetupUserClaims(string userId, string role = "Customer")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetMyTransactions_ReturnsUserTransactions_WhenAuthenticated()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                UserName = "john@test.com"
            };
            _context.Users.Add(user);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 100m,
                Status = "Completed",
                ShippingAddress = "Test Address",
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);

            var transaction = new Transaction
            {
                TransactionID = 1,
                TransactionType = "Purchase",
                Amount = 100m,
                Status = "Completed",
                OrderID = 1,
                UserID = 1,
                TransactionDate = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            SetupUserClaims("1");

            // Act
            var result = await _controller.GetMyTransactions(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<PagedTransactionResult>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Single(response.Data!.Data);
            Assert.Equal(100m, response.Data.Data[0].Amount);
        }

        [Fact]
        public async Task GetTransaction_ReturnsTransaction_WhenUserOwnsIt()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                UserName = "john@test.com"
            };
            _context.Users.Add(user);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 100m,
                Status = "Completed",
                ShippingAddress = "Test Address",
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);

            var transaction = new Transaction
            {
                TransactionID = 1,
                TransactionType = "Purchase",
                Amount = 100m,
                Status = "Completed",
                OrderID = 1,
                UserID = 1,
                TransactionDate = DateTime.UtcNow,
                Description = "Test transaction"
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            SetupUserClaims("1");

            // Act
            var result = await _controller.GetTransaction(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<TransactionDetailDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(1, response.Data!.TransactionID);
            Assert.Equal("Purchase", response.Data.TransactionType);
            Assert.Equal(100m, response.Data.Amount);
        }

        [Fact]
        public async Task GetTransaction_ReturnsForbidden_WhenUserDoesNotOwnTransaction()
        {
            // Arrange
            var user1 = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", UserName = "john@test.com" };
            var user2 = new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", UserName = "jane@test.com" };
            _context.Users.AddRange(user1, user2);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 100m,
                Status = "Completed",
                ShippingAddress = "Test Address",
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);

            var transaction = new Transaction
            {
                TransactionID = 1,
                TransactionType = "Purchase",
                Amount = 100m,
                Status = "Completed",
                OrderID = 1,
                UserID = 1,
                TransactionDate = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            SetupUserClaims("2"); // Different user

            // Act
            var result = await _controller.GetTransaction(1);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetStatistics_ReturnsCorrectStatistics_ForAdmin()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com" };
            _context.Users.Add(user);

            var order1 = new Order { OrderID = 1, UserID = 1, TotalAmount = 100m, Status = "Completed", ShippingAddress = "Test", OrderDate = DateTime.UtcNow };
            var order2 = new Order { OrderID = 2, UserID = 1, TotalAmount = 50m, Status = "Completed", ShippingAddress = "Test", OrderDate = DateTime.UtcNow };
            _context.Orders.AddRange(order1, order2);

            var transactions = new List<Transaction>
            {
                new Transaction { TransactionID = 1, TransactionType = "Purchase", Amount = 100m, Status = "Completed", OrderID = 1, UserID = 1, TransactionDate = DateTime.UtcNow },
                new Transaction { TransactionID = 2, TransactionType = "Purchase", Amount = 50m, Status = "Completed", OrderID = 2, UserID = 1, TransactionDate = DateTime.UtcNow },
                new Transaction { TransactionID = 3, TransactionType = "Refund", Amount = 25m, Status = "Completed", OrderID = 1, UserID = 1, TransactionDate = DateTime.UtcNow }
            };
            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            SetupUserClaims("1", "Admin");

            // Act
            var result = await _controller.GetStatistics(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<TransactionStatisticsDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(150m, response.Data!.TotalRevenue);
            Assert.Equal(25m, response.Data.TotalRefunds);
            Assert.Equal(125m, response.Data.NetRevenue);
            Assert.Equal(3, response.Data.TotalTransactions);
        }

        [Fact]
        public async Task CreateTransaction_CreatesSuccessfully_WhenAdmin()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Admin", LastName = "User", Email = "admin@test.com", UserName = "admin@test.com" };
            _context.Users.Add(user);

            var order = new Order { OrderID = 1, UserID = 1, TotalAmount = 100m, Status = "Completed", ShippingAddress = "Test", OrderDate = DateTime.UtcNow };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            SetupUserClaims("1", "Admin");

            _mockUserManager.Setup(x => x.FindByIdAsync("1"))
                .ReturnsAsync(user);

            var createDto = new CreateTransactionDto
            {
                TransactionType = "Adjustment",
                Amount = 10m,
                Description = "Price adjustment",
                OrderID = 1,
                UserID = 1
            };

            // Act
            var result = await _controller.CreateTransaction(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<TransactionDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Adjustment", response.Data!.TransactionType);
            Assert.Equal(10m, response.Data.Amount);
        }

        [Fact]
        public async Task GetAllTransactions_ReturnsFilteredTransactions_WhenFiltered()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", UserName = "test@test.com" };
            _context.Users.Add(user);

            var order = new Order { OrderID = 1, UserID = 1, TotalAmount = 100m, Status = "Completed", ShippingAddress = "Test", OrderDate = DateTime.UtcNow };
            _context.Orders.Add(order);

            var transactions = new List<Transaction>
            {
                new Transaction { TransactionID = 1, TransactionType = "Purchase", Amount = 100m, Status = "Completed", OrderID = 1, UserID = 1, TransactionDate = DateTime.UtcNow },
                new Transaction { TransactionID = 2, TransactionType = "Refund", Amount = 50m, Status = "Completed", OrderID = 1, UserID = 1, TransactionDate = DateTime.UtcNow }
            };
            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            SetupUserClaims("1", "Admin");

            var filterParams = new TransactionFilterParams
            {
                TransactionType = "Purchase",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetAllTransactions(filterParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<PagedTransactionResult>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Single(response.Data!.Data);
            Assert.Equal("Purchase", response.Data.Data[0].TransactionType);
        }
    }
}
