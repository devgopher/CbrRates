using CbrRatesService.Infrastructure.Cbr.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CbrRatesService.Infrastructure.Cbr.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Регистрирует HTTP-клиент ЦБ как <see cref="CbrRatesReceiver" /> (без привязки к интерфейсу),
    ///     чтобы поверх можно было повесить декоратор кэша.
    /// </summary>
    public static IServiceCollection AddCbrRatesReceiver(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CbrSettings>(configuration.GetSection(CbrSettings.SectionName));

        services.AddHttpClient<CbrRatesReceiver>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptionsMonitor<CbrSettings>>().CurrentValue;

            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
                throw new InvalidOperationException(
                    $"CBR BaseUrl is not configured. Set '{CbrSettings.SectionName}:BaseUrl' in appsettings.");

            client.BaseAddress = new Uri(EnsureTrailingSlash(settings.BaseUrl));
            client.Timeout = TimeSpan.FromSeconds(settings.Timeout);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        });

        return services;
    }

    private static string EnsureTrailingSlash(string baseUrl)
    {
        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }
}