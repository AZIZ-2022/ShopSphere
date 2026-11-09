using ECommerce.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Infrastructure;

public static class ControllerExtensions
{
    public const string BreadcrumbKey = "__Breadcrumb";

    /// <summary>
    /// Sets the breadcrumb trail for the current view. Do NOT include "Home" —
    /// the breadcrumb component prepends it. Pass items deepest-last; the final
    /// item is rendered as the current page (no link) automatically.
    /// </summary>
    public static void SetBreadcrumb(this Controller controller, params BreadcrumbItem[] items)
    {
        controller.ViewData[BreadcrumbKey] = items;
    }
}
