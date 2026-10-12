namespace CbrRatesService.Web.Contracts.Responses;

/// <summary>
///     Курсы валют на дату (тело ответа HTTP 200).
/// </summary>
/// <param name="Date">Дата курса (в JSON — ISO <c>yyyy-MM-dd</c>).</param>
/// <param name="Currencies">Все валюты или одна после фильтра по <c>currency</c>.</param>
public sealed record RatesResponse(DateOnly Date, IReadOnlyList<CurrencyResponse> Currencies);
