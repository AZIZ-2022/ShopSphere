using ECommerce.Application.Cms;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class MessagesController : Controller
{
    private readonly IContactMessageService _service;

    public MessagesController(IContactMessageService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Contact messages";
        var (items, unread) = await _service.ListAsync(ct);
        ViewData["Unread"] = unread;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _service.MarkReadAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Message deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
