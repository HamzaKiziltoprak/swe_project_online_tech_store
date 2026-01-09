using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Tests.Controllers
{
    public class AccountsControllerTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<AccountsController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AccountsController _controller;
        
        // GetMyReviews testi için gerekli DbContext
        private readonly DataContext _dbContext;

        public AccountsControllerTests()
        {
            // Nullable uyarılarını gidermek için store parametreleri mocklandı
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null!, null!, null!, null!);

            var roleStore = new Mock<IRoleStore<Role>>();
            _mockRoleManager = new Mock<RoleManager<Role>>(
                roleStore.Object, null!, null!, null!, null!);

            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<AccountsController>>();
            
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("SuperSecretKeyForTestingTheJwtTokensGeneratedByTheApp!");
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("http://localhost");
            _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("http://localhost");
            _mockConfiguration.Setup(x => x["AppUrl"]).Returns("http://localhost:3000");

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new DataContext(options);

            _controller = new AccountsController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockRoleManager.Object,
                _mockEmailService.Object,
                _mockLogger.Object,
                _mockConfiguration.Object
            );
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        #region Login Tests

        [Fact]
        public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
        {
            var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };
            var user = new User { Id = 1, Email = loginDto.Email, FirstName = "Test", LastName = "User", EmailConfirmed = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });

            var result = await _controller.Login(loginDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response!.Success);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenEmailNotConfirmed()
        {
            var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };
            var user = new User { Email = loginDto.Email, EmailConfirmed = false };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);

            var result = await _controller.Login(loginDto);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
            Assert.Contains("confirmed", response!.Message);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await _controller.Login(new LoginDto { Email = "a", Password = "b" });

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_ShouldReturnOk_WhenSuccess()
        {
            var registerDto = new RegisterDto { Email = "new@test.com", Password = "Pass", FirstName = "A", LastName = "B" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((User?)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password)).ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Customer")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>())).ReturnsAsync("token123");

            var result = await _controller.Register(registerDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserProfileDto>>(okResult.Value);
            Assert.True(response!.Success);
            _mockEmailService.Verify(x => x.SendConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Profile Tests

        [Fact]
        public async Task GetProfile_ShouldReturnOk_WithUserData()
        {
            SetupHttpContextWithUser(99);
            var user = new User { Id = 99, Email = "me@test.com" };

            _mockUserManager.Setup(x => x.FindByIdAsync("99")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            var result = await _controller.GetProfile();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserProfileDto>>(okResult.Value);
            Assert.Equal(99, response!.Data!.Id);
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnOk_WhenUpdateSuccess()
        {
            SetupHttpContextWithUser(99);
            var user = new User { Id = 99, FirstName = "Old" };
            var updateDto = new UpdateProfileDto { FirstName = "New", LastName = "L", Address = "A" };

            _mockUserManager.Setup(x => x.FindByIdAsync("99")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            var result = await _controller.UpdateProfile(updateDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserProfileDto>>(okResult.Value);
            Assert.Equal("New", response!.Data!.FirstName);
        }

        #endregion

        #region Password Change & Reset Tests

        [Fact]
        public async Task ChangePassword_ShouldReturnOk_WhenSuccess()
        {
            SetupHttpContextWithUser(5);
            var user = new User { Id = 5 };
            var dto = new ChangePasswordDto { CurrentPassword = "Old", NewPassword = "New" };

            _mockUserManager.Setup(x => x.FindByIdAsync("5")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ChangePassword(dto);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldSendEmail_WhenUserExists()
        {
            var email = "forgot@test.com";
            var user = new User { Id = 1, Email = email, EmailConfirmed = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("resetToken");

            var result = await _controller.ForgotPassword(email);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnOk_WhenTokenIsValid()
        {
            var dto = new ResetPasswordDto { UserId = 1, Token = "encodedToken", NewPassword = "NewPass" };
            var user = new User { Id = 1 };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetPassword(dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(response!.Success);
        }

        #endregion

        #region Admin Role & User Tests

        [Fact]
        public async Task GetAllUsers_ShouldReturnList()
        {
            var usersList = new List<User> { new User { Id = 1 }, new User { Id = 2 } }.AsQueryable();
            
            _mockUserManager.Setup(x => x.Users).Returns(usersList);
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string>());

            var result = await _controller.GetAllUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<UserProfileDto>>>(okResult.Value);
            Assert.Equal(2, response!.Data!.Count);
        }

        [Fact]
        public async Task AssignRole_ShouldReturnOk_WhenValid()
        {
            var dto = new AssignRoleDto { UserId = 1, RoleName = "Admin" };
            var user = new User { Id = 1 };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.AssignRole(dto);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task RemoveRole_ShouldReturnOk_WhenValid()
        {
            var dto = new AssignRoleDto { UserId = 1, RoleName = "Admin" };
            var user = new User { Id = 1 };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.RemoveFromRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.RemoveRole(dto);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetAllRoles_ShouldReturnRoles()
        {
            var roles = new List<Role> { new Role { Name = "Admin" }, new Role { Name = "Customer" } }.AsQueryable();
            _mockRoleManager.Setup(x => x.Roles).Returns(roles);

            var result = _controller.GetAllRoles();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<string>>>(okResult.Value);
            Assert.Equal(2, response!.Data!.Count);
        }

        #endregion

        #region Email Confirmation Tests

        [Fact]
        public async Task ConfirmEmail_ShouldReturnOk_WhenSuccess()
        {
            var userId = 1;
            var token = "validToken";
            var user = new User { Id = 1, EmailConfirmed = false };

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ConfirmEmail(userId, token);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            // Düzeltme: Controller string'i Data olarak döndüğü için, Message "Operation successful" olabilir. 
            // Bu yüzden Data'yı kontrol ediyoruz.
            var response = (ApiResponse<string>)okResult.Value!;
            Assert.Contains("confirmed successfully", response.Data);
        }

        [Fact]
        public async Task ResendConfirmation_ShouldSendEmail_WhenNotConfirmed()
        {
            var email = "resend@test.com";
            var user = new User { Id = 1, Email = email, EmailConfirmed = false };

            _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("newToken");

            var result = await _controller.ResendConfirmationEmail(email);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockEmailService.Verify(x => x.SendConfirmationEmailAsync(email, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region My Reviews (GetMyReviews) Tests

        [Fact]
        public async Task GetMyReviews_ShouldReturnOnlyUserReviews()
        {
            // 1. InMemory DB'ye veri ekle
            var userId = 10;
            // Düzeltme: 'Required' alanlar (Description, ImageUrl) eklendi.
            var product = new Product { 
                ProductID = 1, 
                ProductName = "Test Laptop", 
                Description = "Test desc", 
                ImageUrl = "http://img.com",
                Price = 100
            };
            _dbContext.Products.Add(product);
            
            _dbContext.ProductReviews.Add(new ProductReview 
            { 
                ReviewID = 1, UserID = userId, ProductID = 1, Rating = 5, Comment = "My Review", ReviewDate = DateTime.Now, Product = product 
            });
            _dbContext.ProductReviews.Add(new ProductReview 
            { 
                ReviewID = 2, UserID = 20, ProductID = 1, Rating = 1, Comment = "Other User Review", ReviewDate = DateTime.Now, Product = product 
            });
            await _dbContext.SaveChangesAsync();

            // 2. Kullanıcıyı Authenticate et
            SetupHttpContextWithUser(userId);

            // 3. Controller'ın ServiceProvider üzerinden DB'ye erişmesini sağla (Mock ServiceProvider)
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(DataContext))).Returns(_dbContext);
            
            _controller.ControllerContext.HttpContext.RequestServices = mockServiceProvider.Object;

            // Act
            var result = await _controller.GetMyReviews();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<MyReviewDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Single(response.Data!); // Sadece 1 tane yorum gelmeli (UserID = 10 olan)
            Assert.Equal("My Review", response.Data![0].ReviewText);
        }

        [Fact]
        public async Task GetMyReviews_ShouldReturnInternalServerError_OnException()
        {
            SetupHttpContextWithUser(1);

            // Bozuk bir ServiceProvider simüle edelim (Exception fırlatan)
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(DataContext))).Throws(new Exception("DB Error"));
            
            _controller.ControllerContext.HttpContext.RequestServices = mockServiceProvider.Object;

            var result = await _controller.GetMyReviews();

            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        // Yardımcı Metot: Kullanıcıyı login olmuş gibi (ClaimsPrincipal) ayarlar
        private void SetupHttpContextWithUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext = new DefaultHttpContext 
            { 
                User = claimsPrincipal 
            };
        }
    }
}