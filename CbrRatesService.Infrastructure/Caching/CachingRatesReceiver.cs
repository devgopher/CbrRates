using CbrRatesService.Infrastructure.Abstractions.Cache;
using CbrRatesService.Infrastructure.Abstractions.Integrations;
using CbrRatesService.Models;

namespace CbrRatesService.Infrastructure.Caching;

/// <summary>
///     Декоратор <see cref="IRatesReceiver" />: кэширует полный ответ ЦБ по дате.
/// </summary>
public sealed class CachingRatesReceiver(
    IRatesReceiver inner,
    ICache<string, CurrencyRate> cache) : IRatesReceiver
{
    public async Task<CurrencyRate?> GetRatesAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var resolvedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var key = resolvedDate.ToString("yyyy-MM-dd");

        var cached = await cache.GetAsync(key).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var rates = await inner.GetRatesAsync(resolvedDate, cancellationToken).ConfigureAwait(false);
        if (rates is not null)
            await cache.SetAsync(key, rates).ConfigureAwait(false);

        return rates;
    }
}