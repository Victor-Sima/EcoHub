 # EcoHub — 8-Week Daily Commit Plan (Mon–Fri)

> 40 commits total = 8 weeks × 5 working days.
> Each day: 1 `git add` + 1 `git commit`. Copy-paste the PowerShell block, done.
> Run everything from the solution root: `c:\Users\victo\Desktop\EcoHub`.

---

## 🔧 ONE-TIME SETUP (run once, BEFORE Day 1)

> The project is already fully written on disk. We initialise an empty Git repo, add a `.gitignore`, and then each day we stage only the files for that day. Everything else stays "untracked" until its scheduled day.

```powershell
cd c:\Users\victo\Desktop\EcoHub

# 1. initialise repo
git init
git branch -M main

# 2. configure identity (change to your data)
git config user.name  "Victor"
git config user.email "victor@example.com"

# 3. create .gitignore
@"
bin/
obj/
*.user
.vs/
_img_check.*
*.log
"@ | Out-File -Encoding utf8 .gitignore

# 4. first empty-tree commit
git add .gitignore
git commit -m "chore: initial repository setup with .gitignore"
```

After this, run **one block per working day** below.
To backdate commits, prefix the commit line with `$env:GIT_AUTHOR_DATE="2026-03-02T10:00:00"; $env:GIT_COMMITTER_DATE="2026-03-02T10:00:00";`

---

# 🗓️ WEEK 1 — Solution & Shared Library

## Day 1 — Solution file
```powershell
git add EcoHub.sln
git commit -m "feat: add Visual Studio solution file"
```

## Day 2 — Shared project + Enums
```powershell
git add EcoHub.Shared/EcoHub.Shared.csproj EcoHub.Shared/Enums/
git commit -m "feat(shared): scaffold shared library with role and status enums"
```

## Day 3 — Shared constants + Auth/User DTOs
```powershell
git add EcoHub.Shared/Constants/ EcoHub.Shared/Models/UserDto.cs EcoHub.Shared/Models/AuthDtos.cs
git commit -m "feat(shared): add constants and user/auth DTOs"
```

## Day 4 — Product / Category DTOs
```powershell
git add EcoHub.Shared/Models/ProductDto.cs EcoHub.Shared/Models/CategoryDto.cs
git commit -m "feat(shared): add product and category DTOs"
```

## Day 5 — Cart / Order / Payment / Notification DTOs
```powershell
git add EcoHub.Shared/Models/
git commit -m "feat(shared): complete cart, order, payment and notification DTOs"
```

---

# 🗓️ WEEK 2 — Backend Data Layer

## Day 6 — API project skeleton
```powershell
git add EcoHub.API/EcoHub.API.csproj EcoHub.API/appsettings.json EcoHub.API/appsettings.Development.json EcoHub.API/Properties/ EcoHub.API/EcoHub.API.http
git commit -m "feat(api): scaffold ASP.NET Core Web API project"
```

## Day 7 — Core entity models: User / Category / Product
```powershell
git add EcoHub.API/Data/Models/User.cs EcoHub.API/Data/Models/Category.cs EcoHub.API/Data/Models/Product.cs
git commit -m "feat(api): add User, Category and Product entity models"
```

## Day 8 — Shopping entities: Cart / CartItem / Order / OrderItem
```powershell
git add EcoHub.API/Data/Models/Cart.cs EcoHub.API/Data/Models/CartItem.cs EcoHub.API/Data/Models/Order.cs EcoHub.API/Data/Models/OrderItem.cs
git commit -m "feat(api): add Cart and Order entity models"
```

## Day 9 — Supporting entities: Payment / Notification / Stock / Settings
```powershell
git add EcoHub.API/Data/Models/Payment.cs EcoHub.API/Data/Models/Notification.cs EcoHub.API/Data/Models/StockTransaction.cs EcoHub.API/Data/Models/SystemSetting.cs
git commit -m "feat(api): add payment, notification, stock and settings entities"
```

## Day 10 — EF Core DbContext + initial migration
```powershell
git add EcoHub.API/Data/AppDbContext.cs EcoHub.API/Migrations/
git commit -m "feat(api): configure AppDbContext and add initial EF Core migration"
```

---

# 🗓️ WEEK 3 — Backend Core Services & Controllers

## Day 11 — DbSeeder (categories + default admin)
```powershell
git add EcoHub.API/Data/DbSeeder.cs
git commit -m "feat(api): seed default categories and administrator account"
```

## Day 12 — Authentication service
```powershell
git add EcoHub.API/Services/IAuthService.cs EcoHub.API/Services/AuthService.cs
git commit -m "feat(api): implement BCrypt-based authentication service"
```

## Day 13 — Auth controller (login / register)
```powershell
git add EcoHub.API/Controllers/AuthController.cs
git commit -m "feat(api): expose /api/auth endpoints for login and register"
```

