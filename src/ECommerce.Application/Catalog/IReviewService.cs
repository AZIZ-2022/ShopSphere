using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Catalog;

public sealed class SubmitReviewRequest
{
    public int ProductId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string ReviewerName { get; init; } = string.Empty;
    public byte Rating { get; init; }
    public string? Title { get; init; }
    public string Comment { get; init; } = string.Empty;
}

public interface IReviewService
{
    Task<Result> SubmitAsync(SubmitReviewRequest request, CancellationToken ct = default);
}
