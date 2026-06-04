using EcoHub.Shared.Models;

namespace EcoHub.API.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(DateTime? since = null);
    }
}
