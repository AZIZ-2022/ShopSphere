using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Key/value store for global configuration surfaced in the UI
/// (logo, contact info, social links, currency settings, shipping thresholds…).
/// </summary>
public class SiteSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}
