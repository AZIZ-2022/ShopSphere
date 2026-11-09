using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Cms;

/// <summary>The fixed set of admin-editable static pages.</summary>
public static class CmsPageSlugs
{
    public const string AboutUs = "about-us";
    public const string Terms = "terms";
    public const string Privacy = "privacy";
    public const string Shipping = "shipping";
    public const string Faq = "faq";

    public static readonly (string Slug, string Label)[] All =
    {
        (AboutUs, "About Us"),
        (Terms, "Terms & Conditions"),
        (Privacy, "Privacy Policy"),
        (Shipping, "Shipping & Returns"),
        (Faq, "FAQ"),
    };
}

public sealed class CmsPageListItem
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
}

public sealed class CmsPageEditModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; } = true;
}

public sealed class CmsPageViewDto
{
    public string Title { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public interface ICmsPageAdminService
{
    Task<IReadOnlyList<CmsPageListItem>> ListAsync(CancellationToken ct = default);
    Task<CmsPageEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<Result> UpdateAsync(CmsPageEditModel model, CancellationToken ct = default);
}

/// <summary>Public read access to published CMS pages, used by the storefront.</summary>
public interface ICmsPageService
{
    Task<CmsPageViewDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    void Invalidate();
}
