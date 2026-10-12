using CbrRatesService.Models;

namespace CbrRatesService.Infrastructure.Abstractions.Cache;

public interface ICache<TKey, TValue>
    where TKey : notnull
    where TValue : CurrencyRate
{
    Task<TValue?> GetAsync(TKey key);

    Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null);
}