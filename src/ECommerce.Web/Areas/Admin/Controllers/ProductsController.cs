using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class ProductsController : Controller
{
    private const int PageSize = 15;

    private readonly IProductAdminService _service;
    private readonly IFileStorage _files;
    private readonly ICurrentUserService _currentUser;

    public ProductsController(IProductAdminService service, IFileStorage files, ICurrentUserService currentUser)
    {
        _service = service;
        _files = files;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Products";
        ViewData["Search"] = q;
        return View(await _service.ListAsync(q, page, PageSize, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Title"] = "New product";
        await PopulateListsAsync(ct);
        return View(new ProductEditModel { IsActive = true, StockQuantity = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Create(ProductEditModel model, List<IFormFile>? images, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var uploaded = await SaveUploadsAsync(images, ct);
            var result = await _service.CreateAsync(model, uploaded, _currentUser.UserId, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Product created.";
                return RedirectToAction(nameof(Edit), new { id = result.Data });
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }

        ViewData["Title"] = "New product";
        await PopulateListsAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();

        ViewData["Title"] = $"Edit: {model.Name}";
        await PopulateListsAsync(ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Edit(ProductEditModel model, List<IFormFile>? images, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var uploaded = await SaveUploadsAsync(images, ct);
            var result = await _service.UpdateAsync(model, uploaded, _currentUser.UserId, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Product updated.";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }

        var fresh = await _service.GetAsync(model.Id, ct);
        model.Images = fresh?.Images ?? model.Images;
        ViewData["Title"] = $"Edit: {model.Name}";
        await PopulateListsAsync(ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, _currentUser.UserId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? "Product deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct)
    {
        var result = await _service.DeleteImageAsync(id, imageId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? "Image removed." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(int id, int imageId, CancellationToken ct)
    {
        var result = await _service.SetMainImageAsync(id, imageId, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] =
            result.Succeeded ? "Main image updated." : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    // -------------------------------------------------------------- helpers

    private async Task<List<NewProductImage>> SaveUploadsAsync(List<IFormFile>? images, CancellationToken ct)
    {
        var list = new List<NewProductImage>();
        if (images is null) return list;

        foreach (var file in images.Where(f => f.Length > 0))
        {
            if (file.Length > 3_000_000)
            {
                ModelState.AddModelError(string.Empty, $"'{file.FileName}' is larger than 3 MB and was skipped.");
                continue;
            }
            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _files.SaveAsync(stream, file.FileName, "products", ct);
                list.Add(new NewProductImage { ImageUrl = url });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"'{file.FileName}' could not be saved: {ex.Message}");
            }
        }
        return list;
    }

    private async Task PopulateListsAsync(CancellationToken ct)
    {
        var categories = await _service.GetCategoryOptionsAsync(ct);
        var brands = await _service.GetBrandOptionsAsync(ct);
        ViewBag.Categories = new SelectList(categories.Select(o => new { o.Id, o.Label }), "Id", "Label");
        ViewBag.Brands = new SelectList(brands.Select(o => new { o.Id, o.Label }), "Id", "Label");
    }
}
