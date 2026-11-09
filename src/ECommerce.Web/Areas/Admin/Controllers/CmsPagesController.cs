using ECommerce.Application.Cms;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class CmsPagesController : Controller
{
    private readonly ICmsPageAdminService _service;

    public CmsPagesController(ICmsPageAdminService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Pages";
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
    public async Task<IActionResult> Edit(CmsPageEditModel model, CancellationToken ct)
    {
        ViewData["Title"] = $"Edit: {model.Label}";
        if (!ModelState.IsValid) return View(model);

        var result = await _service.UpdateAsync(model, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Page updated." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
