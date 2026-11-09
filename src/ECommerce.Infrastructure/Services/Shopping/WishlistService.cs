using ECommerce.Application.Cart;
using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Wishlist;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Shopping;

public sealed class WishlistService : IWishlistService
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IDateTimeProvider _clock;

    public WishlistService(ApplicationDbContext db, ICartService cart, IDateTimeProvider clock)
    {
        _db = db;
        _cart = cart;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetItemsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return Array.Empty<ProductListItemDto>();

        return await _db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId && w.Product.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new ProductListItemDto
            {
                Id = w.Product.Id,
                Name = w.Product.Name,
                Slug = w.Product.Slug,
                CategoryName = w.Product.Category.Name,
                BrandName = w.Product.Brand != null ? w.Product.Brand.Name : null,
                Price = w.Product.Price,
                DiscountPrice = w.Product.DiscountPrice,
                StockQuantity = w.Product.StockQuantity,
                IsFeatured = w.Product.IsFeatured,
                IsNewArrival = w.Product.IsNewArrival,
                AverageRating = w.Product.AverageRating,
                ReviewCount = w.Product.ReviewCount,
                MainImageUrl = w.Product.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<int>> GetProductIdsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return Array.Empty<int>();
        return await _db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return 0;
        return await _db.WishlistItems.CountAsync(w => w.UserId == userId, ct);
    }

    public async Task<Result<WishlistToggleResult>> ToggleAsync(string userId, int productId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return Result<WishlistToggleResult>.Failure("Please log in to use your wishlist.");

        var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct);
        if (!exists) return Result<WishlistToggleResult>.Failure("This product is no longer available.");

        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);
        bool inWishlist;
        if (item is null)
        {
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId, CreatedAt = _clock.UtcNow });
            inWishlist = true;
        }
        else
        {
            _db.WishlistItems.Remove(item);
            inWishlist = false;
        }
        await _db.SaveChangesAsync(ct);

        var count = await CountAsync(userId, ct);
        return Result<WishlistToggleResult>.Success(new WishlistToggleResult { InWishlist = inWishlist, Count = count });
    }

    public async Task<Result> RemoveAsync(string userId, int productId, CancellationToken ct = default)
    {
        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);
        if (item is null) return Result.Success();
        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MoveToCartAsync(string userId, int productId, CancellationToken ct = default)
    {
        var add = await _cart.AddAsync(CartOwner.ForUser(userId), productId, 1, ct);
        if (!add.Succeeded) return Result.Failure(add.Error ?? "Could not add to cart.");

        await RemoveAsync(userId, productId, ct);
        return Result.Success();
    }
}
