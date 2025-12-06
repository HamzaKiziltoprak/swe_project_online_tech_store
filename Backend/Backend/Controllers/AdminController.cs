using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            DataContext context,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcıya rol ata (Employee, ProductManager, Admin)
        /// </summary>
        [HttpPost("assign-role")]
        public async Task<ActionResult<ApiResponse<AssignRoleResponseDto>>> AssignRole(
            [FromBody] AssignRoleRequestDto assignRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AssignRoleResponseDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                // Geçerli roller
                var validRoles = new[] { "Admin", "Employee", "ProductManager", "Customer" };
                if (!validRoles.Contains(assignRoleDto.Role))
                {
                    return BadRequest(ApiResponse<AssignRoleResponseDto>.FailureResponse(
                        $"Geçersiz rol. İzin verilen roller: {string.Join(", ", validRoles)}"
                    ));
                }

                // Kullanıcı var mı kontrol et
                var user = await _userManager.FindByIdAsync(assignRoleDto.UserID.ToString());
                if (user == null)
                {
                    return NotFound(ApiResponse<AssignRoleResponseDto>.FailureResponse(
                        "Kullanıcı bulunamadı"
                    ));
                }

                // Rol var mı kontrol et
                var roleExists = await _roleManager.RoleExistsAsync(assignRoleDto.Role);
                if (!roleExists)
                {
                    return BadRequest(ApiResponse<AssignRoleResponseDto>.FailureResponse(
                        $"Rol '{assignRoleDto.Role}' sistemde mevcut değil"
                    ));
                }

                // Kullanıcının mevcut rollerini al
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Mevcut rollerden çıkar (önceki rolü kaldır)
                if (currentRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        _logger.LogError($"Kullanıcı rolü kaldırılırken hata: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                        return BadRequest(ApiResponse<AssignRoleResponseDto>.FailureResponse(
                            "Eski rol kaldırılırken hata oluştu"
                        ));
                    }
                }

                // Yeni rolü ekle
                var addResult = await _userManager.AddToRoleAsync(user, assignRoleDto.Role);
                if (!addResult.Succeeded)
                {
                    _logger.LogError($"Rol atanırken hata: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                    return BadRequest(ApiResponse<AssignRoleResponseDto>.FailureResponse(
                        "Rol atanırken hata oluştu"
                    ));
                }

                _logger.LogInformation($"Kullanıcıya rol atandı: UserID={assignRoleDto.UserID}, Role={assignRoleDto.Role}");

                var responseDto = new AssignRoleResponseDto
                {
                    UserID = user.Id,
                    UserName = user.UserName ?? "Unknown",
                    Email = user.Email ?? "",
                    AssignedRole = assignRoleDto.Role,
                    Success = true,
                    Message = $"Kullanıcı '{assignRoleDto.Role}' rolüne atandı"
                };

                return Ok(ApiResponse<AssignRoleResponseDto>.SuccessResponse(
                    responseDto,
                    $"'{user.UserName}' kullanıcısına '{assignRoleDto.Role}' rolü başarıyla atandı"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol atanırken hata oluştu");
                return StatusCode(500, ApiResponse<AssignRoleResponseDto>.FailureResponse(
                    "Rol atanırken bir hata oluştu"
                ));
            }
        }

        /// <summary>
        /// Admin istatistiklerini getir (Tüm sistem metrikleri)
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<AdminStatsDto>>> GetStats()
        {
            try
            {
                var stats = new AdminStatsDto();

                // Ürün istatistikleri
                stats.TotalProducts = await _context.Products.CountAsync();
                stats.ActiveProducts = await _context.Products.Where(p => p.IsActive).CountAsync();
                stats.OutOfStockProducts = await _context.Products.Where(p => p.Stock == 0).CountAsync();

                // Kategori istatistikleri
                stats.TotalCategories = await _context.Categories.CountAsync();

                // Sipariş istatistikleri
                stats.TotalOrders = await _context.Orders.CountAsync();
                stats.PendingOrders = await _context.Orders.Where(o => o.Status == "Pending").CountAsync();
                stats.CompletedOrders = await _context.Orders.Where(o => o.Status == "Completed").CountAsync();
                stats.CancelledOrders = await _context.Orders.Where(o => o.Status == "Cancelled").CountAsync();

                // Ciro hesaplaması
                stats.TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
                stats.CompletedRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.TotalAmount);

                // Kullanıcı istatistikleri
                stats.TotalUsers = await _userManager.Users.CountAsync();
                stats.AdminUsers = (await _userManager.GetUsersInRoleAsync("Admin")).Count();
                stats.EmployeeUsers = (await _userManager.GetUsersInRoleAsync("Employee")).Count();
                stats.CustomerUsers = (await _userManager.GetUsersInRoleAsync("Customer")).Count();

                // Review istatistikleri
                stats.TotalReviews = await _context.ProductReviews.CountAsync();
                stats.ApprovedReviews = await _context.ProductReviews.Where(r => r.IsApproved).CountAsync();
                stats.PendingReviews = await _context.ProductReviews.Where(r => !r.IsApproved).CountAsync();

                // İade talebi istatistikleri
                stats.TotalReturns = await _context.OrderReturns.CountAsync();
                stats.PendingReturns = await _context.OrderReturns.Where(r => r.Status == "Pending").CountAsync();
                stats.ApprovedReturns = await _context.OrderReturns.Where(r => r.Status == "Approved").CountAsync();
                stats.RejectedReturns = await _context.OrderReturns.Where(r => r.Status == "Rejected").CountAsync();
                stats.TotalRefundAmount = await _context.OrderReturns
                    .Where(r => r.RefundAmount.HasValue)
                    .SumAsync(r => r.RefundAmount ?? 0);

                _logger.LogInformation("Admin istatistikleri getirildi");

                return Ok(ApiResponse<AdminStatsDto>.SuccessResponse(stats, "Admin istatistikleri başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin istatistikleri getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<AdminStatsDto>.FailureResponse(
                    "Admin istatistikleri getirilirken bir hata oluştu"
                ));
            }
        }
    }
}
