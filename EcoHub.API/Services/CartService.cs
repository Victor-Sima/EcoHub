using EcoHub.API.Data;
using EcoHub.API.Data.Models;
using EcoHub.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartDto?> GetCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return null;
            return MapToDto(cart);
        }

        public async Task<CartDto?> AddItemAsync(int userId, AddCartItemRequest request)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null || product.StockQuantity < request.Quantity || !product.IsActive)
                return null;

            var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductId == request.ProductId);
            if (existingItem != null)
            {
                if (product.StockQuantity < existingItem.Quantity + request.Quantity)
                    return null;
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDto?> UpdateItemAsync(int userId, int cartItemId, UpdateCartItemRequest request)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return null;

            var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
            if (item == null) return null;

            if (request.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                if (item.Product!.StockQuantity < request.Quantity)
                    return null;
                item.Quantity = request.Quantity;
            }

            await _context.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDto?> RemoveItemAsync(int userId, int cartItemId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return null;

            var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
            if (item == null) return null;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return false;

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();
            return true;
        }

        private static CartDto MapToDto(Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = cart.Items.Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    UnitPrice = ci.Product?.Price ?? 0,
                    DiscountPercentage = ci.Product?.DiscountPercentage ?? 0,
                    Quantity = ci.Quantity
                }).ToList()
            };
        }
    }
}
