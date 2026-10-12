using CbrRatesService.Models;

namespace CbrRatesService.Infrastructure.Tests.Helpers;

internal static class CurrencyRateFactory
{
    public static CurrencyRate Create(string charCode = "USD", string value = "90,1234")
    {
        return new CurrencyRate
        {
            Date = "11.10.2026",
            Name = "Foreign Currency Market",
            Currencies =
            [
                new Currency
                {
                    Id = "R01235",
                    NumCode = "840",
                    CharCode = charCode,
                    Nominal = 1,
                    Name = "Доллар США",
                    Value = value,
                    VunitRate = value
                }
            ]
        };
    }
}