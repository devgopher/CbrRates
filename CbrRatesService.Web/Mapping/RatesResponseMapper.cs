using CbrRatesService.Application.Mapping;
using CbrRatesService.Models;
using CbrRatesService.Web.Contracts.Responses;

namespace CbrRatesService.Web.Mapping;

/// <summary>
///     Преобразование доменной модели курсов в response DTO.
/// </summary>
public static class RatesResponseMapper
{
    /// <summary>
    ///     Собирает <see cref="RatesResponse"/> из <see cref="CurrencyRate"/>.
    /// </summary>
    public static RatesResponse ToResponse(CurrencyRate rates, DateOnly? requestedDate = null)
    {
        var date = CurrencyRateMapper.ParseRateDate(rates.Date, requestedDate ?? DateOnly.FromDateTime(DateTime.Today));

        return new RatesResponse(
            date,
            rates.Currencies.Select(ToCurrencyResponse).ToList());
    }

    private static CurrencyResponse ToCurrencyResponse(Currency currency)
    {
        return new CurrencyResponse(
            currency.CharCode,
            currency.NumCode,
            currency.Nominal,
            currency.Name,
            currency.Value,
            currency.VunitRate);
    }
}
