using EcoHub.API.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace EcoHub.API.Data
{
    /// <summary>
    /// Idempotently adds a large catalog of products (200+) grouped by category.
    /// Adds only products whose Name does not already exist in the database.
    /// </summary>
    public static class ExpandedProductSeeder
    {
        // Use Unsplash direct photo IDs (curated, stable CDN URLs).
        private static string Img(string photoId) =>
            $"https://images.unsplash.com/photo-{photoId}?w=400&h=300&fit=crop&auto=format";

        public static async Task SeedAsync(AppDbContext context)
        {
            var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
            if (categories.Count == 0) return;

            var existingNames = (await context.Products.Select(p => p.Name).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toSeed = BuildCatalog();

            var newProducts = new List<Product>();
            foreach (var (name, desc, price, stock, catName, discount, photoId) in toSeed)
            {
                if (existingNames.Contains(name)) continue;
                if (!categories.TryGetValue(catName, out var catId)) continue;

                newProducts.Add(new Product
                {
                    Name = name,
                    Description = desc,
                    Price = price,
                    StockQuantity = stock,
                    CategoryId = catId,
                    DiscountPercentage = discount,
                    IsActive = true,
                    ImageUrl = Img(photoId)
                });
            }

            if (newProducts.Count > 0)
            {
                context.Products.AddRange(newProducts);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Assigns product images from DummyJSON groceries category (https://dummyjson.com/products/category/groceries).
        /// For each EcoHub product, tries to find the best matching DummyJSON grocery item
        /// based on Romanian keyword mapping (e.g. "Mere" → Apple, "Lapte" → Milk).
        /// Unmatched products fall back to a category-themed pool so a fruit never shows a steak image.
        /// Safe to run on every startup: idempotent.
        /// </summary>
        public static async Task FixDuplicateImageUrlsAsync(AppDbContext context)
        {
            var groceries = await FetchDummyJsonGroceriesAsync();
            if (groceries.Count == 0) return;

            // Build name → image lookup (case-insensitive) for keyword matching.
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, img) in groceries)
            {
                if (!string.IsNullOrWhiteSpace(name) && !byName.ContainsKey(name))
                    byName[name] = img;
            }

            // Generic food fallback when category pool is empty too.
            var genericFallback = byName.TryGetValue("Apple", out var apple) ? apple : groceries[0].image;

            var products = await context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .ToListAsync();
            var changed = false;

            foreach (var p in products)
            {
                var matched = FindBestGroceryImage(p.Name, byName);
                if (matched == null)
                {
                    // Category-based fallback: pick from curated pool for the EcoHub category.
                    var pool = GetCategoryPool(p.Category?.Name, byName);
                    matched = pool.Count > 0
                        ? pool[Math.Abs(p.Id) % pool.Count]
                        : genericFallback;
                }

                if (!string.Equals(p.ImageUrl, matched, StringComparison.OrdinalIgnoreCase))
                {
                    p.ImageUrl = matched;
                    changed = true;
                }
            }

            if (changed) await context.SaveChangesAsync();
        }

        // Per-category fallback pools (DummyJSON grocery item names). Only items that actually exist in DummyJSON.
        private static readonly Dictionary<string, string[]> CategoryPools = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fruits"]     = new[] { "Apple", "Kiwi", "Lemon", "Strawberry", "Mulberry" },
            ["Vegetables"] = new[] { "Cucumber", "Green Bell Pepper", "Green Chili Pepper", "Potatoes", "Red Onions" },
            ["Dairy"]      = new[] { "Milk", "Eggs" },
            ["Meat"]       = new[] { "Beef Steak", "Chicken Meat", "Fish Steak" },
            ["Fish"]       = new[] { "Fish Steak" },
            ["Bakery"]     = new[] { "Rice", "Honey Jar", "Eggs" },
            ["Bread"]      = new[] { "Rice", "Eggs", "Honey Jar" },
            ["Beverages"]  = new[] { "Juice", "Soft Drinks", "Water", "Nescafe Coffee" },
            ["Drinks"]     = new[] { "Juice", "Soft Drinks", "Water" },
            ["Pantry"]     = new[] { "Rice", "Cooking Oil", "Honey Jar", "Nescafe Coffee" },
            ["Sweets"]     = new[] { "Ice Cream", "Strawberry", "Honey Jar" },
            ["Candy"]      = new[] { "Ice Cream", "Strawberry", "Honey Jar" },
            ["Snacks"]     = new[] { "Potatoes", "Honey Jar", "Nescafe Coffee" },
            ["Frozen"]     = new[] { "Ice Cream", "Fish Steak", "Chicken Meat" },
            ["PetFood"]    = new[] { "Cat Food", "Dog Food" },
            ["Pet Food"]   = new[] { "Cat Food", "Dog Food" },
            ["BabyCare"]   = new[] { "Tissue Paper Box", "Milk" },
            ["Baby Care"]  = new[] { "Tissue Paper Box", "Milk" },
            ["Cleaning"]   = new[] { "Tissue Paper Box" },
            ["Household"]  = new[] { "Tissue Paper Box" },
            ["Cosmetics"]  = new[] { "Tissue Paper Box" },
            ["Health"]     = new[] { "Protein Powder", "Honey Jar" },
        };

        private static List<string> GetCategoryPool(string? categoryName, Dictionary<string, string> byName)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(categoryName)) return result;

            if (CategoryPools.TryGetValue(categoryName, out var names))
            {
                foreach (var n in names)
                    if (byName.TryGetValue(n, out var url))
                        result.Add(url);
            }
            return result;
        }

        // Romanian keywords → DummyJSON grocery product name (substring match on product.Name).
        // ORDER MATTERS: more-specific keywords must come BEFORE generic ones
        // (e.g. fish/meat must be before "ulei" so "Ton în Ulei" = fish, not oil).
        private static readonly (string[] keywords, string dummyName)[] KeywordMap =
        {
            // Meat / fish FIRST (before oil) so "Ton în Ulei", "Sardine în Ulei" get fish.
            (new[] { "somon", "macrou", "peste", "ton ", "hering", "sardin", "cod " }, "Fish Steak"),
            (new[] { "pui", "piept", "aripi", "pulpe", "ficat de pui" }, "Chicken Meat"),
            (new[] { " vita", "de vita", "muschi", "antricot", "porc", "salam", "carnati", "sunca", "kaizer", "parizer" }, "Beef Steak"),
            (new[] { "pisic" }, "Cat Food"),
            (new[] { "cain", "caine" }, "Dog Food"),
            // Fruits
            (new[] { "mere", "mar " }, "Apple"),
            (new[] { "lamai" }, "Lemon"),
            (new[] { "kiwi" }, "Kiwi"),
            (new[] { "capsun" }, "Strawberry"),
            (new[] { "zmeur", "mur ", "afine" }, "Mulberry"),
            // Vegetables
            (new[] { "cartof" }, "Potatoes"),
            (new[] { "ceapa" }, "Red Onions"),
            (new[] { "castrave" }, "Cucumber"),
            (new[] { "ardei iute", "chili" }, "Green Chili Pepper"),
            (new[] { "ardei" }, "Green Bell Pepper"),
            // Dairy / pantry
            (new[] { "lapte" }, "Milk"),
            (new[] { "oua" }, "Eggs"),
            (new[] { "miere" }, "Honey Jar"),
            (new[] { "orez" }, "Rice"),
            (new[] { "ulei" }, "Cooking Oil"),
            // Drinks / sweets
            (new[] { "apa mineral", "apa plat", "apa izvor" }, "Water"),
            (new[] { "coca-cola", "coca cola", "pepsi", "fanta", "sprite", "limonad", "ciocolata cald", "nesquik", "red bull", "schweppes", "bere ", "vin ", "divin" }, "Soft Drinks"),
            (new[] { "suc", "nectar" }, "Juice"),
            (new[] { "inghetat" }, "Ice Cream"),
            (new[] { "ciocolat", "bomboane", "caramel", "dulce" }, "Honey Jar"),
            (new[] { "cafea" }, "Nescafe Coffee"),
            (new[] { "proteine", "proteina" }, "Protein Powder"),
            (new[] { "servetel", "hartie igien" }, "Tissue Paper Box"),
        };

        private static string? FindBestGroceryImage(string productName, Dictionary<string, string> byName)
        {
            if (string.IsNullOrWhiteSpace(productName)) return null;
            var normalized = StripDiacritics(productName.ToLowerInvariant());

            foreach (var (keywords, dummyName) in KeywordMap)
            {
                if (keywords.Any(k => normalized.Contains(StripDiacritics(k.ToLowerInvariant()), StringComparison.Ordinal)))
                {
                    if (byName.TryGetValue(dummyName, out var url))
                        return url;
                }
            }
            return null;
        }

        private static string StripDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private static async Task<List<(string name, string image)>> FetchDummyJsonGroceriesAsync()
        {
            var list = new List<(string name, string image)>();
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var json = await http.GetStringAsync("https://dummyjson.com/products/category/groceries?limit=50");
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("products", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        string? name = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                        string? img = item.TryGetProperty("thumbnail", out var th) ? th.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(img))
                            list.Add((name!, img!));
                    }
                }
            }
            catch
            {
                // Network failure: return empty so caller keeps existing URLs.
            }
            return list;
        }

        // Tuple: (Name, Description, Price, Stock, Category, DiscountPercentage, UnsplashPhotoId)
        private static List<(string, string, decimal, int, string, decimal, string)> BuildCatalog()
        {
            var list = new List<(string, string, decimal, int, string, decimal, string)>();

            // ========== FRUITS (20) ==========
            list.Add(("Portocale", "Portocale dulci de Sicilia, 1kg", 35m, 80, "Fruits", 0, "1547514701-42782101795e"));
            list.Add(("Lămâi", "Lămâi proaspete, 1kg", 42m, 60, "Fruits", 0, "1590502593747-42a996133562"));
            list.Add(("Struguri Albi", "Struguri albi fără sâmburi, 1kg", 55m, 40, "Fruits", 10, "1537640538966-79f369143f8f"));
            list.Add(("Struguri Negri", "Struguri negri din Moldova, 1kg", 48m, 45, "Fruits", 0, "1596363505729-4190a9506133"));
            list.Add(("Pere", "Pere Williams dulci, 1kg", 32m, 50, "Fruits", 0, "1510627498534-cf7e9002facc"));
            list.Add(("Prune", "Prune coapte de Moldova, 1kg", 25m, 55, "Fruits", 15, "1465869185982-5a1a7522cbcb"));
            list.Add(("Piersici", "Piersici zemoase, 1kg", 38m, 40, "Fruits", 0, "1531171673193-98b26b645d23"));
            list.Add(("Caise", "Caise dulci de sezon, 1kg", 45m, 35, "Fruits", 0, "1595210629143-78ec73d1f62b"));
            list.Add(("Cireșe", "Cireșe roșii mari, 500g", 65m, 30, "Fruits", 0, "1528821128474-27f963b062bf"));
            list.Add(("Vișine", "Vișine acrișoare, 1kg", 42m, 30, "Fruits", 10, "1589876568181-a1d67c8c4303"));
            list.Add(("Căpșuni", "Căpșuni roșii proaspete, 500g", 58m, 50, "Fruits", 20, "1464965911861-746a04b4bca6"));
            list.Add(("Zmeură", "Zmeură de pădure, 250g", 75m, 25, "Fruits", 0, "1577069861033-55d04cec4ef5"));
            list.Add(("Afine", "Afine proaspete, 250g", 85m, 25, "Fruits", 0, "1498557850523-fd3d118b962e"));
            list.Add(("Kiwi", "Kiwi dulce-acrișor, 500g", 48m, 40, "Fruits", 0, "1585059895524-72359e06133a"));
            list.Add(("Ananas", "Ananas tropical, 1 buc", 65m, 25, "Fruits", 0, "1550258987-190a2d41a8ba"));
            list.Add(("Pepene Verde", "Pepene verde dulce, 1kg", 18m, 30, "Fruits", 0, "1587049352851-8d4e89133924"));
            list.Add(("Pepene Galben", "Pepene galben aromat, 1kg", 22m, 30, "Fruits", 0, "1571575173700-afb9492e6a50"));
            list.Add(("Grapefruit", "Grapefruit roz, 1kg", 38m, 35, "Fruits", 0, "1587496679742-bad4e8d33f1f"));
            list.Add(("Mango", "Mango copt, 1 buc", 45m, 30, "Fruits", 0, "1553279768-865429fa0078"));
            list.Add(("Rodie", "Rodie dulce, 1 buc", 42m, 25, "Fruits", 0, "1615485020471-0106b32bab62"));

            // ========== VEGETABLES (22) ==========
            list.Add(("Cartofi", "Cartofi noi din Moldova, 1kg", 12m, 200, "Vegetables", 0, "1518977676601-b53f82aba655"));
            list.Add(("Ceapă", "Ceapă galbenă, 1kg", 14m, 150, "Vegetables", 0, "1518977956812-cd3dbadaaf31"));
            list.Add(("Ceapă Roșie", "Ceapă roșie dulce, 1kg", 18m, 100, "Vegetables", 0, "1580201092675-a0a6a6cafbb1"));
            list.Add(("Usturoi", "Usturoi autohton, 250g", 28m, 80, "Vegetables", 0, "1540148426945-6cf22a6b2383"));
            list.Add(("Ardei Roșu", "Ardei capia roșu, 1kg", 42m, 60, "Vegetables", 10, "1525607551316-4a8e16d1f9ba"));
            list.Add(("Ardei Verde", "Ardei verde gras, 1kg", 38m, 60, "Vegetables", 0, "1580269905893-3b3cea0a31a0"));
            list.Add(("Roșii", "Roșii cherry dulci, 500g", 32m, 70, "Vegetables", 0, "1561136594-7f68413baa99"));
            list.Add(("Roșii Inimă de Bou", "Roșii cărnoase, 1kg", 45m, 50, "Vegetables", 0, "1592924357228-91a4daadcfea"));
            list.Add(("Castraveți", "Castraveți verzi crocanți, 1kg", 28m, 70, "Vegetables", 0, "1604977042946-1eecc30f269e"));
            list.Add(("Varză Albă", "Varză proaspătă, 1kg", 10m, 100, "Vegetables", 0, "1551030173-122aabc4489c"));
            list.Add(("Varză Roșie", "Varză roșie crocantă, 1kg", 15m, 60, "Vegetables", 0, "1594282486552-05b4d80fbb9f"));
            list.Add(("Conopidă", "Conopidă albă, 1 buc", 32m, 40, "Vegetables", 0, "1587334274328-64186a80aeee"));
            list.Add(("Dovlecei", "Dovlecei proaspeți, 1kg", 25m, 50, "Vegetables", 0, "1596097635121-14b8cabd2cd7"));
            list.Add(("Dovleac", "Dovleac de plăcintă, 1kg", 18m, 40, "Vegetables", 0, "1506917728037-b6af01a7d403"));
            list.Add(("Vinete", "Vinete violet, 1kg", 35m, 50, "Vegetables", 0, "1613825787113-e2f0b8d0b9c1"));
            list.Add(("Spanac", "Spanac proaspăt, 250g", 22m, 40, "Vegetables", 0, "1576045057995-568f588f82fb"));
            list.Add(("Salată Verde", "Salată crocantă, 1 buc", 18m, 60, "Vegetables", 0, "1622205313162-be1d5712a43b"));
            list.Add(("Rucola", "Rucola aromată, 100g", 28m, 40, "Vegetables", 0, "1622205313162-be1d5712a43b"));
            list.Add(("Țelină", "Țelină apio, 500g", 25m, 40, "Vegetables", 0, "1591184503968-4b8d7f2c2c6f"));
            list.Add(("Praz", "Praz verde, 1kg", 28m, 50, "Vegetables", 0, "1615485736980-cfc36c0f8e10"));
            list.Add(("Sfeclă Roșie", "Sfeclă roșie dulce, 1kg", 15m, 80, "Vegetables", 0, "1592924357228-91a4daadcfea"));
            list.Add(("Ciuperci Champignon", "Ciuperci proaspete, 500g", 42m, 60, "Vegetables", 0, "1504545102477-9c0e16d3bcac"));

            // ========== DAIRY (20) ==========
            list.Add(("Smântână 20%", "Smântână Nistru 400g", 28m, 80, "Dairy", 0, "1550583724-b2692b85b150"));
            list.Add(("Smântână 30%", "Smântână grasă JLC 400g", 35m, 60, "Dairy", 0, "1550583724-b2692b85b150"));
            list.Add(("Iaurt Natural Activia", "Iaurt Activia 300g", 22m, 100, "Dairy", 10, "1571212515416-fca325a8a3c7"));
            list.Add(("Iaurt cu Fructe Danone", "Iaurt Danone 125g", 12m, 150, "Dairy", 0, "1488477181946-6428a0291777"));
            list.Add(("Kefir JLC", "Kefir natural 1L", 25m, 70, "Dairy", 0, "1563636619-e9143da7973b"));
            list.Add(("Unt 82% Olimp", "Unt de țară Olimp 200g", 38m, 60, "Dairy", 0, "1628088062854-d1870b4553da"));
            list.Add(("Brânză de Vaci", "Brânză de vaci 5% 250g", 32m, 60, "Dairy", 0, "1631379578550-7038263db519"));
            list.Add(("Cașcaval Rucar", "Cașcaval Rucar 300g", 75m, 40, "Dairy", 0, "1486297678162-eb2a19b0a32d"));
            list.Add(("Mozzarella Galbani", "Mozzarella italiană 125g", 45m, 50, "Dairy", 0, "1625944525903-8bde3d0e3bfb"));
            list.Add(("Parmezan", "Parmezan Reggiano 200g", 145m, 30, "Dairy", 0, "1486297678162-eb2a19b0a32d"));
            list.Add(("Ricotta", "Ricotta italiană 250g", 58m, 40, "Dairy", 0, "1550583724-b2692b85b150"));
            list.Add(("Fetă Grecească", "Fetă tradițională grecească 200g", 62m, 45, "Dairy", 0, "1505252929842-837fd4dbe6e8"));
            list.Add(("Lapte Bătut", "Lapte bătut Nistru 500ml", 18m, 80, "Dairy", 0, "1563636619-e9143da7973b"));
            list.Add(("Smântână Fermentată", "Smântână fermentată 500g", 32m, 50, "Dairy", 0, "1550583724-b2692b85b150"));
            list.Add(("Mascarpone", "Mascarpone Galbani 250g", 65m, 30, "Dairy", 0, "1488477181946-6428a0291777"));
            list.Add(("Iaurt Grecesc", "Iaurt grecesc 10% 400g", 38m, 70, "Dairy", 0, "1571212515416-fca325a8a3c7"));
            list.Add(("Lapte Degresat", "Lapte degresat 0.5% 1L", 18m, 100, "Dairy", 0, "1563636619-e9143da7973b"));
            list.Add(("Brânză Topită Hochland", "Brânză topită 200g", 32m, 70, "Dairy", 0, "1486297678162-eb2a19b0a32d"));
            list.Add(("Brânză cu Mucegai", "Roquefort 150g", 95m, 25, "Dairy", 0, "1452195100486-9cc805987862"));
            list.Add(("Camembert President", "Camembert President 125g", 55m, 40, "Dairy", 0, "1486297678162-eb2a19b0a32d"));

            // ========== BAKERY (15) ==========
            list.Add(("Pâine Neagră", "Pâine neagră de secară 500g", 14m, 80, "Bakery", 0, "1568254183919-78a4f43a2877"));
            list.Add(("Pâine Integrală", "Pâine integrală cu semințe 400g", 18m, 70, "Bakery", 10, "1555507036-ab794f4ade50"));
            list.Add(("Chiflă", "Chiflă proaspătă cu susan, 6 buc", 15m, 100, "Bakery", 0, "1608198093002-ad4e005484ec"));
            list.Add(("Baghetă Franceză", "Baghetă franceză 300g", 22m, 50, "Bakery", 0, "1568254183919-78a4f43a2877"));
            list.Add(("Cornuri cu Unt", "Cornuri cu unt, 4 buc", 28m, 60, "Bakery", 0, "1623334044303-241021148842"));
            list.Add(("Covrig", "Covrig cu susan 150g", 8m, 120, "Bakery", 0, "1620189507187-cc5ecb0cb01d"));
            list.Add(("Croissant", "Croissant franțuzesc cu unt", 15m, 80, "Bakery", 0, "1555507036-ab794f4ade50"));
            list.Add(("Brioșă", "Brioșă cu ciocolată", 12m, 70, "Bakery", 0, "1587668178277-295251f900ce"));
            list.Add(("Plăcintă cu Brânză", "Plăcintă tradițională cu brânză", 25m, 50, "Bakery", 0, "1509440159596-02490f8ce2f7"));
            list.Add(("Plăcintă cu Mere", "Plăcintă dulce cu mere", 28m, 45, "Bakery", 15, "1568571780765-9276ac8b75a2"));
            list.Add(("Tartă cu Fructe", "Tartă cu fructe de sezon", 65m, 20, "Bakery", 0, "1488477181946-6428a0291777"));
            list.Add(("Biscuiți Eugenia", "Biscuiți Eugenia cu cacao 360g", 22m, 80, "Bakery", 0, "1558961363-fa8fdf82db35"));
            list.Add(("Cozonac", "Cozonac cu nucă și mac 500g", 85m, 30, "Bakery", 0, "1568571780765-9276ac8b75a2"));
            list.Add(("Pâine de Secară", "Pâine de secară cu chimen 400g", 16m, 60, "Bakery", 0, "1568254183919-78a4f43a2877"));
            list.Add(("Lipii", "Lipii arabești, 4 buc", 14m, 60, "Bakery", 0, "1568254183919-78a4f43a2877"));

            // ========== BEVERAGES (22) ==========
            list.Add(("Suc de Mere Santal", "Suc natural de mere 1L", 35m, 80, "Beverages", 0, "1622597460927-a8a67f4cfb5e"));
            list.Add(("Suc de Struguri", "Suc de struguri Rio 1L", 42m, 60, "Beverages", 10, "1544145945-f90425340c7e"));
            list.Add(("Apă Minerală Borsec", "Apă minerală Borsec 2L", 14m, 150, "Beverages", 0, "1550591063-2deff02b0ba9"));
            list.Add(("Apă Plată Aqua Unică", "Apă plată 2L", 10m, 200, "Beverages", 0, "1550591063-2deff02b0ba9"));
            list.Add(("Coca-Cola 1L", "Coca-Cola Original 1L", 28m, 150, "Beverages", 0, "1554866585-cd94860890b7"));
            list.Add(("Pepsi 1L", "Pepsi Cola 1L", 25m, 140, "Beverages", 0, "1625772299848-391b6a87d7b3"));
            list.Add(("Fanta Portocale", "Fanta portocale 1L", 25m, 100, "Beverages", 0, "1624552184280-9e9631bbeee9"));
            list.Add(("Sprite 1L", "Sprite lămâie-lime 1L", 25m, 100, "Beverages", 0, "1625772299848-391b6a87d7b3"));
            list.Add(("Schweppes Tonic", "Schweppes Indian Tonic 1L", 32m, 80, "Beverages", 0, "1554866585-cd94860890b7"));
            list.Add(("Ceai Verde Dilmah", "Ceai verde Dilmah 25 pliculețe", 45m, 70, "Beverages", 0, "1592155931584-901ac15763e3"));
            list.Add(("Ceai Negru Lipton", "Ceai negru Lipton Yellow 50 pl.", 42m, 90, "Beverages", 0, "1576092768241-dec231879fc3"));
            list.Add(("Cafea Măcinată Jacobs", "Cafea Jacobs Monarch 250g", 85m, 60, "Beverages", 10, "1495474472287-4d71bcdd2085"));
            list.Add(("Cafea Boabe Lavazza", "Cafea boabe Lavazza Qualità 1kg", 245m, 30, "Beverages", 0, "1447933601403-0c6688de566e"));
            list.Add(("Nescafé Gold", "Nescafé Gold instant 100g", 95m, 60, "Beverages", 0, "1559056199-641a0ac8b55e"));
            list.Add(("Limonadă Naturală", "Limonadă de casă 1L", 28m, 50, "Beverages", 0, "1523371054106-bbf80586c33c"));
            list.Add(("Suc de Roșii", "Suc natural de roșii 1L", 32m, 40, "Beverages", 0, "1622597460927-a8a67f4cfb5e"));
            list.Add(("Ciocolată Caldă Nesquik", "Ciocolată caldă Nesquik 400g", 65m, 50, "Beverages", 0, "1542990253-0d0f5be5f0ed"));
            list.Add(("Red Bull", "Energizant Red Bull 250ml", 35m, 100, "Beverages", 0, "1622543925917-763c34d1a86e"));
            list.Add(("Bere Chișinău", "Bere Chișinău Blondă 0.5L", 18m, 120, "Beverages", 0, "1608270586620-248524c67de9"));
            list.Add(("Vin Roșu Purcari", "Vin roșu Purcari Merlot 0.75L", 185m, 30, "Beverages", 0, "1474722883778-792e7990302f"));
            list.Add(("Vin Alb Cricova", "Vin alb Cricova Chardonnay 0.75L", 165m, 35, "Beverages", 15, "1510812431401-41d2bd2722f3"));
            list.Add(("Divin Național", "Divin Național XO 0.5L", 385m, 15, "Beverages", 0, "1569529465841-dfecdab7503b"));

            // ========== MEAT (15) ==========
            list.Add(("Piept de Pui", "Piept de pui proaspăt, 1kg", 95m, 60, "Meat", 0, "1604503468506-a8da13d82791"));
            list.Add(("Pulpe de Pui", "Pulpe de pui, 1kg", 65m, 70, "Meat", 0, "1604503468506-a8da13d82791"));
            list.Add(("Aripi de Pui", "Aripi de pui, 1kg", 55m, 65, "Meat", 10, "1569058242261-9e78e3f65f85"));
            list.Add(("Cotlet de Porc", "Cotlet de porc fără os, 1kg", 145m, 40, "Meat", 0, "1603360946369-dc9bb6258143"));
            list.Add(("Ceafă de Porc", "Ceafă de porc, 1kg", 125m, 45, "Meat", 0, "1603360946369-dc9bb6258143"));
            list.Add(("Mușchi de Vită", "Mușchi fraged de vită, 1kg", 285m, 25, "Meat", 0, "1607623814075-e51df1bdd2a0"));
            list.Add(("Antricot de Vită", "Antricot de vită premium, 1kg", 320m, 20, "Meat", 0, "1607623814075-e51df1bdd2a0"));
            list.Add(("Slănină Afumată", "Slănină afumată autohtonă, 500g", 85m, 50, "Meat", 0, "1544025162-d76694265947"));
            list.Add(("Mititei Tradiționali", "Mititei moldovenești, 500g", 75m, 60, "Meat", 0, "1587593810167-a84920ea0781"));
            list.Add(("Salam Victoria", "Salam Victoria 300g", 65m, 70, "Meat", 0, "1544025162-d76694265947"));
            list.Add(("Parizer", "Parizer de pui 400g", 42m, 80, "Meat", 0, "1544025162-d76694265947"));
            list.Add(("Șuncă Presată", "Șuncă presată Premium 300g", 85m, 50, "Meat", 0, "1544025162-d76694265947"));
            list.Add(("Kaizer", "Kaizer afumat 250g", 72m, 45, "Meat", 0, "1544025162-d76694265947"));
            list.Add(("Cârnăciori de Bere", "Cârnăciori pentru grătar 500g", 68m, 60, "Meat", 15, "1587593810167-a84920ea0781"));
            list.Add(("Ficat de Pui", "Ficat de pui proaspăt, 500g", 35m, 40, "Meat", 0, "1604503468506-a8da13d82791"));

            // ========== FISH (10) ==========
            list.Add(("Somon Proaspăt", "File de somon norvegian, 500g", 185m, 30, "Fish", 0, "1519708227418-c8fd9a32b7a2"));
            list.Add(("Păstrăv", "Păstrăv de lac, 1kg", 125m, 35, "Fish", 0, "1559847844-5315695dadae"));
            list.Add(("Cod File", "File de cod atlantic, 500g", 95m, 40, "Fish", 0, "1615141982883-c7ad0e69fd62"));
            list.Add(("Ton în Ulei", "Ton în ulei Rio Mare 160g", 28m, 100, "Fish", 0, "1565680018160-75e2e53ee96b"));
            list.Add(("Scrumbie Afumată", "Scrumbie afumată 300g", 65m, 40, "Fish", 0, "1559847844-5315695dadae"));
            list.Add(("Sardine în Ulei", "Sardine în ulei 125g", 22m, 120, "Fish", 10, "1565680018160-75e2e53ee96b"));
            list.Add(("Hering Afumat", "File de hering afumat 250g", 45m, 50, "Fish", 0, "1559847844-5315695dadae"));
            list.Add(("Caviar Roșu", "Caviar roșu de somon 100g", 285m, 20, "Fish", 0, "1633436375173-1e0e6b23b8c1"));
            list.Add(("Crevete Congelate", "Crevete regale congelate 500g", 165m, 30, "Fish", 0, "1565680018160-75e2e53ee96b"));
            list.Add(("Calamar", "Inele de calamar 500g", 125m, 25, "Fish", 0, "1565680018160-75e2e53ee96b"));

            // ========== FROZEN (10) ==========
            list.Add(("Pizza Dr. Oetker", "Pizza Ristorante Margherita", 55m, 60, "Frozen", 0, "1574071318508-1cdbab80d002"));
            list.Add(("Cartofi Prăjiți Congelați", "Cartofi pai McCain 1kg", 42m, 70, "Frozen", 10, "1630384060421-cb20d0e0649d"));
            list.Add(("Pui Pane Congelat", "Nuggets de pui 500g", 65m, 50, "Frozen", 0, "1562967914-608f82629710"));
            list.Add(("Pește Pane Congelat", "Pește pane Iglo 450g", 75m, 45, "Frozen", 0, "1562967914-608f82629710"));
            list.Add(("Legume Asortate", "Mix legume congelate 1kg", 38m, 60, "Frozen", 0, "1576045057995-568f588f82fb"));
            list.Add(("Înghețată Vanilie", "Înghețată vanilie Sandro 1L", 48m, 70, "Frozen", 0, "1563805042-7684c019e1cb"));
            list.Add(("Înghețată Ciocolată", "Înghețată ciocolată 1L", 48m, 65, "Frozen", 15, "1576506295286-5cda18df43e7"));
            list.Add(("Plăcintă Congelată", "Plăcintă cu brânză congelată 450g", 35m, 50, "Frozen", 0, "1574071318508-1cdbab80d002"));
            list.Add(("Găluște cu Cartofi", "Găluște cu cartofi 500g", 42m, 40, "Frozen", 0, "1562967914-608f82629710"));
            list.Add(("Pelmeni", "Pelmeni cu carne 900g", 65m, 55, "Frozen", 0, "1562967914-608f82629710"));

            // ========== SNACKS (15) ==========
            list.Add(("Chipsuri Lays Sare", "Chipsuri Lays sare 140g", 22m, 100, "Snacks", 0, "1566478989037-eec170784d0b"));
            list.Add(("Chipsuri Lays Smântână", "Chipsuri Lays smântână-ceapă 140g", 22m, 100, "Snacks", 0, "1613919113640-25732ec5e61f"));
            list.Add(("Pringles Original", "Pringles Original 165g", 42m, 80, "Snacks", 0, "1613919113640-25732ec5e61f"));
            list.Add(("Arahide Sărate", "Arahide prăjite sărate 200g", 28m, 70, "Snacks", 0, "1599599810769-bcde5a160d32"));
            list.Add(("Migdale Crude", "Migdale California 200g", 85m, 50, "Snacks", 0, "1508061253366-f7da158b6d46"));
            list.Add(("Nuci", "Miez de nucă autohton 200g", 75m, 60, "Snacks", 0, "1599599810769-bcde5a160d32"));
            list.Add(("Semințe Floarea Soarelui", "Semințe prăjite 200g", 18m, 100, "Snacks", 0, "1591209662757-396d87ba9d9e"));
            list.Add(("Pufuleți", "Pufuleți cu brânză 80g", 12m, 120, "Snacks", 0, "1566478989037-eec170784d0b"));
            list.Add(("Sticksuri", "Sticksuri sărate 100g", 14m, 100, "Snacks", 0, "1566478989037-eec170784d0b"));
            list.Add(("Popcorn Dulce", "Popcorn dulce 90g", 18m, 80, "Snacks", 0, "1578849278619-a5d6ef1aaaf0"));
            list.Add(("Covrigei Sărați", "Covrigei crocanți 200g", 16m, 90, "Snacks", 10, "1620189507187-cc5ecb0cb01d"));
            list.Add(("Crackers Belvita", "Crackers Belvita 250g", 32m, 70, "Snacks", 0, "1558961363-fa8fdf82db35"));
            list.Add(("Fistic Prăjit", "Fistic prăjit sărat 150g", 95m, 40, "Snacks", 0, "1599599810769-bcde5a160d32"));
            list.Add(("Caju Prăjit", "Caju prăjit 150g", 105m, 40, "Snacks", 0, "1599599810769-bcde5a160d32"));
            list.Add(("Mix Fructe Uscate", "Mix fructe uscate 200g", 58m, 50, "Snacks", 0, "1599599810769-bcde5a160d32"));

            // ========== PANTRY (25) ==========
            list.Add(("Ulei Floarea Soarelui Floris", "Ulei rafinat Floris 1L", 32m, 100, "Pantry", 0, "1474979266404-7eaacbcd87c5"));
            list.Add(("Ulei de Măsline Extra Virgin", "Ulei măsline Monini 500ml", 125m, 40, "Pantry", 0, "1474979266404-7eaacbcd87c5"));
            list.Add(("Zahăr Alb", "Zahăr cristal Moldova 1kg", 22m, 150, "Pantry", 0, "1596040033229-a9821ebd058d"));
            list.Add(("Zahăr Brun", "Zahăr brun cane 500g", 28m, 60, "Pantry", 0, "1596040033229-a9821ebd058d"));
            list.Add(("Sare Iodată", "Sare iodată 1kg", 8m, 200, "Pantry", 0, "1588165171080-c89acfa5ee83"));
            list.Add(("Făină Albă", "Făină albă 1kg", 14m, 180, "Pantry", 0, "1568718247028-f9b9dad1b76f"));
            list.Add(("Orez Basmati", "Orez basmati 1kg", 45m, 80, "Pantry", 0, "1586201375761-83865001e31c"));
            list.Add(("Orez Bob Rotund", "Orez cu bob rotund 1kg", 25m, 100, "Pantry", 10, "1586201375761-83865001e31c"));
            list.Add(("Paste Barilla Spaghetti", "Paste Barilla Spaghetti 500g", 28m, 120, "Pantry", 0, "1551462147-ff29053bfc14"));
            list.Add(("Paste Penne Rigate", "Paste Barilla Penne 500g", 28m, 110, "Pantry", 0, "1551462147-ff29053bfc14"));
            list.Add(("Mălai", "Mălai pentru mămăligă 1kg", 16m, 120, "Pantry", 0, "1568718247028-f9b9dad1b76f"));
            list.Add(("Fasole Uscată", "Fasole albă uscată 1kg", 32m, 80, "Pantry", 0, "1607301406259-dfb186e15de8"));
            list.Add(("Linte Roșie", "Linte roșie 500g", 28m, 70, "Pantry", 0, "1607301406259-dfb186e15de8"));
            list.Add(("Năut", "Năut uscat 500g", 25m, 60, "Pantry", 0, "1607301406259-dfb186e15de8"));
            list.Add(("Hrișcă", "Hrișcă 1kg", 32m, 80, "Pantry", 0, "1586201375761-83865001e31c"));
            list.Add(("Mei", "Mei decorticat 500g", 22m, 50, "Pantry", 0, "1586201375761-83865001e31c"));
            list.Add(("Oțet de Mere", "Oțet de mere natural 500ml", 22m, 80, "Pantry", 0, "1474979266404-7eaacbcd87c5"));
            list.Add(("Oțet Alb", "Oțet alb 1L", 12m, 100, "Pantry", 0, "1474979266404-7eaacbcd87c5"));
            list.Add(("Sos de Soia Kikkoman", "Sos de soia Kikkoman 150ml", 45m, 60, "Pantry", 0, "1474979266404-7eaacbcd87c5"));
            list.Add(("Ketchup Heinz", "Ketchup Heinz 570g", 55m, 80, "Pantry", 0, "1607118750000-c4a7b4f3e1b0"));
            list.Add(("Muștar Tecuci", "Muștar dulce Tecuci 300g", 22m, 70, "Pantry", 0, "1607118750000-c4a7b4f3e1b0"));
            list.Add(("Maioneză Calvé", "Maioneză Calvé 400g", 38m, 90, "Pantry", 10, "1607118750000-c4a7b4f3e1b0"));
            list.Add(("Miere de Albine", "Miere polifloră 500g", 75m, 60, "Pantry", 0, "1589308078059-be1415eab4c3"));
            list.Add(("Dulceață Căpșuni", "Dulceață de căpșuni 400g", 52m, 50, "Pantry", 0, "1528821128474-27f963b062bf"));
            list.Add(("Gem de Caise", "Gem de caise 400g", 48m, 45, "Pantry", 0, "1528821128474-27f963b062bf"));

            // ========== SWEETS (20) ==========
            list.Add(("Ciocolată Milka", "Ciocolată Milka Alpine Milk 100g", 28m, 150, "Sweets", 0, "1548907040-4baa42d10919"));
            list.Add(("Ciocolată Lindt", "Ciocolată Lindt 70% cacao 100g", 55m, 80, "Sweets", 10, "1548907040-4baa42d10919"));
            list.Add(("Bomboane M&Ms", "M&Ms Chocolate 125g", 42m, 100, "Sweets", 0, "1623660053975-cf75a8be0908"));
            list.Add(("Snickers", "Snickers clasic 50g", 12m, 200, "Sweets", 0, "1623660053975-cf75a8be0908"));
            list.Add(("Twix", "Twix clasic 50g", 12m, 180, "Sweets", 0, "1623660053975-cf75a8be0908"));
            list.Add(("Mars", "Mars clasic 51g", 12m, 180, "Sweets", 0, "1623660053975-cf75a8be0908"));
            list.Add(("Bounty", "Bounty cu cocos 57g", 14m, 150, "Sweets", 0, "1623660053975-cf75a8be0908"));
            list.Add(("KitKat", "KitKat 4 degete 41g", 12m, 180, "Sweets", 0, "1623660053975-cf75a8be0908"));
            list.Add(("Kinder Surprise", "Kinder Surprise 20g", 18m, 120, "Sweets", 0, "1548907040-4baa42d10919"));
            list.Add(("Kinder Bueno", "Kinder Bueno 43g", 18m, 130, "Sweets", 0, "1548907040-4baa42d10919"));
            list.Add(("Ferrero Rocher", "Ferrero Rocher T16 200g", 165m, 40, "Sweets", 0, "1548907040-4baa42d10919"));
            list.Add(("Biscuiți Oreo", "Oreo Original 154g", 32m, 100, "Sweets", 0, "1558961363-fa8fdf82db35"));
            list.Add(("Prăjituri de Casă", "Asortiment prăjituri 500g", 95m, 30, "Sweets", 0, "1488477181946-6428a0291777"));
            list.Add(("Halva", "Halva de floarea soarelui 400g", 48m, 50, "Sweets", 0, "1582058091505-f87a2e55a40f"));
            list.Add(("Baklava", "Baklava cu fistic 500g", 145m, 25, "Sweets", 0, "1582058091505-f87a2e55a40f"));
            list.Add(("Pastilă de Fructe", "Pastilă naturală de mere 150g", 32m, 50, "Sweets", 0, "1582058091505-f87a2e55a40f"));
            list.Add(("Jeleuri Haribo", "Haribo Goldbears 100g", 18m, 120, "Sweets", 0, "1582058091505-f87a2e55a40f"));
            list.Add(("Acadele", "Acadele asortate 200g", 22m, 80, "Sweets", 15, "1582058091505-f87a2e55a40f"));
            list.Add(("Covrigei Dulci", "Covrigei dulci cu glazură 300g", 28m, 60, "Sweets", 0, "1620189507187-cc5ecb0cb01d"));
            list.Add(("Rahat Turcesc", "Rahat asortat 400g", 62m, 40, "Sweets", 0, "1582058091505-f87a2e55a40f"));

            // ========== BABY CARE (15) ==========
            list.Add(("Scutece Pampers Mărime 3", "Pampers Baby-Dry mărime 3, 60 buc", 185m, 50, "BabyCare", 0, "1555252333-9f8e92e65df9"));
            list.Add(("Scutece Pampers Mărime 4", "Pampers Baby-Dry mărime 4, 54 buc", 195m, 45, "BabyCare", 10, "1555252333-9f8e92e65df9"));
            list.Add(("Scutece Huggies Mărime 5", "Huggies Ultra Comfort mărime 5, 42 buc", 185m, 40, "BabyCare", 0, "1555252333-9f8e92e65df9"));
            list.Add(("Șervețele Umede Pampers", "Pampers Sensitive 80 buc", 55m, 100, "BabyCare", 0, "1555252333-9f8e92e65df9"));
            list.Add(("Lapte Praf Nestle", "Nestle NAN 1 800g", 285m, 30, "BabyCare", 0, "1546552696-0f1a5c8b0e07"));
            list.Add(("Lapte Praf Nan 2", "Nestle NAN 2 800g", 285m, 28, "BabyCare", 0, "1546552696-0f1a5c8b0e07"));
            list.Add(("Piure Gerber Măr", "Piure Gerber măr 190g", 22m, 80, "BabyCare", 0, "1546552696-0f1a5c8b0e07"));
            list.Add(("Piure Gerber Banane", "Piure Gerber banane 190g", 22m, 80, "BabyCare", 0, "1546552696-0f1a5c8b0e07"));
            list.Add(("Biscuiți pentru Bebeluși", "Biscuiți Hipp 150g", 35m, 60, "BabyCare", 0, "1558961363-fa8fdf82db35"));
            list.Add(("Șampon Johnson's", "Șampon Johnson's Baby 300ml", 45m, 70, "BabyCare", 0, "1584306670957-acf935f5033c"));
            list.Add(("Cremă Bepanthen", "Cremă Bepanthen Baby 50g", 75m, 50, "BabyCare", 0, "1584306670957-acf935f5033c"));
            list.Add(("Săpun pentru Bebeluși", "Săpun Johnson's Baby 100g", 15m, 90, "BabyCare", 0, "1584306670957-acf935f5033c"));
            list.Add(("Biberon Avent", "Biberon Philips Avent 260ml", 95m, 30, "BabyCare", 0, "1584306670957-acf935f5033c"));
            list.Add(("Tetină Avent", "Set tetine Avent 2 buc", 55m, 40, "BabyCare", 0, "1584306670957-acf935f5033c"));
            list.Add(("Suzetă", "Suzetă silicon Chicco", 35m, 60, "BabyCare", 0, "1584306670957-acf935f5033c"));

            // ========== CLEANING (22) ==========
            list.Add(("Detergent Ariel 3kg", "Detergent rufe Ariel Mountain Spring 3kg", 225m, 40, "Cleaning", 10, "1563453392212-326f5e854473"));
            list.Add(("Detergent Persil 2kg", "Detergent Persil Color 2kg", 185m, 45, "Cleaning", 0, "1563453392212-326f5e854473"));
            list.Add(("Detergent Lichid Persil", "Persil lichid 2.1L", 165m, 50, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Balsam Lenor", "Balsam rufe Lenor 1.2L", 75m, 80, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Înălbitor Ace", "Înălbitor Ace 1L", 28m, 90, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Dezinfectant Domestos", "Domestos WC 750ml", 42m, 100, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Detergent Vase Fairy", "Fairy Lemon 900ml", 48m, 90, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Cif Cremă", "Cif Cremă curățare 500ml", 35m, 70, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Mr. Muscle Geamuri", "Soluție geamuri Mr. Muscle 500ml", 32m, 80, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Odorizant Glade", "Odorizant Glade spray 300ml", 38m, 60, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Sac Menajer 60L", "Saci menajeri 60L, 20 buc", 22m, 100, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Prosop Hârtie Zewa", "Prosop bucătărie Zewa 2 role", 28m, 90, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Hârtie Igienică Regina", "Regina Blitz 8 role", 55m, 80, "Cleaning", 15, "1527515637462-cff94eecc1ac"));
            list.Add(("Șervețele de Masă", "Șervețele de masă 100 buc", 18m, 120, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Burete pentru Vase", "Set 5 bureți pentru vase", 12m, 150, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Mănuși de Cauciuc", "Mănuși menaj mărimea M", 18m, 100, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Mătură", "Mătură cu coadă", 48m, 40, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Mop cu Storcător", "Set mop cu găleată", 185m, 30, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Pronto Parchet", "Pronto lustruire parchet 500ml", 55m, 50, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Săpun Lichid Palmolive", "Palmolive lichid 300ml", 32m, 80, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Pastă Dinți Colgate", "Colgate Total 100ml", 28m, 100, "Cleaning", 0, "1527515637462-cff94eecc1ac"));
            list.Add(("Gel de Duș Nivea", "Nivea Men Deep 250ml", 42m, 70, "Cleaning", 0, "1527515637462-cff94eecc1ac"));

            return list;
        }
    }
}
