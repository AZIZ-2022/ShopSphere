using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Admin-configurable homepage section: order, visibility, and (for sections that have
/// free-text content) title/subtitle/button overrides. The set of valid <see cref="Key"/>
/// values is fixed — see <c>HomeSectionKeys</c> in the Application layer — because each key
/// corresponds to a specific rendering block on the homepage, not an arbitrary admin-created one.
/// </summary>
public class HomeSection : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ButtonText { get; set; }
    public string? LinkUrl { get; set; }

    /// <summary>Section-specific structured content (e.g. the feature strip's icon items) as JSON.</summary>
    public string? ConfigJson { get; set; }
}
