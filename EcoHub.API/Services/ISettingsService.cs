using EcoHub.Shared.Models;

namespace EcoHub.API.Services
{
    public interface ISettingsService
    {
        Task<List<SystemSettingDto>> GetAllAsync();
        Task<SystemSettingDto?> GetByKeyAsync(string key);
        Task<bool> SetAsync(string key, string value);
        Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    }
}
