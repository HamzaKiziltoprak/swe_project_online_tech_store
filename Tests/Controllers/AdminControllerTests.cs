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
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            // 1. InMemory Database Kurulumu
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DataContext(options);

            // 2. UserManager Mock
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // 3. RoleManager Mock
            var roleStore = new Mock<IRoleStore<Role>>();
            _mockRoleManager = new Mock<RoleManager<Role>>(
                roleStore.Object, null!, null!, null!, null!);

            // 4. Logger Mock
            _mockLogger = new Mock<ILogger<AdminController>>();

            // 5. Controller Başlatma
            _controller = new AdminController(
                _context,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockLogger.Object
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region AssignRole Tests

        [Fact]
        public async Task AssignRole_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            _controller.ModelState.AddModelError("Role", "Required");
            var dto = new AssignRoleRequestDto();

            var result = await _controller.AssignRole(dto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(badRequestResult.Value);
            Assert.False(response!.Success);
        }

        [Fact]
        public async Task AssignRole_ShouldReturnBadRequest_WhenRoleNameIsInvalid()
        {
            var dto = new AssignRoleRequestDto { UserID = 1, Role = "SuperHacker" };

            var result = await _controller.AssignRole(dto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(badRequestResult.Value);
            Assert.Contains("Geçersiz rol", response!.Message);
        }

        [Fact]
        public async Task AssignRole_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            var dto = new AssignRoleRequestDto { UserID = 99, Role = "Admin" };
            
            _mockUserManager.Setup(x => x.FindByIdAsync("99"))
                .ReturnsAsync((User?)null);

            var result = await _controller.AssignRole(dto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(notFoundResult.Value);
            Assert.Equal("Kullanıcı bulunamadı", response!.Message);
        }

        [Fact]
        public async Task AssignRole_ShouldReturnBadRequest_WhenRoleDoesNotExistInDb()
        {
            var dto = new AssignRoleRequestDto { UserID = 1, Role = "Admin" };
            var user = new User { Id = 1 };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(false);

            var result = await _controller.AssignRole(dto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(badRequestResult.Value);
            Assert.Contains("sistemde mevcut değil", response!.Message);
        }

        [Fact]
        public async Task AssignRole_ShouldReturnOk_WhenRoleAssignmentIsSuccessful()
        {
            var dto = new AssignRoleRequestDto { UserID = 1, Role = "ProductManager" };
            var user = new User { Id = 1, UserName = "testuser", Email = "test@test.com" };
            var existingRoles = new List<string> { "Customer" };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("ProductManager")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(existingRoles);
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, existingRoles))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "ProductManager"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.AssignRole(dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("ProductManager", response.Data!.AssignedRole);
        }

        [Fact]
        public async Task AssignRole_ShouldReturnBadRequest_WhenRemoveRoleFails()
        {
            var dto = new AssignRoleRequestDto { UserID = 1, Role = "Admin" };
            var user = new User { Id = 1 };
            
            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "OldRole" });
            
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error removing role" }));

            var result = await _controller.AssignRole(dto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(badRequestResult.Value);
            Assert.Contains("Eski rol kaldırılırken hata", response!.Message);
        }

        #endregion

        #region GetStats Tests

        [Fact]
        public async Task GetStats_ShouldReturnCorrectCounts()
        {
            // Arrange
            await SeedDatabaseForStats();

            // Mock Users Queryable
            var users = new List<User> 
            { 
                new User { Id = 1 }, new User { Id = 2 }, new User { Id = 3 } 
            }.AsQueryable();

            _mockUserManager.Setup(x => x.Users).Returns(MockAsyncQueryable(users));
            
            _mockUserManager.Setup(x => x.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<User> { new User() });
            _mockUserManager.Setup(x => x.GetUsersInRoleAsync("Employee"))
                .ReturnsAsync(new List<User> { new User(), new User() });
            _mockUserManager.Setup(x => x.GetUsersInRoleAsync("Customer"))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _controller.GetStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<AdminStatsDto>>(okResult.Value);
            var stats = response!.Data;

            Assert.Equal(3, stats!.TotalProducts);
            Assert.Equal(2, stats.ActiveProducts);
            Assert.Equal(1, stats.OutOfStockProducts);

            Assert.Equal(2, stats.TotalCategories);

            Assert.Equal(3, stats.TotalOrders);
            Assert.Equal(1, stats.CompletedOrders);
            
            Assert.Equal(500, stats.CompletedRevenue);
            Assert.Equal(650, stats.TotalRevenue);

            Assert.Equal(2, stats.TotalReviews);
            Assert.Equal(1, stats.ApprovedReviews);

            Assert.Equal(2, stats.TotalReturns);
            Assert.Equal(150, stats.TotalRefundAmount);

            Assert.Equal(3, stats.TotalUsers);
            Assert.Equal(1, stats.AdminUsers);
        }

        [Fact]
        public async Task GetStats_ShouldReturn500_OnException()
        {
            _context.Dispose(); 

            var result = await _controller.GetStats();

            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        // Helper: DB Seed
        private async Task SeedDatabaseForStats()
        {
            _context.Categories.AddRange(
                new Category { CategoryID = 1, CategoryName = "Elektronik" },
                new Category { CategoryID = 2, CategoryName = "Giyim" }
            );

            _context.Products.AddRange(
                new Product { 
                    ProductID = 1, 
                    ProductName = "Laptop",
                    IsActive = true, 
                    Stock = 10, 
                    Price = 1000,
                    Description = "Test Desc 1",
                    ImageUrl = "img1.jpg"
                },
                new Product { 
                    ProductID = 2, 
                    ProductName = "Mouse", 
                    IsActive = true, 
                    Stock = 50, 
                    Price = 50,
                    Description = "Test Desc 2",
                    ImageUrl = "img2.jpg"
                },
                new Product { 
                    ProductID = 3, 
                    ProductName = "Old Phone", 
                    IsActive = false, 
                    Stock = 0, 
                    Price = 200,
                    Description = "Test Desc 3",
                    ImageUrl = "img3.jpg"
                }
            );

            _context.Orders.AddRange(
                new Order { OrderID = 1, Status = "Completed", TotalAmount = 500, ShippingAddress = "Address 1" },
                new Order { OrderID = 2, Status = "Pending", TotalAmount = 100, ShippingAddress = "Address 2" },
                new Order { OrderID = 3, Status = "Cancelled", TotalAmount = 50, ShippingAddress = "Address 3" }
            );

            _context.ProductReviews.AddRange(
                new ProductReview { ReviewID = 1, IsApproved = true, Rating = 5, Comment = "Good" },
                new ProductReview { ReviewID = 2, IsApproved = false, Rating = 1, Comment = "Bad" }
            );

            // DÜZELTME: ReturnReason eklendi
            _context.OrderReturns.AddRange(
                new OrderReturn { ReturnID = 1, Status = "Approved", RefundAmount = 100, ReturnReason = "Defective" },
                new OrderReturn { ReturnID = 2, Status = "Pending", RefundAmount = 50, ReturnReason = "Changed Mind" }
            );

            await _context.SaveChangesAsync();
        }

        // Helper: Async Queryable Mock
        private static IQueryable<T> MockAsyncQueryable<T>(IQueryable<T> data)
        {
            var mock = new Mock<IQueryable<T>>();
            var asyncEnumerable = new TestAsyncEnumerable<T>(data);

            mock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

            mock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(data.Provider));

            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            return mock.Object;
        }

        internal class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            internal TestAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestAsyncEnumerable<TEntity>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestAsyncEnumerable<TElement>(expression);
            }

            public object? Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            // Düzeltme: TResult? yerine TResult kullanılarak IQueryProvider ile uyumlu hale getirildi
            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public TResult ExecuteAsync<TResult>(Expression expression, System.Threading.CancellationToken cancellationToken = default)
            {
                var resultType = typeof(TResult).GetGenericArguments()[0];
                var executionResult = typeof(IQueryProvider)
                    .GetMethod(
                        name: nameof(IQueryProvider.Execute),
                        genericParameterCount: 1,
                        types: new[] { typeof(Expression) })!
                    .MakeGenericMethod(resultType)
                    .Invoke(_inner, new[] { expression });

                return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new[] { executionResult })!;
            }
        }

        internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
            public TestAsyncEnumerable(Expression expression) : base(expression) { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return ValueTask.FromResult(_inner.MoveNext());
            }
        }

        #endregion
    }
}