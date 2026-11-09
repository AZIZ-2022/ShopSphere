using ECommerce.Application.Cms;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class SlidersController : Controller
{
    private readonly ISliderAdminService _service;
    private readonly IFileStorage _files;

    public SlidersController(ISliderAdminService service, IFileStorage files)
    {
        _service = service;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Hero sliders";
        return View(await _service.ListAsync(ct));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "New slide";
        return View(new SliderEditModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8_000_000)]
    public async Task<IActionResult> Create(SliderEditModel model, IFormFile? imageFile, IFormFile? mobileImageFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(imageFile, "sliders", ModelState, ct: ct);
        if (uploadedUrl is not null) model.ImageUrl = uploadedUrl;
        var uploadedMobileUrl = await _files.SaveImageAsync(mobileImageFile, "sliders", ModelState, ct: ct);
        if (uploadedMobileUrl is not null) model.MobileImageUrl = uploadedMobileUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(model, ct);
            if (result.Succeeded) { TempData["Flash"] = "Slide created."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "New slide";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();
        ViewData["Title"] = "Edit slide";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8_000_000)]
    public async Task<IActionResult> Edit(SliderEditModel model, IFormFile? imageFile, IFormFile? mobileImageFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(imageFile, "sliders", ModelState, ct: ct);
        if (uploadedUrl is not null) model.ImageUrl = uploadedUrl;
        var uploadedMobileUrl = await _files.SaveImageAsync(mobileImageFile, "sliders", ModelState, ct: ct);
        if (uploadedMobileUrl is not null) model.MobileImageUrl = uploadedMobileUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(model, ct);
            if (result.Succeeded) { TempData["Flash"] = "Slide updated."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "Edit slide";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Slide deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
