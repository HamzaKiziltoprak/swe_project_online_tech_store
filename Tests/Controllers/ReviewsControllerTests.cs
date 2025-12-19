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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class ReviewsControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<ReviewsController>> _mockLogger;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            // Test ortamı için hafızada çalışan bir veritabanı oluşturuyoruz.
            // Her test yeni bir veritabanı ile başlıyor, böylece testler birbirini etkilemiyor.
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            // UserManager yapısı dış servislere bağlı olduğu için mock'lanıyor.
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Logger mock'lanıyor çünkü gerçek log yazma işlemine ihtiyacımız yok.
            _mockLogger = new Mock<ILogger<ReviewsController>>();

            // Controller test için gerçek bağımlılıklarla değil, mock ve in memory db ile oluşturuluyor.
            _controller = new ReviewsController(
                _context,
                _mockUserManager.Object,
                _mockLogger.Object
            );
        }

        // Ürün yorumlarını çeken fonksiyonun, ürün varsa doğru sonuç döndürüp döndürmediğini test eder.
        [Fact]
        public async Task GetProductReviews_ShouldReturnReviews_WhenProductExists()
        {
            // Test verisi olarak ürün ve iki yorum ekliyoruz.
            var product = new Product { ProductID = 1, ProductName = "Phone", Price = 100, Stock=10, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);
            
            _context.ProductReviews.Add(new ProductReview { ReviewID = 1, ProductID = 1, UserID = 1, Rating = 5, Comment = "Good", ReviewDate = DateTime.UtcNow });
            _context.ProductReviews.Add(new ProductReview { ReviewID = 2, ProductID = 1, UserID = 2, Rating = 3, Comment = "Okay", ReviewDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var filter = new ReviewFilterParams();

            // Controller üzerinden yorumları çekiyoruz.
            var result = await _controller.GetProductReviews(1, filter);

            // Dönen cevabın başarılı olup olmadığını ve 2 yorum içerip içermediğini kontrol ediyoruz.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedReviewResult>>(actionResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.TotalCount);
        }

        // Rating filtresi çalışıyor mu test ediyoruz.
        [Fact]
        public async Task GetProductReviews_ShouldFilterByRating()
        {
            // Yorumun bir kullanıcıya ait olması gerektiği için kullanıcı ekliyoruz.
            var user = new User { Id = 1, UserName = "User1", FirstName = "F", LastName = "L", Email = "e@m.com" };
            _context.Users.Add(user);

            // Ürün oluşturuyoruz.
            var product = new Product { ProductID = 1, ProductName = "Phone", Price=10, Stock=1, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);
            
            // Aynı kullanıcı tarafından verilen iki farklı rating ekleniyor.
            _context.ProductReviews.Add(new ProductReview { ReviewID = 1, ProductID = 1, UserID = 1, Rating = 5, Comment="Test" });
            _context.ProductReviews.Add(new ProductReview { ReviewID = 2, ProductID = 1, UserID = 1, Rating = 1, Comment="Test" });
            await _context.SaveChangesAsync();

            var filter = new ReviewFilterParams { Rating = 5 };

            // Rating filtresi uygulanmış şekilde yorumlar çekiliyor.
            var result = await _controller.GetProductReviews(1, filter);

            // Sadece rating değeri 5 olan yorum dönmeli.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedReviewResult>>(actionResult.Value);
            
            Assert.Single(apiResponse.Data.Reviews);
            Assert.Equal(5, apiResponse.Data.Reviews[0].Rating);
        }

        // Bir kullanıcı satın aldığı ürüne yorum yapabilir mi test ediyoruz.
        [Fact]
        public async Task CreateReview_ShouldReturnCreated_WhenUserHasPurchased()
        {
            // Sisteme giriş yapmış kullanıcıyı simüle ediyoruz.
            var userId = 1;
            var user = new User { Id = userId, UserName = "Reviewer" };
            SetupHttpContextWithUser(userId);
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Test ürünü oluşturuyoruz.
            var product = new Product { ProductID = 10, ProductName = "Item", Price=10, Stock=1, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);

            // Kullanıcının ürünü satın aldığı bir sipariş ekliyoruz.
            var order = new Order { OrderID = 100, UserID = userId, Status = "Delivered", TotalAmount=10, ShippingAddress="Addr" };
            _context.Orders.Add(order);
            _context.OrderItems.Add(new OrderItem { OrderItemID = 1, OrderID = 100, ProductID = 10, Quantity = 1, UnitPrice=10 });
            
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Yeni yorum DTO'su oluşturuyoruz.
            var dto = new CreateReviewDto { Rating = 5, ReviewText = "Amazing!" };

            // Yorum oluşturma isteği gönderiliyor.
            var result = await _controller.CreateReview(10, dto);

            // Yorumun başarıyla oluştuğunu ve kullanıcının ürünü gerçekten satın aldığı için verified olduğunu kontrol ediyoruz.
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.True(apiResponse.Data.IsVerifiedPurchase);
        }

        // Aynı kullanıcı aynı ürüne ikinci kez yorum atınca hata dönmesi bekleniyor.
        [Fact]
        public async Task CreateReview_ShouldReturnBadRequest_WhenAlreadyReviewed()
        {
            // Giriş yapan kullanıcıyı ayarlıyoruz.
            var userId = 2;
            var user = new User { Id = userId };
            SetupHttpContextWithUser(userId);
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Ürün ekleniyor.
            var product = new Product { ProductID = 20, ProductName="P", Price=10, Stock=1, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);
            
            // Daha önce atılmış yorum ekleniyor.
            _context.ProductReviews.Add(new ProductReview { ReviewID = 1, ProductID = 20, UserID = userId, Rating = 4, Comment = "Test" });
            await _context.SaveChangesAsync();

            var dto = new CreateReviewDto { Rating = 5 };

            // Yeni yorum denemesi yapılıyor.
            var result = await _controller.CreateReview(20, dto);

            // BadRequest bekleniyor çünkü kullanıcı zaten yorum yapmış.
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            
            Assert.Contains("already reviewed", apiResponse.Message);
        }

        // Kullanıcı kendi yorumunu güncelleyebiliyor mu test ediyoruz.
        [Fact]
        public async Task UpdateReview_ShouldReturnOk_WhenUserOwnsReview()
        {
            // Kullanıcı login ediliyor.
            var userId = 3;
            SetupHttpContextWithUser(userId);

            // Kullanıcı ve ürün test için ekleniyor.
            var user = new User { Id = userId, UserName = "Reviewer", FirstName = "F", LastName = "L", Email = "e@m.com" };
            _context.Users.Add(user);
            
            var product = new Product { ProductID = 1, ProductName = "P", Price=10, Stock=1, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);

            // Kullanıcıya ait bir yorum ekliyoruz.
            var review = new ProductReview { ReviewID = 50, ProductID = 1, UserID = userId, Rating = 3, Comment = "Old" };
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Yeni içerik DTO'su
            var dto = new UpdateReviewDto { Rating = 5, ReviewText = "New" };

            // Yorum güncelleniyor.
            var result = await _controller.UpdateReview(1, 50, dto);

            // Yeni yorumun doğru şekilde güncellenip güncellenmediğini test ediyoruz.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            
            Assert.Equal("New", apiResponse.Data.ReviewText);
            Assert.Equal(5, apiResponse.Data.Rating);
        }

        // Kullanıcı kendine ait olmayan bir yorumu güncellemeye çalışırsa hata dönmeli.
        [Fact]
        public async Task UpdateReview_ShouldReturnForbid_WhenUserDoesNotOwnReview()
        {
            // Giriş yapan kullanıcı id’si
            var userId = 4;
            var otherUserId = 5;
            SetupHttpContextWithUser(userId);

            // Sisteme başka bir kullanıcı ekliyoruz (yorum onun)
            var otherUser = new User { Id = otherUserId, UserName = "Other", FirstName = "F", LastName = "L", Email = "e@m.com" };
            _context.Users.Add(otherUser);

            var product = new Product { ProductID = 1, ProductName = "P", Price=10, Stock=1, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);

            // Yorum başka kullanıcıya ait
            var review = new ProductReview { ReviewID = 60, ProductID = 1, UserID = otherUserId, Rating = 3, Comment = "Test" }; 
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            var dto = new UpdateReviewDto { Rating = 5 };

            // Giriş yapan kullanıcı başkasının yorumunu güncellemeye çalışıyor.
            var result = await _controller.UpdateReview(1, 60, dto);

            // Yetkisiz işlem olduğu için Forbid dönmesi bekleniyor.
            Assert.IsType<ForbidResult>(result.Result);
        }

        // Bir çalışan yorum onayladığında çalışma mantığının doğruluğunu test eder.
        [Fact]
        public async Task ApproveReview_ShouldReturnOk_WhenUserIsEmployee()
        {
            // Test için gerekli temel verileri oluşturuyoruz.
            var reviewId = 70;
            var productId = 1;
            var userId = 10;

            var user = new User { Id = userId, UserName = "Reviewer", FirstName = "F", LastName = "L", Email = "e@m.com" };
            _context.Users.Add(user);

            var product = new Product { ProductID = productId, ProductName = "P", Price=10, Stock=1, BrandID=1, Description="D", ImageUrl="I", IsActive=true };
            _context.Products.Add(product);

            var review = new ProductReview { ReviewID = reviewId, ProductID = productId, UserID = userId, IsApproved = false, Comment = "Test" };
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Yorumun onaylanması isteniyor.
            var result = await _controller.ApproveReview(productId, reviewId);

            // Yorum onaylandı mı kontrol ediyoruz.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            
            Assert.True(apiResponse.Data.IsVerifiedPurchase);

            // Veritabanında gerçekten onaylı hale gelip gelmediğini doğruluyoruz.
            var dbReview = await _context.ProductReviews.FindAsync(reviewId);
            Assert.True(dbReview.IsApproved);
        }

        [Fact]
        public async Task RejectReview_ShouldDeleteReview()
        {
            // Arrange
            var productId = 1;
            var reviewId = 1;
            var userId = 1;

            var user = new User { Id = userId, UserName = "testuser", FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);

            var product = new Product { ProductID = productId, ProductName = "Test Product", Price = 100, Stock = 10, BrandID = 1, Description = "Test", ImageUrl = "test.jpg" };
            _context.Products.Add(product);

            var review = new ProductReview { ReviewID = reviewId, ProductID = productId, UserID = userId, IsApproved = false, Comment = "Test" };
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.RejectReview(productId, reviewId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(actionResult.Value);
            Assert.True(apiResponse.Success);

            // Veritabanından silindiğini kontrol et
            var dbReview = await _context.ProductReviews.FindAsync(reviewId);
            Assert.Null(dbReview);
        }

        [Fact]
        public async Task RejectReview_ShouldReturnNotFound_WhenReviewDoesNotExist()
        {
            // Arrange
            var productId = 1;
            var reviewId = 999;

            // Act
            var result = await _controller.RejectReview(productId, reviewId);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(actionResult.Value);
            Assert.False(apiResponse.Success);
        }

        [Fact]
        public async Task GetPendingReviews_ShouldReturnOnlyUnapprovedReviews()
        {
            // Arrange
            var userId = 1;
            var productId = 1;

            var user = new User { Id = userId, UserName = "testuser", FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);

            var product = new Product { ProductID = productId, ProductName = "Test Product", Price = 100, Stock = 10, BrandID = 1, Description = "Test", ImageUrl = "test.jpg" };
            _context.Products.Add(product);

            var approvedReview = new ProductReview { ReviewID = 1, ProductID = productId, UserID = userId, IsApproved = true, Comment = "Approved", Rating = 5 };
            var pendingReview1 = new ProductReview { ReviewID = 2, ProductID = productId, UserID = userId, IsApproved = false, Comment = "Pending 1", Rating = 4 };
            var pendingReview2 = new ProductReview { ReviewID = 3, ProductID = productId, UserID = userId, IsApproved = false, Comment = "Pending 2", Rating = 3 };

            _context.ProductReviews.AddRange(approvedReview, pendingReview1, pendingReview2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPendingReviews();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ReviewDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count); // Sadece 2 bekleyen review olmalı
            Assert.All(apiResponse.Data, r => Assert.False(r.IsVerifiedPurchase)); // Hepsi onaysız olmalı
        }

        [Fact]
        public async Task GetPendingReviews_ShouldReturnEmptyList_WhenNoPendingReviews()
        {
            // Arrange
            var userId = 1;
            var productId = 1;

            var user = new User { Id = userId, UserName = "testuser", FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);

            var product = new Product { ProductID = productId, ProductName = "Test Product", Price = 100, Stock = 10, BrandID = 1, Description = "Test", ImageUrl = "test.jpg" };
            _context.Products.Add(product);

            var approvedReview = new ProductReview { ReviewID = 1, ProductID = productId, UserID = userId, IsApproved = true, Comment = "Approved", Rating = 5 };
            _context.ProductReviews.Add(approvedReview);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPendingReviews();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ReviewDto>>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Empty(apiResponse.Data); // Bekleyen review yok
        }

        // Test içinde bir kullanıcıyı login etmiş gibi göstermek için kullanılan yardımcı fonksiyon.
        private void SetupHttpContextWithUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()) 
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }
    }
}
