using System.Text.Json;
using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Home;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services;

public sealed class HomePageService : IHomePageService
{
    private const string CacheKey = "home-page-dto";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IFeatureFlagService _features;

    public HomePageService(ApplicationDbContext db, IMemoryCache cache, IFeatureFlagService features)
    {
        _db = db;
        _cache = cache;
        _features = features;
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    public async Task<HomePageDto> GetAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out HomePageDto? cached) && cached is not null)
            return cached;

        var now = DateTime.UtcNow;

        var sections = await _db.HomeSections.AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        var sectionOrder = sections.Count > 0
            ? sections.Where(s => s.IsVisible).Select(s => s.Key).ToList()
            : HomeSectionKeys.All.Select(x => x.Key).ToList(); // not seeded yet — fall back to the original fixed order

        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Newsletter, ct))
            sectionOrder.Remove(HomeSectionKeys.Newsletter);

        var sectionMeta = sections.ToDictionary(
            s => s.Key,
            s => new HomeSectionMeta { Title = s.Title, Subtitle = s.Subtitle, ButtonText = s.ButtonText, LinkUrl = s.LinkUrl },
            StringComparer.OrdinalIgnoreCase);

        var featureStripJson = sections.FirstOrDefault(s => s.Key == HomeSectionKeys.FeatureStrip)?.ConfigJson;
        var featureStripItems = ParseFeatureItems(featureStripJson);
        if (featureStripItems.Count == 0)
            featureStripItems = DefaultFeatureItems();

        var slides = await _db.Sliders.AsNoTracking()
            .Where(s => s.IsActive && (s.StartsAt == null || s.StartsAt <= now) && (s.EndsAt == null || s.EndsAt >= now))
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new HeroSlideDto
            {
                Title = s.Title,
                Subtitle = s.Subtitle,
                ImageUrl = s.ImageUrl,
                MobileImageUrl = s.MobileImageUrl,
                LinkUrl = s.LinkUrl,
                ButtonText = s.ButtonText
            })
            .ToListAsync(ct);

        var featuredCategories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Take(6)
            .Select(c => new FeaturedCategoryDto
            {
                Name = c.Name,
                Slug = c.Slug,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(ct);

        var dealProducts = await ProductQuery()
            .Where(p => p.DiscountPrice != null && p.DiscountPrice > 0 && p.DiscountPrice < p.Price)
            .OrderByDescending(p => (p.Price - p.DiscountPrice!.Value) / p.Price)
            .Take(8).Select(Projection).ToListAsync(ct);

        var featuredProducts = await ProductQuery()
            .Where(p => p.IsFeatured)
            .OrderByDescending(p => p.SoldCount)
            .Take(8).Select(Projection).ToListAsync(ct);

        var newArrivals = await ProductQuery()
            .Where(p => p.IsNewArrival)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8).Select(Projection).ToListAsync(ct);

        var bestSellers = await ProductQuery()
            .OrderByDescending(p => p.SoldCount).ThenByDescending(p => p.AverageRating)
            .Take(8).Select(Projection).ToListAsync(ct);

        var testimonials = await _db.Testimonials.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new TestimonialDto
            {
                CustomerName = t.CustomerName,
                Designation = t.Designation,
                PhotoUrl = t.PhotoUrl,
                Message = t.Message,
                Rating = t.Rating
            })
            .ToListAsync(ct);

        var brands = await _db.Brands.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            .Take(12)
            .Select(b => new BrandLogoDto { Name = b.Name, Slug = b.Slug, LogoUrl = b.LogoUrl })
            .ToListAsync(ct);

        var dto = new HomePageDto
        {
            Slides = slides,
            FeaturedCategories = featuredCategories,
            DealProducts = dealProducts,
            FeaturedProducts = featuredProducts,
            NewArrivals = newArrivals,
            BestSellers = bestSellers,
            Testimonials = testimonials,
            Brands = brands,
            SectionOrder = sectionOrder,
            SectionMeta = sectionMeta,
            FeatureStripItems = featureStripItems
        };

        _cache.Set(CacheKey, dto, CacheFor);
        return dto;
    }

    private static List<FeatureStripItem> ParseFeatureItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<FeatureStripItem>>(json) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    private static List<FeatureStripItem> DefaultFeatureItems() => new()
    {
        new() { Icon = "truck", Title = "Free shipping", Subtitle = "On qualifying orders" },
        new() { Icon = "undo", Title = "7-day returns", Subtitle = "Easy & hassle-free" },
        new() { Icon = "shield-check", Title = "Secure checkout", Subtitle = "Cash on delivery available" },
        new() { Icon = "messages", Title = "Support", Subtitle = "We're here to help" },
    };

    private IQueryable<Domain.Entities.Product> ProductQuery() =>
        _db.Products.AsNoTracking().Where(p => p.IsActive);

    private static readonly System.Linq.Expressions.Expression<Func<Domain.Entities.Product, ProductListItemDto>> Projection =
        p => new ProductListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            CategoryName = p.Category.Name,
            BrandName = p.Brand != null ? p.Brand.Name : null,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            StockQuantity = p.StockQuantity,
            IsFeatured = p.IsFeatured,
            IsNewArrival = p.IsNewArrival,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            MainImageUrl = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl).FirstOrDefault()
        };
}