## Day 14 — Products controller (listing, search, discounts)
```powershell
git add EcoHub.API/Controllers/ProductsController.cs
git commit -m "feat(api): add products controller with search and discount endpoints"
```

## Day 15 — Categories controller
```powershell
git add EcoHub.API/Controllers/CategoriesController.cs
git commit -m "feat(api): add categories CRUD controller"
```

---

# 🗓️ WEEK 4 — Backend Advanced Services

## Day 16 — Cart service + controller
```powershell
git add EcoHub.API/Services/ICartService.cs EcoHub.API/Services/CartService.cs EcoHub.API/Controllers/CartController.cs
git commit -m "feat(api): implement shopping cart service and endpoints"
```

## Day 17 — Order service + controller
```powershell
git add EcoHub.API/Services/IOrderService.cs EcoHub.API/Services/OrderService.cs EcoHub.API/Controllers/OrdersController.cs
git commit -m "feat(api): implement order processing service and endpoints"
```

## Day 18 — Payments + Notifications service
```powershell
git add EcoHub.API/Controllers/PaymentsController.cs EcoHub.API/Services/INotificationService.cs EcoHub.API/Services/NotificationService.cs
git commit -m "feat(api): add payments controller and notification service"
```

## Day 19 — Notifications controller + SignalR hub
```powershell
git add EcoHub.API/Controllers/NotificationsController.cs EcoHub.API/Hubs/
git commit -m "feat(api): expose notifications endpoint and real-time SignalR hub"
```

## Day 20 — Dashboard / Reports / Settings / Users + Program.cs wire-up + expanded seeder
```powershell
git add EcoHub.API/Services/IDashboardService.cs EcoHub.API/Services/DashboardService.cs EcoHub.API/Services/IReportService.cs EcoHub.API/Services/ReportService.cs EcoHub.API/Services/ISettingsService.cs EcoHub.API/Services/SettingsService.cs EcoHub.API/Controllers/DashboardController.cs EcoHub.API/Controllers/ReportsController.cs EcoHub.API/Controllers/SettingsController.cs EcoHub.API/Controllers/UsersController.cs EcoHub.API/Data/ExpandedProductSeeder.cs EcoHub.API/Program.cs
git commit -m "feat(api): wire admin services, users controller and 200+ product seeder"
```

---

# 🗓️ WEEK 5 — Blazor Web Foundation

## Day 21 — Web project scaffold + static assets
```powershell
git add EcoHub.Web/EcoHub.Web.csproj EcoHub.Web/Program.cs EcoHub.Web/Properties/ EcoHub.Web/wwwroot/
git commit -m "feat(web): scaffold Blazor WebAssembly project with static assets"
```

## Day 22 — App shell, imports, layout, navigation
```powershell
git add EcoHub.Web/App.razor EcoHub.Web/_Imports.razor EcoHub.Web/Layout/
git commit -m "feat(web): add App shell, main layout and navigation menu"
```

## Day 23 — Authentication state provider
```powershell
git add EcoHub.Web/Auth/
git commit -m "feat(web): implement JWT-based custom authentication state provider"
```

## Day 24 — API client services (products, cart, auth)
```powershell
git add EcoHub.Web/Services/
git commit -m "feat(web): add typed HTTP services for API communication"
```

## Day 25 — Login and Register pages
```powershell
git add EcoHub.Web/Pages/Login.razor EcoHub.Web/Pages/Register.razor
git commit -m "feat(web): add login and registration pages"
```

---

# 🗓️ WEEK 6 — Blazor Web Pages

## Day 26 — Home page (hero + featured products)
```powershell
git add EcoHub.Web/Pages/Home.razor
git commit -m "feat(web): add home page with featured products"
```

## Day 27 — Products catalogue page
```powershell
git add EcoHub.Web/Pages/Products.razor
git commit -m "feat(web): add products catalogue with filter and search"
```

## Day 28 — Product details page
```powershell
git add EcoHub.Web/Pages/ProductDetails.razor
git commit -m "feat(web): add product details page with add-to-cart"
```

## Day 29 — Shopping cart + Orders history
```powershell
git add EcoHub.Web/Pages/Cart.razor EcoHub.Web/Pages/Orders.razor
git commit -m "feat(web): add shopping cart and order history pages"
```

## Day 30 — Account profile + Discounts page
```powershell
git add EcoHub.Web/Pages/Account.razor EcoHub.Web/Pages/Discounts.razor
git commit -m "feat(web): add account profile and discounts pages"
```

---

# 🗓️ WEEK 7 — WPF Admin Core

## Day 31 — Admin project scaffold + App + themes
```powershell
git add EcoHub.Admin/EcoHub.Admin.csproj EcoHub.Admin/App.xaml EcoHub.Admin/App.xaml.cs EcoHub.Admin/AssemblyInfo.cs EcoHub.Admin/Themes/
git commit -m "feat(admin): scaffold WPF admin project with themes"
```

