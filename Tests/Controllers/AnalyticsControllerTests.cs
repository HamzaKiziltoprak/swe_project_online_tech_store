using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class AnalyticsControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<AnalyticsController>> _mockLogger;
        private readonly AnalyticsController _controller;

        public AnalyticsControllerTests()
        {
            // 1. Her test için izole bir InMemory veritabanı oluştur
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);
            _mockLogger = new Mock<ILogger<AnalyticsController>>();

            // 2. Test verilerini yükle
            SeedDatabase();

            // 3. Controller'ı başlat
            _controller = new AnalyticsController(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            // DÜZELTME: Exception testlerinde context manuel dispose edildiği için
            // burada erişmeye çalışmak ObjectDisposedException hatası verir.
            // Bu durumu try-catch ile yönetiyoruz.
            try
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Context zaten dispose edilmişse (hata testi senaryosu), işlemi yoksay.
            }
        }

        private void SeedDatabase()
        {
            // Kategori ve Marka
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            var brand = new Brand { BrandID = 1, BrandName = "TechCorp" };

            // Ürünler
            var products = new List<Product>
            {
                new Product { 
                    ProductID = 1, 
                    ProductName = "Laptop", 
                    IsActive = true, 
                    Stock = 5, 
                    CriticalStockLevel = 10, 
                    CategoryID = 1, 
                    Category = category, 
                    BrandID = 1, 
                    Brand = brand, 
                    Price = 1000, 
                    ImageUrl = "img1.jpg",
                    Description = "High performance laptop" 
                },
                new Product { 
                    ProductID = 2, 
                    ProductName = "Mouse", 
                    IsActive = true, 
                    Stock = 50, 
                    CriticalStockLevel = 5, 
                    CategoryID = 1, 
                    Category = category, 
                    BrandID = 1, 
                    Brand = brand, 
                    Price = 50, 
                    ImageUrl = "img2.jpg",
                    Description = "Wireless mouse"
                },
                new Product { 
                    ProductID = 3, 
                    ProductName = "Old Phone", 
                    IsActive = false, 
                    Stock = 0, 
                    CriticalStockLevel = 5, 
                    CategoryID = 1, 
                    Category = category, 
                    BrandID = 1, 
                    Brand = brand, 
                    Price = 200, 
                    ImageUrl = "img3.jpg",
                    Description = "Refurbished phone"
                }
            };

            // Kullanıcılar
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe" };

            // Siparişler
            var orders = new List<Order>
            {
                // Tamamlanmış Sipariş (Dün)
                new Order 
                { 
                    OrderID = 1, 
                    UserID = 1, 
                    User = user, 
                    Status = "Completed", 
                    OrderDate = DateTime.UtcNow.AddDays(-1), 
                    TotalAmount = 1050,
                    ShippingAddress = "Test Address 1",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { OrderID = 1, ProductID = 1, Product = products[0], Quantity = 1, UnitPrice = 1000 }, // 1 Laptop
                        new OrderItem { OrderID = 1, ProductID = 2, Product = products[1], Quantity = 1, UnitPrice = 50 }    // 1 Mouse
                    }
                },
                // Bekleyen Sipariş (Bugün)
                new Order 
                { 
                    OrderID = 2, 
                    UserID = 1, 
                    User = user, 
                    Status = "Pending", 
                    OrderDate = DateTime.UtcNow, 
                    TotalAmount = 2000,
                    ShippingAddress = "Test Address 2",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { OrderID = 2, ProductID = 1, Product = products[0], Quantity = 2, UnitPrice = 1000 } // 2 Laptop
                    }
                }
            };

            // İşlemler (Transactions)
            var transactions = new List<Transaction>
            {
                new Transaction { TransactionID = 1, TransactionType = "Purchase", Status = "Completed", Amount = 1050, TransactionDate = DateTime.UtcNow.AddDays(-1) },
                new Transaction { TransactionID = 2, TransactionType = "Purchase", Status = "Pending", Amount = 2000, TransactionDate = DateTime.UtcNow }, // Dashboard sadece Completed sayar
                new Transaction { TransactionID = 3, TransactionType = "Refund", Status = "Completed", Amount = 50, TransactionDate = DateTime.UtcNow }
            };

            _context.Categories.Add(category);
            _context.Brands.Add(brand);
            _context.Products.AddRange(products);
            _context.Users.Add(user);
            _context.Orders.AddRange(orders);
            _context.Transactions.AddRange(transactions);
            
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetDashboardOverview_ShouldReturnCorrectMetrics()
        {
            // Act
            var result = await _controller.GetDashboardOverview();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<DashboardOverviewDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            var data = response.Data!;

            // TotalRevenue: Sadece "Purchase" ve "Completed" olanlar. (Transaction 1: 1050)
            Assert.Equal(1050, data.TotalRevenue);

            // TotalOrders: Tüm siparişler (2 adet)
            Assert.Equal(2, data.TotalOrders);

            // TotalCustomers: Siparişi olan kullanıcılar (1 adet - John Doe)
            Assert.Equal(1, data.TotalCustomers);

            // TotalProducts: IsActive olanlar (Laptop ve Mouse - 2 adet)
            Assert.Equal(2, data.TotalProducts);

            // LowStock: Stock <= CriticalStockLevel ve Active olanlar (Laptop: 5 <= 10 -> Evet)
            Assert.Equal(1, data.LowStockProductsCount);

            // PendingOrders: Status == "Pending" (Sipariş 2)
            Assert.Equal(1, data.PendingOrdersCount);

            // AverageOrderValue: TotalRevenue (1050) / TotalOrders (2) = 525
            Assert.Equal(525, data.AverageOrderValue);
        }

        [Fact]
        public async Task GetTopSellingProducts_ShouldReturnProductsOrderedByQuantity()
        {
            // Act
            var result = await _controller.GetTopSellingProducts(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<TopSellingProductDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            var list = response.Data!;

            Assert.Equal(2, list.Count); // Sadece satışı olan ürünler gelir (Laptop ve Mouse)

            // Laptop: Order1(1 adet) + Order2(2 adet) = 3 adet satıldı
            // Mouse: Order1(1 adet) = 1 adet satıldı
            
            // İlk sırada Laptop olmalı
            Assert.Equal("Laptop", list[0].ProductName);
            Assert.Equal(3, list[0].TotalQuantitySold);
            Assert.Equal("TechCorp", list[0].Brand);

            // İkinci sırada Mouse olmalı
            Assert.Equal("Mouse", list[1].ProductName);
            Assert.Equal(1, list[1].TotalQuantitySold);
        }

        [Fact]
        public async Task GetCategorySales_ShouldGroupSalesByCategory()
        {
            // Act
            var result = await _controller.GetCategorySales();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<CategorySalesDto>>>(okResult.Value);
            
            Assert.NotNull(response!.Data);
            var list = response.Data!;

            Assert.Single(list); // Sadece "Electronics" kategorisi var
            var electronics = list[0];

            Assert.Equal("Electronics", electronics.CategoryName);
            // Toplam Satış Miktarı: 3 Laptop + 1 Mouse = 4
            Assert.Equal(4, electronics.TotalQuantitySold);
            // Toplam Gelir: (3 * 1000) + (1 * 50) = 3050
            Assert.Equal(3050, electronics.TotalRevenue);
        }

        [Fact]
        public async Task GetSalesTrend_ShouldReturnDataGroupedByDate()
        {
            // Act
            // Tarih aralığını geniş tutuyoruz ki seed datalar gelsin
            var result = await _controller.GetSalesTrend(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(1));

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<SalesTrendDto>>>(okResult.Value);
            
            Assert.NotNull(response!.Data);
            var trend = response.Data!;

            // Seed verisinde 2 farklı tarihte sipariş var (Dün ve Bugün)
            Assert.Equal(2, trend.Count);

            // İlk kayıt (Dün)
            Assert.Equal(1, trend[0].OrderCount);
            Assert.Equal(1050, trend[0].Revenue);

            // İkinci kayıt (Bugün)
            Assert.Equal(1, trend[1].OrderCount);
            Assert.Equal(2000, trend[1].Revenue); // Pending siparişler de SalesTrend'e dahil ediliyor kodda
        }

        [Fact]
        public async Task GetRevenueAnalytics_ShouldCalculateNetRevenueCorrectly()
        {
            // Act
            var result = await _controller.GetRevenueAnalytics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<RevenueAnalyticsDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            var analytics = response.Data!;

            // TotalRevenue (Completed Purchases): 1050
            Assert.Equal(1050, analytics.TotalRevenue);

            // RefundedAmount (Completed Refunds): 50
            Assert.Equal(50, analytics.RefundedAmount);

            // NetRevenue: 1050 - 50 = 1000
            Assert.Equal(1000, analytics.NetRevenue);

            // TotalTransactions: 1 Purchase + 1 Purchase(Pending) + 1 Refund = 3
            // Not: Controller kodunda Transaction count için Status filtresi yok, sadece tarih var.
            Assert.Equal(3, analytics.TotalTransactions);
        }

        [Fact]
        public async Task GetDashboardOverview_ShouldReturn500_OnDatabaseException()
        {
            // Arrange
            // Context'i dispose ederek hata fırlatmasını sağlıyoruz
            _context.Dispose();

            // Act
            var result = await _controller.GetDashboardOverview();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var response = Assert.IsType<ApiResponse<DashboardOverviewDto>>(statusCodeResult.Value);
            Assert.False(response!.Success);
            Assert.Equal("Failed to fetch dashboard overview", response.Message);
        }
    }
}