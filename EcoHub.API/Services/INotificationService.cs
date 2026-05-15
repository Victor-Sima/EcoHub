using EcoHub.API.Data.Models;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;

namespace EcoHub.API.Services
{
    public interface INotificationService
    {
        Task NotifyNewUserAsync(User user);
        Task NotifyNewOrderAsync(Order order);
        Task NotifyOrderStatusUpdateAsync(Order order);
        Task NotifyLowStockAsync(Product product);
        Task NotifyClientAsync(int userId, string message, NotificationType type);
        Task<List<NotificationDto>> GetNotificationsAsync(int? userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int? userId);
    }
}
