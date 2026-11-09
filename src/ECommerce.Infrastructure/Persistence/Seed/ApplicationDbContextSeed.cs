using ECommerce.Application.Cms;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Home;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seeding used when a developer runs the app against an empty database.
/// The canonical seed data also lives in <c>Database/ECommerceDB.sql</c>; keep the two in sync.
/// </summary>
public static class ApplicationDbContextSeed
{
    public const string AdminEmail = "admin@ecommerce.local";
    public const string AdminPassword = "Admin@123";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);
        await SeedSiteSettingsAsync(context);
        await SeedHomeSectionsAsync(context);
        await SeedCmsPagesAsync(context);
        await SeedFeatureFlagsAsync(context);
        await SeedCatalogAsync(context);
        await SeedCmsAsync(context);
        await SeedCouponAsync(context);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seed complete.");
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role) { Description = $"{role} role" });
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync(AdminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
            FullName = "Store Administrator",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, Roles.Admin);
    }

    private static async Task SeedSiteSettingsAsync(ApplicationDbContext context)
    {
        // Per-key upsert-if-missing (not an all-or-nothing guard) so newly introduced settings
        // keys get added to databases that were already seeded under an earlier version.
        var existing = new HashSet<string>(
            await context.SiteSettings.Select(s => s.Key).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        void AddIfMissing(string key, string value, string description)
        {
            if (!existing.Contains(key))
                context.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Description = description });
        }

        AddIfMissing("Site.Name", "ShopSphere", "Store display name");
        AddIfMissing("Site.Tagline", "Everything you need, delivered.", "Short tagline");
        AddIfMissing("Site.LogoUrl", "", "Header logo image URL (leave blank to show the text mark)");
        AddIfMissing("Site.FaviconUrl", "", "Browser tab icon URL (leave blank to use the default)");
        AddIfMissing("Contact.Email", "support@shopsphere.local", "Support email");
        AddIfMissing("Contact.Phone", "+880 1700-000000", "Support phone");
        AddIfMissing("Contact.Address", "House 12, Road 5, Dhaka 1207, Bangladesh", "Business address");
        AddIfMissing("Social.Facebook", "https://facebook.com", "Facebook URL");
        AddIfMissing("Social.Instagram", "https://instagram.com", "Instagram URL");
        AddIfMissing("Social.Twitter", "https://twitter.com", "Twitter/X URL");
        AddIfMissing("Currency.Base", "USD", "Base currency all prices are stored in");
        AddIfMissing("Currency.Supported", "USD,BDT", "Comma separated currency codes");
        AddIfMissing("Currency.Rate.BDT", "120.00", "1 USD in BDT");
        AddIfMissing("Shipping.FreeThreshold", "100", "Free shipping over this base-currency amount");
        AddIfMissing("Shipping.FlatRate", "5", "Flat shipping cost below the free threshold");
        AddIfMissing("Theme.PrimaryColor", "#2563eb", "Primary brand colour (hex)");
        AddIfMissing("Theme.AccentColor", "#ef4444", "Accent / sale colour (hex)");
        AddIfMissing("Theme.FontFamily", "Inter", "Site font: Inter, Roboto, Poppins, Nunito, or OpenSans");
        AddIfMissing("Theme.ButtonStyle", "rounded", "Button corner style: rounded, pill, or square");
        AddIfMissing("Tax.Rate", "0", "Sales tax / VAT percentage applied to the discounted subtotal");
        AddIfMissing("Payment.EnableCod", "true", "Allow Cash on Delivery at checkout");
        AddIfMissing("Payment.EnableOnlineGateway", "true", "Allow online payment at checkout");
        AddIfMissing("Store.BusinessHours", "", "Shown on the Contact page, e.g. Mon-Sat, 9:00 AM - 8:00 PM");
    }

    private static async Task SeedHomeSectionsAsync(ApplicationDbContext context)
    {
        var existing = new HashSet<string>(
            await context.HomeSections.Select(s => s.Key).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        void AddIfMissing(string key, int order, string? title = null, string? subtitle = null, string? buttonText = null, string? linkUrl = null)
        {
            if (existing.Contains(key)) return;
            context.HomeSections.Add(new HomeSection
            {
                Key = key,
                DisplayOrder = order,
                IsVisible = true,
                Title = title,
                Subtitle = subtitle,
                ButtonText = buttonText,
                LinkUrl = linkUrl
            });
        }

        AddIfMissing(HomeSectionKeys.HeroSlider, 10);
        AddIfMissing(HomeSectionKeys.FeatureStrip, 20);
        AddIfMissing(HomeSectionKeys.FeaturedCategories, 30, "Shop by category", "Browse our most popular departments");
        AddIfMissing(HomeSectionKeys.Deals, 40, "Deals of the day", buttonText: "View all deals", linkUrl: "/deals");
        AddIfMissing(HomeSectionKeys.FeaturedProducts, 50, "Featured products", "Hand-picked favourites", "See more", "/search?sort=5");
        AddIfMissing(HomeSectionKeys.NewArrivals, 60, "New arrivals", "Just landed in store", "See more", "/search?sort=3");
        AddIfMissing(HomeSectionKeys.PromoBanner, 70, "Save big across every category", "Discounts refreshed daily — don't miss out.", "Shop the sale", "/deals");
        AddIfMissing(HomeSectionKeys.BestSellers, 80, "Best sellers", "What everyone's buying", "See more", "/search?sort=5");
        AddIfMissing(HomeSectionKeys.Testimonials, 90, "What our customers say");
        AddIfMissing(HomeSectionKeys.Brands, 100, "Popular brands");
        AddIfMissing(HomeSectionKeys.Newsletter, 110, "Join our newsletter", "Be first to hear about new arrivals, exclusive offers and sales.");
    }

    private static async Task SeedCmsPagesAsync(ApplicationDbContext context)
    {
        var existing = new HashSet<string>(
            await context.CmsPages.Select(p => p.Slug).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        void AddIfMissing(string slug, string title, string bodyHtml)
        {
            if (existing.Contains(slug)) return;
            context.CmsPages.Add(new CmsPage { Slug = slug, Title = title, BodyHtml = bodyHtml, IsPublished = true });
        }

        AddIfMissing(CmsPageSlugs.Privacy, "Privacy Policy", """
            <p class="text-muted">This is a demo store. The policy below describes how the application handles data.</p>
            <h2>What we store</h2>
            <ul>
                <li><strong>Account:</strong> your name, email, phone and a securely hashed password.</li>
                <li><strong>Orders:</strong> order contents, amounts and a snapshot of the delivery address at the time of purchase.</li>
                <li><strong>Addresses:</strong> the entries you save to your address book.</li>
                <li><strong>Cart &amp; wishlist:</strong> items you add, linked to your account or a guest cookie.</li>
            </ul>
            <h2>How it's used</h2>
            <ul>
                <li>To process and deliver your orders and let you track them.</li>
                <li>To show prices in your selected currency (a cookie stores that choice).</li>
                <li>Newsletter subscriptions are only used to send store updates and can be cancelled any time.</li>
            </ul>
            <h2>Payments</h2>
            <p>Online payments in this demo go through a simulated gateway. No real card data is stored or charged.</p>
            <h2>Contact</h2>
            <p>For any data request, <a href="/contact">contact us</a>.</p>
            """);

        AddIfMissing(CmsPageSlugs.Shipping, "Shipping & Returns", """
            <h2>Shipping</h2>
            <ul>
                <li>Orders are processed within 1–2 business days.</li>
                <li>Standard delivery takes 3–5 business days depending on your location.</li>
                <li>Free shipping applies automatically once your subtotal reaches the threshold shown in the top bar; otherwise a small flat rate applies and is shown at checkout.</li>
                <li>You can follow every status change (Confirmed → Processing → Shipped → Delivered) on your order page.</li>
            </ul>
            <h2>Returns</h2>
            <ul>
                <li>Request a return within <strong>7 days</strong> of delivery from your order detail page, or by contacting support with your order number.</li>
                <li>Items should be unused and in their original packaging.</li>
                <li>Once approved, the product is restocked and any online payment is refunded to the original method.</li>
                <li>Cash-on-Delivery orders are refunded via the details you provide when the return is approved.</li>
            </ul>
            <p>Questions? <a href="/contact">Get in touch</a>.</p>
            """);

        AddIfMissing(CmsPageSlugs.Faq, "Frequently Asked Questions", """
            <div class="faq">
                <details open>
                    <summary>How do I place an order?</summary>
                    <p>Add items to your cart, go to checkout, choose a delivery address and a payment
                    method (Cash on Delivery or online), then place your order. You'll get an order number
                    and can track it any time.</p>
                </details>
                <details>
                    <summary>What payment methods are available?</summary>
                    <p>Cash on Delivery, and online card payment via our secure gateway. Online orders are
                    confirmed once payment succeeds.</p>
                </details>
                <details>
                    <summary>Can I track my order without an account?</summary>
                    <p>Yes — use <a href="/order/track">Track Order</a> with your order
                    number and the email you used at checkout.</p>
                </details>
                <details>
                    <summary>How do returns work?</summary>
                    <p>Contact us within 7 days of delivery. Once your return is approved the item is restocked
                    and, for online payments, your refund is processed.</p>
                </details>
                <details>
                    <summary>Do you offer free shipping?</summary>
                    <p>Yes, on orders above the threshold shown in the top bar. Smaller orders have a small flat
                    shipping fee, shown at checkout.</p>
                </details>
            </div>
            """);

        AddIfMissing(CmsPageSlugs.AboutUs, "About Us", """
            <p class="text-muted">This is a demo e-commerce store built to showcase the platform.</p>
            <p>We're focused on making online shopping simple: a clear catalog, transparent pricing,
            fast checkout, and order tracking every step of the way.</p>
            <h2>Our promise</h2>
            <ul>
                <li>Quality products at fair prices.</li>
                <li>Clear shipping and return policies — no surprises.</li>
                <li>Responsive support when you need help.</li>
            </ul>
            <p>Have a question? <a href="/contact">Contact us</a> any time.</p>
            """);

        AddIfMissing(CmsPageSlugs.Terms, "Terms & Conditions", """
            <p class="text-muted">These terms govern your use of this store. This is demo/placeholder text — replace it with your business's actual terms.</p>
            <h2>Orders & payment</h2>
            <p>By placing an order you confirm the details provided are accurate. Prices are shown in your
            selected currency at the exchange rate in effect at checkout.</p>
            <h2>Shipping & returns</h2>
            <p>See our <a href="/shipping">Shipping &amp; Returns</a> page for delivery times and the return window.</p>
            <h2>Account use</h2>
            <p>You're responsible for keeping your account credentials confidential and for all activity under your account.</p>
            <h2>Changes</h2>
            <p>We may update these terms from time to time; continued use of the store after a change means you accept the update.</p>
            """);
    }

    private static async Task SeedFeatureFlagsAsync(ApplicationDbContext context)
    {
        var existing = new HashSet<string>(
            await context.FeatureFlags.Select(f => f.Key).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (key, _, description) in FeatureFlagKeys.All)
        {
            if (existing.Contains(key)) continue;
            context.FeatureFlags.Add(new FeatureFlag { Key = key, IsEnabled = true, Description = description });
        }
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var categories = new[]
        {
            new Category { Name = "Electronics", Slug = "electronics", DisplayOrder = 1, IsActive = true, Description = "Phones, laptops, audio and accessories" },
            new Category { Name = "Fashion", Slug = "fashion", DisplayOrder = 2, IsActive = true, Description = "Clothing, footwear and accessories" },
            new Category { Name = "Home & Living", Slug = "home-living", DisplayOrder = 3, IsActive = true, Description = "Furniture, kitchen and decor" },
            new Category { Name = "Beauty", Slug = "beauty", DisplayOrder = 4, IsActive = true, Description = "Skincare, makeup and fragrance" },
            new Category { Name = "Sports", Slug = "sports", DisplayOrder = 5, IsActive = true, Description = "Fitness, outdoor and gear" },
            new Category { Name = "Books", Slug = "books", DisplayOrder = 6, IsActive = true, Description = "Fiction, non-fiction and children" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        var subs = new[]
        {
            new Category { Name = "Smartphones", Slug = "smartphones", ParentCategoryId = categories[0].Id, DisplayOrder = 1, IsActive = true },
            new Category { Name = "Laptops", Slug = "laptops", ParentCategoryId = categories[0].Id, DisplayOrder = 2, IsActive = true },
            new Category { Name = "Audio", Slug = "audio", ParentCategoryId = categories[0].Id, DisplayOrder = 3, IsActive = true },
            new Category { Name = "Men", Slug = "men", ParentCategoryId = categories[1].Id, DisplayOrder = 1, IsActive = true },
            new Category { Name = "Women", Slug = "women", ParentCategoryId = categories[1].Id, DisplayOrder = 2, IsActive = true },
            new Category { Name = "Kitchen", Slug = "kitchen", ParentCategoryId = categories[2].Id, DisplayOrder = 1, IsActive = true },
            new Category { Name = "Furniture", Slug = "furniture", ParentCategoryId = categories[2].Id, DisplayOrder = 2, IsActive = true }
        };
        context.Categories.AddRange(subs);

        var brands = new[]
        {
            new Brand { Name = "Aurora", Slug = "aurora", DisplayOrder = 1, IsActive = true },
            new Brand { Name = "NimbusTech", Slug = "nimbustech", DisplayOrder = 2, IsActive = true },
            new Brand { Name = "Vantage", Slug = "vantage", DisplayOrder = 3, IsActive = true },
            new Brand { Name = "Meadow", Slug = "meadow", DisplayOrder = 4, IsActive = true },
            new Brand { Name = "PulseFit", Slug = "pulsefit", DisplayOrder = 5, IsActive = true }
        };
        context.Brands.AddRange(brands);
        await context.SaveChangesAsync();

        var rnd = new Random(20260908);
        var names = new (string Name, int CatIndex)[]
        {
            ("Aurora Wireless Headphones", 0), ("NimbusTech 5G Smartphone X", 0), ("Vantage UltraBook 14", 0),
            ("Aurora Bluetooth Speaker Mini", 0), ("NimbusTech Smartwatch Pulse", 0), ("Vantage 4K Action Camera", 0),
            ("Meadow Cotton Crew T-Shirt", 1), ("Vantage Slim-Fit Denim Jeans", 1), ("Aurora Running Sneakers", 1),
            ("Meadow Linen Summer Dress", 1), ("Vantage Leather Belt Classic", 1), ("Aurora Canvas Backpack", 1),
            ("Meadow Ceramic Dinner Set 16pc", 2), ("Vantage Non-Stick Fry Pan 28cm", 2), ("Meadow Oak Coffee Table", 2),
            ("Aurora LED Desk Lamp", 2), ("Meadow Memory Foam Pillow", 2), ("Vantage Stainless Cookware Set", 2),
            ("Aurora Vitamin C Face Serum", 3), ("Meadow Hydrating Day Cream", 3), ("Aurora Matte Lipstick Trio", 3),
            ("PulseFit Adjustable Dumbbell 20kg", 4), ("PulseFit Yoga Mat Pro", 4), ("PulseFit Insulated Water Bottle 1L", 4),
            ("The Silent Horizon (Novel)", 5), ("Clean Code Habits (Paperback)", 5)
        };

        var products = new List<Product>();
        for (int i = 0; i < names.Length; i++)
        {
            var (name, catIndex) = names[i];
            var basePrice = Math.Round((decimal)(rnd.Next(8, 260) + rnd.NextDouble()), 2);
            var hasDiscount = i % 3 == 0;
            var slug = name.ToLowerInvariant()
                .Replace("(", "").Replace(")", "")
                .Replace("  ", " ").Replace(' ', '-').Replace("--", "-");

            products.Add(new Product
            {
                Name = name,
                Slug = slug,
                Sku = $"SKU-{1000 + i}",
                ShortDescription = $"{name} — quality {categories[catIndex].Name.ToLowerInvariant()} pick.",
                Description = $"<p>{name} is part of our curated {categories[catIndex].Name} range. "
                              + "Durable materials, thoughtful design and a warranty you can rely on.</p>",
                CategoryId = categories[catIndex].Id,
                BrandId = brands[i % brands.Length].Id,
                Price = basePrice,
                DiscountPrice = hasDiscount ? Math.Round(basePrice * 0.82m, 2) : null,
                StockQuantity = rnd.Next(0, 120),
                IsActive = true,
                IsFeatured = i % 4 == 0,
                IsNewArrival = i % 5 == 0,
                AverageRating = Math.Round((decimal)(3.5 + rnd.NextDouble() * 1.5), 2),
                ReviewCount = rnd.Next(0, 240),
                ViewCount = rnd.Next(20, 5000),
                SoldCount = rnd.Next(0, 900),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = $"/images/products/sample-{(i % 12) + 1}.jpg", IsMain = true, DisplayOrder = 0, AltText = name }
                }
            });
        }
        context.Products.AddRange(products);
    }

    private static async Task SeedCmsAsync(ApplicationDbContext context)
    {
        if (!await context.Sliders.AnyAsync())
        {
            context.Sliders.AddRange(
                new Slider { Title = "Big Season Sale", Subtitle = "Up to 40% off across Electronics", ImageUrl = "/images/sliders/slide-1.jpg", ButtonText = "Shop Deals", LinkUrl = "/c/electronics", DisplayOrder = 1, IsActive = true },
                new Slider { Title = "New Arrivals Are Here", Subtitle = "Fresh styles for the new season", ImageUrl = "/images/sliders/slide-2.jpg", ButtonText = "Explore", LinkUrl = "/c/fashion", DisplayOrder = 2, IsActive = true },
                new Slider { Title = "Upgrade Your Home", Subtitle = "Comfort and style for every room", ImageUrl = "/images/sliders/slide-3.jpg", ButtonText = "Browse Home", LinkUrl = "/c/home-living", DisplayOrder = 3, IsActive = true }
            );
        }

        if (!await context.Testimonials.AnyAsync())
        {
            context.Testimonials.AddRange(
                new Testimonial { CustomerName = "Farhana R.", Designation = "Verified Buyer", Message = "Fast delivery and the product quality exceeded my expectations.", Rating = 5, DisplayOrder = 1, IsActive = true },
                new Testimonial { CustomerName = "Tanvir A.", Designation = "Verified Buyer", Message = "Great prices and the checkout was smooth. Will order again.", Rating = 5, DisplayOrder = 2, IsActive = true },
                new Testimonial { CustomerName = "Nabila K.", Designation = "Verified Buyer", Message = "Customer support helped me swap a size with zero hassle.", Rating = 4, DisplayOrder = 3, IsActive = true },
                new Testimonial { CustomerName = "Sabbir H.", Designation = "Verified Buyer", Message = "The packaging was premium and everything arrived intact.", Rating = 5, DisplayOrder = 4, IsActive = true }
            );
        }
    }

    private static async Task SeedCouponAsync(ApplicationDbContext context)
    {
        if (await context.Coupons.AnyAsync()) return;

        context.Coupons.Add(new Coupon
        {
            Code = "WELCOME10",
            Description = "10% off your first order (min $50)",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            MinOrderAmount = 50m,
            MaxDiscountAmount = 30m,
            UsageLimit = 1000,
            UsedCount = 0,
            StartsAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndsAt = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            IsActive = true
        });
    }
}
