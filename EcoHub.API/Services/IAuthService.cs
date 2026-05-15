using EcoHub.API.Data.Models;
using EcoHub.Shared.Models;

namespace EcoHub.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<User?> GetCurrentUserAsync(int userId);
    }
}
