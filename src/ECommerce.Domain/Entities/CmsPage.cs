using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// An admin-editable static content page (About Us, Terms, Privacy, Shipping, FAQ, …).
/// The set of valid slugs is fixed in code (see <c>CmsPageSlugs</c> in the Application layer) —
/// admins edit content/SEO/publish state, they don't create arbitrary new routed pages.
/// </summary>
public class CmsPage : AuditableEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; } = true;
}
