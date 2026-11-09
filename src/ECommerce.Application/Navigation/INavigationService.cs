namespace ECommerce.Application.Navigation;

public interface INavigationService
{
    /// <summary>Active category tree (top level with nested children), cached.</summary>
    Task<IReadOnlyList<NavCategoryDto>> GetMenuAsync(CancellationToken ct = default);

    /// <summary>Clears the cached menu after an admin edits categories.</summary>
    void Invalidate();
}
