using System.Security.Claims;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Wishlist;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public sealed class WishlistApiController : ControllerBase
{
    private readonly IWishlistService _wishlist;
    private readonly IFeatureFlagService _features;

    public WishlistApiController(IWishlistService wishlist, IFeatureFlagService features)
    {
        _wishlist = wishlist;
        _features = features;
    }

    public sealed record ToggleRequest(int ProductId);

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("ids")]
    public async Task<IActionResult> Ids(CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Wishlist, ct)) return NotFound();

        var ids = await _wishlist.GetProductIdsAsync(UserId, ct);
        return Ok(ApiResponse.Ok(new { ids }));
    }

    [HttpPost("toggle")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Toggle([FromBody] ToggleRequest request, CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Wishlist, ct)) return NotFound();

        var result = await _wishlist.ToggleAsync(UserId, request.ProductId, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse.Fail(result.Error ?? "Could not update wishlist."));

        return Ok(ApiResponse.Ok(new
        {
            inWishlist = result.Data!.InWishlist,
            count = result.Data.Count
        }, result.Data.InWishlist ? "Added to wishlist." : "Removed from wishlist."));
    }
}
