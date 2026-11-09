using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.ViewComponents;

public sealed class SiteFooterViewComponent : ViewComponent
{
    private readonly ISiteSettingsService _settings;
    private readonly INavigationService _navigation;

    public SiteFooterViewComponent(ISiteSettingsService settings, INavigationService navigation)
    {
        _settings = settings;
        _navigation = navigation;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await _settings.GetAsync();
        var menu = await _navigation.GetMenuAsync();

        var vm = new SiteFooterViewModel
        {
            Settings = settings,
            TopCategories = menu.Take(6).ToList(),
            Year = DateTime.UtcNow.Year
        };

        return View(vm);
    }
}

public sealed class SiteFooterViewModel
{
    public required SiteSettingsSnapshot Settings { get; init; }
    public required IReadOnlyList<NavCategoryDto> TopCategories { get; init; }
    public int Year { get; init; }
}
