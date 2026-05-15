using EcoHub.API.Data.Models;
using EcoHub.API.Services;
using EcoHub.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context, ISettingsService settingsService)
        {
            // Seed categories (idempotent per category)
            var defaultCategories = new[]
            {
                new Category { Name = "Fruits", Description = "Fresh fruits" },
                new Category { Name = "Vegetables", Description = "Organic vegetables" },
                new Category { Name = "Dairy", Description = "Milk, cheese, yogurt" },
                new Category { Name = "Bakery", Description = "Bread and pastries" },
                new Category { Name = "Beverages", Description = "Drinks and juices" },
                new Category { Name = "Meat", Description = "Meat and poultry" },
                new Category { Name = "Fish", Description = "Fish and seafood" },
                new Category { Name = "Frozen", Description = "Frozen foods" },
                new Category { Name = "Snacks", Description = "Snacks and chips" },
                new Category { Name = "Pantry", Description = "Pantry staples" },
                new Category { Name = "Sweets", Description = "Candy and chocolates" },
                new Category { Name = "BabyCare", Description = "Baby products" },
                new Category { Name = "Cleaning", Description = "Household cleaning" }
            };
            foreach (var cat in defaultCategories)
            {
                if (!await context.Categories.AnyAsync(c => c.Name == cat.Name))
                    context.Categories.Add(cat);
            }
            await context.SaveChangesAsync();

            // Seed admin user
            if (!await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            {
                var admin = new User
                {
                    Email = "admin@ecohub.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    FirstName = "System",
                    LastName = "Admin",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };
                context.Users.Add(admin);
                await context.SaveChangesAsync();

                context.Carts.Add(new Cart { UserId = admin.Id });
                await context.SaveChangesAsync();
            }

            // Seed sample products
            if (!await context.Products.AnyAsync())
            {
                var fruits = await context.Categories.FirstAsync(c => c.Name == "Fruits");
                var vegetables = await context.Categories.FirstAsync(c => c.Name == "Vegetables");
                var dairy = await context.Categories.FirstAsync(c => c.Name == "Dairy");
                var bakery = await context.Categories.FirstAsync(c => c.Name == "Bakery");
                var beverages = await context.Categories.FirstAsync(c => c.Name == "Beverages");

                context.Products.AddRange(
                    new Product { Name = "Mere", Description = "Mere roşi dulci, proaspete", Price = 28, StockQuantity = 50, CategoryId = fruits.Id, DiscountPercentage = 15, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400&h=300&fit=crop" },
                    new Product { Name = "Banane", Description = "Banane organice", Price = 18, StockQuantity = 60, CategoryId = fruits.Id, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400&h=300&fit=crop" },
                    new Product { Name = "Morcovi", Description = "Morcovi proaspeůi", Price = 15, StockQuantity = 40, CategoryId = vegetables.Id, DiscountPercentage = 10, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1447175008436-054170c2e979?w=400&h=300&fit=crop" },
                    new Product { Name = "Broccoli", Description = "Broccoli verde", Price = 35, StockQuantity = 30, CategoryId = vegetables.Id, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1459411552884-841db9b3cc2d?w=400&h=300&fit=crop" },
                    new Product { Name = "Lapte", Description = "Lapte integral 1L", Price = 22, StockQuantity = 100, CategoryId = dairy.Id, DiscountPercentage = 20, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400&h=300&fit=crop" },
                    new Product { Name = "Brânză Cheddar", Description = "Brânză maturată", Price = 85, StockQuantity = 25, CategoryId = dairy.Id, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?w=400&h=300&fit=crop" },
                    new Product { Name = "Pâine Albă", Description = "Pâine feliată", Price = 12, StockQuantity = 45, CategoryId = bakery.Id, DiscountPercentage = 25, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1509440159596-02490f8ce2f7?w=400&h=300&fit=crop" },
                    new Product { Name = "Suc de Portocale", Description = "Suc natural de portocale 1L", Price = 45, StockQuantity = 35, CategoryId = beverages.Id, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1621506289935-18a677b0a497?w=400&h=300&fit=crop" }
                );
                await context.SaveChangesAsync();
            }

            // Seed settings
            var defaultSettings = new Dictionary<string, string>
            {
                { "OrdersEnabled", "true" },
                { "RegistrationEnabled", "true" },
                { "ProductsVisible", "true" }
            };

            foreach (var setting in defaultSettings)
            {
                var existing = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == setting.Key);
                if (existing == null)
                {
                    context.SystemSettings.Add(new SystemSetting
                    {
                        Key = setting.Key,
                        Value = setting.Value,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
