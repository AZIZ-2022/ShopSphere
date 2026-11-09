namespace ECommerce.Web.Models.Catalog;

public sealed record StarRating(double Value, int Count = 0, bool ShowCount = true, bool ShowEmpty = true);

public sealed record PagerModel(int Page, int TotalPages, Func<int, string> UrlForPage);
