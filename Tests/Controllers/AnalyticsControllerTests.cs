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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class AnalyticsControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<AnalyticsController>> _mockLogger;
        private readonly AnalyticsController _controller;

        public AnalyticsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            _mockLogger = new Mock<ILogger<AnalyticsController>>();

            _controller = new AnalyticsController(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task GetDashboardOverview_ShouldReturnCorrectMetrics()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var user1 = new User { Id = 1, UserName = "user1", FirstName = "User", LastName = "One", Email = "user1@test.com" };
            var user2 = new User { Id = 2, UserName = "user2", FirstName = "User", LastName = "Two", Email = "user2@test.com" };
            _context.Users.AddRange(user1, user2);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Product 1",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 50,
                CriticalStockLevel = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Product 2",
                Brand = "Brand B",
                Description = "Description",
                Price = 200,
                Stock = 5,
                CriticalStockLevel = 10,
                ImageUrl = "test2.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.AddRange(product1, product2);

            var order1 = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 300,
                Status = "Completed",
                OrderDate = DateTime.UtcNow,
                ShippingAddress = "Test Address 1"
            };

            var order2 = new Order
            {
                OrderID = 2,
                UserID = 2,
                TotalAmount = 200,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                ShippingAddress = "Test Address 2"
            };

            _context.Orders.AddRange(order1, order2);

            var transaction1 = new Transaction
            {
                TransactionID = 1,
                OrderID = 1,
                UserID = 1,
                Amount = 300,
                TransactionType = "Purchase",
                Status = "Completed",
                TransactionDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction1);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDashboardOverview();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<DashboardOverviewDto>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(300, apiResponse.Data.TotalRevenue);
            Assert.Equal(2, apiResponse.Data.TotalOrders);
            Assert.Equal(2, apiResponse.Data.TotalCustomers);
            Assert.Equal(2, apiResponse.Data.TotalProducts);
            Assert.Equal(1, apiResponse.Data.LowStockProductsCount); // Product 2 has low stock
            Assert.Equal(1, apiResponse.Data.PendingOrdersCount);
            Assert.Equal(150, apiResponse.Data.AverageOrderValue); // 300 / 2
        }

        [Fact]
        public async Task GetTopSellingProducts_ShouldReturnCorrectRanking()
        {
            // Arrange
            var category = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(category);

            var user = new User { Id = 1, UserName = "user1", FirstName = "User", LastName = "One", Email = "user1@test.com" };
            _context.Users.Add(user);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Best Seller",
                Brand = "Brand A",
                Description = "Description",
                Price = 100,
                Stock = 50,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Average Seller",
                Brand = "Brand B",
                Description = "Description",
                Price = 200,
                Stock = 30,
                ImageUrl = "test2.jpg",
                CategoryID = 1,
                IsActive = true
            };

            _context.Products.AddRange(product1, product2);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 800,
                Status = "Completed",
                OrderDate = DateTime.UtcNow,
                ShippingAddress = "Test Address"
            };

            _context.Orders.Add(order);

            var orderItem1 = new OrderItem
            {
                OrderItemID = 1,
                OrderID = 1,
                ProductID = 1,
                Quantity = 5,
                UnitPrice = 100
            };

            var orderItem2 = new OrderItem
            {
                OrderItemID = 2,
                OrderID = 1,
                ProductID = 2,
                Quantity = 2,
                UnitPrice = 200
            };

            _context.OrderItems.AddRange(orderItem1, orderItem2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetTopSellingProducts(10);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<TopSellingProductDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count);
            Assert.Equal("Best Seller", apiResponse.Data[0].ProductName); // Top seller
            Assert.Equal(5, apiResponse.Data[0].TotalQuantitySold);
            Assert.Equal(500, apiResponse.Data[0].TotalRevenue);
        }

        [Fact]
        public async Task GetCategorySales_ShouldReturnSalesDistribution()
        {
            // Arrange
            var category1 = new Category { CategoryID = 1, CategoryName = "Electronics" };
            var category2 = new Category { CategoryID = 2, CategoryName = "Clothing" };
            _context.Categories.AddRange(category1, category2);

            var user = new User { Id = 1, UserName = "user1", FirstName = "User", LastName = "One", Email = "user1@test.com" };
            _context.Users.Add(user);

            var product1 = new Product
            {
                ProductID = 1,
                ProductName = "Laptop",
                Brand = "Brand A",
                Description = "Description",
                Price = 1000,
                Stock = 10,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };

            var product2 = new Product
            {
                ProductID = 2,
                ProductName = "Shirt",
                Brand = "Brand B",
                Description = "Description",
                Price = 50,
                Stock = 100,
                ImageUrl = "test2.jpg",
                CategoryID = 2,
                IsActive = true
            };

            _context.Products.AddRange(product1, product2);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 1100,
                Status = "Completed",
                OrderDate = DateTime.UtcNow,
                ShippingAddress = "Test Address"
            };

            _context.Orders.Add(order);

            var orderItem1 = new OrderItem
            {
                OrderItemID = 1,
                OrderID = 1,
                ProductID = 1,
                Quantity = 1,
                UnitPrice = 1000
            };

            var orderItem2 = new OrderItem
            {
                OrderItemID = 2,
                OrderID = 1,
                ProductID = 2,
                Quantity = 2,
                UnitPrice = 50
            };

            _context.OrderItems.AddRange(orderItem1, orderItem2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetCategorySales();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<CategorySalesDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count);
            
            // Electronics should be first (higher revenue)
            Assert.Equal("Electronics", apiResponse.Data[0].CategoryName);
            Assert.Equal(1000, apiResponse.Data[0].TotalRevenue);
            Assert.Equal(1, apiResponse.Data[0].TotalQuantitySold);
        }

        [Fact]
        public async Task GetSalesTrend_ShouldReturnDailyData()
        {
            // Arrange
            var user = new User { Id = 1, UserName = "user1", FirstName = "User", LastName = "One", Email = "user1@test.com" };
            _context.Users.Add(user);

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var order1 = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 100,
                Status = "Completed",
                OrderDate = today,
                ShippingAddress = "Test Address 1"
            };

            var order2 = new Order
            {
                OrderID = 2,
                UserID = 1,
                TotalAmount = 200,
                Status = "Completed",
                OrderDate = today,
                ShippingAddress = "Test Address 2"
            };

            var order3 = new Order
            {
                OrderID = 3,
                UserID = 1,
                TotalAmount = 150,
                Status = "Completed",
                OrderDate = yesterday,
                ShippingAddress = "Test Address 3"
            };

            _context.Orders.AddRange(order1, order2, order3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSalesTrend(yesterday, today);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<SalesTrendDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count); // 2 days
            
            var todayData = apiResponse.Data.FirstOrDefault(d => d.Date.Date == today);
            Assert.NotNull(todayData);
            Assert.Equal(2, todayData.OrderCount);
            Assert.Equal(300, todayData.Revenue);
        }

        [Fact]
        public async Task GetRevenueAnalytics_ShouldCalculateNetRevenue()
        {
            // Arrange
            var testDate = DateTime.UtcNow.Date; // Use consistent date
            var user = new User { Id = 1, UserName = "user1", FirstName = "User", LastName = "One", Email = "user1@test.com" };
            _context.Users.Add(user);

            var category = new Category { CategoryID = 1, CategoryName = "Test Category" };
            _context.Categories.Add(category);

            var product = new Product
            {
                ProductID = 1,
                ProductName = "Test Product",
                Brand = "Test Brand",
                Description = "Description",
                Price = 500,
                Stock = 10,
                CriticalStockLevel = 5,
                ImageUrl = "test.jpg",
                CategoryID = 1,
                IsActive = true
            };
            _context.Products.Add(product);

            var order = new Order
            {
                OrderID = 1,
                UserID = 1,
                TotalAmount = 500,
                Status = "Completed",
                OrderDate = testDate,
                ShippingAddress = "Test Address"
            };

            _context.Orders.Add(order);

            // Add OrderItem for dailyTrend calculation
            var orderItem = new OrderItem
            {
                OrderItemID = 1,
                OrderID = 1,
                ProductID = 1,
                Quantity = 1,
                UnitPrice = 500
            };
            _context.OrderItems.Add(orderItem);

            await _context.SaveChangesAsync(); // Save Order and OrderItem first

            var purchaseTransaction = new Transaction
            {
                OrderID = 1,
                UserID = 1,
                Amount = 500,
                TransactionType = "Purchase",
                Status = "Completed",
                TransactionDate = testDate
            };

            var refundTransaction = new Transaction
            {
                OrderID = 1,
                UserID = 1,
                Amount = 100,
                TransactionType = "Refund",
                Status = "Completed",
                TransactionDate = testDate
            };

            _context.Transactions.AddRange(purchaseTransaction, refundTransaction);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetRevenueAnalytics(
                testDate.AddDays(-1), 
                testDate.AddDays(1));

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<RevenueAnalyticsDto>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(500, apiResponse.Data.TotalRevenue);
            Assert.Equal(100, apiResponse.Data.RefundedAmount);
            Assert.Equal(400, apiResponse.Data.NetRevenue); // 500 - 100
            Assert.Equal(2, apiResponse.Data.TotalTransactions);
        }

        [Fact]
        public async Task GetDashboardOverview_ShouldReturnZeroWhenNoData()
        {
            // Act
            var result = await _controller.GetDashboardOverview();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<DashboardOverviewDto>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(0, apiResponse.Data.TotalRevenue);
            Assert.Equal(0, apiResponse.Data.TotalOrders);
            Assert.Equal(0, apiResponse.Data.TotalCustomers);
        }
    }
}
