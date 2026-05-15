using EcoHub.API.Data;
using EcoHub.API.Data.Models;
using EcoHub.API.Hubs;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task NotifyNewUserAsync(User user)
        {
            var notification = new Notification
            {
                Message = $"New user registered: {user.Email}",
                Type = NotificationType.NewUser,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group("Admins").SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt,
                IsRead = false
            });
        }

        public async Task NotifyNewOrderAsync(Order order)
        {
            var notification = new Notification
            {
                Message = $"New order #{order.Id} placed by {order.User?.Email} for {order.TotalPrice:C}",
                Type = NotificationType.NewOrder,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group("Admins").SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt,
                IsRead = false
            });
        }

        public async Task NotifyOrderStatusUpdateAsync(Order order)
        {
            var clientNotification = new Notification
            {
                UserId = order.UserId,
                Message = $"Your order #{order.Id} status changed to {order.Status}",
                Type = NotificationType.OrderStatusUpdated,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(clientNotification);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"User_{order.UserId}").SendAsync("OrderStatusUpdated", new
            {
                OrderId = order.Id,
                Status = order.Status.ToString()
            });

            await _hub.Clients.Group($"User_{order.UserId}").SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = clientNotification.Id,
                Message = clientNotification.Message,
                Type = clientNotification.Type,
                CreatedAt = clientNotification.CreatedAt,
                IsRead = false
            });
        }

        public async Task NotifyLowStockAsync(Product product)
        {
            var notification = new Notification
            {
                Message = $"Low stock alert: {product.Name} has only {product.StockQuantity} units left",
                Type = NotificationType.LowStock,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group("Admins").SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt,
                IsRead = false
            });
        }

        public async Task NotifyClientAsync(int userId, string message, NotificationType type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt,
                IsRead = false
            });
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int? userId)
        {
            var query = _context.Notifications.AsQueryable();
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value || n.UserId == null);
            else
                query = query.Where(n => n.UserId == null);

            return await query.OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int? userId)
        {
            var query = _context.Notifications.Where(n => !n.IsRead);
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value || n.UserId == null);
            else
                query = query.Where(n => n.UserId == null);

            await query.ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
        }
    }
}
