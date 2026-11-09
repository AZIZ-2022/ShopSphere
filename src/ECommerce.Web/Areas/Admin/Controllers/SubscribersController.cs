using System.Text;
using ECommerce.Application.Cms;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class SubscribersController : Controller
{
    private readonly ISubscriberAdminService _service;

    public SubscribersController(ISubscriberAdminService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Newsletter subscribers";
        return View(await _service.ListAsync(page, 50, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var result = await _service.ToggleAsync(id, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Subscriber updated." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var csv = await _service.ExportCsvAsync(ct);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"subscribers-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
