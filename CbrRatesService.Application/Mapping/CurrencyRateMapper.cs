using System.Globalization;
using CbrRatesService.Models;

namespace CbrRatesService.Application.Mapping;

/// <summary>
///     Маппинг доменной модели <see cref="CurrencyRate"/> для API-слоя.
/// </summary>
public static class CurrencyRateMapper
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "yyyy-MM-dd"];

    /// <summary>
    ///     Разбирает дату курса из строки ответа ЦБ РФ.
    /// </summary>
    public static DateOnly ParseRateDate(string date, DateOnly? fallback = null)
    {
        if (DateOnly.TryParseExact(
                date,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            return parsed;

        if (DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            return parsed;

        return fallback ?? DateOnly.FromDateTime(DateTime.Today);
    }

    /// <summary>
    ///     Копирует модель валюты.
    /// </summary>
    public static Currency MapCurrency(Currency source)
    {
        return new Currency
        {
            Id = source.Id,
            NumCode = source.NumCode,
            CharCode = source.CharCode,
            Nominal = source.Nominal,
            Name = source.Name,
            Value = source.Value,
            VunitRate = source.VunitRate
        };
    }

    /// <summary>
    ///     Копирует модель курсов с валютами.
    /// </summary>
    public static CurrencyRate Map(CurrencyRate source)
    {
        return new CurrencyRate
        {
            Date = source.Date,
            Name = source.Name,
            Currencies = source.Currencies.Select(MapCurrency).ToList()
        };
    }
}
