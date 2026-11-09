using ECommerce.Application.Common.Features;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class FeatureFlagsController : Controller
{
    private readonly IFeatureFlagAdminService _service;

    public FeatureFlagsController(IFeatureFlagAdminService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Features";
        return View(await _service.ListAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string key, bool enabled, CancellationToken ct)
    {
        var result = await _service.SetEnabledAsync(key, enabled, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded
            ? $"{key} {(enabled ? "enabled" : "disabled")}."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
