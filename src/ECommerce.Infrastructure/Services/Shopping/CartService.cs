using ECommerce.Application.Cart;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Shopping;

public sealed class CartService : ICartService
{
    private const int MaxLineQuantity = 99;

    private readonly ApplicationDbContext _db;
    private readonly ISiteSettingsService _settings;

    public CartService(ApplicationDbContext db, ISiteSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<CartDto> GetCartAsync(CartOwner owner, CancellationToken ct = default)
    {
        if (!owner.IsValid) return await BuildDtoAsync(Array.Empty<CartLineDto>(), Array.Empty<string>(), ct);

        var cart = await FindCartAsync(owner, includeItems: true, ct);
        if (cart is null || cart.Items.Count == 0)
            return await BuildDtoAsync(Array.Empty<CartLineDto>(), Array.Empty<string>(), ct);

        var productIds = cart.Items.Select(i => i.ProductId).ToArray();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id, p.Name, p.Slug, p.StockQuantity, p.IsActive,
                Image = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToDictionaryAsync(p => p.Id, ct);

        var lines = new List<CartLineDto>();
        var removedNames = new List<string>();
        var staleItems = new List<CartItem>();

        foreach (var item in cart.Items.OrderBy(i => i.Id))
        {
            if (!products.TryGetValue(item.ProductId, out var p) || !p.IsActive)
            {
                removedNames.Add(p?.Name ?? "An item");
                staleItems.Add(item);
                continue;
            }

            lines.Add(new CartLineDto
            {
                ProductId = item.ProductId,
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = p.Image,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                StockAvailable = p.StockQuantity
            });
        }

        if (staleItems.Count > 0)
        {
            // Self-heal: a deactivated/deleted product is never coming back as a valid line,
            // so drop the stale row now instead of silently re-skipping it on every future load.
            _db.CartItems.RemoveRange(staleItems);
            await _db.SaveChangesAsync(ct);
        }

        return await BuildDtoAsync(lines, removedNames, ct);
    }

    public async Task<CartSummaryDto> GetSummaryAsync(CartOwner owner, CancellationToken ct = default)
    {
        if (!owner.IsValid) return new CartSummaryDto();

        var cart = await FindCartAsync(owner, includeItems: true, ct);
        if (cart is null) return new CartSummaryDto();

        return new CartSummaryDto
        {
            ItemCount = cart.Items.Sum(i => i.Quantity),
            SubTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity)
        };
    }

    public async Task<Result<CartSummaryDto>> AddAsync(CartOwner owner, int productId, int quantity, CancellationToken ct = default)
    {
        if (!owner.IsValid) return Result<CartSummaryDto>.Failure("Your session has expired. Please refresh and try again.");
        if (quantity < 1) quantity = 1;

        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId && p.IsActive)
            .Select(p => new { p.Id, p.Price, p.DiscountPrice, p.StockQuantity })
            .FirstOrDefaultAsync(ct);

        if (product is null) return Result<CartSummaryDto>.Failure("This product is no longer available.");
        if (product.StockQuantity <= 0) return Result<CartSummaryDto>.Failure("This product is out of stock.");

        var cart = await FindCartAsync(owner, includeItems: true, ct) ?? await CreateCartAsync(owner, ct);

        var line = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var effectivePrice = product.DiscountPrice is > 0 && product.DiscountPrice < product.Price
            ? product.DiscountPrice.Value : product.Price;

        var target = (line?.Quantity ?? 0) + quantity;
        var capped = Math.Min(target, Math.Min(product.StockQuantity, MaxLineQuantity));

