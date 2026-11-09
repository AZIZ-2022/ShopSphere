using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.ViewComponents;

public sealed class BreadcrumbViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(object? items = null)
    {
        var source = items
            ?? ViewData[ControllerExtensions.BreadcrumbKey]
            ?? ViewContext.ViewData[ControllerExtensions.BreadcrumbKey];

        var trail = source as IEnumerable<BreadcrumbItem> ?? Enumerable.Empty<BreadcrumbItem>();
        return View(trail.ToList());
    }
}
