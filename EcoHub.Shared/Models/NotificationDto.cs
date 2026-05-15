using EcoHub.Shared.Enums;

namespace EcoHub.Shared.Models
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
