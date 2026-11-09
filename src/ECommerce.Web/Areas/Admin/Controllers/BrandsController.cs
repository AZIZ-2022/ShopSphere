using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class BrandsController : Controller
{
    private readonly IBrandAdminService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _files;

    public BrandsController(IBrandAdminService service, ICurrentUserService currentUser, IFileStorage files)
    {
        _service = service;
        _currentUser = currentUser;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Brands";
        return View(await _service.ListAsync(ct));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "New brand";
        return View(new BrandEditModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Create(BrandEditModel model, IFormFile? logoFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(logoFile, "brands", ModelState, ct: ct);
        if (uploadedUrl is not null) model.LogoUrl = uploadedUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(model, _currentUser.UserId, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Brand created.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "New brand";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();
        ViewData["Title"] = $"Edit: {model.Name}";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Edit(BrandEditModel model, IFormFile? logoFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(logoFile, "brands", ModelState, ct: ct);
        if (uploadedUrl is not null) model.LogoUrl = uploadedUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(model, _currentUser.UserId, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Brand updated.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = $"Edit: {model.Name}";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, _currentUser.UserId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? "Brand deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
