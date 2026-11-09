using System.Security.Claims;
using ECommerce.Application.Orders;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class OrdersController : Controller
{
    private readonly IOrderAdminService _service;

    public OrdersController(IOrderAdminService service) => _service = service;

    private string? Actor => User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> Index(string? q, OrderStatus? status, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Orders";
        ViewData["Search"] = q;
        ViewData["Status"] = status;
        var result = await _service.ListAsync(new OrderAdminFilter { Search = q, Status = status, Page = page, PageSize = 20 }, ct);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var order = await _service.GetAsync(id, ct);
        if (order is null) return NotFound();
        ViewData["Title"] = $"Order {order.OrderNumber}";
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, OrderStatus status, string? note, CancellationToken ct)
    {
        var result = await _service.ChangeStatusAsync(id, status, note, Actor, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? $"Order marked {status}." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(int id, PaymentStatus status, CancellationToken ct)
    {
        var result = await _service.ChangePaymentStatusAsync(id, status, Actor, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? $"Payment marked {status}." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}
