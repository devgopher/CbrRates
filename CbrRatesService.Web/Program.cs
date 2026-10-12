using CbrRatesService.Application.Extensions;
using CbrRatesService.Infrastructure.Extensions;
using CbrRatesService.Web.Extensions;
using NLog;
using NLog.Web;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();
    builder.Services.AddCbrSwagger();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
        app.UseCbrSwagger();

    app.UseHttpsRedirection();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Приложение завершилось с ошибкой");
    throw;
}
finally
{
    LogManager.Shutdown();
}