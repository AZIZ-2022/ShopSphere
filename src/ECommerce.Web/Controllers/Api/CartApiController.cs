using ECommerce.Application.Cart;
using ECommerce.Application.Common.Models;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
[Route("api/cart")]
public sealed class CartApiController : ControllerBase
{
    private readonly ICartService _cart;
    private readonly ICartOwnerAccessor _owner;
    private readonly IMoneyFormatter _money;

    public CartApiController(ICartService cart, ICartOwnerAccessor owner, IMoneyFormatter money)
    {
        _cart = cart;
        _owner = owner;
        _money = money;
    }

    public sealed record ItemRequest(int ProductId, int Quantity = 1);
    public sealed record RemoveRequest(int ProductId);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var s = await _cart.GetSummaryAsync(_owner.GetOwner(), ct);
        return Ok(ApiResponse.Ok(Payload(s)));
    }

    [HttpPost("add")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Add([FromBody] ItemRequest request, CancellationToken ct)
    {
        var result = await _cart.AddAsync(_owner.GetOwner(), request.ProductId, request.Quantity, ct);
        return Respond(result, "Added to cart.");
    }

    [HttpPost("update")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Update([FromBody] ItemRequest request, CancellationToken ct)
    {
        var result = await _cart.UpdateQuantityAsync(_owner.GetOwner(), request.ProductId, request.Quantity, ct);
        return Respond(result, "Cart updated.");
    }

    [HttpPost("remove")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Remove([FromBody] RemoveRequest request, CancellationToken ct)
    {
        var result = await _cart.RemoveAsync(_owner.GetOwner(), request.ProductId, ct);
        return Respond(result, "Item removed.");
    }

    private IActionResult Respond(Result<CartSummaryDto> result, string okMessage)
    {
        if (!result.Succeeded)
            return BadRequest(ApiResponse.Fail(result.Error ?? "Something went wrong."));

        // A succeeded result may still carry an advisory message (e.g. stock capped).
        var message = string.IsNullOrWhiteSpace(result.Error) ? okMessage : result.Error;
        return Ok(ApiResponse.Ok(Payload(result.Data ?? new CartSummaryDto()), message));
    }

    private object Payload(CartSummaryDto s) => new
    {
        itemCount = s.ItemCount,
        subTotal = s.SubTotal,
        subTotalDisplay = _money.Format(s.SubTotal)
    };
}
