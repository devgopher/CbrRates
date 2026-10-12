using CbrRatesService.Models;

namespace CbrRatesService.Application.Services;

/// <summary>
///     Application-сервис получения курсов валют.
/// </summary>
public interface IRatesService
{
    /// <summary>
    ///     Возвращает курсы на <paramref name="date"/> (или на сегодня, если null).
    ///     Если задан <paramref name="currency"/> и валюта не найдена — <c>null</c> (HTTP 204).
    /// </summary>
    Task<CurrencyRate?> GetRatesAsync(
        DateOnly? date = null,
        string? currency = null,
        CancellationToken cancellationToken = default);
}
