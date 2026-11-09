# ShopSphere — সম্পূর্ণ প্রজেক্ট গাইড (বাংলা)

এই ডকুমেন্টে আছে —

1. [প্রজেক্ট পরিচিতি](#১-প্রজেক্ট-পরিচিতি)
2. [কী কী লাগবে (Prerequisites)](#২-কী-কী-লাগবে-prerequisites)
3. [ধাপে ধাপে Setup](#৩-ধাপে-ধাপে-setup)
4. [Login তথ্য ও Seed ডেটা](#৪-login-তথ্য-ও-seed-ডেটা)
5. [ফোল্ডার ও ফাইল কাঠামো — প্রতিটার কাজ](#৫-ফোল্ডার-ও-ফাইল-কাঠামো--প্রতিটার-কাজ)
6. [Feature ধরে ধরে টেস্ট করার গাইড](#৬-feature-ধরে-ধরে-টেস্ট-করার-গাইড)
7. [সমস্যা ও সমাধান (Troubleshooting)](#৭-সমস্যা-ও-সমাধান-troubleshooting)
8. [কীভাবে পরিবর্তন / নতুন feature যোগ করবেন](#৮-কীভাবে-পরিবর্তন--নতুন-feature-যোগ-করবেন)

---

## ১. প্রজেক্ট পরিচিতি

**ShopSphere** একটা সম্পূর্ণ e-commerce web application — সামনে **storefront** (কাস্টমার যা দেখে) আর পেছনে
**admin panel** (দোকান পরিচালনা)।

| বিষয় | ব্যবহৃত টেকনোলজি |
|---|---|
| Framework | ASP.NET Core 8 (MVC + Areas) |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core 8 (code-first + migration) |
| Authentication | ASP.NET Core Identity (Cookie) |
| Frontend | Bootstrap 5 + jQuery + অল্প vanilla JS |
| Logging | Serilog (console + ফাইল) |
| Architecture | Clean / Layered — `Domain → Application → Infrastructure → Web` |

**সবচেয়ে গুরুত্বপূর্ণ কথা:** পুরো database (গঠন + sample data) একটামাত্র ফাইলে —
`Database/ECommerceDB.sql`। এই ফাইলটা import করলেই database তৈরি; আলাদা করে migration চালানোর দরকার নেই।

---

## ২. কী কী লাগবে (Prerequisites)

| টুল | ভার্সন | কেন |
|---|---|---|
| **.NET SDK** | 8.0 | প্রজেক্ট build ও run করতে |
| **SQL Server** | 2019+ / Express / **LocalDB** / Developer | database |
| **sqlcmd** অথবা **SSMS** | যেকোনো একটা | `.sql` ফাইল import করতে |
| (ঐচ্ছিক) Visual Studio 2022 / VS Code / Rider | — | কোড এডিট করতে |

চেক করুন:
```powershell
dotnet --version      # 8.0.xxx দেখাবে
sqlcmd -?             # command পাওয়া গেলে ঠিক আছে
```

---

## ৩. ধাপে ধাপে Setup

### ধাপ ১ — Database তৈরি (`.sql` ফাইল import)

প্রজেক্ট ফোল্ডারের ভেতরে গিয়ে —

**পদ্ধতি ক — sqlcmd দিয়ে (সবচেয়ে সহজ):**
```powershell
# Windows Authentication, লোকাল SQL Server
sqlcmd -S localhost -E -i "Database\ECommerceDB.sql"

# LocalDB হলে
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "Database\ECommerceDB.sql"

# SQL Login (sa) হলে
sqlcmd -S localhost -U sa -P "your_password" -i "Database\ECommerceDB.sql"
```
শেষে `ECommerceDB seed complete.` দেখালে সফল।

**পদ্ধতি খ — SSMS দিয়ে:**
1. SSMS খুলে `Database/ECommerceDB.sql` ফাইলটা open করুন।
2. উপরে **Query → SQLCMD Mode** on করুন (স্ক্রিপ্টে `GO` batch আছে, তাই এটা লাগবে)।
3. **Execute (F5)** চাপুন।

এই স্ক্রিপ্ট যা করে:
- `ECommerceDB` database না থাকলে তৈরি করে
- সব table, primary key, foreign key, index, constraint তৈরি করে (Identity-র table সহ)
- seed data ঢোকায় (admin user, roles, category, brand, ~২৪টা product, slider, testimonial, setting, `WELCOME10` coupon)
- `__EFMigrationsHistory` টেবিলে migration record লেখে — তাই app চালু হলে EF ভাবে schema আপডেটেড

> স্ক্রিপ্টটা বারবার চালানো নিরাপদ — প্রতিটা object আর row guard করা আছে।

### ধাপ ২ — Connection string ঠিক করা

`src/ECommerce.Web/appsettings.json` ফাইল খুলে `DefaultConnection` লাইনটা আপনার SQL Server অনুযায়ী বদলান:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ECommerceDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

| পরিস্থিতি | Server= অংশ |
|---|---|
| লোকাল SQL Server (Windows auth) | `Server=localhost;...;Trusted_Connection=True` |
| SQL Server Express | `Server=localhost\SQLEXPRESS;...;Trusted_Connection=True` |
| LocalDB | `Server=(localdb)\MSSQLLocalDB;...;Trusted_Connection=True` |
| SQL Login | `Server=localhost;...;User Id=sa;Password=your_password` |

সব ক্ষেত্রে শেষে `;TrustServerCertificate=True` রাখুন।

> নিরাপত্তার জন্য চাইলে `appsettings.json` না বদলে **user-secrets** ব্যবহার করতে পারেন:
> ```powershell
> cd src/ECommerce.Web
> dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;..."
> ```

### ধাপ ৩ — প্রজেক্ট run করা

প্রজেক্টের root ফোল্ডার থেকে:
```powershell
dotnet run --project src/ECommerce.Web
```
Console-এ যে URL দেখাবে (যেমন `https://localhost:7022` বা `http://localhost:5066`) সেটা browser-এ খুলুন।

প্রথমবার build হতে কিছুক্ষণ লাগবে। "Now listening on:" লাইন দেখালে app চালু।

### ধাপ ৪ — কাজ করছে কিনা যাচাই

- `/` — home page লোড হবে (slider, category, product ইত্যাদি)
- `/c/electronics` — category listing
- `/account/login` দিয়ে admin login করুন → `/Admin/Dashboard`-এ যাবে

### `.sql` ফাইল ছাড়া চালাতে চাইলে (শুধু EF দিয়ে)

`appsettings.json`-এ:
```json
"DatabaseInitialization": { "MigrateOnStartup": true, "SeedOnStartup": true }
```
তারপর একবার `dotnet run` করলে EF নিজেই database বানিয়ে seed করবে। অথবা ম্যানুয়ালি:
```powershell
dotnet tool restore
dotnet ef database update --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Web
```

---

## ৪. Login তথ্য ও Seed ডেটা

| ভূমিকা | Email | Password |
|---|---|---|
| **Admin** | `admin@ecommerce.local` | `Admin@123` |
| Customer | নিজে register করুন (যেকোনো email) — email confirm লাগে না |

**Seed data:**
- ৬টা মূল category + ৭টা sub-category (Electronics → Smartphones/Laptops/Audio ইত্যাদি)
- ৫টা brand (Aurora, NimbusTech, Vantage, Meadow, PulseFit)
- ~২৪টা product (কিছু featured, কিছু new arrival, কিছুতে discount)
- ৩টা hero slider, ৪টা testimonial, ~১৪টা site setting
- Coupon **`WELCOME10`** — ১০% ছাড়, সর্বনিম্ন অর্ডার $50, সর্বোচ্চ ছাড় $30

**Payment টেস্ট কার্ড (নকল gateway):**
- `4242 4242 4242 4242` → সফল
- Luhn-valid যেকোনো নম্বর যেটা `0000` দিয়ে শেষ → declined

---

## ৫. ফোল্ডার ও ফাইল কাঠামো — প্রতিটার কাজ

```
ECommerce.sln                      ← Visual Studio solution ফাইল
├── src/
│   ├── ECommerce.Domain/          ← Layer 1: শুধু entity, কোনো dependency নেই
│   ├── ECommerce.Application/      ← Layer 2: interface, DTO, business contract
│   ├── ECommerce.Infrastructure/  ← Layer 3: EF Core, Identity, service implementation
│   └── ECommerce.Web/             ← Layer 4: MVC — controller, view, wwwroot
├── Database/
│   └── ECommerceDB.sql            ← পুরো database এক ফাইলে (import করার জিনিস)
├── docs/
│   ├── Planning.md                ← architecture ও ১০ phase-এর পরিকল্পনা
│   └── PROJECT-GUIDE-BN.md        ← এই ফাইল
├── README.md                      ← ইংরেজি সংক্ষিপ্ত গাইড
├── .gitignore, .editorconfig
└── .config/dotnet-tools.json      ← dotnet-ef টুল (migration চালাতে)
```

Dependency-র দিক: **Domain ← Application ← Infrastructure ← Web** (নিচের layer উপরের কিছু জানে না)।

---

### 5.1 `ECommerce.Domain` — মূল entity (কোনো লজিক নেই, শুধু ডেটার গঠন)

| ফাইল | কাজ |
|---|---|
| `Common/BaseEntity.cs` | সব entity-র `int Id` |
| `Common/AuditableEntity.cs` | `CreatedAt/By`, `UpdatedAt/By` — কে কখন বানাল/বদলাল |
| `Common/IAuditableEntity.cs`, `ISoftDeletable.cs` | audit ও soft-delete চিহ্নিত করার interface |
| `Entities/Category.cs` | product category — `ParentCategoryId` দিয়ে নিজের ভেতর নিজে (multi-level) |
| `Entities/Brand.cs` | brand |
| `Entities/Product.cs` | product — দাম, discount, stock, `IsFeatured/IsNewArrival`, rating cache |
| `Entities/ProductImage.cs` | product-এর ছবি (একটা `IsMain`) |
| `Entities/ProductReview.cs` | কাস্টমার review (rating + comment) |
| `Entities/Cart.cs`, `CartItem.cs` | শপিং কার্ট — `UserId` (লগইন) বা `AnonymousId` (গেস্ট) |
| `Entities/WishlistItem.cs` | wishlist (user + product) |
| `Entities/Address.cs` | কাস্টমারের সেভ করা ঠিকানা |
| `Entities/Order.cs` | অর্ডার — সব amount **base currency (USD)**-তে, সাথে `CurrencyCode` + `ExchangeRate` snapshot, ঠিকানার snapshot |
| `Entities/OrderItem.cs` | অর্ডারের প্রতিটা লাইন — product-এর নাম/দাম snapshot করা |
| `Entities/OrderStatusHistory.cs` | অর্ডার status বদলের ইতিহাস (timeline) |
| `Entities/Payment.cs` | পেমেন্ট রেকর্ড (method, status, transaction id) |
| `Entities/Coupon.cs` | ডিসকাউন্ট কুপন |
| `Entities/Slider.cs` | home page-এর hero banner |
| `Entities/Testimonial.cs` | কাস্টমার মতামত (home page) |
| `Entities/NewsletterSubscriber.cs` | newsletter-এ subscribe করা email |
| `Entities/ContactMessage.cs` | Contact form থেকে আসা মেসেজ |
| `Entities/SiteSetting.cs` | key/value setting (logo, ফোন, currency rate ইত্যাদি) |
| `Enums/Enums.cs` | `OrderStatus`, `PaymentStatus`, `PaymentMethodType`, `DiscountType`, `AddressType` |

---

### 5.2 `ECommerce.Application` — interface ও contract (কী করা হবে, কীভাবে নয়)

এখানে শুধু **interface + DTO (data transfer object)**। আসল কোড থাকে Infrastructure-এ।

| ফোল্ডার / ফাইল | কাজ |
|---|---|
| `Common/Models/Result.cs` | operation সফল/ব্যর্থ + error message ফেরত দেওয়ার class |
| `Common/Models/PagedResult.cs` | পেজিনেশনসহ লিস্ট (items + page + total) |
| `Common/Models/SiteSettingsSnapshot.cs` | `SiteSetting` টেবিলের strongly-typed রূপ |
| `Common/SlugGenerator.cs` | "Nice Product" → "nice-product" (URL-friendly) |
| `Common/Interfaces/ISiteSettingsService.cs` | site setting পড়া (cache করা) |
| `Common/Interfaces/ICurrentUserService.cs` | এখন কোন user request করছে |
| `Common/Interfaces/IDateTimeProvider.cs` | `DateTime.UtcNow`-এর abstraction (test-friendly) |
| `Common/Interfaces/IFileStorage.cs` | ফাইল আপলোড/ডিলিট (`wwwroot/uploads`) |
| `Common/Interfaces/IEmailSender.cs` | email পাঠানো (dev-এ শুধু log/ফাইল) |
| `Common/Interfaces/INewsletterService.cs` | newsletter subscribe |
| `Navigation/INavigationService.cs` + `NavCategoryDto.cs` | header/footer-এর category মেনু (cache করা) |
| `Catalog/ICatalogQueryService.cs` + `CatalogDtos.cs` + `ProductQuery.cs` | storefront: product list (filter/sort/page), product detail, related, search |
| `Catalog/IReviewService.cs` | review জমা দেওয়া |
| `Catalog/Admin/AdminCatalogContracts.cs` | admin: Category / Brand / Product CRUD-এর interface ও edit-model |
| `Home/HomePageDto.cs` (+ `IHomePageService`) | পুরো home page-এর ডেটা এক call-এ (cache করা) |
| `Cms/CmsContracts.cs` | admin CMS: Slider, Testimonial, Settings, Subscriber, ContactMessage |
| `Cart/CartContracts.cs` | `ICartService` + `CartOwner` (user নাকি guest) |
| `Wishlist/WishlistContracts.cs` | `IWishlistService` |
| `Orders/AddressContracts.cs` | `IAddressService` (ঠিকানা CRUD) |
| `Orders/OrderContracts.cs` | `IOrderService` (checkout, order place, history, track), `IOrderAdminService` (admin), `ICouponService` |
| `Payments/PaymentContracts.cs` | `IPaymentGateway` (gateway abstraction), `IPaymentService` (পেমেন্ট process) |
| `Admin/DashboardContracts.cs` | `IDashboardService` (KPI, chart), `IUserAdminService` (user manage) |

---

### 5.3 `ECommerce.Infrastructure` — আসল implementation

| ফোল্ডার / ফাইল | কাজ |
|---|---|
| **`DependencyInjection.cs`** | `AddInfrastructure()` — সব service, DbContext, Identity DI-তে register করে |
| `Persistence/ApplicationDbContext.cs` | EF Core-এর DbContext — সব `DbSet<T>`, global soft-delete filter |
| `Persistence/ApplicationDbContextFactory.cs` | design-time factory (`dotnet ef` command-এর জন্য) |
| `Persistence/Configurations/*.cs` | প্রতিটা entity-র table config — column length, index, unique, foreign key, decimal precision |
| `Persistence/Interceptors/AuditableEntitySaveChangesInterceptor.cs` | save করার সময় নিজে থেকে `CreatedAt/By` ইত্যাদি বসায়, hard-delete → soft-delete করে |
| `Persistence/Seed/ApplicationDbContextSeed.cs` | C# seeder (EF দিয়ে বানালে যে ডেটা ঢুকবে — `ECommerceDB.sql`-এর seed অংশের সাথে মিল রাখা) |
| `Persistence/DbInitializer.cs` | startup-এ migrate/seed চালানোর helper (`appsettings`-এ toggle) |
| `Migrations/20260908045042_InitialCreate*.cs` | একমাত্র EF migration — পুরো schema |
| `Identity/ApplicationUser.cs` | Identity user + `FullName`, `ProfileImageUrl`, `IsActive`, `LastLoginAt` |
| `Identity/ApplicationRole.cs` | role + `Roles.Admin` / `Roles.Customer` constant |
| `Identity/CurrentUserService.cs` | `HttpContext.User` থেকে user id/name বের করে |
| `Identity/AdditionalUserClaimsPrincipalFactory.cs` | login-এ `full_name` claim যোগ করে (header-এ "Hi, নাম" দেখাতে) |
| `Services/SiteSettingsService.cs` | `SiteSetting` টেবিল → `SiteSettingsSnapshot`, `IMemoryCache` (১০ মিনিট) |
| `Services/NavigationService.cs` | category tree cache; admin category বদলালে `Invalidate()` |
| `Services/NewsletterService.cs` | email validate করে subscribe |
| `Services/HomePageService.cs` | home page-এর সব section এক call-এ, cache (৫ মিনিট) |
| `Services/DateTimeProvider.cs`, `LocalFileStorage.cs`, `DevEmailSender.cs` | ছোট utility service |
| `Services/Catalog/CatalogQueryService.cs` | storefront product query — filter, sort, brand facet, price range, pagination |
| `Services/Catalog/ReviewService.cs` | review জমা → auto-approve → product rating recalculate |
| `Services/Catalog/CategoryAdminService.cs` | Category CRUD — slug unique, cycle guard, delete-এ child/product চেক |
| `Services/Catalog/BrandAdminService.cs` | Brand CRUD |
| `Services/Catalog/ProductAdminService.cs` | Product CRUD + image add/delete/set-main |
| `Services/Catalog/SlugHelper.cs` | slug unique করা (`-2`, `-3` suffix) |
| `Services/Cms/CmsServices.cs` | Slider, Testimonial, Settings, Subscriber, ContactMessage — সব admin CMS service একসাথে |
| `Services/Shopping/CartService.cs` | কার্ট add/update/remove, guest→user merge, stock cap, shipping estimate |
| `Services/Shopping/WishlistService.cs` | wishlist toggle, move-to-cart |
| `Services/Orders/AddressService.cs` | ঠিকানা CRUD, set-default |
| `Services/Orders/CouponService.cs` | কুপন validate (তারিখ, limit, min-order, max-discount) |
| `Services/Orders/OrderService.cs` | **সবচেয়ে বড়** — checkout context বানানো, order place করা (transaction: snapshot, stock কমানো, cart clear, order number `ORD-yyyyMMdd-NNNN`), history/track query |
| `Services/Orders/OrderAdminService.cs` | admin order list/detail, status transition + history, cancel-এ restock, payment status, dashboard metric |
| `Services/Payments/FakePaymentGateway.cs` | নকল gateway — Luhn চেক, `appsettings`-এ success/decline config, transaction id বানায় |
| `Services/Payments/PaymentService.cs` | পেমেন্ট process — gateway ডাকা, সফলে `Payment.Paid` + `Order.Confirmed` + history; ব্যর্থে `Failed` + retry |
| `Services/Admin/DashboardService.cs` | KPI, ১৪ দিনের sales series, recent order, top product, low stock |
| `Services/Admin/UserAdminService.cs` | user list, admin role দেওয়া/নেওয়া, activate/deactivate (নিজেকে নয়, শেষ admin নয়) |

---

### 5.4 `ECommerce.Web` — যা browser-এ দেখা যায়

#### গুরুত্বপূর্ণ ফাইল

| ফাইল | কাজ |
|---|---|
| **`Program.cs`** | app-এর starting point — DI, middleware pipeline, routing, error handling |
| `appsettings.json` | connection string, `DatabaseInitialization`, `Payment`, Serilog config |
| `appsettings.Development.json` | dev-এ শুধু logging override |
| `Properties/launchSettings.json` | `dotnet run`-এর port ও profile |
| `ECommerce.Web.csproj` | project ফাইল — package reference |

#### `Controllers/` — storefront

| Controller | Route | কাজ |
|---|---|---|
| `HomeController` | `/`, `/contact`, `/faq`, `/shipping`, `/privacy` | home page, info page, contact form |
| `CatalogController` | `/c/{slug}`, `/search`, `/deals` | category listing, search, deals — filter/sort/page |
| `ProductController` | `/p/{slug}`, `/p/{slug}/review` | product detail, review জমা |
| `CartController` | `/cart`, `/cart/update`, `/cart/remove` | কার্ট পেজ |
| `WishlistController` | `/wishlist`, `/wishlist/remove`, `/wishlist/move-to-cart` | wishlist পেজ (`[Authorize]`) |
| `CheckoutController` | `/checkout`, `/checkout/apply-coupon`, `/checkout/place` | checkout (`[Authorize]`) |
| `OrderController` | `/order/confirmation/{n}`, `/order/pay/{n}`, `/order/track` | confirmation, payment page, guest tracking |
| `AccountController` | `/account/...` | register, login, logout, profile, password, address book, order history |
| `PreferencesController` | `/preferences/currency` | currency cookie সেট |
| `ErrorController` | `/error/{code}` | 404/500 friendly page |

#### `Controllers/Api/` — AJAX (JSON ফেরত, `ApiResponse` খামে)

| Controller | Route | কাজ |
|---|---|---|
| `CartApiController` | `/api/cart/add|update|remove|summary` | কার্ট AJAX + header badge আপডেট |
| `WishlistApiController` | `/api/wishlist/toggle|ids` | wishlist heart toggle (`[Authorize]` → না হলে 401) |
| `NewsletterApiController` | `/api/newsletter` | footer/home-এর newsletter form |

#### `Areas/Admin/Controllers/` — admin panel (`[Authorize(Roles="Admin")]`)

| Controller | Route | কাজ |
|---|---|---|
| `DashboardController` | `/Admin/Dashboard` | KPI, chart, recent order, low stock |
| `CategoriesController` | `/Admin/Categories` | Category CRUD |
| `BrandsController` | `/Admin/Brands` | Brand CRUD |
| `ProductsController` | `/Admin/Products` | Product CRUD + image আপলোড |
| `OrdersController` | `/Admin/Orders` | order list/detail, status ও payment বদলানো |
| `UsersController` | `/Admin/Users` | user list, role, activate/deactivate |
| `SlidersController` | `/Admin/Sliders` | hero slider CRUD |
| `TestimonialsController` | `/Admin/Testimonials` | testimonial CRUD |
| `SubscribersController` | `/Admin/Subscribers` | newsletter subscriber list + CSV export |
| `MessagesController` | `/Admin/Messages` | contact message inbox |
| `SettingsController` | `/Admin/Settings` | সব site setting একসাথে edit |

#### `Infrastructure/` — Web-এর ছোট helper

| ফাইল | কাজ |
|---|---|
| `ApiResponse.cs` | `/api/*`-এর standard JSON: `{ success, data, message, errors }` |
| `CartOwnerAccessor.cs` | এখনকার কার্টের মালিক কে — লগইন user নাকি guest cookie (`cart_ref`) |
| `CurrencyCookie.cs` | currency choice cookie পড়া/লেখা |
| `MoneyFormatter.cs` | base USD → নির্বাচিত currency-তে রূপান্তর ও format (`$`, `৳`) |
| `OrderUi.cs` | order status → CSS class / label, পরের valid status list |
| `ControllerExtensions.cs` | `this.SetBreadcrumb(...)` — breadcrumb সেট করা |

#### `ViewComponents/` — page-এর পুনঃব্যবহারযোগ্য অংশ

| Component | কাজ |
|---|---|
| `SiteHeaderViewComponent` | header — logo, search, currency switcher, wishlist/cart badge, mini-cart, account menu |
| `MainNavViewComponent` | multi-level category navbar (desktop dropdown + mobile off-canvas) |
| `SiteFooterViewComponent` | footer — contact, social, quick link, newsletter, trust badge |
| `BreadcrumbViewComponent` | breadcrumb (Home বাদে সব পেজে) |
| `CurrencySwitcherViewComponent` | `$ / ৳` switcher dropdown |

#### `TagHelpers/`

| ফাইল | কাজ |
|---|---|
| `ActiveRouteTagHelper.cs` | বর্তমান পেজের সাথে মিললে nav link-এ `is-active`/`active` class বসায় |

#### `Models/` — view model (form binding + validation)

`Account/`, `Catalog/`, `Home/`, `Orders/`, `BreadcrumbItem.cs`, `ErrorPageViewModel.cs` — প্রতিটা পেজের ফর্মের জন্য
DataAnnotation সহ ছোট class।

#### `Views/` — Razor পেজ

| ফোল্ডার | কী আছে |
|---|---|
| `Views/Shared/_Layout.cshtml` | storefront-এর মূল template (header, breadcrumb, flash message, footer, toast) |
| `Views/Shared/_AdminLayout.cshtml` | admin panel-এর template (sidebar + topbar) |
| `Views/Shared/_ProductCard.cshtml` | একটা product card (list-এ বারবার ব্যবহৃত) |
| `Views/Shared/_ProductRow.cshtml` | home page-এ product-এর সারি |
| `Views/Shared/_Pagination.cshtml` | পেজিনেশন কন্ট্রোল |
| `Views/Shared/_StarRating.cshtml` | ★ rating |
| `Views/Shared/_OrderSummary.cshtml`, `_OrderTimeline.cshtml` | order item টেবিল + status timeline (confirmation/detail/admin-এ পুনঃব্যবহৃত) |
| `Views/Shared/Components/*/Default.cshtml` | উপরের ViewComponent-গুলোর HTML |
| `Views/Home/` | Index (home), Contact, Faq, Shipping, Privacy |
| `Views/Catalog/List.cshtml` | category / search / deals — একই পেজ |
| `Views/Product/Details.cshtml` | product detail + review |
| `Views/Cart/`, `Views/Wishlist/` | কার্ট ও wishlist পেজ |
| `Views/Checkout/Index.cshtml` | checkout ফর্ম |
| `Views/Order/` | Confirmation, Pay (card ফর্ম), Track |
| `Views/Account/` | Login, Register, Profile, Addresses, AddressForm, Orders, OrderDetail, AccessDenied, `_AccountNav` |
| `Views/Error/Status.cshtml` | 404/500 পেজ |
| `Areas/Admin/Views/**` | সব admin পেজ — প্রতিটা module-এ `Index` + `Create`/`Edit` + `_Form` partial |

#### `wwwroot/` — static ফাইল

| ফাইল/ফোল্ডার | কাজ |
|---|---|
| `css/site.css` | পুরো storefront-এর design (color token, header, product card, cart, checkout, error page — সব) |
| `css/admin.css` | admin panel-এর design (table, form, dashboard KPI, chart) |
| `js/site.js` | add-to-cart, wishlist toggle, quantity stepper, toast, badge আপডেট, newsletter, sticky header |
| `lib/` | Bootstrap 5, jQuery, jquery-validation (LibMan দিয়ে আনা) |
| `uploads/products/` | admin থেকে আপলোড করা product ছবি (git-ignored) |
| `favicon.ico` | tab icon |

#### `App_Data/logs/`

Serilog-এর দৈনিক log ফাইল (`log-yyyyMMdd.txt`)। git-ignored।

---

### 5.5 `Database/ECommerceDB.sql` — এক ফাইলে পুরো database

তিনটা অংশ:
1. **Header** — `CREATE DATABASE`, `SET QUOTED_IDENTIFIER ON` (Identity-র filtered index-এর জন্য জরুরি)
2. **Schema** — `dotnet ef migrations script --idempotent` দিয়ে বানানো, প্রতিটা table guard করা
3. **Seed** — admin user (valid Identity password hash), role, category, brand, product, slider, testimonial, setting, coupon — প্রতিটা `IF NOT EXISTS` দিয়ে guard করা

Entity model বদলালে এই ফাইল কীভাবে update করবেন — `README.md`-এর *Keeping the SQL file in sync* অংশে।

---

## ৬. Feature ধরে ধরে টেস্ট করার গাইড

> প্রতিটা টেস্টে "কী করবেন" আর "কী দেখতে পাবেন" আলাদা করে দেওয়া। App চালু রেখে browser-এ করুন।

### ৬.১ Storefront — অতিথি (login ছাড়া)

| # | কী করবেন | প্রত্যাশিত ফল |
|---|---|---|
| 1 | `/` খুলুন | Hero slider (৩টা slide, নিজে নিজে ঘোরে), feature strip, "Shop by category" (৬টা card), "Deals of the day" (countdown timer চলছে), Featured / New arrivals / Best sellers সারি, testimonial, brand strip, newsletter band |
| 2 | Header-এ **currency** `$ / ৳` toggle করুন → `৳` বাছুন | পুরো পেজের দাম টাকায় বদলে যাবে (`৳12,791` এর মতো), header-এ `৳ BDT` |
| 3 | Navbar-এ **Electronics**-এ hover করুন | dropdown-এ Smartphones / Laptops / Audio দেখাবে; click করলে `/c/electronics` |
| 4 | মোবাইল সাইজে window ছোট করুন → **hamburger** icon | বাম দিক থেকে off-canvas menu, accordion দিয়ে category খোলে |
| 5 | Category পেজে **বাম sidebar** — একটা brand checkbox দিন, price min/max দিন, rating 4★ দিন, "Apply filters" | শুধু মিল থাকা product; উপরে active-filter chip; chip-এ click করলে ঐ filter বাদ |
| 6 | **Sort** dropdown → "Price: low to high" | কম দাম আগে; URL-এ `?sort=1` |
| 7 | নিচে **pagination** (product বেশি হলে) | পরের পেজ, URL-এ `?page=2` |
| 8 | Header **search**-এ "headphones" লিখে enter | `/search?q=headphones` — মিল থাকা product |
| 9 | যেকোনো product-এ click → **product detail** | ছবির gallery (thumbnail-এ click করলে main ছবি বদলায়), দাম (discount থাকলে কাটা দাম), stock, description, "You may also like", breadcrumb `Home / Electronics / Audio / ...` |
| 10 | Product card বা detail-এ **"Add to cart"** | ডান-নিচে সবুজ toast "Added to cart", header-এ cart badge বাড়ে |
| 11 | Header cart icon-এ hover/click → **mini-cart** | item গুলো, subtotal, "View cart" / "Checkout" |
| 12 | `/cart` — quantity **+ / −** চাপুন, একটা item **✕** দিয়ে remove | মোট আপডেট, subtotal $50+ হলে "free shipping unlocked", না হলে "Add $X more for free shipping" bar |
| 13 | কোনো product-এ **heart** icon চাপুন (login ছাড়া) | toast "Please log in", ~1 সেকেন্ড পর login পেজে redirect |
| 14 | ভুল URL — `/p/nonexistent` বা `/xyz` | সুন্দর **404 page** ("Page not found"), header/footer সহ |
| 15 | Footer-এ **newsletter** email দিয়ে Subscribe | toast "You're subscribed" |
| 16 | `/faq`, `/shipping`, `/privacy` | আসল content (FAQ accordion ইত্যাদি) |
| 17 | `/order/track` — ভুল নম্বর/email দিন | "We couldn't find an order..." |

### ৬.২ Account (কাস্টমার)

| # | কী করবেন | প্রত্যাশিত ফল |
|---|---|---|
| 1 | `/account/register` — নাম, email, phone, password (৬+ অক্ষর, বড়/ছোট হাতের অক্ষর, সংখ্যা, চিহ্ন) | সফলে সরাসরি login হয়ে home-এ; header-এ "Hi, নাম" + "Log out" |
| 2 | ভুল দিন (password mismatch, ছোট password) | field-এর নিচে লাল error (client-side + server-side) |
| 3 | Login করা অবস্থায় **heart** চাপুন | toast "Added to wishlist", header wishlist badge বাড়ে; আবার চাপলে remove |
| 4 | `/wishlist` — একটা item **"Move to cart"** | cart-এ যায়, wishlist থেকে সরে |
| 5 | Guest অবস্থায় কার্টে কিছু রাখুন → তারপর **login/register** | guest কার্ট merge হয়ে user কার্টে চলে আসে |
| 6 | Account menu → **Profile** — নাম/phone বদলে Save | সবুজ flash "profile updated" |
| 7 | Profile-এ **Change password** | সফলে flash; ভুল current password দিলে error |
| 8 | Account → **Addresses** → "Add address" | নতুন ঠিকানা; প্রথমটা auto-default; "Make default" / "Delete" |
| 9 | Product-এ **review** লিখুন (rating + ১০+ অক্ষর comment) | flash "review posted", রিভিউ তালিকায় দেখায়, একই product-এ আবার দিলে "already reviewed" |

### ৬.৩ Checkout ও Order

| # | কী করবেন | প্রত্যাশিত ফল |
|---|---|---|
| 1 | কার্টে $50+ এর জিনিস রেখে `/checkout` | ঠিকানা নির্বাচন / নতুন ঠিকানা ফর্ম, payment method, coupon box, order note, ডানে summary |
| 2 | Coupon box-এ **`WELCOME10`** → Apply | summary-তে "Discount" লাইন, "Coupon applied" |
| 3 | **Cash on Delivery** বেছে "Place order" | সরাসরি `/order/confirmation/ORD-...` — "Thank you", COD note; cart খালি হয়ে যায় |
| 4 | নতুন order-এ **Online Payment** বেছে "Place order" | `/order/pay/ORD-...` — card ফর্ম + মোট টাকা |
| 5 | Card `4242 4242 4242 4242`, expiry `12/29`, CVC `123` → Pay | সফল → confirmation; order status **Confirmed**, payment **Paid**, transaction id দেখায় |
| 6 | CVC-তে `1` দিন | "3–4 digits" validation error |
| 7 | ভুল card নম্বর দিন | "Enter a valid card number" — একই পেজে থেকে retry করা যায় |
| 8 | Account → **My Orders** → একটা order | items, ঠিকানা, payment, **status timeline** (Pending → ...) |
| 9 | Online order যদি pay না করে থাকেন — order detail-এ | "waiting for online payment" banner + "Complete payment" button |
| 10 | Logout করে `/order/track` — order নম্বর + checkout-এর email | order-এর status ও timeline দেখায় (login ছাড়াই) |

### ৬.৪ Admin — Dashboard

`admin@ecommerce.local` / `Admin@123` দিয়ে login → নিজে থেকে `/Admin/Dashboard`।

| দেখবেন | বিস্তারিত |
|---|---|
| **KPI cards** | Paid revenue (+ আজকের), Orders (+ আজ), Pending orders (0-এর বেশি হলে হলুদ + "Review now"), Products (+ low stock সংখ্যা), Customers (+ এ সপ্তাহে নতুন) |
| **Revenue chart** | শেষ ১৪ দিনের bar chart — bar-এ hover করলে ঐ দিনের revenue ও order সংখ্যা |
| **Top products** | সবচেয়ে বেশি বিক্রি হওয়া ৫টা |
| **Recent orders** | শেষ ৮টা — click করে detail |
| **Low stock** | ৫ বা কম stock-এর product — click করে edit |
| Sidebar-এ বর্তমান পেজ **highlight** থাকে |

### ৬.৫ Admin — Catalog

| # | কী করবেন | প্রত্যাশিত ফল |
|---|---|---|
| 1 | **Categories** → "New category" — নাম দিন, parent বাছুন | তালিকায় indent সহ দেখায়; slug auto; storefront navbar-এও সাথে সাথে যোগ হয় (cache invalidate) |
| 2 | Category **Edit** — parent বদলান | নিজেকে বা নিজের child-কে parent করা যায় না |
| 3 | যে category-তে product/child আছে সেটা **Delete** | "Move or delete the sub-categories first" / "reassign products first" |
| 4 | **Brands** — CRUD | product-এ ব্যবহৃত brand delete করা যায় না |
| 5 | **Products** → "New product" — নাম, SKU, category, দাম, stock, flag, **ছবি আপলোড** (একাধিক, ৩MB পর্যন্ত, jpg/png/webp) | সফলে edit পেজে; storefront-এ `/p/{slug}` দেখা যায় |
| 6 | Product edit-এ **image manager** | "Set main" / "Delete"; নতুন ছবি যোগ |
| 7 | Discount price ≥ regular price দিন | "Discount price must be below the regular price" |
| 8 | Product list-এ **search** ও pagination | নাম/SKU দিয়ে খোঁজে |

### ৬.৬ Admin — Orders

| # | কী করবেন | প্রত্যাশিত ফল |
|---|---|---|
| 1 | **Orders** — status dropdown দিয়ে filter, order নম্বর/customer দিয়ে search | মিল থাকা order |
| 2 | একটা order **Open** | items, দুই ঠিকানা, totals, **payment history table** (transaction id, gateway response), status **timeline** |
| 3 | Status dropdown — শুধু বৈধ পরের ধাপ দেখায় (Pending → Confirmed/Cancelled ইত্যাদি), note দিয়ে "Update status" | timeline-এ নতুন entry |
| 4 | Order **Cancelled** করুন | ঐ order-এর product-এর stock **ফেরত যোগ** হয় |
| 5 | COD order **Delivered** করুন | payment নিজে থেকে **Paid** হয়ে যায় |
| 6 | Payment buttons — "Mark Paid" / "Mark Refunded" | payment status ও timeline আপডেট |

### ৬.৭ Admin — Users

| # | কী করবেন | প্রত্যাশিত ফল |
|---|---|---|
| 1 | **Users** — search, role filter (Admin/Customer) | মিল থাকা user, order সংখ্যা সহ |
| 2 | কোনো customer-কে **"Make admin"** | পরের বার তারা `/Admin` ঢুকতে পারবে; "Admin" chip দেখায় |
| 3 | কোনো user **"Deactivate"** | সেই user আর login করতে পারবে না ("This account has been deactivated") |
| 4 | **নিজেকে** deactivate করার চেষ্টা | "You can't deactivate your own account" |
| 5 | শেষ admin-এর admin role নেওয়ার চেষ্টা | "There must be at least one administrator" |

### ৬.৮ Admin — CMS

| Module | টেস্ট |
|---|---|
| **Sliders** | নতুন slide যোগ → home page-এ carousel-এ সাথে সাথে আসে |
| **Testimonials** | নতুন testimonial → home page-এ দেখায় |
| **Settings** | tagline / ফোন / free-shipping threshold / BDT rate বদলে "Save all settings" → header/footer/দাম সব জায়গায় প্রভাব |
| **Subscribers** | list; "Unsubscribe"/"Re-subscribe" toggle; "Export CSV" ফাইল download |
| **Messages** | storefront `/contact` থেকে পাঠানো মেসেজ inbox-এ; "New" badge; "Mark as read"; "Delete" |

### ৬.৯ কমান্ড-লাইন থেকে দ্রুত smoke test (ঐচ্ছিক)

```powershell
# app চালু থাকা অবস্থায়
curl http://localhost:5199/            # 200
curl http://localhost:5199/c/electronics
curl -i http://localhost:5199/xyz-not-real   # 404 + error page
```

---

## ৭. সমস্যা ও সমাধান (Troubleshooting)

| সমস্যা | সমাধান |
|---|---|
| `Cannot open database "ECommerceDB"` | আগে `Database/ECommerceDB.sql` import করুন, তারপর connection string মিলিয়ে দেখুন |
| `A network-related or instance-specific error` | `Server=` ভুল। LocalDB হলে `Server=(localdb)\MSSQLLocalDB` |
| SSMS-এ `Incorrect syntax near 'GO'` | Query menu → **SQLCMD Mode** on করুন |
| `CREATE INDEX failed ... QUOTED_IDENTIFIER` | `.sql` ফাইলের শুরুতে `SET QUOTED_IDENTIFIER ON` আছে কিনা দেখুন (আছে); sqlcmd দিয়ে চালান, পুরনো টুল নয় |
| Login হয় কিন্তু `/Admin` "Access denied" | আপনি customer হিসেবে login — `admin@ecommerce.local` দিয়ে করুন |
| আপলোড করা product ছবি 404 | ছবি `src/ECommerce.Web/wwwroot/uploads/`-এ থাকে (git-ignored) — fresh checkout-এ আবার আপলোড করতে হবে |
| App বারবার migration চালাতে চায় | `appsettings.json`-এ `MigrateOnStartup: false` রাখুন (`.sql` ফাইল নিজেই migration record লেখে) |
| Port already in use | `Properties/launchSettings.json`-এ port বদলান, বা পুরনো `dotnet` process বন্ধ করুন |
| `dotnet ef` command পাওয়া যায় না | `dotnet tool restore` চালান (প্রজেক্ট root থেকে) |
| Payment সব সময় decline হয় | `appsettings.json` → `Payment:Fake:SimulateSuccess` `true` আছে কিনা; card নম্বর `0000` দিয়ে শেষ নয় তো |

---

## ৮. কীভাবে পরিবর্তন / নতুন feature যোগ করবেন

### নতুন entity/field যোগ করলে

1. `ECommerce.Domain/Entities/`-এ class বা property যোগ করুন
2. দরকার হলে `ECommerce.Infrastructure/Persistence/Configurations/`-এ config দিন
3. migration বানান:
   ```powershell
   dotnet ef migrations add <নাম> --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Web
   ```
4. `Database/ECommerceDB.sql` update করুন (README-র *Keeping the SQL file in sync* দেখুন) — **এটা করা বাধ্যতামূলক**, কারণ প্রজেক্ট এই ফাইল দিয়েই চলে

### নতুন business logic যোগ করলে

1. `ECommerce.Application/`-এ interface + DTO
2. `ECommerce.Infrastructure/Services/`-এ implementation
3. `ECommerce.Infrastructure/DependencyInjection.cs`-এ `services.AddScoped<IXxx, Xxx>()`
4. `ECommerce.Web/Controllers/`-এ controller, `Views/`-এ Razor পেজ

### আসল payment gateway লাগাতে

`ECommerce.Application/Payments/IPaymentGateway.cs` interface-টা নতুন class-এ implement করুন
(যেমন `SslCommerzGateway`), তারপর `DependencyInjection.cs`-এ `FakePaymentGateway`-এর জায়গায় সেটা register
করুন। বাকি order flow-এ হাত দিতে হবে না।

### Design বদলাতে

- storefront: `wwwroot/css/site.css` (উপরে `:root`-এ color/spacing token)
- admin: `wwwroot/css/admin.css`
- JS behaviour: `wwwroot/js/site.js`

---

*এই গাইডের পাশাপাশি ইংরেজি সংক্ষিপ্ত version আছে `README.md`-তে, আর পূর্ণ architecture + phase পরিকল্পনা
`docs/Planning.md`-তে।*
