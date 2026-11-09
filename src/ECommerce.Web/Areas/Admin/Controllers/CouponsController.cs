using ECommerce.Application.Orders;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class CouponsController : Controller
{
    private readonly ICouponAdminService _service;

    public CouponsController(ICouponAdminService service) => _service = service;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Coupons";
        return View(await _service.ListAsync(ct));
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "New coupon";
        return View(new CouponEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponEditModel model, CancellationToken ct)
    {
        ViewData["Title"] = "New coupon";
        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(model, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Coupon created.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(model);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();

        ViewData["Title"] = $"Edit: {model.Code}";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CouponEditModel model, CancellationToken ct)
    {
        ViewData["Title"] = $"Edit: {model.Code}";
        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(model, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Coupon updated.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Coupon deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
