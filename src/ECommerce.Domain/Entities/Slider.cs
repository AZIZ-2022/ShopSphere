using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Home page hero banner slide.</summary>
public class Slider : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Optional scheduling window; null means no bound on that side.</summary>
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
