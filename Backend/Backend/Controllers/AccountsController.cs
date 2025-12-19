using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Web;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountsController> _logger;
        private readonly IConfiguration _configuration;

        public AccountsController(
            UserManager<User> userManager, 
            RoleManager<Role> roleManager,
            IEmailService emailService,
            ILogger<AccountsController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        // POST: api/accounts/register
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserProfileDto>.FailureResponse(
                    "Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<UserProfileDto>.FailureResponse(
                    "User with this email already exists"
                ));
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<UserProfileDto>.FailureResponse(
                    "User registration failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }
            
            // Check if "Customer" role exists, if not create it
            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                await _roleManager.CreateAsync(new Role { Name = "Customer" });
            }

            // Assign "Customer" role to the new user
            await _userManager.AddToRoleAsync(user, "Customer");

            // Generate email confirmation token and send confirmation email
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = HttpUtility.UrlEncode(token);
                var confirmationLink = $"{_configuration["AppUrl"]}/confirm-email?userId={user.Id}&token={encodedToken}";
                
                _logger.LogInformation($"Generated confirmation token for user {user.Email}: {token}");
                _logger.LogInformation($"Encoded token: {encodedToken}");
                _logger.LogInformation($"Confirmation link: {confirmationLink}");
                
                await _emailService.SendConfirmationEmailAsync(user.Email!, confirmationLink);
                
                _logger.LogInformation($"New user registered and confirmation email sent: {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation email to {user.Email}");
                // Continue even if email fails
            }

            var userDto = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Roles = new List<string> { "Customer" }
            };

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(
                userDto,
                "User registered successfully! Please check your email to confirm your account."
            ));
        }

        // GET: api/accounts/profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.FailureResponse("User not authenticated"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.FailureResponse("User not found"));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(userDto, "Profile retrieved successfully"));
        }

        // PUT: api/accounts/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserProfileDto>.FailureResponse(
                    "Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            
            if (user == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.FailureResponse("User not found"));
            }

            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.Address = updateDto.Address;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<UserProfileDto>.FailureResponse(
                    "Profile update failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };

            _logger.LogInformation($"User profile updated: {user.Email}");

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(userDto, "Profile updated successfully"));
        }

        // POST: api/accounts/change-password
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword
            );

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Password change failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }

            _logger.LogInformation($"Password changed for user: {user.Email}");

            return Ok(ApiResponse<string>.SuccessResponse(
                "Password changed successfully",
                "Your password has been updated"
            ));
        }

        // GET: api/accounts/users (Admin only)
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<UserProfileDto>>>> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var userDtos = new List<UserProfileDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Address = user.Address,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }

            return Ok(ApiResponse<List<UserProfileDto>>.SuccessResponse(
                userDtos,
                $"Retrieved {userDtos.Count} users"
            ));
        }

        // GET: api/accounts/users/{id} (Admin only)
        [HttpGet("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetUserById(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            
            if (user == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.FailureResponse("User not found"));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(userDto, "User retrieved successfully"));
        }

        // POST: api/accounts/assign-role (Admin only)
        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> AssignRole([FromBody] AssignRoleDto assignRoleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var user = await _userManager.FindByIdAsync(assignRoleDto.UserId.ToString());
            
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            if (!await _roleManager.RoleExistsAsync(assignRoleDto.RoleName))
            {
                return BadRequest(ApiResponse<string>.FailureResponse($"Role '{assignRoleDto.RoleName}' does not exist"));
            }

            if (await _userManager.IsInRoleAsync(user, assignRoleDto.RoleName))
            {
                return BadRequest(ApiResponse<string>.FailureResponse($"User already has the '{assignRoleDto.RoleName}' role"));
            }

            var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Role assignment failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }

            _logger.LogInformation($"Role '{assignRoleDto.RoleName}' assigned to user: {user.Email}");

            return Ok(ApiResponse<string>.SuccessResponse(
                $"Role '{assignRoleDto.RoleName}' assigned successfully",
                $"User '{user.Email}' now has the '{assignRoleDto.RoleName}' role"
            ));
        }

        // DELETE: api/accounts/remove-role (Admin only)
        [HttpDelete("remove-role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> RemoveRole([FromBody] AssignRoleDto assignRoleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var user = await _userManager.FindByIdAsync(assignRoleDto.UserId.ToString());
            
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            if (!await _userManager.IsInRoleAsync(user, assignRoleDto.RoleName))
            {
                return BadRequest(ApiResponse<string>.FailureResponse($"User does not have the '{assignRoleDto.RoleName}' role"));
            }

            var result = await _userManager.RemoveFromRoleAsync(user, assignRoleDto.RoleName);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Role removal failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }

            _logger.LogInformation($"Role '{assignRoleDto.RoleName}' removed from user: {user.Email}");

            return Ok(ApiResponse<string>.SuccessResponse(
                $"Role '{assignRoleDto.RoleName}' removed successfully",
                $"User '{user.Email}' no longer has the '{assignRoleDto.RoleName}' role"
            ));
        }

        // GET: api/accounts/roles (Admin only)
        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public ActionResult<ApiResponse<List<string>>> GetAllRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name!).ToList();
            
            return Ok(ApiResponse<List<string>>.SuccessResponse(
                roles,
                $"Retrieved {roles.Count} roles"
            ));
        }

        // POST: api/accounts/confirm-email
        [HttpPost("confirm-email")]
        public async Task<ActionResult<ApiResponse<string>>> ConfirmEmail([FromQuery] int userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid token"));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            if (user.EmailConfirmed)
            {
                // Email zaten onaylanmış - bu bir success durumu olarak değerlendirilmeli
                // (React StrictMode veya kullanıcının linke birden fazla tıklaması durumunda)
                return Ok(ApiResponse<string>.SuccessResponse(
                    "Email already confirmed! You can login."
                ));
            }

            // Token is automatically URL-decoded by ASP.NET Core when coming from query string
            // No need to decode again - it causes "Invalid token" errors
            _logger.LogInformation($"Confirming email for user {user.Email} with token: {token.Substring(0, Math.Min(20, token.Length))}...");
            
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Email confirmation failed for user {user.Email}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Email confirmation failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }

            _logger.LogInformation($"Email confirmed for user: {user.Email}");

            return Ok(ApiResponse<string>.SuccessResponse(
                "Email confirmed successfully! You can now login."
            ));
        }

        // POST: api/accounts/resend-confirmation
        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<ApiResponse<string>>> ResendConfirmationEmail([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Email is required"));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Ok(ApiResponse<string>.SuccessResponse(
                    "If the email exists, a confirmation link has been sent."
                ));
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Email already confirmed"));
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = HttpUtility.UrlEncode(token);
                var confirmationLink = $"{_configuration["AppUrl"]}/confirm-email?userId={user.Id}&token={encodedToken}";
                
                await _emailService.SendConfirmationEmailAsync(user.Email!, confirmationLink);
                
                _logger.LogInformation($"Confirmation email resent to: {user.Email}");

                return Ok(ApiResponse<string>.SuccessResponse(
                    "Confirmation email has been resent."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending confirmation email to {email}");
                return StatusCode(500, ApiResponse<string>.FailureResponse(
                    "Error sending confirmation email"
                ));
            }
        }

        // POST: api/accounts/forgot-password
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Email is required"));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.EmailConfirmed)
            {
                return Ok(ApiResponse<string>.SuccessResponse(
                    "If the email exists and is confirmed, a password reset link has been sent."
                ));
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = HttpUtility.UrlEncode(token);
                var resetLink = $"{_configuration["AppUrl"]}/reset-password?userId={user.Id}&token={encodedToken}";
                
                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);
                
                _logger.LogInformation($"Password reset email sent to: {user.Email}");

                return Ok(ApiResponse<string>.SuccessResponse(
                    "Password reset link has been sent to your email."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset email to {email}");
                return StatusCode(500, ApiResponse<string>.FailureResponse(
                    "Error sending password reset email"
                ));
            }
        }

        // POST: api/accounts/reset-password
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var user = await _userManager.FindByIdAsync(resetPasswordDto.UserId.ToString());
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            var decodedToken = HttpUtility.UrlDecode(resetPasswordDto.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(
                    "Password reset failed",
                    result.Errors.Select(e => e.Description).ToList()
                ));
            }

            _logger.LogInformation($"Password reset successful for user: {user.Email}");

            return Ok(ApiResponse<string>.SuccessResponse(
                "Password has been reset successfully."
            ));
        }
    }
}
