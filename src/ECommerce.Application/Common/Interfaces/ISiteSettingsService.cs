using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface ISiteSettingsService
{
    Task<SiteSettingsSnapshot> GetAsync(CancellationToken ct = default);

    /// <summary>Clears the cache after an admin edits settings (used from Phase 5+).</summary>
    void Invalidate();
}
