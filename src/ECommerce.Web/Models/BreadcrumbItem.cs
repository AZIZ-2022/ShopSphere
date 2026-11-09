namespace ECommerce.Web.Models;

/// <summary>One node in the breadcrumb trail. A null <see cref="Url"/> marks the current page.</summary>
public sealed record BreadcrumbItem(string Text, string? Url = null);
