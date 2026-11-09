using ECommerce.Application.Home;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class HomeSectionsController : Controller
{
    private readonly IHomeSectionAdminService _service;

    public HomeSectionsController(IHomeSectionAdminService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Homepage sections";
        return View(await _service.ListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();

        ViewData["Title"] = $"Edit: {model.Label}";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HomeSectionEditModel model, CancellationToken ct)
    {
        ViewData["Title"] = $"Edit: {model.Label}";
        if (!ModelState.IsValid) return View(model);

        var result = await _service.UpdateAsync(model, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Section updated." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveUp(int id, CancellationToken ct)
    {
        await _service.MoveAsync(id, up: true, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveDown(int id, CancellationToken ct)
    {
        await _service.MoveAsync(id, up: false, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id, bool visible, CancellationToken ct)
    {
        await _service.SetVisibleAsync(id, visible, ct);
        return RedirectToAction(nameof(Index));
    }
}
