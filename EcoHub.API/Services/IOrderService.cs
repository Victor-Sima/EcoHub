using EcoHub.API.Data.Models;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;

namespace EcoHub.API.Services
{
    public interface IOrderService
    {
        Task<OrderDto?> CreateOrderFromCartAsync(int userId, PaymentMethod paymentMethod);
        Task<OrderDto?> UpdateStatusAsync(int orderId, OrderStatus status);
        Task<OrderDto?> GetByIdAsync(int orderId);
        Task<List<OrderDto>> GetByUserAsync(int userId);
        Task<List<OrderDto>> GetAllAsync();
        Task<List<OrderDto>> GetNewOrdersAsync(DateTime since);
    }
}
