using ECommerce.Application.Cms;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class TestimonialsController : Controller
{
    private readonly ITestimonialAdminService _service;
    private readonly IFileStorage _files;

    public TestimonialsController(ITestimonialAdminService service, IFileStorage files)
    {
        _service = service;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Testimonials";
        return View(await _service.ListAsync(ct));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "New testimonial";
        return View(new TestimonialEditModel { IsActive = true, Rating = 5 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Create(TestimonialEditModel model, IFormFile? photoFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(photoFile, "testimonials", ModelState, ct: ct);
        if (uploadedUrl is not null) model.PhotoUrl = uploadedUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(model, ct);
            if (result.Succeeded) { TempData["Flash"] = "Testimonial created."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "New testimonial";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _service.GetAsync(id, ct);
        if (model is null) return NotFound();
        ViewData["Title"] = "Edit testimonial";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Edit(TestimonialEditModel model, IFormFile? photoFile, CancellationToken ct)
    {
        var uploadedUrl = await _files.SaveImageAsync(photoFile, "testimonials", ModelState, ct: ct);
        if (uploadedUrl is not null) model.PhotoUrl = uploadedUrl;

        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(model, ct);
            if (result.Succeeded) { TempData["Flash"] = "Testimonial updated."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "Edit testimonial";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Testimonial deleted." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
