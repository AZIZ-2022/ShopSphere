using ECommerce.Application.Cms;
using ECommerce.Application.Home;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using ECommerce.Web.Models.Home;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHomePageService _home;
    private readonly IContactMessageService _contact;
    private readonly ICmsPageService _pages;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHomePageService home, IContactMessageService contact, ICmsPageService pages, ILogger<HomeController> logger)
    {
        _home = home;
        _contact = contact;
        _pages = pages;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await _home.GetAsync(ct);
        return View(data);
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        this.SetBreadcrumb(new BreadcrumbItem("Contact Us"));
        return View(new ContactFormModel());
    }

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model, CancellationToken ct)
    {
        this.SetBreadcrumb(new BreadcrumbItem("Contact Us"));

        if (ModelState.IsValid)
        {
            var result = await _contact.SubmitAsync(new ContactSubmitRequest
            {
                Name = model.Name,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message
            }, ct);

            if (result.Succeeded)
            {
                TempData["Flash"] = "Thanks for reaching out — we'll get back to you soon.";
                return RedirectToAction(nameof(Contact));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }

        return View(model);
    }

    [HttpGet("/faq")]
    public Task<IActionResult> Faq(CancellationToken ct) => CmsPageAsync(CmsPageSlugs.Faq, "FAQ", ct);

    [HttpGet("/shipping")]
    public Task<IActionResult> Shipping(CancellationToken ct) => CmsPageAsync(CmsPageSlugs.Shipping, "Shipping & Returns", ct);

    [HttpGet("/privacy")]
    public Task<IActionResult> Privacy(CancellationToken ct) => CmsPageAsync(CmsPageSlugs.Privacy, "Privacy Policy", ct);

    [HttpGet("/about-us")]
    public Task<IActionResult> AboutUs(CancellationToken ct) => CmsPageAsync(CmsPageSlugs.AboutUs, "About Us", ct);

    [HttpGet("/terms")]
    public Task<IActionResult> Terms(CancellationToken ct) => CmsPageAsync(CmsPageSlugs.Terms, "Terms & Conditions", ct);

    private async Task<IActionResult> CmsPageAsync(string slug, string breadcrumbLabel, CancellationToken ct)
    {
        var page = await _pages.GetBySlugAsync(slug, ct);
        if (page is null) return NotFound();

        this.SetBreadcrumb(new BreadcrumbItem(breadcrumbLabel));
        ViewData["Title"] = string.IsNullOrWhiteSpace(page.MetaTitle) ? page.Title : page.MetaTitle;
        ViewData["MetaDescription"] = page.MetaDescription;
        return View("CmsPage", page);
    }
}
