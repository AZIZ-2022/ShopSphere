using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Cart;

/// <summary>Identifies whose cart we are working with: a logged-in user or an anonymous browser.</summary>
public readonly record struct CartOwner(string? UserId, string? AnonymousId)
{
    public bool IsUser => !string.IsNullOrEmpty(UserId);
    public bool IsAnonymous => !IsUser && !string.IsNullOrEmpty(AnonymousId);
    public bool IsValid => IsUser || IsAnonymous;

    public static CartOwner ForUser(string userId) => new(userId, null);
    public static CartOwner ForGuest(string anonymousId) => new(null, anonymousId);
}

public sealed class CartLineDto
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public int StockAvailable { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
    public bool ExceedsStock => Quantity > StockAvailable;
}

public sealed class CartDto
{
    public IReadOnlyList<CartLineDto> Lines { get; init; } = Array.Empty<CartLineDto>();
    public int ItemCount => Lines.Sum(l => l.Quantity);
    public int DistinctCount => Lines.Count;
    public decimal SubTotal => Lines.Sum(l => l.LineTotal);

    public decimal ShippingCost { get; init; }
    public decimal FreeShippingThreshold { get; init; }
    public bool QualifiesForFreeShipping => FreeShippingThreshold > 0 && SubTotal >= FreeShippingThreshold;
    public decimal AmountToFreeShipping =>
        FreeShippingThreshold <= 0 || QualifiesForFreeShipping ? 0 : FreeShippingThreshold - SubTotal;

    public decimal GrandTotal => SubTotal + ShippingCost;
    public bool IsEmpty => Lines.Count == 0;

    /// <summary>Names of items that were in the cart but silently dropped because the product
    /// was deactivated or deleted since it was added — surfaced so the customer isn't left
    /// wondering where an item went.</summary>
    public IReadOnlyList<string> RemovedItemNames { get; init; } = Array.Empty<string>();
}

public sealed class CartSummaryDto
{
    public int ItemCount { get; init; }
    public decimal SubTotal { get; init; }
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(CartOwner owner, CancellationToken ct = default);
    Task<CartSummaryDto> GetSummaryAsync(CartOwner owner, CancellationToken ct = default);

    Task<Result<CartSummaryDto>> AddAsync(CartOwner owner, int productId, int quantity, CancellationToken ct = default);
    Task<Result<CartSummaryDto>> UpdateQuantityAsync(CartOwner owner, int productId, int quantity, CancellationToken ct = default);
    Task<Result<CartSummaryDto>> RemoveAsync(CartOwner owner, int productId, CancellationToken ct = default);
    Task ClearAsync(CartOwner owner, CancellationToken ct = default);

    /// <summary>Called on login: folds the guest cart into the user's cart, then deletes the guest cart.</summary>
    Task MergeGuestCartAsync(string anonymousId, string userId, CancellationToken ct = default);
}
