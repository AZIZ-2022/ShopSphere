using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Features;

/// <summary>The fixed set of admin-toggleable storefront capabilities.</summary>
public static class FeatureFlagKeys
{
    public const string Wishlist = "Wishlist";
    public const string Reviews = "Reviews";
    public const string Newsletter = "Newsletter";

    public static readonly (string Key, string Label, string Description)[] All =
    {
        (Wishlist, "Wishlist", "Lets customers save products to a wishlist and move them to cart later."),
        (Reviews, "Product reviews", "Lets customers read and submit ratings/reviews on product pages."),
        (Newsletter, "Newsletter signup", "Shows newsletter signup forms and accepts new subscriptions."),
    };
}

public sealed class FeatureFlagListItem
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

/// <summary>Public read access, used by both views and controllers to gate a feature.</summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken ct = default);
    void Invalidate();
}

public interface IFeatureFlagAdminService
{
    Task<IReadOnlyList<FeatureFlagListItem>> ListAsync(CancellationToken ct = default);
    Task<Result> SetEnabledAsync(string key, bool enabled, CancellationToken ct = default);
}
