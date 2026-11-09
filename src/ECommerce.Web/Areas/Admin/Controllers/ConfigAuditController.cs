using ECommerce.Application.Common.Audit;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class ConfigAuditController : Controller
{
    private readonly IConfigAuditService _audit;

    public ConfigAuditController(IConfigAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> Index(string? type, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Change history";
        ViewData["Type"] = type;
        var result = await _audit.ListAsync(type, page, 20, ct);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rollback(int id, string? type, CancellationToken ct)
    {
        var result = await _audit.RollbackAsync(id, User.Identity?.Name, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Change rolled back." : result.Error;
        return RedirectToAction(nameof(Index), new { type });
    }
}
