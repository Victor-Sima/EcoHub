using EcoHub.API.Data;
using EcoHub.API.Data.Models;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public OrderService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<OrderDto?> CreateOrderFromCartAsync(int userId, PaymentMethod paymentMethod)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return null;

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            decimal total = 0;
            foreach (var cartItem in cart.Items)
            {
                if (cartItem.Product == null || cartItem.Product.StockQuantity < cartItem.Quantity || !cartItem.Product.IsActive)
                    return null;

                order.Items.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Product.Price
                });

                cartItem.Product.StockQuantity -= cartItem.Quantity;
                total += cartItem.Product.Price * cartItem.Quantity;

                _context.StockTransactions.Add(new StockTransaction
                {
                    ProductId = cartItem.ProductId,
                    QuantityChange = -cartItem.Quantity,
                    Reason = $"Order created",
                    CreatedAt = DateTime.UtcNow
                });
            }

            order.TotalPrice = total;
            _context.Orders.Add(order);

            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = total,
                Method = paymentMethod,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await _notificationService.NotifyNewOrderAsync(order);
            await _notificationService.NotifyClientAsync(userId, $"Order #{order.Id} placed successfully!", NotificationType.NewOrder);

            return await GetByIdAsync(order.Id);
        }

        public async Task<OrderDto?> UpdateStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return null;

            order.Status = status;
            await _context.SaveChangesAsync();

            await _notificationService.NotifyOrderStatusUpdateAsync(order);

            return await GetByIdAsync(orderId);
        }

        public async Task<OrderDto?> GetByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;
            return MapToDto(order);
        }

        public async Task<List<OrderDto>> GetByUserAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => MapToDto(o))
                .ToListAsync();
        }

        public async Task<List<OrderDto>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => MapToDto(o))
                .ToListAsync();
        }

        public async Task<List<OrderDto>> GetNewOrdersAsync(DateTime since)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CreatedAt > since)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => MapToDto(o))
                .ToListAsync();
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = order.User?.Email ?? "",
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                Items = order.Items.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    DiscountPercentage = oi.Product?.DiscountPercentage ?? 0
                }).ToList()
            };
        }
    }
}
