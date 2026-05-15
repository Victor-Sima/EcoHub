using EcoHub.API.Services;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ISettingsService _settingsService;

        public CartController(ICartService cartService, ISettingsService settingsService)
        {
            _cartService = cartService;
            _settingsService = settingsService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var cart = await _cartService.GetCartAsync(CurrentUserId);
            if (cart == null) return Ok(new CartDto { UserId = CurrentUserId });
            return Ok(cart);
        }

        [HttpPost("items")]
        public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request)
        {
            if (request.Quantity <= 0) return BadRequest(new { message = "Quantity must be greater than 0" });
            var cart = await _cartService.AddItemAsync(CurrentUserId, request);
            if (cart == null) return BadRequest(new { message = "Product not available or insufficient stock" });
            return Ok(cart);
        }

        [HttpPut("items/{cartItemId}")]
        public async Task<ActionResult<CartDto>> UpdateItem(int cartItemId, UpdateCartItemRequest request)
        {
            var cart = await _cartService.UpdateItemAsync(CurrentUserId, cartItemId, request);
            if (cart == null) return BadRequest(new { message = "Item not found or insufficient stock" });
            return Ok(cart);
        }

        [HttpDelete("items/{cartItemId}")]
        public async Task<ActionResult<CartDto>> RemoveItem(int cartItemId)
        {
            var cart = await _cartService.RemoveItemAsync(CurrentUserId, cartItemId);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync(CurrentUserId);
            return NoContent();
        }

        [HttpPost("checkout")]
        public async Task<ActionResult<OrderDto>> Checkout([FromBody] PaymentMethod method)
        {
            if (!await _settingsService.GetBoolAsync("OrdersEnabled", true))
                return BadRequest(new { message = "Orders are currently disabled" });

            var cart = await _cartService.GetCartAsync(CurrentUserId);
            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Cart is empty" });

            return Ok(new { message = "Use OrdersController to create order from cart" });
        }
    }
}
