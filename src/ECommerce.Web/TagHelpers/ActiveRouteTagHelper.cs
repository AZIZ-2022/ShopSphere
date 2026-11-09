using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ECommerce.Web.TagHelpers;

/// <summary>
/// Adds <c>is-active</c> (or a custom class) to an element when the current request
/// matches the given controller/action, or when the current path starts with
/// <c>active-when-path</c>. Use on nav links.
///
/// <code>&lt;a asp-controller="Catalog" asp-action="Index" active-when-controller="Catalog"&gt;...&lt;/a&gt;</code>
/// </summary>
[HtmlTargetElement("a", Attributes = "active-when-controller")]
[HtmlTargetElement("a", Attributes = "active-when-path")]
public sealed class ActiveRouteTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeName("active-when-controller")]
    public string? Controller { get; set; }

    [HtmlAttributeName("active-when-action")]
    public string? Action { get; set; }

    [HtmlAttributeName("active-when-path")]
    public string? PathPrefix { get; set; }

    [HtmlAttributeName("active-class")]
    public string ActiveClass { get; set; } = "is-active";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!IsActive())
            return;

        var existing = output.Attributes["class"]?.Value?.ToString();
        output.Attributes.SetAttribute("class",
            string.IsNullOrEmpty(existing) ? ActiveClass : $"{existing} {ActiveClass}");
    }

    private bool IsActive()
    {
        if (!string.IsNullOrEmpty(PathPrefix))
        {
            var path = ViewContext.HttpContext.Request.Path.Value ?? "/";
            return path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase);
        }

        var routeValues = ViewContext.RouteData.Values;
        var currentController = routeValues["controller"]?.ToString();
        var currentAction = routeValues["action"]?.ToString();

        if (!string.IsNullOrEmpty(Controller) &&
            !string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(Action) &&
            !string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
