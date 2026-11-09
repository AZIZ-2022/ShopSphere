using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Features;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using ECommerce.Web.Models.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public sealed class ProductController : Controller
{
    private readonly ICatalogQueryService _catalog;
    private readonly IReviewService _reviews;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFeatureFlagService _features;

    public ProductController(
        ICatalogQueryService catalog,
        IReviewService reviews,
        UserManager<ApplicationUser> userManager,
        IFeatureFlagService features)
    {
        _catalog = catalog;
        _reviews = reviews;
        _userManager = userManager;
        _features = features;
    }

    [HttpGet("/p/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var product = await _catalog.GetProductBySlugAsync(slug, ct);
        if (product is null)
            return NotFound();

        await _catalog.IncrementViewCountAsync(product.Id, ct);
        var related = await _catalog.GetRelatedAsync(product.Id, 4, ct);

        var trail = product.CategoryTrail
            .Select(c => new BreadcrumbItem(c.Name, $"/c/{c.Slug}"))
            .Append(new BreadcrumbItem(product.Name))
            .ToArray();
        this.SetBreadcrumb(trail);

        return View(BuildViewModel(product, related));
    }

    [HttpPost("/p/{slug}/review")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(string slug, [Bind(Prefix = "ReviewForm")] ReviewFormModel form, CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Reviews, ct))
            return NotFound();

        var product = await _catalog.GetProductBySlugAsync(slug, ct);
        if (product is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var related = await _catalog.GetRelatedAsync(product.Id, 4, ct);
            this.SetBreadcrumb(new BreadcrumbItem(product.CategoryName, $"/c/{product.CategorySlug}"), new BreadcrumbItem(product.Name));
            return View(nameof(Details), BuildViewModel(product, related, form));
        }

        var user = await _userManager.GetUserAsync(User);
        var result = await _reviews.SubmitAsync(new SubmitReviewRequest
        {
            ProductId = product.Id,
            UserId = user!.Id,
            ReviewerName = string.IsNullOrWhiteSpace(user.FullName) ? "Verified Buyer" : user.FullName,
            Rating = form.Rating,
            Title = form.Title,
            Comment = form.Comment
        }, ct);

        if (result.Succeeded)
            TempData["Flash"] = "Thanks! Your review has been posted.";
        else
            TempData["FlashError"] = result.Error;

        return RedirectToAction(nameof(Details), new { slug });
    }

    private ProductDetailViewModel BuildViewModel(
        ProductDetailDto product,
        IReadOnlyList<ProductListItemDto> related,
        ReviewFormModel? form = null)
    {
        var isAuth = User.Identity?.IsAuthenticated == true;
        return new ProductDetailViewModel
        {
            Product = product,
            Related = related,
            CanReview = isAuth,
            HasReviewed = false,
            ReviewForm = form ?? new ReviewFormModel { ProductId = product.Id }
        };
    }
}
