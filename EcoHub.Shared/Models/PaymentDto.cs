using EcoHub.Shared.Enums;

namespace EcoHub.Shared.Models
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public int OrderId { get; set; }
        public PaymentMethod Method { get; set; }
    }
}
