using CbrRatesService.Application.Mapping;
using CbrRatesService.Application.Tests.Helpers;

namespace CbrRatesService.Application.Tests.Mapping;

public sealed class CurrencyRateMapperTests
{
    [Theory]
    [InlineData("11.10.2026", 2026, 10, 11)]
    [InlineData("2026-10-11", 2026, 10, 11)]
    public void ParseRateDate_WhenKnownFormat_ReturnsParsedDate(
        string input,
        int year,
        int month,
        int day)
    {
        var result = CurrencyRateMapper.ParseRateDate(input);

        Assert.Equal(new DateOnly(year, month, day), result);
    }

    [Fact]
    public void ParseRateDate_WhenInvalid_ReturnsFallback()
    {
        var fallback = new DateOnly(2020, 1, 1);

        var result = CurrencyRateMapper.ParseRateDate("not-a-date", fallback);

        Assert.Equal(fallback, result);
    }

    [Fact]
    public void ParseRateDate_WhenInvalidAndNoFallback_ReturnsToday()
    {
        var result = CurrencyRateMapper.ParseRateDate("not-a-date");

        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), result);
    }

    [Fact]
    public void Map_CopiesRateAndCurrencies()
    {
        var source = CurrencyRateFactory.Create();

        var result = CurrencyRateMapper.Map(source);

        Assert.Equal(source.Date, result.Date);
        Assert.Equal(source.Name, result.Name);
        Assert.Equal(source.Currencies.Count, result.Currencies.Count);
        Assert.Equal(source.Currencies[0].CharCode, result.Currencies[0].CharCode);
        Assert.NotSame(source, result);
        Assert.NotSame(source.Currencies, result.Currencies);
    }
}
