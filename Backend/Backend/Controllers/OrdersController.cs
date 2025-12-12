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
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<OrdersController> _logger;
        private readonly Services.IPaymentService _paymentService;

        public OrdersController(
            DataContext context,
            UserManager<User> userManager,
            ILogger<OrdersController> logger,
            Services.IPaymentService paymentService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _paymentService = paymentService;
        }

        /// <summary>
        /// Sepetten sipariş oluştur
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<OrderDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<OrderDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<OrderDto>.FailureResponse("Kullanıcı bulunamadı"));
                }

                // Sepeti kontrol et
                var cartItems = await _context.CartItems
                    .Where(ci => ci.UserID.ToString() == userId)
                    .Include(ci => ci.Product)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return BadRequest(ApiResponse<OrderDto>.FailureResponse("Sepetiniz boş"));
                }

                // Stok kontrol et
                foreach (var item in cartItems)
                {
                    if (item.Product == null || item.Product.Stock < item.Count)
                    {
                        return BadRequest(ApiResponse<OrderDto>.FailureResponse(
                            $"Ürün '{item.Product?.ProductName}' stokta yeterli miktarda mevcut değildir"
                        ));
                    }
                }

                // Sipariş oluştur
                var order = new Order
                {
                    UserID = int.Parse(userId),
                    Status = "Pending",
                    OrderDate = DateTime.Now,
                    ShippingAddress = createOrderDto.ShippingAddress
                };

                // Sipariş ürünlerini ekle
                decimal totalAmount = 0;
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        ProductID = cartItem.ProductID,
                        UnitPrice = cartItem.Product!.Price,
                        Quantity = cartItem.Count
                    };
                    order.OrderItems.Add(orderItem);
                    totalAmount += orderItem.UnitPrice * orderItem.Quantity;

                    // Stoktan düş
                    cartItem.Product.Stock -= cartItem.Count;
                }

                order.TotalAmount = totalAmount;

                // Veritabanına ekle
                _context.Orders.Add(order);

                // Sepeti boşalt
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();

                // Transaction kaydı oluştur
                var transaction = new Transaction
                {
                    TransactionType = "Purchase",
                    Amount = totalAmount,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Order #{order.OrderID} - {cartItems.Count} items",
                    Status = "Completed",
                    OrderID = order.OrderID,
                    UserID = int.Parse(userId)
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sipariş oluşturuldu: OrderID={order.OrderID}, UserID={userId}, Total={totalAmount}");

                var orderDto = MapOrderToDto(order, user.Email ?? "");
                return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderID },
                    ApiResponse<OrderDto>.SuccessResponse(orderDto, "Sipariş başarıyla oluşturuldu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş oluşturulurken hata oluştu");
                return StatusCode(500, ApiResponse<OrderDto>.FailureResponse("Sipariş oluşturulurken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Kullanıcının siparişlerini getir
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedOrderResult>>> GetMyOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<PagedOrderResult>.FailureResponse("Kullanıcı tanınmadı"));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<PagedOrderResult>.FailureResponse("Kullanıcı bulunamadı"));
                }

                // Sorgu oluştur
                var query = _context.Orders
                    .Where(o => o.UserID == userIdInt)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                // Statüye göre filtrele
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                // Sıralama
                query = query.OrderByDescending(o => o.OrderDate);

                // Sayfalama
                var totalCount = await query.CountAsync();
                var orders = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var orderDtos = orders.Select(o => MapOrderToDto(o, user.Email ?? "")).ToList();

                var result = new PagedOrderResult
                {
                    Orders = orderDtos,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(ApiResponse<PagedOrderResult>.SuccessResponse(result, "Siparişleriniz getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Siparişler getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<PagedOrderResult>.FailureResponse("Siparişler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sipariş detaylarını getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderById([FromRoute] int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<OrderDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.FailureResponse("Sipariş bulunamadı"));
                }

                // Kontrol et: Kullanıcı kendi siparişini görebilir, admin tüm siparişleri görebilir
                var isAdmin = User.IsInRole("Admin");
                if (order.UserID != userIdInt && !isAdmin)
                {
                    return Forbid();
                }

                var user = await _userManager.FindByIdAsync(order.UserID.ToString());
                var orderDto = MapOrderToDto(order, user?.Email ?? "");

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto, "Sipariş detayları getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş detayları getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<OrderDto>.FailureResponse("Sipariş detayları getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sipariş durumunu güncelle (Admin Only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(
            [FromRoute] int id,
            [FromBody] UpdateOrderStatusDto updateStatusDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<OrderDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.FailureResponse("Sipariş bulunamadı"));
                }

                var oldStatus = order.Status;
                order.Status = updateStatusDto.Status;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sipariş durumu güncellendi: OrderID={id}, OldStatus={oldStatus}, NewStatus={updateStatusDto.Status}");

                var user = await _userManager.FindByIdAsync(order.UserID.ToString());
                var orderDto = MapOrderToDto(order, user?.Email ?? "");

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto, $"Sipariş durumu '{updateStatusDto.Status}' olarak güncellendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durumu güncellenirken hata oluştu");
                return StatusCode(500, ApiResponse<OrderDto>.FailureResponse("Sipariş durumu güncellenirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Tüm siparişleri listele (Admin Only) - Filtreleme ve Sayfalama ile
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedOrderResult>>> GetAllOrders(
            [FromQuery] OrderFilterParams filterParams)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                // Filtreleme
                if (!string.IsNullOrEmpty(filterParams.Status))
                {
                    query = query.Where(o => o.Status == filterParams.Status);
                }

                if (filterParams.StartDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= filterParams.StartDate);
                }

                if (filterParams.EndDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate <= filterParams.EndDate);
                }

                if (filterParams.UserID.HasValue)
                {
                    query = query.Where(o => o.UserID == filterParams.UserID);
                }

                if (filterParams.MinAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount >= filterParams.MinAmount);
                }

                if (filterParams.MaxAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount <= filterParams.MaxAmount);
                }

                // Sıralama
                if (filterParams.SortBy == "TotalAmount")
                {
                    query = filterParams.SortDescending
                        ? query.OrderByDescending(o => o.TotalAmount)
                        : query.OrderBy(o => o.TotalAmount);
                }
                else
                {
                    query = filterParams.SortDescending
                        ? query.OrderByDescending(o => o.OrderDate)
                        : query.OrderBy(o => o.OrderDate);
                }

                // Sayfalama
                var totalCount = await query.CountAsync();
                var orders = await query
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .ToListAsync();

                var orderDtos = new List<OrderDto>();
                foreach (var order in orders)
                {
                    var user = await _userManager.FindByIdAsync(order.UserID.ToString());
                    orderDtos.Add(MapOrderToDto(order, user?.Email ?? ""));
                }

                var result = new PagedOrderResult
                {
                    Orders = orderDtos,
                    TotalCount = totalCount,
                    PageNumber = filterParams.PageNumber,
                    PageSize = filterParams.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                };

                return Ok(ApiResponse<PagedOrderResult>.SuccessResponse(result, "Tüm siparişler getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm siparişler getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<PagedOrderResult>.FailureResponse("Siparişler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Siparişi iptal et
        /// </summary>
        [HttpPatch("{id}/cancel")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CancelOrder([FromRoute] int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<OrderDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<OrderDto>.FailureResponse("Sipariş bulunamadı"));
                }

                // Kontrol et: Sadece kendi siparişini iptal edebilir veya admin
                var isAdmin = User.IsInRole("Admin");
                if (order.UserID != userIdInt && !isAdmin)
                {
                    return Forbid();
                }

                // Sadece Pending durumdaki siparişler iptal edilebilir
                if (order.Status != "Pending")
                {
                    return BadRequest(ApiResponse<OrderDto>.FailureResponse(
                        $"Sadece 'Pending' durumundaki siparişler iptal edilebilir. Mevcut durum: {order.Status}"
                    ));
                }

                // Stokları geri ver
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.Stock += item.Quantity;
                    }
                }

                order.Status = "Cancelled";

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sipariş iptal edildi: OrderID={id}");

                var user = await _userManager.FindByIdAsync(order.UserID.ToString());
                var orderDto = MapOrderToDto(order, user?.Email ?? "");

                return Ok(ApiResponse<OrderDto>.SuccessResponse(orderDto, "Sipariş başarıyla iptal edildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş iptal edilirken hata oluştu");
                return StatusCode(500, ApiResponse<OrderDto>.FailureResponse("Sipariş iptal edilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sipariş için iade isteği oluştur
        /// </summary>
        [HttpPost("{id}/return")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> RequestReturn(
            [FromRoute] int id,
            [FromBody] CreateReturnDto createReturnDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<ReturnDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                // Sipariş var mı kontrol et
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("Sipariş bulunamadı"));
                }

                // Kontrol et: Sadece kendi siparişi için iade talebinde bulunabilir veya admin
                var isAdmin = User.IsInRole("Admin");
                if (order.UserID != userIdInt && !isAdmin)
                {
                    return Forbid();
                }

                // Cancelled siparişler için iade talebinde bulunamaz
                if (order.Status == "Cancelled")
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "İptal edilmiş siparişler için iade talebinde bulunamaz"
                    ));
                }

                // Aynı sipariş için aktif bir iade talebi var mı kontrol et
                var existingReturn = await _context.OrderReturns
                    .FirstOrDefaultAsync(r => r.OrderID == id && r.Status == "Pending");

                if (existingReturn != null)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Bu sipariş için zaten bir iade talebi beklemede"
                    ));
                }

                // Yeni iade talebini oluştur
                var orderReturn = new OrderReturn
                {
                    OrderID = id,
                    UserID = userIdInt,
                    ReturnReason = createReturnDto.ReturnReason,
                    ReturnDescription = createReturnDto.ReturnDescription,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderReturns.Add(orderReturn);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Yeni iade talebi oluşturuldu: ReturnID={orderReturn.ReturnID}, OrderID={id}, UserID={userIdInt}");

                var returnDto = MapReturnToDto(orderReturn);
                return CreatedAtAction(nameof(GetReturnById), new { id = orderReturn.ReturnID }, 
                    ApiResponse<ReturnDto>.SuccessResponse(returnDto, "İade talebi başarıyla oluşturuldu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade talebi oluşturulurken hata oluştu");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse("İade talebi oluşturulurken bir hata oluştu"));
            }
        }

        /// <summary>
        /// İade talebini görüntüle
        /// </summary>
        [HttpGet("return/{id}")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> GetReturnById([FromRoute] int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<ReturnDto>.FailureResponse("Kullanıcı tanınmadı"));
                }

                var orderReturn = await _context.OrderReturns
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.ReturnID == id);

                if (orderReturn == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("İade talebi bulunamadı"));
                }

                // Kontrol et: Sadece kendi iade talebini görebilir veya admin
                var isAdmin = User.IsInRole("Admin");
                if (orderReturn.UserID != userIdInt && !isAdmin)
                {
                    return Forbid();
                }

                var returnDto = MapReturnToDto(orderReturn);
                return Ok(ApiResponse<ReturnDto>.SuccessResponse(returnDto, "İade talebi detayları getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade talebi detayları getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse("İade talebi detayları getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Kullanıcının iade taleplerini listele
        /// </summary>
        [HttpGet("returns")]
        public async Task<ActionResult<ApiResponse<PagedReturnResult>>> GetMyReturns([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 12)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(ApiResponse<PagedReturnResult>.FailureResponse("Kullanıcı tanınmadı"));
                }

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 12;

                var totalCount = await _context.OrderReturns
                    .Where(r => r.UserID == userIdInt)
                    .CountAsync();

                var returns = await _context.OrderReturns
                    .Where(r => r.UserID == userIdInt)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var returnDtos = returns.Select(MapReturnToDto).ToList();

                var result = new PagedReturnResult
                {
                    Data = returnDtos,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(ApiResponse<PagedReturnResult>.SuccessResponse(result, "İade talepleri getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade talepleri getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<PagedReturnResult>.FailureResponse("İade talepleri getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// İade talebini onayla ve geri ödeme işle (Admin Only)
        /// </summary>
        [HttpPatch("return/{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> ApproveReturn(
            [FromRoute] int id,
            [FromBody] ApproveReturnDto approveDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                var orderReturn = await _context.OrderReturns
                    .Include(r => r.Order)
                    .ThenInclude(o => o!.OrderItems)
                    .FirstOrDefaultAsync(r => r.ReturnID == id);

                if (orderReturn == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("İade talebi bulunamadı"));
                }

                // Sadece Pending durumundaki talepler onaylanabilir
                if (orderReturn.Status != "Pending")
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        $"Sadece 'Pending' durumundaki talepler onaylanabilir. Mevcut durum: {orderReturn.Status}"
                    ));
                }

                // İade talebini onayla
                orderReturn.Status = "Approved";
                orderReturn.RefundAmount = approveDto.RefundAmount;
                orderReturn.AdminNote = approveDto.AdminNote;
                orderReturn.UpdatedAt = DateTime.UtcNow;

                // Sipariş durumunu "Returned" olarak güncelle
                if (orderReturn.Order != null)
                {
                    orderReturn.Order.Status = "Returned";

                    // Stokları geri ver
                    foreach (var item in orderReturn.Order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Stock += item.Quantity;
                            _logger.LogInformation($"Ürün stoğu geri yüklendi: ProductID={item.ProductID}, Quantity={item.Quantity}");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"İade talebi onaylandı: ReturnID={id}, RefundAmount={approveDto.RefundAmount}");

                var returnDto = MapReturnToDto(orderReturn);
                return Ok(ApiResponse<ReturnDto>.SuccessResponse(returnDto, "İade talebi başarıyla onaylandı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade talebi onaylanırken hata oluştu");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse("İade talebi onaylanırken bir hata oluştu"));
            }
        }

        /// <summary>
        /// İade talebini reddet (Admin Only)
        /// </summary>
        [HttpPatch("return/{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ReturnDto>>> RejectReturn(
            [FromRoute] int id,
            [FromBody] RejectReturnDto rejectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        "Validation failed",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    ));
                }

                var orderReturn = await _context.OrderReturns
                    .FirstOrDefaultAsync(r => r.ReturnID == id);

                if (orderReturn == null)
                {
                    return NotFound(ApiResponse<ReturnDto>.FailureResponse("İade talebi bulunamadı"));
                }

                // Sadece Pending durumundaki talepler reddedilebilir
                if (orderReturn.Status != "Pending")
                {
                    return BadRequest(ApiResponse<ReturnDto>.FailureResponse(
                        $"Sadece 'Pending' durumundaki talepler reddedilebilir. Mevcut durum: {orderReturn.Status}"
                    ));
                }

                // İade talebini reddet
                orderReturn.Status = "Rejected";
                orderReturn.AdminNote = rejectDto.AdminNote;
                orderReturn.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"İade talebi reddedildi: ReturnID={id}");

                var returnDto = MapReturnToDto(orderReturn);
                return Ok(ApiResponse<ReturnDto>.SuccessResponse(returnDto, "İade talebi başarıyla reddedildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade talebi reddedilirken hata oluştu");
                return StatusCode(500, ApiResponse<ReturnDto>.FailureResponse("İade talebi reddedilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Tüm iade taleplerini listele (Admin Only)
        /// </summary>
        [HttpGet("all-returns")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedReturnResult>>> GetAllReturns([FromQuery] ReturnFilterParams filterParams)
        {
            try
            {
                if (filterParams.PageNumber < 1) filterParams.PageNumber = 1;
                if (filterParams.PageSize < 1 || filterParams.PageSize > 100) filterParams.PageSize = 12;

                var query = _context.OrderReturns.AsQueryable();

                // Status filtresi
                if (!string.IsNullOrWhiteSpace(filterParams.Status))
                {
                    query = query.Where(r => r.Status == filterParams.Status);
                }

                // ReturnReason filtresi
                if (!string.IsNullOrWhiteSpace(filterParams.ReturnReason))
                {
                    query = query.Where(r => r.ReturnReason.Contains(filterParams.ReturnReason));
                }

                // OrderID filtresi
                if (filterParams.OrderID.HasValue)
                {
                    query = query.Where(r => r.OrderID == filterParams.OrderID);
                }

                // UserID filtresi
                if (filterParams.UserID.HasValue)
                {
                    query = query.Where(r => r.UserID == filterParams.UserID);
                }

                // Tarih aralığı filtresi
                if (filterParams.StartDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt >= filterParams.StartDate);
                }

                if (filterParams.EndDate.HasValue)
                {
                    var endDate = filterParams.EndDate.Value.AddDays(1); // Gün sonuna kadar
                    query = query.Where(r => r.CreatedAt < endDate);
                }

                var totalCount = await query.CountAsync();

                var returns = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                    .Take(filterParams.PageSize)
                    .ToListAsync();

                var returnDtos = returns.Select(MapReturnToDto).ToList();

                var result = new PagedReturnResult
                {
                    Data = returnDtos,
                    TotalCount = totalCount,
                    PageNumber = filterParams.PageNumber,
                    PageSize = filterParams.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filterParams.PageSize),
                    HasPreviousPage = filterParams.PageNumber > 1,
                    HasNextPage = filterParams.PageNumber < (int)Math.Ceiling(totalCount / (double)filterParams.PageSize)
                };

                return Ok(ApiResponse<PagedReturnResult>.SuccessResponse(result, "Tüm iade talepleri getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm iade talepleri getirilirken hata oluştu");
                return StatusCode(500, ApiResponse<PagedReturnResult>.FailureResponse("Tüm iade talepleri getirilirken bir hata oluştu"));
            }
        }

        // Helper methods
        private OrderDto MapOrderToDto(Order order, string userEmail)
        {
            return new OrderDto
            {
                OrderID = order.OrderID,
                UserID = order.UserID,
                UserEmail = userEmail,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemID = oi.OrderItemID,
                    ProductID = oi.ProductID,
                    ProductName = oi.Product?.ProductName ?? "Bilinmeyen Ürün",
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
            };
        }

        private ReturnDto MapReturnToDto(OrderReturn orderReturn)
        {
            return new ReturnDto
            {
                ReturnID = orderReturn.ReturnID,
                OrderID = orderReturn.OrderID,
                UserID = orderReturn.UserID,
                ReturnReason = orderReturn.ReturnReason,
                ReturnDescription = orderReturn.ReturnDescription,
                Status = orderReturn.Status,
                RefundAmount = orderReturn.RefundAmount,
                CreatedAt = orderReturn.CreatedAt,
                UpdatedAt = orderReturn.UpdatedAt,
                AdminNote = orderReturn.AdminNote
            };
        }

        /// <summary>
        /// One-Click Buy - Hızlı satın alma (sepetteki tüm ürünleri tek tıkla satın al)
        /// </summary>
        [HttpPost("one-click-buy")]
        public async Task<ActionResult<OneClickBuyResponse>> OneClickBuy([FromBody] OneClickBuyDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new OneClickBuyResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new OneClickBuyResponse
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var userIdInt = int.Parse(userId);

                // 1. Sepetteki ürünleri getir
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.UserID == userIdInt)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return BadRequest(new OneClickBuyResponse
                    {
                        Success = false,
                        Message = "Cart is empty"
                    });
                }

                // 2. Stok kontrolü
                var stockErrors = new List<string>();
                foreach (var item in cartItems)
                {
                    if (!item.Product.IsActive)
                    {
                        stockErrors.Add($"{item.Product.ProductName} is no longer available");
                    }
                    else if (item.Product.Stock < item.Count)
                    {
                        stockErrors.Add($"{item.Product.ProductName} - insufficient stock (Available: {item.Product.Stock}, Requested: {item.Count})");
                    }
                }

                if (stockErrors.Any())
                {
                    return BadRequest(new OneClickBuyResponse
                    {
                        Success = false,
                        Message = "Stock validation failed",
                        Errors = stockErrors
                    });
                }

                // 3. Toplam tutarı hesapla
                decimal totalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Count);

                // 4. Payment Service ile ödemeyi işle (Mock)
                var paymentResponse = await _paymentService.ProcessPaymentAsync(
                    totalAmount,
                    dto.PaymentMethod ?? "Default",
                    userIdInt
                );

                // 5. Ödeme başarısız ise
                if (!paymentResponse.Success)
                {
                    _logger.LogWarning($"One-Click Buy payment failed for user {userId}: {paymentResponse.Message}");
                    
                    return BadRequest(new OneClickBuyResponse
                    {
                        Success = false,
                        Message = paymentResponse.Message,
                        PaymentStatus = paymentResponse.Status,
                        TransactionId = paymentResponse.TransactionId
                    });
                }

                // 6. Ödeme başarılı - Siparişi oluştur
                var order = new Order
                {
                    UserID = userIdInt,
                    TotalAmount = totalAmount,
                    Status = "Processing", // One-Click Buy direkt Processing'e geçer
                    ShippingAddress = dto.ShippingAddress,
                    OrderDate = DateTime.UtcNow,
                    OrderItems = cartItems.Select(ci => new OrderItem
                    {
                        ProductID = ci.ProductID,
                        Quantity = ci.Count,
                        UnitPrice = ci.Product.Price
                    }).ToList()
                };

                _context.Orders.Add(order);

                // 7. Stokları güncelle (rezervasyon)
                foreach (var item in cartItems)
                {
                    item.Product.Stock -= item.Count;
                    _logger.LogInformation($"Stock reserved: {item.Product.ProductName}, Quantity: {item.Count}, Remaining: {item.Product.Stock}");
                }

                // 8. Sepeti temizle
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();

                // 9. Transaction kaydı oluştur
                var transaction = new Transaction
                {
                    TransactionType = "Purchase",
                    Amount = totalAmount,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"One-Click Buy - Order #{order.OrderID} - {cartItems.Count} items - Payment: {paymentResponse.TransactionId}",
                    Status = "Completed",
                    OrderID = order.OrderID,
                    UserID = userIdInt
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"One-Click Buy successful: OrderID={order.OrderID}, UserID={userId}, Amount={totalAmount}, PaymentTxn={paymentResponse.TransactionId}");

                // 10. Yanıt oluştur
                var user = await _userManager.FindByIdAsync(userId);
                var orderDto = new OrderDto
                {
                    OrderID = order.OrderID,
                    UserID = order.UserID,
                    UserEmail = user?.Email ?? "",
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    OrderDate = order.OrderDate,
                    ShippingAddress = order.ShippingAddress,
                    Items = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        OrderItemID = oi.OrderItemID,
                        ProductID = oi.ProductID,
                        ProductName = cartItems.First(ci => ci.ProductID == oi.ProductID).Product.ProductName,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        Subtotal = oi.UnitPrice * oi.Quantity
                    }).ToList()
                };

                return Ok(new OneClickBuyResponse
                {
                    Success = true,
                    Message = "Order placed successfully via One-Click Buy",
                    Order = orderDto,
                    PaymentStatus = paymentResponse.Status,
                    TransactionId = paymentResponse.TransactionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing One-Click Buy");
                return StatusCode(500, new OneClickBuyResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your order. Please try again."
                });
            }
        }
    }
}

