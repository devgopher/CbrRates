using CbrRatesService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CbrRatesService.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRatesService, RatesService>();
        return services;
    }
}