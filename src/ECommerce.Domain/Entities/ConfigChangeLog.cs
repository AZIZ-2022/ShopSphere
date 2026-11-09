using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// One recorded change to an admin-managed configuration entity (SiteSetting, FeatureFlag,
/// Coupon, CmsPage, HomeSection). <see cref="OldValueJson"/> is the pre-change snapshot used
/// to roll back; it is always populated (this log only ever records updates to existing
/// config, never creates/deletes, since there is nothing meaningful to "roll back to" for those).
/// </summary>
public class ConfigChangeLog : AuditableEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;

    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
}
