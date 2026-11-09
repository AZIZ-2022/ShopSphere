using ECommerce.Application.Cms;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class SettingsController : Controller
{
    private readonly ISiteSettingsAdminService _service;
    private readonly IFileStorage _files;

    public SettingsController(ISiteSettingsAdminService service, IFileStorage files)
    {
        _service = service;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Site settings";
        return View(await _service.ListAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Index(Dictionary<string, string?> values, IFormFile? logoFile, IFormFile? faviconFile, CancellationToken ct)
    {
        values ??= new();

        var logoUrl = await _files.SaveImageAsync(logoFile, "site", ModelState, ct: ct);
        if (logoUrl is not null) values["Site.LogoUrl"] = logoUrl;

        var faviconUrl = await _files.SaveImageAsync(faviconFile, "site", ModelState, maxBytes: 500_000, ct: ct);
        if (faviconUrl is not null) values["Site.FaviconUrl"] = faviconUrl;

        await _service.SaveAsync(values, ct);
        TempData["Flash"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPreset(Dictionary<string, string?> values, string preset, CancellationToken ct)
    {
        values ??= new();
        var match = ThemeCatalog.Presets.FirstOrDefault(p => p.Key == preset);
        if (match != default)
        {
            values["Theme.PrimaryColor"] = match.Primary;
            values["Theme.AccentColor"] = match.Accent;
        }

        await _service.SaveAsync(values, ct);
        TempData["Flash"] = match != default ? $"Applied the \"{match.Label}\" theme preset." : "Settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
