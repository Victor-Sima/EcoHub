namespace EcoHub.Shared.Models
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<ProductStockAlertDto> LowStockProducts { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
    }

    public class ProductStockAlertDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class TopProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class NewActivitySummaryDto
    {
        public int NewUsersCount { get; set; }
        public List<UserDto> NewUsers { get; set; } = new();
        public int NewOrdersCount { get; set; }
        public List<OrderDto> NewOrders { get; set; } = new();
    }
}
