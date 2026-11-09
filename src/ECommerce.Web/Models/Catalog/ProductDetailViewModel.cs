using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Catalog;

namespace ECommerce.Web.Models.Catalog;

public sealed class ProductDetailViewModel
{
    public required ProductDetailDto Product { get; init; }
    public IReadOnlyList<ProductListItemDto> Related { get; init; } = Array.Empty<ProductListItemDto>();
    public bool CanReview { get; init; }
    public bool HasReviewed { get; init; }
    public ReviewFormModel ReviewForm { get; init; } = new();
}

public sealed class ReviewFormModel
{
    public int ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "Please pick a star rating.")]
    public byte Rating { get; set; } = 5;

    [StringLength(150)]
    public string? Title { get; set; }

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Comment { get; set; } = string.Empty;
}
