using EcoHub.API.Data;
using EcoHub.API.Services;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ISettingsService _settingsService;

        public ProductsController(AppDbContext context, INotificationService notificationService, ISettingsService settingsService)
        {
            _context = context;
            _notificationService = notificationService;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll(
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? onDiscount)
        {
            if (!await _settingsService.GetBoolAsync("ProductsVisible", true))
                return BadRequest(new { message = "Product display is currently disabled" });

            var query = _context.Products.Where(p => p.IsActive).AsQueryable();
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);
            if (onDiscount.HasValue && onDiscount.Value)
                query = query.Where(p => p.DiscountPercentage > 0);

            var products = await query
                .Include(p => p.Category)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? "",
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    DiscountPercentage = p.DiscountPercentage,
                    IsActive = p.IsActive
                })
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("discounts")]
        public async Task<ActionResult<List<ProductDto>>> GetDiscounted()
        {
            if (!await _settingsService.GetBoolAsync("ProductsVisible", true))
                return BadRequest(new { message = "Product display is currently disabled" });

            var products = await _context.Products
                .Where(p => p.IsActive && p.DiscountPercentage > 0)
                .Include(p => p.Category)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? "",
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    DiscountPercentage = p.DiscountPercentage,
                    IsActive = p.IsActive
                })
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<ProductDto>>> Search([FromQuery] string q)
        {
            if (!await _settingsService.GetBoolAsync("ProductsVisible", true))
                return BadRequest(new { message = "Product display is currently disabled" });

            var query = _context.Products
                .Where(p => p.IsActive && (p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q))));

            var products = await query
                .Include(p => p.Category)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? "",
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    DiscountPercentage = p.DiscountPercentage,
                    IsActive = p.IsActive
                })
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                ImageUrl = product.ImageUrl,
                DiscountPercentage = product.DiscountPercentage,
                IsActive = product.IsActive
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
        {
            if (request.Price <= 0) return BadRequest(new { message = "Price must be greater than 0" });
            if (request.StockQuantity < 0) return BadRequest(new { message = "Stock cannot be negative" });

            var product = new Data.Models.Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl,
                DiscountPercentage = request.DiscountPercentage,
                IsActive = true
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = (await _context.Categories.FindAsync(product.CategoryId))?.Name ?? "",
                ImageUrl = product.ImageUrl,
                DiscountPercentage = product.DiscountPercentage,
                IsActive = product.IsActive
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CreateProductRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (request.Price <= 0) return BadRequest(new { message = "Price must be greater than 0" });
            if (request.StockQuantity < 0) return BadRequest(new { message = "Stock cannot be negative" });

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.CategoryId = request.CategoryId;
            product.ImageUrl = request.ImageUrl;
            product.DiscountPercentage = request.DiscountPercentage;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, UpdateStockRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.StockQuantity += request.QuantityChange;
            if (product.StockQuantity < 0) product.StockQuantity = 0;

            _context.StockTransactions.Add(new Data.Models.StockTransaction
            {
                ProductId = id,
                QuantityChange = request.QuantityChange,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            if (product.StockQuantity <= 5)
                await _notificationService.NotifyLowStockAsync(product);

            return Ok(new { product.Id, product.StockQuantity });
        }
    }
}
