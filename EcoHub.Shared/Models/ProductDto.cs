namespace EcoHub.Shared.Models
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountedPrice => DiscountPercentage > 0 ? Price * (1 - DiscountPercentage / 100) : Price;
        public bool IsActive { get; set; }
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public decimal DiscountPercentage { get; set; }
    }

    public class UpdateStockRequest
    {
        public int QuantityChange { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
