using System.Globalization;
using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Infrastructure;

/// <summary>
/// Formats a base-currency (USD) amount in the visitor's selected display currency.
/// Prices are stored in the base currency; this only affects presentation.
/// </summary>
public interface IMoneyFormatter
{
    /// <summary>e.g. <c>$79.99</c> or <c>৳9,599</c>, in the visitor's currently selected currency.</summary>
    string Format(decimal baseAmount);

    /// <summary>Formats a base-currency amount in an explicit currency (used for historical order snapshots).</summary>
    string FormatIn(decimal baseAmount, string currencyCode, decimal exchangeRate);

    string CurrencyCode { get; }
    string Symbol { get; }
}

public sealed class MoneyFormatter : IMoneyFormatter
{
    private readonly string _code;
    private readonly string _symbol;
    private readonly decimal _rate;
    private readonly bool _wholeNumbers;

    public MoneyFormatter(IHttpContextAccessor accessor, ISiteSettingsService settingsService)
    {
        var settings = settingsService.GetAsync().GetAwaiter().GetResult();
        var http = accessor.HttpContext;

        _code = http is not null
            ? CurrencyCookie.Resolve(http, settings.SupportedCurrencies, settings.BaseCurrency)
            : settings.BaseCurrency;

        if (string.Equals(_code, "BDT", StringComparison.OrdinalIgnoreCase))
        {
            _symbol = "৳";
            _rate = settings.BdtRate <= 0 ? 1m : settings.BdtRate;
            _wholeNumbers = true;
        }
        else
        {
            _code = "USD";
            _symbol = "$";
            _rate = 1m;
            _wholeNumbers = false;
        }
    }

    public string CurrencyCode => _code;
    public string Symbol => _symbol;

    public string Format(decimal baseAmount) => Render(baseAmount * _rate, _symbol, _wholeNumbers);

    public string FormatIn(decimal baseAmount, string currencyCode, decimal exchangeRate)
    {
        var code = (currencyCode ?? "USD").ToUpperInvariant();
        var rate = exchangeRate <= 0 ? 1m : exchangeRate;
        var symbol = code switch { "BDT" => "৳", "EUR" => "€", "GBP" => "£", _ => "$" };
        return Render(baseAmount * rate, symbol, code == "BDT");
    }

    private static string Render(decimal amount, string symbol, bool whole) => whole
        ? $"{symbol}{Math.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("#,##0", CultureInfo.InvariantCulture)}"
        : $"{symbol}{amount.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
}
