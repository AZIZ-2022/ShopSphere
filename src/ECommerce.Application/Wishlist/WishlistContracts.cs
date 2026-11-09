using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Wishlist;

public sealed class WishlistToggleResult
{
    public bool InWishlist { get; init; }
    public int Count { get; init; }
}

public interface IWishlistService
{
    Task<IReadOnlyList<ProductListItemDto>> GetItemsAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<int>> GetProductIdsAsync(string userId, CancellationToken ct = default);
    Task<int> CountAsync(string userId, CancellationToken ct = default);

    Task<Result<WishlistToggleResult>> ToggleAsync(string userId, int productId, CancellationToken ct = default);
    Task<Result> RemoveAsync(string userId, int productId, CancellationToken ct = default);

    /// <summary>Adds the product to the cart and removes it from the wishlist.</summary>
    Task<Result> MoveToCartAsync(string userId, int productId, CancellationToken ct = default);
}
