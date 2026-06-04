using EcoHub.API.Data;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> GetStatsAsync(DateTime? since = null)
        {
            var sinceDate = since ?? DateTime.MinValue;

            var totalUsers = await _context.Users.CountAsync();
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt > sinceDate);

            var totalOrders = await _context.Orders.CountAsync();
            var newOrders = await _context.Orders.CountAsync(o => o.CreatedAt > sinceDate);

            var totalRevenue = await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.TotalPrice);

            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 5 && p.IsActive)
                .Select(p => new ProductStockAlertDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StockQuantity = p.StockQuantity
                })
                .ToListAsync();

            var topProducts = await _context.OrderItems
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new TopProductDto
                {
                    Id = g.Key.ProductId,
                    Name = g.Key.Name,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(tp => tp.TotalSold)
                .Take(5)
                .ToListAsync();

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                NewUsers = newUsers,
                TotalOrders = totalOrders,
                NewOrders = newOrders,
                TotalRevenue = totalRevenue,
                LowStockProducts = lowStockProducts,
                TopProducts = topProducts
            };
        }
    }
}
