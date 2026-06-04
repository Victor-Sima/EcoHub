namespace EcoHub.Admin.Services
{
    public static class AppState
    {
        public static string? AuthToken { get; set; }
        public static DateTime? LastLoginAt { get; set; }
        public static Shared.Models.UserDto? CurrentUser { get; set; }
    }
}
