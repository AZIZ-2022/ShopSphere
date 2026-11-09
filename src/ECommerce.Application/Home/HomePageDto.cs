using ECommerce.Application.Catalog;

namespace ECommerce.Application.Home;

public sealed class HeroSlideDto
{
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string? MobileImageUrl { get; init; }
    public string? LinkUrl { get; init; }
    public string? ButtonText { get; init; }
}

public sealed class FeaturedCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int ProductCount { get; init; }
}

public sealed class TestimonialDto
{
    public string CustomerName { get; init; } = string.Empty;
    public string? Designation { get; init; }
    public string? PhotoUrl { get; init; }
    public string Message { get; init; } = string.Empty;
    public byte Rating { get; init; }
}

public sealed class BrandLogoDto
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
}

public sealed class HomeSectionMeta
{
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public string? ButtonText { get; init; }
    public string? LinkUrl { get; init; }
}

public sealed class HomePageDto
{
    public IReadOnlyList<HeroSlideDto> Slides { get; init; } = Array.Empty<HeroSlideDto>();
    public IReadOnlyList<FeaturedCategoryDto> FeaturedCategories { get; init; } = Array.Empty<FeaturedCategoryDto>();
    public IReadOnlyList<ProductListItemDto> DealProducts { get; init; } = Array.Empty<ProductListItemDto>();
    public IReadOnlyList<ProductListItemDto> FeaturedProducts { get; init; } = Array.Empty<ProductListItemDto>();
    public IReadOnlyList<ProductListItemDto> NewArrivals { get; init; } = Array.Empty<ProductListItemDto>();
    public IReadOnlyList<ProductListItemDto> BestSellers { get; init; } = Array.Empty<ProductListItemDto>();
    public IReadOnlyList<TestimonialDto> Testimonials { get; init; } = Array.Empty<TestimonialDto>();
    public IReadOnlyList<BrandLogoDto> Brands { get; init; } = Array.Empty<BrandLogoDto>();

    /// <summary>Visible section keys (see <see cref="HomeSectionKeys"/>), in the admin-configured order.</summary>
    public IReadOnlyList<string> SectionOrder { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, HomeSectionMeta> SectionMeta { get; init; } = new Dictionary<string, HomeSectionMeta>();
    public IReadOnlyList<FeatureStripItem> FeatureStripItems { get; init; } = Array.Empty<FeatureStripItem>();
}

public interface IHomePageService
{
    Task<HomePageDto> GetAsync(CancellationToken ct = default);
    void Invalidate();
}
