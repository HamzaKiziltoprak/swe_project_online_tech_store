using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class AdminControllerTests
    {
        // Identity yapısını in-memory database ile çalıştırmak için gerçek UserManager ve RoleManager kullanıyoruz
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly DataContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            // InMemory database oluşturuluyor. Her test izole bir veritabanı kullanacak.
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);

            // Identity için UserStore ve RoleStore hazırlanıyor.
            // Bu yapı UserManager ile RoleManager'ın gerçek database ile çalışmasını sağlıyor.
            var userStore = new UserStore<User, Role, DataContext, int>(_context);
            var roleStore = new RoleStore<Role, DataContext, int>(_context);

            // Gerçek UserManager oluşturuluyor. Çünkü mock UserManager role ekleme vb. işlemleri desteklemiyor.
            _userManager = new UserManager<User>(
                userStore,
                new Mock<IOptions<IdentityOptions>>().Object,
                new PasswordHasher<User>(),
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object
            );

            // Gerçek RoleManager oluşturuluyor.
            _roleManager = new RoleManager<Role>(
                roleStore,
                new IRoleValidator<Role>[0],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<Role>>>().Object
            );

            // Controller içindeki Logger mocklanıyor.
            _mockLogger = new Mock<ILogger<AdminController>>();

            // Testlerde kullanacağımız AdminController örneği oluşturuluyor.
            _controller = new AdminController(
                _context,
                _userManager,
                _roleManager,
                _mockLogger.Object
            );
        }

        // Kullanıcı rolü atama başarılı olduğunda OK dönmesi beklenen test
        [Fact]
        public async Task AssignRole_ShouldReturnOk_WhenRoleAssignmentIsSuccessful()
        {
            // Test için rol önce gerçekten veritabanına ekleniyor.
            await _roleManager.CreateAsync(new Role { Name = "ProductManager" });

            // Zorunlu alanları doldurulmuş bir kullanıcı oluşturuluyor.
            var user = new User
            {
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            await _userManager.CreateAsync(user);

            // Rol atama isteği DTO'su hazırlanıyor.
            var requestDto = new AssignRoleRequestDto 
            { 
                UserID = user.Id, 
                Role = "ProductManager" 
            };

            // Controller çağrılıyor.
            var result = await _controller.AssignRole(requestDto);

            // Sonuç doğrulanıyor.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("ProductManager", apiResponse.Data.AssignedRole);
        }

        // Geçersiz rol gönderildiğinde BadRequest dönmesini test eden senaryo
        [Fact]
        public async Task AssignRole_ShouldReturnBadRequest_WhenRoleIsInvalid()
        {
            var requestDto = new AssignRoleRequestDto 
            { 
                UserID = 1, 
                Role = "SuperAdmin" 
            };

            var result = await _controller.AssignRole(requestDto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(actionResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Contains("Geçersiz rol", apiResponse.Message);
        }

        // Kullanıcı bulunamadığında NotFound dönmesi gereken senaryo
        [Fact]
        public async Task AssignRole_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            var requestDto = new AssignRoleRequestDto 
            { 
                UserID = 999, 
                Role = "Admin" 
            };

            var result = await _controller.AssignRole(requestDto);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AssignRoleResponseDto>>(actionResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Equal("Kullanıcı bulunamadı", apiResponse.Message);
        }

        // Admin paneli için istatistik dönen fonksiyonun doğru çalışıp çalışmadığını test eden senaryo
        [Fact]
        public async Task GetStats_ShouldReturnCorrectStatistics()
        {
            // Roller ekleniyor
            await _roleManager.CreateAsync(new Role { Name = "Admin" });
            await _roleManager.CreateAsync(new Role { Name = "Employee" });
            await _roleManager.CreateAsync(new Role { Name = "Customer" });

            // Rollerine atanmış kullanıcılar ekleniyor
            var adminUser = new User { UserName = "admin", Email = "admin@test.com", FirstName = "Admin", LastName = "User" };
            await _userManager.CreateAsync(adminUser);
            await _userManager.AddToRoleAsync(adminUser, "Admin");

            var empUser1 = new User { UserName = "emp1", Email = "emp1@test.com", FirstName = "Emp", LastName = "One" };
            await _userManager.CreateAsync(empUser1);
            await _userManager.AddToRoleAsync(empUser1, "Employee");

            var empUser2 = new User { UserName = "emp2", Email = "emp2@test.com", FirstName = "Emp", LastName = "Two" };
            await _userManager.CreateAsync(empUser2);
            await _userManager.AddToRoleAsync(empUser2, "Employee");

            // Ürün, kategori, sipariş ve diğer veriler ekleniyor
            _context.Products.Add(new Product { ProductID = 1, ProductName = "P1", Brand = "B1", Description = "D1", ImageUrl = "I1", IsActive = true, Stock = 10, Price = 100 });
            _context.Products.Add(new Product { ProductID = 2, ProductName = "P2", Brand = "B2", Description = "D2", ImageUrl = "I2", IsActive = false, Stock = 0, Price = 50 });

            _context.Categories.Add(new Category { CategoryID = 1, CategoryName = "Electronics" });

            _context.Orders.Add(new Order { OrderID = 1, Status = "Completed", TotalAmount = 500, UserID = adminUser.Id, ShippingAddress = "Addr1" });
            _context.Orders.Add(new Order { OrderID = 2, Status = "Pending", TotalAmount = 200, UserID = empUser1.Id, ShippingAddress = "Addr2" });
            _context.Orders.Add(new Order { OrderID = 3, Status = "Cancelled", TotalAmount = 100, UserID = empUser2.Id, ShippingAddress = "Addr3" });

            _context.ProductReviews.Add(new ProductReview { ReviewID = 1, IsApproved = true, Rating = 5, Comment = "C1" });
            _context.ProductReviews.Add(new ProductReview { ReviewID = 2, IsApproved = false, Rating = 3, Comment = "C2" });

            _context.OrderReturns.Add(new OrderReturn { ReturnID = 1, Status = "Approved", RefundAmount = 50, OrderID = 1, ReturnReason = "R1" });

            await _context.SaveChangesAsync();

            // Controller fonksiyonu çağrılıyor
            var result = await _controller.GetStats();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AdminStatsDto>>(actionResult.Value);
            var stats = apiResponse.Data;

            // İstatistikler kontrol ediliyor
            Assert.True(apiResponse.Success);
            Assert.Equal(2, stats.TotalProducts);
            Assert.Equal(3, stats.TotalOrders);
            Assert.Equal(800, stats.TotalRevenue);
            Assert.Equal(3, stats.TotalUsers);
            Assert.Equal(1, stats.AdminUsers);
            Assert.Equal(2, stats.EmployeeUsers);
            Assert.Equal(1, stats.TotalReturns);
        }
    }
}
