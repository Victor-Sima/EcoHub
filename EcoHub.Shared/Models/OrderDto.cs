using EcoHub.Shared.Enums;

namespace EcoHub.Shared.Models
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountedPrice => DiscountPercentage > 0 ? UnitPrice * (1 - DiscountPercentage / 100) : UnitPrice;
        public decimal TotalPrice => DiscountedPrice * Quantity;
    }

    public class CreateOrderRequest
    {
        public int CartId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }
}