## Day 32 — MainWindow shell + API client service
```powershell
git add EcoHub.Admin/MainWindow.xaml EcoHub.Admin/MainWindow.xaml.cs EcoHub.Admin/Services/
git commit -m "feat(admin): add main window shell and HTTP service layer"
```

## Day 33 — Login screen
```powershell
git add EcoHub.Admin/Views/LoginView.xaml EcoHub.Admin/Views/LoginView.xaml.cs
git commit -m "feat(admin): add administrator login view"
```

## Day 34 — Dashboard (KPIs + charts)
```powershell
git add EcoHub.Admin/Views/DashboardView.xaml EcoHub.Admin/Views/DashboardView.xaml.cs
git commit -m "feat(admin): add dashboard with KPIs and sales charts"
```

## Day 35 — Products management + product editor dialog
```powershell
git add EcoHub.Admin/Views/ProductsView.xaml EcoHub.Admin/Views/ProductsView.xaml.cs EcoHub.Admin/Views/ProductDialog.xaml EcoHub.Admin/Views/ProductDialog.xaml.cs
git commit -m "feat(admin): add product management view with edit dialog"
```

---

# 🗓️ WEEK 8 — WPF Admin Finish + Polish

## Day 36 — Categories + Orders views
```powershell
git add EcoHub.Admin/Views/CategoriesView.xaml EcoHub.Admin/Views/CategoriesView.xaml.cs EcoHub.Admin/Views/OrdersView.xaml EcoHub.Admin/Views/OrdersView.xaml.cs
git commit -m "feat(admin): add categories and orders management views"
```

## Day 37 — Users + Notifications views
```powershell
git add EcoHub.Admin/Views/UsersView.xaml EcoHub.Admin/Views/UsersView.xaml.cs EcoHub.Admin/Views/NotificationsView.xaml EcoHub.Admin/Views/NotificationsView.xaml.cs
git commit -m "feat(admin): add user administration and notifications center"
```

## Day 38 — Settings view + final admin wiring
```powershell
git add EcoHub.Admin/Views/SettingsView.xaml EcoHub.Admin/Views/SettingsView.xaml.cs
git commit -m "feat(admin): add system settings view"
```

## Day 39 — Robust product images (DummyJSON keyword mapping)
```powershell
# ExpandedProductSeeder was updated earlier to fetch themed grocery images
# Razor pages also got onerror fallbacks — commit them together as a polish pass.
git add EcoHub.API/Data/ExpandedProductSeeder.cs EcoHub.Web/Pages/Home.razor EcoHub.Web/Pages/Products.razor EcoHub.Web/Pages/ProductDetails.razor EcoHub.Web/Pages/Discounts.razor
git commit -m "fix: assign thematic grocery images and add onerror fallback"
```

> ⚠️ Only works if on earlier days you committed an **older** version of `ExpandedProductSeeder.cs` / the Razor pages. If you already committed the latest version on Day 20 / Week 6, replace Day 39 with a small polish commit (e.g., a README tweak or a cosmetic CSS change).

## Day 40 — Release: README + tag v1.0.0
```powershell
# create project README
@"
# EcoHub

Full-stack grocery e-commerce platform.

## Projects
- **EcoHub.API** — ASP.NET Core 9 Web API + EF Core + SignalR
- **EcoHub.Web** — Blazor WebAssembly storefront
- **EcoHub.Admin** — WPF administration desktop app
- **EcoHub.Shared** — DTOs / enums shared by all projects

## Run
\`\`\`powershell
dotnet run --project EcoHub.API --launch-profile https
dotnet run --project EcoHub.Web
dotnet run --project EcoHub.Admin
\`\`\`
"@ | Out-File -Encoding utf8 README.md

git add README.md DAILY_COMMITS.md
git commit -m "docs: add project README and finalise v1.0.0"
git tag -a v1.0.0 -m "EcoHub 1.0.0 — full-stack release"
```

---

# ✅ Verify at the end

```powershell
git log --oneline
# should print exactly 41 lines (setup + 40 days)

git status
# should print: "nothing to commit, working tree clean"
```

If `git status` shows leftover files, append them on Day 40 before tagging:

```powershell
git add .
git commit --amend --no-edit
git tag -d v1.0.0
git tag -a v1.0.0 -m "EcoHub 1.0.0 — full-stack release"
```

---

# 📌 Tips

1. **Nothing forces you to commit in a single day.** If you miss a day, run two blocks the next day.
2. **Each block is idempotent for `git add`**: if a path is already tracked, `git add` is harmless.
3. **To push to GitHub** at the end of Week 8:
   ```powershell
   git remote add origin https://github.com/<you>/EcoHub.git
   git push -u origin main --tags
   ```
4. **To push incrementally** (e.g., every Friday):
   ```powershell
   git push origin main
   ```
