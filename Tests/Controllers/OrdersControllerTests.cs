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
    public class OrdersControllerTests
    {
        // Testler için kullanılacak InMemory veritabanı
        private readonly DataContext _context;

        // Kullanıcı yönetimi için mock UserManager
        private readonly Mock<UserManager<User>> _mockUserManager;

        // Controller loglaması için mock Logger
        private readonly Mock<ILogger<OrdersController>> _mockLogger;

        // Test edilen controller
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            // InMemory veritabanı oluşturma
            // Her testte izole bir veritabanı sağlamak için rastgele bir isim kullanılıyor
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            _context = new DataContext(options);

            // UserManager için gerekli olan UserStore'u mockluyoruz
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Logger mock
            _mockLogger = new Mock<ILogger<OrdersController>>();

            // Controller örneğini oluşturuyoruz
            _controller = new OrdersController(
                _context,
                _mockUserManager.Object,
                _mockLogger.Object
            );
        }

        // Yeni sipariş oluşturma testleri

        [Fact]
        public async Task CreateOrder_ShouldReturnCreated_WhenCartIsValidAndStockIsAvailable()
        {
            // Kullanıcıyı temsil eden kimlik bilgilerini test için hazırlıyoruz
            var userId = 1;
            var user = new User { Id = userId, Email = "test@user.com", FirstName="T", LastName="U" };
            SetupHttpContextWithUser(userId);

            // Mock UserManager bu kullanıcıyı döndürecek şekilde ayarlanıyor
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Ürün ve sepet içeriğinin veritabanına eklenmesi
            var product = new Product { ProductID = 10, ProductName = "Laptop", Price = 1000, Stock = 5, Brand="B", Description="D", ImageUrl="I" };
            _context.Products.Add(product);
            _context.CartItems.Add(new CartItem { CartItemID = 1, UserID = userId, ProductID = 10, Count = 1 });
            await _context.SaveChangesAsync();

            // Test sırasında EF nin takip ettiği entityleri temizliyoruz
            _context.ChangeTracker.Clear();

            var dto = new CreateOrderDto { ShippingAddress = "123 Main St" };

            // Sipariş oluşturma isteği gönderiliyor
            var result = await _controller.CreateOrder(dto);

            // Dönen yanıtın Created olup olmadığını kontrol ediyoruz
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            var order = apiResponse.Data;

            // Sipariş detaylarının doğruluğunu test ediyoruz
            Assert.True(apiResponse.Success);
            Assert.Equal("Pending", order.Status);
            Assert.Equal(1000, order.TotalAmount);

            // Ürün stok miktarının doğru güncellenip güncellenmediğini kontrol ediyoruz
            var dbProduct = await _context.Products.FindAsync(10);
            Assert.Equal(4, dbProduct.Stock);

            // Sipariş oluşturulduktan sonra kullanıcının sepetinin boşalmış olması gerekiyor
            var cartItems = await _context.CartItems.Where(c => c.UserID == userId).ToListAsync();
            Assert.Empty(cartItems);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnBadRequest_WhenStockIsInsufficient()
        {
            // Test kullanıcısının hazırlanması
            var userId = 2;
            var user = new User { Id = userId, FirstName="T", LastName="U" };
            SetupHttpContextWithUser(userId);

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Ürünün stokta az olması ve sepetin daha fazla istemesi durumu
            var product = new Product { ProductID = 20, ProductName = "Phone", Price = 500, Stock = 2, Brand="B", Description="D", ImageUrl="I" };
            _context.Products.Add(product);
            _context.CartItems.Add(new CartItem { UserID = userId, ProductID = 20, Count = 5 });
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var dto = new CreateOrderDto { ShippingAddress = "Address" };

            // Sipariş isteği gönderiliyor
            var result = await _controller.CreateOrder(dto);

            // Stok yetersiz olduğunda BadRequest dönmeli
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Contains("stokta yeterli miktarda mevcut değildir", apiResponse.Message);
        }

        // Kullanıcının geçmiş siparişlerini listeleme testi

        [Fact]
        public async Task GetMyOrders_ShouldReturnOrders_WhenUserHasOrders()
        {
            var userId = 3;
            var user = new User { Id=userId, Email="test@user.com", FirstName="T", LastName="U" };
            SetupHttpContextWithUser(userId);

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Test için iki sipariş ekleniyor
            _context.Orders.Add(new Order { OrderID = 100, UserID = userId, Status = "Pending", TotalAmount = 50, ShippingAddress="Addr" });
            _context.Orders.Add(new Order { OrderID = 101, UserID = userId, Status = "Completed", TotalAmount = 100, ShippingAddress="Addr" });

            await _context.SaveChangesAsync();

            var result = await _controller.GetMyOrders();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedOrderResult>>(actionResult.Value);

            // Kullanıcının toplam iki siparişi olmalı
            Assert.Equal(2, apiResponse.Data.TotalCount);
            Assert.Equal(2, apiResponse.Data.Orders.Count);
        }

        // Sipariş iptali testleri

        [Fact]
        public async Task CancelOrder_ShouldReturnOk_WhenOrderIsPending()
        {
            var userId = 4;
            var user = new User { Id=userId, Email="cancel@user.com", FirstName="T", LastName="U" };
            SetupHttpContextWithUser(userId);

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Ürün ve sipariş test ortamına ekleniyor
            var product = new Product { ProductID = 30, Stock = 10, Brand="B", Description="D", ImageUrl="I", ProductName="P", Price=10 };
            _context.Products.Add(product);

            var order = new Order { OrderID = 200, UserID = userId, Status = "Pending", TotalAmount = 10, ShippingAddress="Addr" };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Bu siparişe ait item ekleniyor
            _context.OrderItems.Add(new OrderItem { OrderItemID = 1, OrderID = 200, ProductID = 30, Quantity = 5, UnitPrice = 10 });
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Sipariş iptali isteği gönderiliyor
            var result = await _controller.CancelOrder(200);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);

            // İptal işlemi başarılı olmalı
            Assert.Equal("Sipariş başarıyla iptal edildi", apiResponse.Message);
            Assert.Equal("Cancelled", apiResponse.Data.Status);

            // İade edilen stok miktarı kontrol ediliyor
            var dbProduct = await _context.Products.FindAsync(30);
            Assert.Equal(15, dbProduct.Stock);
        }

        [Fact]
        public async Task CancelOrder_ShouldReturnBadRequest_WhenOrderIsAlreadyCompleted()
        {
            var userId = 5;
            SetupHttpContextWithUser(userId);

            // Tamamlanmış sipariş iptal edilemez
            _context.Orders.Add(new Order { OrderID = 300, UserID = userId, Status = "Completed", TotalAmount = 50, ShippingAddress="Addr" });
            await _context.SaveChangesAsync();

            var result = await _controller.CancelOrder(300);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<OrderDto>>(actionResult.Value);

            Assert.Contains("Sadece 'Pending'", apiResponse.Message);
        }

        // İade talebi testleri

        [Fact]
        public async Task RequestReturn_ShouldReturnCreated_WhenEligible()
        {
            var userId = 6;
            SetupHttpContextWithUser(userId);

            // İade talebi oluşturulabilecek bir sipariş var
            _context.Orders.Add(new Order { OrderID = 400, UserID = userId, Status = "Completed", TotalAmount = 100, ShippingAddress="Addr" });
            await _context.SaveChangesAsync();

            var dto = new CreateReturnDto { ReturnReason="Defective", ReturnDescription="Screen broken" };

            var result = await _controller.RequestReturn(400, dto);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReturnDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Pending", apiResponse.Data.Status);

            // İade talebinin veritabanında oluşturulduğunu doğruluyoruz
            var dbReturn = await _context.OrderReturns.FirstOrDefaultAsync(r => r.OrderID == 400);
            Assert.NotNull(dbReturn);
        }

        [Fact]
        public async Task RequestReturn_ShouldReturnBadRequest_WhenDuplicateRequest()
        {
            var userId = 7;
            SetupHttpContextWithUser(userId);

            _context.Orders.Add(new Order { OrderID = 500, UserID = userId, Status = "Completed", TotalAmount = 100, ShippingAddress="Addr" });

            // Kullanıcının aynı sipariş için zaten bekleyen bir iade talebi var
            _context.OrderReturns.Add(new OrderReturn { ReturnID = 1, OrderID = 500, UserID = userId, Status="Pending", ReturnReason="R" });
            await _context.SaveChangesAsync();

            var dto = new CreateReturnDto { ReturnReason="Broken" };

            var result = await _controller.RequestReturn(500, dto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReturnDto>>(actionResult.Value);

            Assert.Contains("zaten bir iade talebi beklemede", apiResponse.Message);
        }

        // Testler sırasında HTTP context içine kullanıcı bilgisi eklemek için yardımcı metot
        private void SetupHttpContextWithUser(int userId, string role = "Customer")
        {
            // Kullanıcıya kimlik bilgileri ve rol atıyoruz
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Controller'ın HttpContext'i bu kullanıcıyla çalışacak şekilde ayarlanıyor
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }
    }
}
