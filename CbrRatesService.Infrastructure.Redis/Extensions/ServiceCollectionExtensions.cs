using CbrRatesService.Infrastructure.Abstractions.Cache;
using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.Redis.Settings;
using CbrRatesService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CbrRatesService.Infrastructure.Redis.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCache<TKey, TValue>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TKey : notnull
        where TValue : CurrencyRate
    {
        var section = configuration.GetSection(
            $"{CacheSettings<RedisCacheSettings>.SectionName}:{RedisCacheSettings.SectionName}");

        services.Configure<CacheSettings<RedisCacheSettings>>(section);

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CacheSettings<RedisCacheSettings>>>().Value;
            var connectionString = settings.EngineSettings?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Redis connection string is not configured. " +
                    $"Set '{section.Path}:EngineSettings:ConnectionString' in appsettings.");

            return ConnectionMultiplexer.Connect(connectionString);
        });

        services.AddSingleton<ICache<TKey, TValue>, RedisCache<TKey, TValue>>();

        return services;
    }
}