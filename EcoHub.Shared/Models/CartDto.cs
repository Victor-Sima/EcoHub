namespace EcoHub.Shared.Models
{
    public class CartDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => i.TotalPrice);
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountedPrice => DiscountPercentage > 0 ? UnitPrice * (1 - DiscountPercentage / 100) : UnitPrice;
        public int Quantity { get; set; }
        public decimal TotalPrice => DiscountedPrice * Quantity;
        public decimal Savings => (UnitPrice - DiscountedPrice) * Quantity;
    }

    public class AddCartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }
}
