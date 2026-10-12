using System.Text.Json;
using CbrRatesService.Infrastructure.Abstractions.Cache;
using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.Redis.Settings;
using CbrRatesService.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CbrRatesService.Infrastructure.Redis;

public class RedisCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
    where TValue : CurrencyRate
{
    private readonly IDatabase _database;
    private readonly IOptionsMonitor<CacheSettings<RedisCacheSettings>> _settings;

    public RedisCache(
        IConnectionMultiplexer connection,
        IOptionsMonitor<CacheSettings<RedisCacheSettings>> settings)
    {
        _database = connection.GetDatabase();
        _settings = settings;
    }

    public async Task<TValue?> GetAsync(TKey key)
    {
        var value = await _database.StringGetAsync(ToRedisKey(key));
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<TValue>(value!);
    }

    public async Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(value);
        var ttl = expiration ?? _settings.CurrentValue.DefaultExpiration;

        if (ttl.HasValue)
            await _database.StringSetAsync(ToRedisKey(key), json, new Expiration(ttl.Value));
        else
            await _database.StringSetAsync(ToRedisKey(key), json);
    }

    private static RedisKey ToRedisKey(TKey key)
    {
        return key.ToString()!;
    }
}