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
    public class ReviewsControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<ReviewsController>> _mockLogger;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _mockLogger = new Mock<ILogger<ReviewsController>>();
            _controller = new ReviewsController(_context, _mockUserManager.Object, _mockLogger.Object);
        }

        private void MockUserLogin(int userId, string role = "Customer")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "TestUser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var user = CreateValidUser(userId);
            if (!_context.Users.Any(u => u.Id == userId))
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
        }

        private User CreateValidUser(int id)
        {
            return new User
            {
                Id = id,
                UserName = $"User{id}",
                Email = $"user{id}@test.com",
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true
            };
        }

        private Product CreateValidProduct(int id)
        {
            return new Product
            {
                ProductID = id,
                ProductName = $"Product {id}",
                Description = "Test Description",
                ImageUrl = "http://example.com/image.jpg",
                Price = 100,
                Stock = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Brand = new Brand { BrandID = 1, BrandName = "TestBrand", Description = "Desc", LogoUrl = "url" },
                Category = new Category { CategoryID = 1, CategoryName = "TestCat" } 
            };
        }

        private ProductReview CreateValidReview(int reviewId, int productId, int userId, int rating, bool isApproved)
        {
            return new ProductReview
            {
                ReviewID = reviewId,
                ProductID = productId,
                UserID = userId,
                Rating = rating,
                Comment = "Valid comment",
                IsApproved = isApproved,
                ReviewDate = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetProductReviews_ShouldReturnOk_WhenProductExists()
        {
            var productId = 1;
            var userId = 10;
            
            _context.Users.Add(CreateValidUser(userId));
            _context.Products.Add(CreateValidProduct(productId));
            _context.ProductReviews.Add(CreateValidReview(1, productId, userId, 5, true));
            _context.ProductReviews.Add(CreateValidReview(2, productId, userId, 1, false));
            await _context.SaveChangesAsync();

            var result = await _controller.GetProductReviews(productId, new ReviewFilterParams { PageNumber = 1, PageSize = 10 });

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedReviewResult>>(actionResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(1, apiResponse.Data!.TotalCount);
        }

        [Fact]
        public async Task GetProductReviews_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            var result = await _controller.GetProductReviews(999, new ReviewFilterParams());

            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedReviewResult>>(actionResult.Value);
            Assert.False(apiResponse.Success);
        }

        [Fact]
        public async Task GetProductReviewSummary_ShouldReturnCorrectStats()
        {
            var productId = 1;
            _context.Products.Add(CreateValidProduct(productId));
            _context.Users.AddRange(CreateValidUser(10), CreateValidUser(11), CreateValidUser(12));

            _context.ProductReviews.AddRange(
                CreateValidReview(1, productId, 10, 5, true),
                CreateValidReview(2, productId, 11, 3, true),
                CreateValidReview(3, productId, 12, 1, false)
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetProductReviewSummary(productId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductReviewSummaryDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(4, apiResponse.Data!.AverageRating);
            Assert.Equal(2, apiResponse.Data!.TotalReviews);
        }

        [Fact]
        public async Task CreateReview_ShouldCreateReview_WhenValid()
        {
            var productId = 1;
            var userId = 10;
            MockUserLogin(userId);

            _context.Products.Add(CreateValidProduct(productId));
            await _context.SaveChangesAsync();

            var dto = new CreateReviewDto { Rating = 5, ReviewText = "New Review" };

            var result = await _controller.CreateReview(productId, dto);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(5, apiResponse.Data!.Rating);
            
            var dbReview = await _context.ProductReviews.FirstOrDefaultAsync();
            Assert.NotNull(dbReview);
            Assert.False(dbReview!.IsApproved);
        }

        [Fact]
        public async Task CreateReview_ShouldReturnBadRequest_WhenAlreadyReviewed()
        {
            var productId = 1;
            var userId = 10;
            MockUserLogin(userId);

            _context.Products.Add(CreateValidProduct(productId));
            _context.ProductReviews.Add(CreateValidReview(1, productId, userId, 5, true));
            await _context.SaveChangesAsync();

            var dto = new CreateReviewDto { Rating = 4, ReviewText = "Spam" };

            var result = await _controller.CreateReview(productId, dto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            Assert.Equal("You have already reviewed this product", apiResponse.Message);
        }

        [Fact]
        public async Task UpdateReview_ShouldUpdate_WhenUserIsOwner()
        {
            var productId = 1;
            var reviewId = 100;
            var userId = 10;
            MockUserLogin(userId);

            _context.Products.Add(CreateValidProduct(productId));
            _context.ProductReviews.Add(new ProductReview 
            { 
                ReviewID = reviewId, 
                ProductID = productId, 
                UserID = userId, 
                Rating = 3, 
                Comment = "Old",
                IsApproved = true,
                ReviewDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var dto = new UpdateReviewDto { Rating = 5, ReviewText = "New" };

            var result = await _controller.UpdateReview(productId, reviewId, dto);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(5, apiResponse.Data!.Rating);
            Assert.Equal("New", apiResponse.Data!.ReviewText);
        }

        [Fact]
        public async Task UpdateReview_ShouldReturnForbid_WhenUserIsNotOwner()
        {
            var productId = 1;
            var reviewId = 100;
            var userId = 10;
            var otherUserId = 20;
            
            MockUserLogin(userId);
            _context.Users.Add(CreateValidUser(otherUserId));
            _context.Products.Add(CreateValidProduct(productId));

            _context.ProductReviews.Add(CreateValidReview(reviewId, productId, otherUserId, 3, true));
            await _context.SaveChangesAsync();

            var dto = new UpdateReviewDto { Rating = 5, ReviewText = "Hack" };

            var result = await _controller.UpdateReview(productId, reviewId, dto);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task DeleteReview_ShouldDelete_WhenUserIsOwner()
        {
            var productId = 1;
            var reviewId = 100;
            var userId = 10;
            MockUserLogin(userId);
            
            _context.Products.Add(CreateValidProduct(productId));
            _context.ProductReviews.Add(CreateValidReview(reviewId, productId, userId, 5, true));
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteReview(productId, reviewId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(actionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Null(await _context.ProductReviews.FindAsync(reviewId));
        }

        [Fact]
        public async Task GetMyReviews_ShouldReturnOnlyUserReviews()
        {
            var userId = 10;
            var otherUserId = 99;
            MockUserLogin(userId);
            _context.Users.Add(CreateValidUser(otherUserId));
            _context.Products.Add(CreateValidProduct(1));
            
            _context.ProductReviews.AddRange(
                CreateValidReview(1, 1, userId, 5, true),
                CreateValidReview(2, 1, otherUserId, 1, true)
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetMyReviews(new ReviewFilterParams());

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedReviewResult>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(1, apiResponse.Data!.TotalCount);
            Assert.Equal(5, apiResponse.Data!.Reviews.First().Rating);
        }

        [Fact]
        public async Task ApproveReview_ShouldApprove_WhenAdmin()
        {
            var productId = 1;
            var reviewId = 100;
            var userId = 10;
            
            MockUserLogin(1, "Admin");
            
            _context.Users.Add(CreateValidUser(userId));
            _context.Products.Add(CreateValidProduct(productId));
            _context.ProductReviews.Add(CreateValidReview(reviewId, productId, userId, 4, false));
            await _context.SaveChangesAsync();

            var result = await _controller.ApproveReview(productId, reviewId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReviewDto>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.True(apiResponse.Data!.IsVerifiedPurchase);

            var dbReview = await _context.ProductReviews.FindAsync(reviewId);
            Assert.NotNull(dbReview);
            Assert.True(dbReview!.IsApproved);
        }

        [Fact]
        public async Task RejectReview_ShouldDeleteReview_WhenAdmin()
        {
            var productId = 1;
            var reviewId = 100;
            var userId = 10;
            MockUserLogin(1, "Admin");
            
            _context.Users.Add(CreateValidUser(userId));
            _context.Products.Add(CreateValidProduct(productId));
            _context.ProductReviews.Add(CreateValidReview(reviewId, productId, userId, 4, false));
            await _context.SaveChangesAsync();

            var result = await _controller.RejectReview(productId, reviewId);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(actionResult.Value);
            Assert.Equal("Review başarıyla reddedildi", apiResponse.Message);
            Assert.Null(await _context.ProductReviews.FindAsync(reviewId));
        }

        [Fact]
        public async Task GetPendingReviews_ShouldReturnOnlyUnapproved()
        {
            MockUserLogin(1, "Admin");
            _context.Products.Add(CreateValidProduct(1));
            _context.Users.Add(CreateValidUser(10));
            _context.Users.Add(CreateValidUser(11));
            
            _context.ProductReviews.AddRange(
                CreateValidReview(1, 1, 10, 5, false),
                CreateValidReview(2, 1, 11, 4, true)
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetPendingReviews();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ReviewDto>>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Single(apiResponse.Data!);
            Assert.Equal(1, apiResponse.Data!.First().ProductReviewID);
        }

        [Fact]
        public async Task GetLatestReviews_ShouldReturnLatestApproved()
        {
            MockUserLogin(1, "Admin");
            _context.Products.Add(CreateValidProduct(1));
            _context.Users.AddRange(CreateValidUser(10), CreateValidUser(11), CreateValidUser(12));
            
            var r1 = CreateValidReview(1, 1, 10, 5, true);
            r1.ReviewDate = DateTime.UtcNow.AddDays(-1);
            
            var r2 = CreateValidReview(2, 1, 11, 4, true);
            r2.ReviewDate = DateTime.UtcNow;
            
            var r3 = CreateValidReview(3, 1, 12, 1, false);
            
            _context.ProductReviews.AddRange(r1, r2, r3);
            await _context.SaveChangesAsync();

            var result = await _controller.GetLatestReviews(10);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ReviewDto>>>(actionResult.Value);
            
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(2, apiResponse.Data!.Count);
            Assert.Equal(2, apiResponse.Data!.First().ProductReviewID);
        }
    }
}