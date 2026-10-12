using System.Reflection;
using Microsoft.OpenApi.Models;

namespace CbrRatesService.Web.Extensions;

/// <summary>
///     Регистрация Swagger / OpenAPI для Web API.
/// </summary>
public static class SwaggerExtensions
{
    private const string OpenApiFileName = "openapi.yaml";

    /// <summary>
    ///     Подключает SwaggerGen и XML-комментарии сборки.
    /// </summary>
    public static IServiceCollection AddCbrSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Cbr Rates Service API",
                Version = "1.0.0",
                Description =
                    $"Внутренний API. Контракт: /{OpenApiFileName}."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, true);
        });

        return services;
    }

    /// <summary>
    ///     Отдаёт <c>openapi.yaml</c> и UI Swagger.
    /// </summary>
    public static WebApplication UseCbrSwagger(this WebApplication app)
    {
        app.MapGet($"/{OpenApiFileName}", async context =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, OpenApiFileName);
            if (!File.Exists(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "application/yaml; charset=utf-8";
            await context.Response.SendFileAsync(path);
        });

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/{OpenApiFileName}", "Cbr Rates Service API v1 (OpenAPI)");
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cbr Rates Service API v1 (generated)");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Cbr Rates Service API";
        });

        return app;
    }
}
