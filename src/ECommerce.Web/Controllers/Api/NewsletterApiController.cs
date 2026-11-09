using ECommerce.Application.Common.Features;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
[Route("api/newsletter")]
public sealed class NewsletterApiController : ControllerBase
{
    private readonly INewsletterService _newsletter;
    private readonly IFeatureFlagService _features;

    public NewsletterApiController(INewsletterService newsletter, IFeatureFlagService features)
    {
        _newsletter = newsletter;
        _features = features;
    }

    public sealed record SubscribeRequest(string? Email);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        if (!await _features.IsEnabledAsync(FeatureFlagKeys.Newsletter, ct))
            return NotFound(ApiResponse.Fail("Newsletter signups are currently unavailable."));

        var result = await _newsletter.SubscribeAsync(request.Email ?? string.Empty, ct);

        return result.Succeeded
            ? Ok(ApiResponse.Ok(message: "You're subscribed. Thanks!"))
            : BadRequest(ApiResponse.Fail(result.Error ?? "Could not subscribe."));
    }
}
