using EcoHub.Shared.Enums;

namespace EcoHub.API.Data.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }
    }
}
