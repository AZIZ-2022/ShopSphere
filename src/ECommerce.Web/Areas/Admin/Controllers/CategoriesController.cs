using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class CategoriesController : Controller
{
    private readonly ICategoryAdminService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _files;

    public CategoriesController(ICategoryAdminService service, ICurrentUserService currentUser, IFileStorage files)
    {
        _service = service;
        _currentUser = currentUser;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Categories";
        return View(await _service.ListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "New category";
        await PopulateParentsAsync(null, ct);
        return View(new CategoryEditModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Create(CategoryEditModel model, IFormFile? imageFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(imageFile, "categories", ModelState, ct: ct);
        if (uploadedUrl is not null) model.ImageUrl = uploadedUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(model, _currentUser.UserId, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Category created.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }

        ViewData["Title"] = "New category";
        await PopulateParentsAsync(model.ParentCategoryId, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();

        ViewData["Title"] = $"Edit: {model.Name}";
        await PopulateParentsAsync(model.ParentCategoryId, ct, id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Edit(CategoryEditModel model, IFormFile? imageFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(imageFile, "categories", ModelState, ct: ct);
        if (uploadedUrl is not null) model.ImageUrl = uploadedUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(model, _currentUser.UserId, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Category updated.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }

        ViewData["Title"] = $"Edit: {model.Name}";
        await PopulateParentsAsync(model.ParentCategoryId, ct, model.Id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, _currentUser.UserId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? "Category deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentsAsync(int? selected, CancellationToken ct, int? excludingId = null)
    {
        var options = await _service.GetParentOptionsAsync(excludingId, ct);
        ViewBag.Parents = new SelectList(
            options.Select(o => new { o.Id, o.Label }), "Id", "Label", selected);
    }
}
