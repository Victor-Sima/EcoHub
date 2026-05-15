using EcoHub.Shared.Models;

namespace EcoHub.API.Services
{
    public interface ICartService
    {
        Task<CartDto?> GetCartAsync(int userId);
        Task<CartDto?> AddItemAsync(int userId, AddCartItemRequest request);
        Task<CartDto?> UpdateItemAsync(int userId, int cartItemId, UpdateCartItemRequest request);
        Task<CartDto?> RemoveItemAsync(int userId, int cartItemId);
        Task<bool> ClearCartAsync(int userId);
    }
}
