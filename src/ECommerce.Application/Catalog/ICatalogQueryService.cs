namespace ECommerce.Application.Catalog;

public interface ICatalogQueryService
{
    Task<ProductListResult> ListProductsAsync(ProductQuery query, CancellationToken ct = default);

    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<ProductListItemDto>> GetRelatedAsync(int productId, int take = 4, CancellationToken ct = default);

    /// <summary>Registers a product view (fire-and-forget style).</summary>
    Task IncrementViewCountAsync(int productId, CancellationToken ct = default);

    Task<CategoryHeaderDto?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default);
}
