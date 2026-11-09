using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Catalog;

/// <summary>
/// Product reviews. Reviews are auto-approved and the product's rating cache is
/// recalculated immediately (no separate moderation UI in scope).
/// </summary>
public sealed class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ReviewService(ApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> SubmitAsync(SubmitReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5)
            return Result.Failure("Please choose a rating between 1 and 5 stars.");

        var comment = (request.Comment ?? string.Empty).Trim();
        if (comment.Length < 10)
            return Result.Failure("Your review is a little short — tell us a bit more (10+ characters).");
        if (comment.Length > 2000)
            return Result.Failure("Your review is too long (2000 characters max).");

        var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive, ct);
        if (!productExists)
            return Result.Failure("Product not found.");

        var already = await _db.ProductReviews
            .AnyAsync(r => r.ProductId == request.ProductId && r.UserId == request.UserId, ct);
        if (already)
            return Result.Failure("You've already reviewed this product.");

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = request.ProductId,
            UserId = request.UserId,
            ReviewerName = string.IsNullOrWhiteSpace(request.ReviewerName) ? "Anonymous" : request.ReviewerName.Trim(),
            Rating = request.Rating,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Comment = comment,
            IsApproved = true,
            CreatedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        await RecalculateRatingAsync(request.ProductId, ct);
        return Result.Success();
    }

    private async Task RecalculateRatingAsync(int productId, CancellationToken ct)
    {
        var stats = await _db.ProductReviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .GroupBy(_ => 1)
            .Select(g => new { Avg = g.Average(r => (decimal)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(ct);

        var avg = stats is null ? 0m : Math.Round(stats.Avg, 2);
        var count = stats?.Count ?? 0;

        await _db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.AverageRating, avg)
                .SetProperty(p => p.ReviewCount, count), ct);
    }
}
