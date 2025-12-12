using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    public class CartController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(DataContext context, UserManager<User> userManager, ILogger<CartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get user's cart items
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<CartSummaryDto>>> GetCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                
                var cartItems = await _context.CartItems
                    .Where(c => c.UserID == userId)
                    .Include(c => c.Product)
                    .OrderByDescending(c => c.CartItemID)
                    .ToListAsync();

                var cartDto = new CartSummaryDto
                {
                    Items = cartItems.Select(item => new CartItemDto
                    {
                        CartItemID = item.CartItemID,
                        ProductID = item.ProductID,
                        ProductName = item.Product.ProductName,
                        Price = item.Product.Price,
                        Count = item.Count,
                        ProductImageUrl = item.Product.ImageUrl,
                        Subtotal = item.Product.Price * item.Count
                    }).ToList(),
                    TotalItems = cartItems.Sum(c => c.Count),
                    TotalPrice = cartItems.Sum(c => c.Product.Price * c.Count)
                };

                _logger.LogInformation($"User {userId} retrieved cart with {cartDto.TotalItems} items");
                return Ok(ApiResponse<CartSummaryDto>.SuccessResponse(cartDto, "Cart retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving cart: {ex.Message}");
                return StatusCode(500, ApiResponse<CartSummaryDto>.FailureResponse("An error occurred while retrieving cart", null));
            }
        }

        /// <summary>
        /// Add product to cart
        /// </summary>
        [HttpPost("add")]
        public async Task<ActionResult<ApiResponse<CartItemDto>>> AddToCart([FromBody] AddToCartDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<CartItemDto>.FailureResponse("Invalid request data", errors));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                // Validate product exists
                var product = await _context.Products.FindAsync(request.ProductID);
                if (product == null)
                    return NotFound(ApiResponse<CartItemDto>.FailureResponse("Product not found", null));

                // Validate quantity
                if (request.Count <= 0)
                    return BadRequest(ApiResponse<CartItemDto>.FailureResponse("Quantity must be greater than 0", null));

                if (request.Count > product.Stock)
                    return BadRequest(ApiResponse<CartItemDto>.FailureResponse($"Not enough stock. Available: {product.Stock}", null));

                // Check if item already in cart
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == request.ProductID);

                CartItem cartItem;
                if (existingItem != null)
                {
                    // Update quantity if item already exists
                    if (existingItem.Count + request.Count > product.Stock)
                        return BadRequest(ApiResponse<CartItemDto>.FailureResponse($"Not enough stock. Available: {product.Stock}, Current in cart: {existingItem.Count}", null));

                    existingItem.Count += request.Count;
                    cartItem = existingItem;
                    _context.CartItems.Update(cartItem);
                }
                else
                {
                    // Add new item
                    cartItem = new CartItem
                    {
                        ProductID = request.ProductID,
                        UserID = userId,
                        Count = request.Count
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                var cartItemDto = new CartItemDto
                {
                    CartItemID = cartItem.CartItemID,
                    ProductID = cartItem.ProductID,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Count = cartItem.Count,
                    ProductImageUrl = product.ImageUrl,
                    Subtotal = product.Price * cartItem.Count
                };

                _logger.LogInformation($"User {userId} added product {request.ProductID} to cart (quantity: {request.Count})");
                return CreatedAtAction(nameof(GetCart), cartItemDto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to cart: {ex.Message}");
                return StatusCode(500, ApiResponse<CartItemDto>.FailureResponse("An error occurred while adding to cart", null));
            }
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<ActionResult<ApiResponse<CartItemDto>>> UpdateCartItem(int id, [FromBody] UpdateCartItemDto request)
        {
            try
            {
                if (request.Count <= 0)
                    return BadRequest(ApiResponse<CartItemDto>.FailureResponse("Quantity must be greater than 0", null));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.CartItemID == id && c.UserID == userId);

                if (cartItem == null)
                    return NotFound(ApiResponse<CartItemDto>.FailureResponse("Cart item not found", null));

                // Validate quantity against stock
                if (request.Count > cartItem.Product.Stock)
                    return BadRequest(ApiResponse<CartItemDto>.FailureResponse($"Not enough stock. Available: {cartItem.Product.Stock}", null));

                cartItem.Count = request.Count;
                _context.CartItems.Update(cartItem);
                await _context.SaveChangesAsync();

                var cartItemDto = new CartItemDto
                {
                    CartItemID = cartItem.CartItemID,
                    ProductID = cartItem.ProductID,
                    ProductName = cartItem.Product.ProductName,
                    Price = cartItem.Product.Price,
                    Count = cartItem.Count,
                    ProductImageUrl = cartItem.Product.ImageUrl,
                    Subtotal = cartItem.Product.Price * cartItem.Count
                };

                _logger.LogInformation($"User {userId} updated cart item {id} to quantity {request.Count}");
                return Ok(ApiResponse<CartItemDto>.SuccessResponse(cartItemDto, "Cart item updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating cart item: {ex.Message}");
                return StatusCode(500, ApiResponse<CartItemDto>.FailureResponse("An error occurred while updating cart item", null));
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveFromCart(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CartItemID == id && c.UserID == userId);

                if (cartItem == null)
                    return NotFound(ApiResponse<object>.FailureResponse("Cart item not found", null));

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} removed cart item {id}");
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Item removed from cart"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing from cart: {ex.Message}");
                return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred while removing from cart", null));
            }
        }

        /// <summary>
        /// Clear entire cart
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult<ApiResponse<object>>> ClearCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var cartItems = await _context.CartItems
                    .Where(c => c.UserID == userId)
                    .ToListAsync();

                if (cartItems.Count == 0)
                    return Ok(ApiResponse<object>.SuccessResponse(new { }, "Cart is already empty"));

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} cleared cart ({cartItems.Count} items removed)");
                return Ok(ApiResponse<object>.SuccessResponse(new { itemsRemoved = cartItems.Count }, "Cart cleared successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing cart: {ex.Message}");
                return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred while clearing cart", null));
            }
        }
    }
}
