using CbrRatesService.Infrastructure.Abstractions.Cache;
using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.MemoryCache.Settings;
using CbrRatesService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CbrRatesService.Infrastructure.MemoryCache.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemoryCache<TKey, TValue>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TKey : notnull
        where TValue : CurrencyRate
    {
        var section = configuration.GetSection(
            $"{CacheSettings<MemoryCacheSettings>.SectionName}:{MemoryCacheSettings.SectionName}");

        services.Configure<CacheSettings<MemoryCacheSettings>>(section);

        services.AddOptions<MemoryCacheOptions>()
            .Configure<IOptions<CacheSettings<MemoryCacheSettings>>>((options, cacheSettings) =>
            {
                var engine = cacheSettings.Value.EngineSettings;
                if (engine is null)
                    return;

                if (engine.ExpirationScanFrequency.HasValue)
                    options.ExpirationScanFrequency = engine.ExpirationScanFrequency.Value;
            });

        services.AddMemoryCache();
        services.AddSingleton<ICache<TKey, TValue>, MemoryCache<TKey, TValue>>();

        return services;
    }
}