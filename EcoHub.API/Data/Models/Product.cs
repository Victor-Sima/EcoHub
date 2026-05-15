using System.ComponentModel.DataAnnotations;

namespace EcoHub.API.Data.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string? ImageUrl { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}