        if (line is null)
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = capped, UnitPrice = effectivePrice });
        else
        {
            line.Quantity = capped;
            line.UnitPrice = effectivePrice;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var summary = Summarize(cart);
        return capped < target
            ? new Result<CartSummaryDto> { Succeeded = true, Data = summary, Error = $"Only {product.StockQuantity} in stock — quantity adjusted." }
            : Result<CartSummaryDto>.Success(summary);
    }

    public async Task<Result<CartSummaryDto>> UpdateQuantityAsync(CartOwner owner, int productId, int quantity, CancellationToken ct = default)
    {
        if (!owner.IsValid) return Result<CartSummaryDto>.Failure("Your session has expired.");

        var cart = await FindCartAsync(owner, includeItems: true, ct);
        var line = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
        if (cart is null || line is null) return Result<CartSummaryDto>.Failure("That item is not in your cart.");

        if (quantity <= 0)
        {
            cart.Items.Remove(line);
            _db.Remove(line);
        }
        else
        {
            var stock = await _db.Products.Where(p => p.Id == productId).Select(p => p.StockQuantity).FirstOrDefaultAsync(ct);
            line.Quantity = Math.Min(quantity, Math.Min(Math.Max(stock, 1), MaxLineQuantity));
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<CartSummaryDto>.Success(Summarize(cart));
    }

    public async Task<Result<CartSummaryDto>> RemoveAsync(CartOwner owner, int productId, CancellationToken ct = default)
    {
        if (!owner.IsValid) return Result<CartSummaryDto>.Failure("Your session has expired.");

        var cart = await FindCartAsync(owner, includeItems: true, ct);
        var line = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
        if (cart is null || line is null) return Result<CartSummaryDto>.Success(new CartSummaryDto());

        cart.Items.Remove(line);
        _db.Remove(line);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<CartSummaryDto>.Success(Summarize(cart));
    }

    public async Task ClearAsync(CartOwner owner, CancellationToken ct = default)
    {
        var cart = await FindCartAsync(owner, includeItems: true, ct);
        if (cart is null || cart.Items.Count == 0) return;

        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MergeGuestCartAsync(string anonymousId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(anonymousId) || string.IsNullOrEmpty(userId)) return;

        var guestCart = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.AnonymousId == anonymousId && c.UserId == null, ct);
        if (guestCart is null || guestCart.Items.Count == 0)
        {
            if (guestCart is not null) _db.Carts.Remove(guestCart);
            await _db.SaveChangesAsync(ct);
            return;
        }

        var userCart = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? await CreateCartAsync(CartOwner.ForUser(userId), ct);

        var stockLookup = await _db.Products.AsNoTracking()
            .Where(p => guestCart.Items.Select(i => i.ProductId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.StockQuantity, ct);

        foreach (var g in guestCart.Items)
        {
            var stock = stockLookup.TryGetValue(g.ProductId, out var s) ? s : 0;
            if (stock <= 0) continue;

            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == g.ProductId);
            if (existing is null)
                userCart.Items.Add(new CartItem
                {
                    ProductId = g.ProductId,
                    Quantity = Math.Min(g.Quantity, Math.Min(stock, MaxLineQuantity)),
                    UnitPrice = g.UnitPrice
                });
            else
                existing.Quantity = Math.Min(existing.Quantity + g.Quantity, Math.Min(stock, MaxLineQuantity));
        }

        _db.CartItems.RemoveRange(guestCart.Items);
        _db.Carts.Remove(guestCart);
        userCart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------- helpers

    private async Task<Cart?> FindCartAsync(CartOwner owner, bool includeItems, CancellationToken ct)
    {
        IQueryable<Cart> q = _db.Carts;
        if (includeItems) q = q.Include(c => c.Items);

        return owner.IsUser
            ? await q.FirstOrDefaultAsync(c => c.UserId == owner.UserId, ct)
            : await q.FirstOrDefaultAsync(c => c.AnonymousId == owner.AnonymousId && c.UserId == null, ct);
    }

    private async Task<Cart> CreateCartAsync(CartOwner owner, CancellationToken ct)
    {
        var cart = new Cart
        {
            UserId = owner.IsUser ? owner.UserId : null,
            AnonymousId = owner.IsUser ? null : owner.AnonymousId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(ct);
        return cart;
    }

    private static CartSummaryDto Summarize(Cart cart) => new()
    {
        ItemCount = cart.Items.Sum(i => i.Quantity),
        SubTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity)
    };

    private async Task<CartDto> BuildDtoAsync(IReadOnlyList<CartLineDto> lines, IReadOnlyList<string> removedNames, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        var subTotal = lines.Sum(l => l.LineTotal);

        var threshold = settings.FreeShippingThreshold;
        var flat = settings.FlatShippingRate;
        var shipping = lines.Count == 0 ? 0m
            : (threshold > 0 && subTotal >= threshold) ? 0m : flat;

        return new CartDto
        {
            Lines = lines,
            ShippingCost = shipping,
            FreeShippingThreshold = threshold,
            RemovedItemNames = removedNames
        };
    }
}
