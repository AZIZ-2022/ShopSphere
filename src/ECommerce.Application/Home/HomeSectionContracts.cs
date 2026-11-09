using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Home;

/// <summary>
/// The fixed set of homepage sections. Admins can reorder, hide, and (for sections with
/// free-text content) edit these — but the set itself is fixed, since each one maps to a
/// specific rendering block in the homepage view.
/// </summary>
public static class HomeSectionKeys
{
    public const string HeroSlider = "HeroSlider";
    public const string FeatureStrip = "FeatureStrip";
    public const string FeaturedCategories = "FeaturedCategories";
    public const string Deals = "Deals";
    public const string FeaturedProducts = "FeaturedProducts";
    public const string NewArrivals = "NewArrivals";
    public const string PromoBanner = "PromoBanner";
    public const string BestSellers = "BestSellers";
    public const string Testimonials = "Testimonials";
    public const string Brands = "Brands";
    public const string Newsletter = "Newsletter";

    public static readonly (string Key, string Label)[] All =
    {
        (HeroSlider, "Hero slider"),
        (FeatureStrip, "Feature strip (shipping / returns / support icons)"),
        (FeaturedCategories, "Shop by category"),
        (Deals, "Deals of the day"),
        (FeaturedProducts, "Featured products"),
        (NewArrivals, "New arrivals"),
        (PromoBanner, "Special offer banner"),
        (BestSellers, "Best sellers"),
        (Testimonials, "Customer testimonials"),
        (Brands, "Popular brands"),
        (Newsletter, "Newsletter signup band"),
    };
}

public sealed class FeatureStripItem
{
    public string Icon { get; set; } = "truck";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}

public sealed class HomeSectionEditModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ButtonText { get; set; }
    public string? LinkUrl { get; set; }

    public List<FeatureStripItem> FeatureItems { get; set; } = new();
}

public interface IHomeSectionAdminService
{
    Task<IReadOnlyList<HomeSectionEditModel>> ListAsync(CancellationToken ct = default);
    Task<HomeSectionEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<Result> UpdateAsync(HomeSectionEditModel model, CancellationToken ct = default);
    Task<Result> MoveAsync(int id, bool up, CancellationToken ct = default);
    Task<Result> SetVisibleAsync(int id, bool visible, CancellationToken ct = default);
}
