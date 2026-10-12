using System.Text.Json;
using CbrRatesService.Infrastructure.Abstractions.Cache;
using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.MemoryCache.Settings;
using CbrRatesService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CbrRatesService.Infrastructure.MemoryCache;

public class MemoryCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
    where TValue : CurrencyRate
{
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<CacheSettings<MemoryCacheSettings>> _settings;

    public MemoryCache(IMemoryCache cache, IOptionsMonitor<CacheSettings<MemoryCacheSettings>> settings)
    {
        _cache = cache;
        _settings = settings;
    }

    public Task<TValue?> GetAsync(TKey key)
    {
        if (!_cache.TryGetValue(key, out string? json) || string.IsNullOrEmpty(json))
            return Task.FromResult<TValue?>(null);

        return Task.FromResult(JsonSerializer.Deserialize<TValue>(json));
    }

    public Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        var ttl = expiration ?? _settings.CurrentValue.DefaultExpiration;
        if (ttl.HasValue)
            options.AbsoluteExpirationRelativeToNow = ttl;

        var json = JsonSerializer.Serialize(value);
        _cache.Set(key, json, options);
        return Task.CompletedTask;
    }
}