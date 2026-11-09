namespace ECommerce.Application.Navigation;

/// <summary>A category node for the storefront navigation menu.</summary>
public sealed class NavCategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public IReadOnlyList<NavCategoryDto> Children { get; init; } = Array.Empty<NavCategoryDto>();

    public bool HasChildren => Children.Count > 0;
}
