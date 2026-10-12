using CbrRatesService.Infrastructure.Abstractions.Cache;
using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.Abstractions.Integrations;
using CbrRatesService.Infrastructure.Caching;
using CbrRatesService.Infrastructure.Cbr;
using CbrRatesService.Infrastructure.Cbr.Extensions;
using CbrRatesService.Infrastructure.MemoryCache.Extensions;
using CbrRatesService.Infrastructure.MemoryCache.Settings;
using CbrRatesService.Infrastructure.Redis.Extensions;
using CbrRatesService.Infrastructure.Redis.Settings;
using CbrRatesService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CbrRatesService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Добавляет инфраструктуру приложения
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCbrRatesReceiver(configuration);
        services.AddRatesCache(configuration);
        services.AddRatesReceiver();

        return services;
    }

    /// <summary>
    ///     Выбирает провайдер кэша по флагам Enabled: Redis имеет приоритет над Memory.
    /// </summary>
    private static IServiceCollection AddRatesCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisEnabled = configuration.GetValue<bool>(
            $"{CacheSettings<object>.SectionName}:{RedisCacheSettings.SectionName}:Enabled");
        var memoryEnabled = configuration.GetValue<bool>(
            $"{CacheSettings<object>.SectionName}:{MemoryCacheSettings.SectionName}:Enabled");

        if (redisEnabled)
        {
            services.AddRedisCache<string, CurrencyRate>(configuration);
            return services;
        }

        if (memoryEnabled)
            services.AddMemoryCache<string, CurrencyRate>(configuration);

        return services;
    }

    private static IServiceCollection AddRatesReceiver(this IServiceCollection services)
    {
        services.AddTransient<IRatesReceiver>(sp =>
        {
            var inner = sp.GetRequiredService<CbrRatesReceiver>();
            var cache = sp.GetService<ICache<string, CurrencyRate>>();

            return cache is null
                ? inner
                : new CachingRatesReceiver(inner, cache);
        });

        return services;
    }
}