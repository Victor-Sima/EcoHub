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
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly ISettingsService _settingsService;

        public OrdersController(IOrderService orderService, ICartService cartService, ISettingsService settingsService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _settingsService = settingsService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
        {
            if (!await _settingsService.GetBoolAsync("OrdersEnabled", true))
                return BadRequest(new { message = "Orders are currently disabled" });

            var cart = await _cartService.GetCartAsync(CurrentUserId);
            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Cart is empty" });

            var order = await _orderService.CreateOrderFromCartAsync(CurrentUserId, request.PaymentMethod);
            if (order == null) return BadRequest(new { message = "Could not create order" });
            return Ok(order);
        }

        [HttpGet("my")]
        public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
        {
            var orders = await _orderService.GetByUserAsync(CurrentUserId);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetById(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (order.UserId != CurrentUserId && !User.IsInRole("Admin"))
                return Forbid();
            return Ok(order);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<List<OrderDto>>> GetAll()
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(orders);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("new")]
        public async Task<ActionResult<List<OrderDto>>> GetNewOrders([FromQuery] DateTime since)
        {
            var orders = await _orderService.GetNewOrdersAsync(since);
            return Ok(orders);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateOrderStatusRequest request)
        {
            var order = await _orderService.UpdateStatusAsync(id, request.Status);
            if (order == null) return NotFound();
            return Ok(order);
        }
    }
}
