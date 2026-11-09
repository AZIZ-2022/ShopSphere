using System.Security.Claims;
using ECommerce.Application.Admin;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController : Controller
{
    private readonly IUserAdminService _service;

    public UsersController(IUserAdminService service) => _service = service;

    private string ActingUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? role, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Users";
        ViewData["Search"] = q;
        ViewData["Role"] = role;
        var result = await _service.ListAsync(new UserAdminFilter { Search = q, Role = role, Page = page, PageSize = 20 }, ct);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool active, CancellationToken ct)
    {
        var result = await _service.SetActiveAsync(id, active, ActingUserId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? (active ? "User activated." : "User deactivated.") : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, bool admin, CancellationToken ct)
    {
        var result = await _service.SetAdminRoleAsync(id, admin, ActingUserId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? (admin ? "Admin role granted." : "Admin role removed.") : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
