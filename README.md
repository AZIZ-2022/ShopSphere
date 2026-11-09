# ShopSphere — ECommerce

A full e-commerce web application: storefront + admin, built with **ASP.NET Core 8 (MVC)**,
**Entity Framework Core 8**, **SQL Server** and **ASP.NET Core Identity**, in a layered / clean
architecture.

> **Status: complete.** All 10 build phases are done — see [`docs/Planning.md`](docs/Planning.md)
> for the phase-by-phase plan.
>
> 🇧🇩 **বাংলায় বিস্তারিত গাইড** (setup + প্রতিটা ফাইলের কাজ + feature ধরে ধরে টেস্ট):
> [`docs/PROJECT-GUIDE-BN.md`](docs/PROJECT-GUIDE-BN.md)

The whole database — schema **and** seed data — ships as a single importable file:
[`Database/ECommerceDB.sql`](Database/ECommerceDB.sql). Import it, point the app at it, run.

---

## Contents

- [Quick start](#quick-start)
- [What's included](#whats-included)
- [Solution layout](#solution-layout)
- [Configuration](#configuration)
- [Default accounts & test data](#default-accounts--test-data)
- [Keeping the SQL file in sync](#keeping-the-sql-file-in-sync)
- [Troubleshooting](#troubleshooting)

---

## Quick start

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0 |
| SQL Server | 2019+ / Express / LocalDB / Developer |
| `sqlcmd` or SSMS | to import the database |

### 1 — Create the database from the SQL file

You do **not** need EF migrations or any manual table creation.

**`sqlcmd`:**
```powershell
sqlcmd -S localhost -E -i "Database\ECommerceDB.sql"
```

**SSMS:**
1. Open `Database/ECommerceDB.sql`.
2. Query menu → **SQLCMD Mode** on (the script uses `GO` batches).
3. Execute (F5).

The script:
- creates the `ECommerceDB` database if it doesn't exist,
- creates every table, key, index and constraint (ASP.NET Identity tables included),
- inserts the seed data (admin user, roles, categories, brands, ~24 products, sliders, testimonials,
  site settings, a `WELCOME10` coupon),
- records the EF migration in `__EFMigrationsHistory` so the app treats the schema as up to date.

It's safe to re-run — every object and every seed row is guarded.

### 2 — Point the app at your database

Edit `src/ECommerce.Web/appsettings.json` (or use `dotnet user-secrets`):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ECommerceDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

- **LocalDB:** `Server=(localdb)\\MSSQLLocalDB;Database=ECommerceDB;Trusted_Connection=True;TrustServerCertificate=True`
- **SQL auth:** `Server=localhost;Database=ECommerceDB;User Id=sa;Password=Your_password;TrustServerCertificate=True`

### 3 — Run

```powershell
dotnet run --project src/ECommerce.Web
```

Open the URL shown in the console (e.g. `https://localhost:7022`). Log in as the admin (below) to
reach `/Admin`.

---

## What's included

### Storefront

- **Home** — hero slider, feature strip, featured categories, deals of the day (with countdown),
  featured products, new arrivals, best sellers, promo banner, testimonials, brand strip, newsletter.
- **Catalog** — `/c/{slug}` category pages with brand / price / rating / in-stock filters, sorting,
  pagination; `/search?q=`; `/deals`.
- **Product detail** — `/p/{slug}` — image gallery, discount pricing, stock, description, customer
  reviews (submit when logged in), related products.
- **Cart** — guest cart (cookie) + user cart, merged on login; mini-cart dropdown; AJAX add / update /
  remove with toasts; free-shipping progress bar.
- **Wishlist** — heart toggle on every product, wishlist page, move-to-cart.
- **Currency** — `$` / `৳` switcher; prices stored in USD, converted for display; orders snapshot the
  currency + rate.
- **Checkout** — address book (saved or new), coupon, Cash on Delivery **or** online card payment,
  order note, live summary.
- **Payment** — a hosted mock card form (`/order/pay/{orderNumber}`) backed by a swappable
  `IPaymentGateway`; declines are handled with a retry. Test card `4242 4242 4242 4242` succeeds.
- **Orders** — confirmation page, order history + detail with a status timeline, guest order tracking
  (`/order/track` by number + email).
- **Account** — register / login / logout, profile, change password, address book.
- **Info** — Contact (writes to the admin inbox), FAQ, Shipping & Returns, Privacy.
- Friendly **404 / error pages**; empty / loading / error states throughout.

### Admin (`/Admin`, requires the `Admin` role)

- **Dashboard** — KPIs (paid revenue, orders, pending, products, low stock, customers), a 14-day
  revenue chart, recent orders, top products, low-stock list.
- **Catalog** — Category CRUD (multi-level), Brand CRUD, Product CRUD with multi-image upload
  (`wwwroot/uploads/products/`), feature flags, stock, soft delete.
- **Orders** — list (search + status filter), detail, status transitions (with history), payment
  status, automatic restock on cancel / return.
- **Users** — list (search, role filter), grant / revoke admin, activate / deactivate
  (with self-lockout and last-admin safeguards).
- **CMS** — Sliders, Testimonials, Site Settings (bulk edit), Newsletter subscribers (+ CSV export),
  Contact-message inbox.

### Cross-cutting

- Soft delete + audit columns (`CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`) via an EF interceptor
  and global query filters.
- Serilog structured logging (console + `App_Data/logs`).
- `/api/*` endpoints return a consistent `{ success, data, message, errors }` envelope and JSON errors.
- Anti-forgery on every state-changing form; `[Authorize(Roles = "Admin")]` on the whole admin area.

---

## Solution layout

```
ECommerce.sln
├── src/
│   ├── ECommerce.Domain/          Entities, enums, base types — no dependencies
│   ├── ECommerce.Application/     Interfaces, DTOs, result & paging models
│   ├── ECommerce.Infrastructure/  EF Core DbContext, configurations, migrations,
│   │                              Identity, seeding, all service implementations
│   └── ECommerce.Web/             MVC controllers, Razor views, view components,
│                                  tag helpers, wwwroot, composition root
├── Database/
│   └── ECommerceDB.sql            Full schema + seed — the single import artifact
├── docs/
│   └── Planning.md                Architecture & phase plan
└── README.md
```

Dependency direction: `Domain ← Application ← Infrastructure ← Web`.

---

## Configuration

`src/ECommerce.Web/appsettings.json`:

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `DatabaseInitialization:MigrateOnStartup` | `true` = run EF migrations on boot (default `false`) |
| `DatabaseInitialization:SeedOnStartup` | `true` = run the C# seeder if tables are empty (default `false`) |
| `Payment:Fake:SimulateSuccess` | `false` = the mock gateway declines every charge |
| `Payment:Fake:DeclineCardSuffix` | card numbers ending with this are always declined (default `0000`) |
| `Serilog:*` | logging levels and sinks |

Editable at runtime from **Admin → Settings**: store name, contact details, social links, base
currency, supported currencies, BDT exchange rate, free-shipping threshold, flat shipping rate.

### Running without the SQL file (EF only)

```powershell
dotnet tool restore
dotnet ef database update --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Web
```
…or set both `DatabaseInitialization` flags to `true` and run the app once.

---

## Default accounts & test data

| Role | Email | Password |
|---|---|---|
| Admin | `admin@ecommerce.local` | `Admin@123` |

Register any email for a customer account (accounts are auto-confirmed — no email step).

Seed data: 6 top-level + 7 sub categories, 5 brands, ~24 products (some featured / new / discounted),
3 hero slides, 4 testimonials, ~14 site settings, coupon **`WELCOME10`** (10% off, min $50).

Payment test cards (mock gateway): `4242 4242 4242 4242` → approved · any Luhn-valid number ending
`0000` → declined.

---

## Keeping the SQL file in sync

`Database/ECommerceDB.sql` = database-creation header + EF-generated idempotent schema + hand-maintained
seed section. Whenever the entity model changes:

```powershell
dotnet ef migrations add <Name> --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Web
dotnet ef migrations script --idempotent --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Web -o Database/_schema.sql
```

Then rebuild `ECommerceDB.sql`: the `CREATE DATABASE` / `SET QUOTED_IDENTIFIER ON` header, the fresh
`_schema.sql` body, and the seed section (kept aligned with `ApplicationDbContextSeed`).

> The `SET QUOTED_IDENTIFIER ON` at the top of the file is required — the ASP.NET Identity tables use
> filtered unique indexes that won't create under `sqlcmd`'s default settings otherwise.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Cannot open database "ECommerceDB"` | Import `Database/ECommerceDB.sql` first, then check the connection string. |
| `A network-related or instance-specific error` | Wrong `Server=`. For LocalDB use `Server=(localdb)\MSSQLLocalDB`. |
| SSMS: `Incorrect syntax near 'GO'` | Enable **SQLCMD Mode** (Query menu). |
| Login works but `/Admin` gives Access denied | You're signed in as a customer — use `admin@ecommerce.local`. |
| Uploaded product images 404 | They're written to `src/ECommerce.Web/wwwroot/uploads/` (git-ignored); re-upload after a clean checkout. |
| App tries to re-run migrations on an imported DB | The SQL file already writes the `__EFMigrationsHistory` row; keep `MigrateOnStartup` = `false`. |
