using EcoHub.API.Data;
using EcoHub.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SystemSettingDto>> GetAllAsync()
        {
            return await _context.SystemSettings
                .Select(s => new SystemSettingDto
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<SystemSettingDto?> GetByKeyAsync(string key)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return null;
            return new SystemSettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<bool> SetAsync(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new Data.Models.SystemSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return defaultValue;
            return bool.TryParse(setting.Value, out var result) ? result : defaultValue;
        }
    }
}
