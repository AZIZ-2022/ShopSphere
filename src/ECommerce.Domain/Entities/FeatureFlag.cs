using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Admin on/off switch for an optional storefront capability (Wishlist, Reviews, Newsletter, …).
/// The set of valid keys is fixed — see <c>FeatureFlagKeys</c> in the Application layer — each
/// one gates both the UI and the corresponding backend endpoint(s).
/// </summary>
public class FeatureFlag : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}
