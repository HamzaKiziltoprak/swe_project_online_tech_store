using Backend.Controllers;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Tests.Controllers
{
    public class AccountsControllerTests
    {
        // UserManager yapısını taklit etmek için oluşturduğumuz sahte nesne
        // Gerçek veritabanına gitmeden kullanıcı işlemlerini test etmemizi sağlıyor
        private readonly Mock<UserManager<User>> _mockUserManager;

        // RoleManager yapısını taklit ediyoruz
        // Rollerle ilgili testlerde gerçek role store'a ihtiyaç duymadan işlem yapmamızı sağlıyor
        private readonly Mock<RoleManager<Role>> _mockRoleManager;

        // Controller içindeki loglama işlemlerinin testte hata vermemesi için sahte logger oluşturuyoruz
        private readonly Mock<ILogger<AccountsController>> _mockLogger;

        // Testlerde kullanacağımız AccountsController örneği
        private readonly AccountsController _controller;

        public AccountsControllerTests()
        {
            // UserManager'ın ihtiyaç duyduğu user store'u taklit ediyoruz
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // RoleManager'ın ihtiyaç duyduğu role store da sahte olarak oluşturuluyor
            var roleStore = new Mock<IRoleStore<Role>>();
            _mockRoleManager = new Mock<RoleManager<Role>>(
                roleStore.Object, null, null, null, null);

            _mockLogger = new Mock<ILogger<AccountsController>>();

            // Controller'ı tüm bu sahte bağımlılıklarla kuruyoruz
            _controller = new AccountsController(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockLogger.Object
            );
        }

        // Kayıt işlemi başarılı olduğunda controller'ın doğru sonucu döndürdüğünü test ediyoruz
        [Fact]
        public async Task Register_ShouldReturnOk_WhenRegistrationIsSuccessful()
        {
            var registerDto = new RegisterDto
            {
                Email = "new@user.com",
                Password = "Password123!",
                FirstName = "Ali",
                LastName = "Veli"
            };

            // Bu email ile kullanıcı olmadığını simüle ediyoruz
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((User)null);

            // Kullanıcının başarıyla oluşturulduğunu söylüyoruz
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Customer rolünün sistemde mevcut olduğunu belirtiyoruz
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Customer"))
                .ReturnsAsync(true);

            // Kullanıcıyı role ekleme işleminin başarılı olduğunu iletiyoruz
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Register(registerDto);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<UserProfileDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("new@user.com", apiResponse.Data.Email);
        }

        // Eğer email zaten varsa kayıt işleminin hata dönmesini test ediyoruz
        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
        {
            var registerDto = new RegisterDto { Email = "existing@user.com" };

            // Bu email ile bir kullanıcının mevcut olduğunu söylüyoruz
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync(new User());

            var result = await _controller.Register(registerDto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<UserProfileDto>>(actionResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Contains("already exists", apiResponse.Message);
        }

        // Kullanıcı giriş yaptıysa profil bilgisinin doğru döndüğünü test ediyoruz
        [Fact]
        public async Task GetProfile_ShouldReturnOk_WhenUserIsAuthenticated()
        {
            var userId = 100;
            var user = new User { Id = userId, Email = "test@user.com", FirstName = "Test", LastName = "User" };

            // HttpContext içine kullanıcı kimliği yerleştiriyoruz
            SetupHttpContextWithUser(userId);

            // UserManager'dan kullanıcıyı döndürüyoruz
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            // Kullanıcının rollerini simüle ediyoruz
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            var result = await _controller.GetProfile();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<UserProfileDto>>(actionResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal(userId, apiResponse.Data.Id);
        }

        // Kullanıcı profilini güncellediğinde başarılı senaryoyu test ediyoruz
        [Fact]
        public async Task UpdateProfile_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            var userId = 101;
            var user = new User { Id = userId, Email = "old@test.com", FirstName = "OldName" };
            var updateDto = new UpdateProfileDto { FirstName = "NewName", LastName = "NewLast", Address = "NewAddr" };

            // HttpContext kullanıcı kimliği
            SetupHttpContextWithUser(userId);

            // Kullanıcının var olduğunu söylüyoruz
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            // Güncelleme işleminin başarılı olduğunu bildiriyoruz
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            var result = await _controller.UpdateProfile(updateDto);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<UserProfileDto>>(actionResult.Value);

            Assert.Equal("NewName", apiResponse.Data.FirstName);
        }

        // Kullanıcı yanlış mevcut şifre girerse şifre değişikliğinin hata döndüğünü test ediyoruz
        [Fact]
        public async Task ChangePassword_ShouldReturnBadRequest_WhenPasswordChangeFails()
        {
            var userId = 102;
            var user = new User { Id = userId };
            var pwdDto = new ChangePasswordDto { CurrentPassword = "WrongPass", NewPassword = "NewPass" };

            SetupHttpContextWithUser(userId);

            // Kullanıcıyı bulduğumuzu söylüyoruz
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            // Şifre değiştirme işlemi başarısız olarak simüle ediliyor
            _mockUserManager.Setup(x => x.ChangePasswordAsync(user, pwdDto.CurrentPassword, pwdDto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

            var result = await _controller.ChangePassword(pwdDto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(actionResult.Value);

            Assert.False(apiResponse.Success);
        }

        // Bir kullanıcıya rol atama işleminin başarılı olduğu durumu test ediyoruz
        [Fact]
        public async Task AssignRole_ShouldReturnOk_WhenRoleExistsAndUserDoesNotHaveIt()
        {
            var dto = new AssignRoleDto { UserId = 5, RoleName = "Admin" };
            var user = new User { Id = 5, Email = "user@test.com" };

            // Kullanıcının bulunduğunu belirtiyoruz
            _mockUserManager.Setup(x => x.FindByIdAsync("5"))
                .ReturnsAsync(user);

            // Atanacak rolün mevcut olduğunu söylüyoruz
            _mockRoleManager.Setup(x => x.RoleExistsAsync("Admin"))
                .ReturnsAsync(true);

            // Kullanıcının bu role sahip olmadığını bildiriyoruz
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Role eklemenin başarılı olduğunu iletiyoruz
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.AssignRole(dto);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(actionResult.Value);

            Assert.True(apiResponse.Success);
        }

        // Testlerde kimliği doğrulanmış kullanıcı oluşturmak için yardımcı fonksiyon
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
