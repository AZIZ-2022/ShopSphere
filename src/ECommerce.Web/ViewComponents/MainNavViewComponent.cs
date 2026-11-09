using ECommerce.Application.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.ViewComponents;

public sealed class MainNavViewComponent : ViewComponent
{
    private readonly INavigationService _navigation;

    public MainNavViewComponent(INavigationService navigation) => _navigation = navigation;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menu = await _navigation.GetMenuAsync();
        return View(menu);
    }
}
