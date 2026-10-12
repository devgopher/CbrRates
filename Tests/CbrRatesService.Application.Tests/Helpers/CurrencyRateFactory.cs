using CbrRatesService.Models;

namespace CbrRatesService.Application.Tests.Helpers;

internal static class CurrencyRateFactory
{
    public static CurrencyRate Create(
        string date = "11.10.2026",
        string name = "Foreign Currency Market",
        params (string CharCode, string Value)[] currencies)
    {
        if (currencies.Length == 0)
            currencies = [("USD", "90,1234"), ("EUR", "98,7654")];

        return new CurrencyRate
        {
            Date = date,
            Name = name,
            Currencies = currencies.Select(c => new Currency
            {
                Id = $"R-{c.CharCode}",
                NumCode = "000",
                CharCode = c.CharCode,
                Nominal = 1,
                Name = c.CharCode,
                Value = c.Value,
                VunitRate = c.Value
            }).ToList()
        };
    }
}
