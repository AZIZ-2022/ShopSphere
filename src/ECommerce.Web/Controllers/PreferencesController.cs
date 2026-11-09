using ECommerce.Application.Common.Interfaces;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("preferences")]
public sealed class PreferencesController : Controller
{
    private readonly ISiteSettingsService _settings;

    public PreferencesController(ISiteSettingsService settings) => _settings = settings;

    [HttpPost("currency")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrency(string code, string? returnUrl)
    {
        var settings = await _settings.GetAsync();
        var normalized = (code ?? string.Empty).ToUpperInvariant();

        if (settings.SupportedCurrencies.Contains(normalized))
            CurrencyCookie.Write(HttpContext, normalized);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }
}
