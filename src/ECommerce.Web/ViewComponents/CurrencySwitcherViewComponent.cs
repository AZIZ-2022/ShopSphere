using ECommerce.Application.Common.Interfaces;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.ViewComponents;

public sealed class CurrencySwitcherViewComponent : ViewComponent
{
    private readonly ISiteSettingsService _settings;

    public CurrencySwitcherViewComponent(ISiteSettingsService settings) => _settings = settings;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await _settings.GetAsync();
        var current = CurrencyCookie.Resolve(HttpContext, settings.SupportedCurrencies, settings.BaseCurrency);

        var vm = new CurrencySwitcherViewModel
        {
            Current = current,
            Options = settings.SupportedCurrencies,
            ReturnUrl = Request.Path + Request.QueryString
        };
        return View(vm);
    }
}

public sealed class CurrencySwitcherViewModel
{
    public required string Current { get; init; }
    public required IReadOnlyList<string> Options { get; init; }
    public required string ReturnUrl { get; init; }

    public static string Symbol(string code) => code switch
    {
        "USD" => "$",
        "BDT" => "৳",
        "EUR" => "€",
        "GBP" => "£",
        _ => code
    };
}
