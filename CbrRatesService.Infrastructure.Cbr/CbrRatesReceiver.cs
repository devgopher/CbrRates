using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CbrRatesService.Infrastructure.Abstractions.Integrations;
using CbrRatesService.Infrastructure.Cbr.Settings;
using CbrRatesService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace CbrRatesService.Infrastructure.Cbr;

/// <summary>
///     Получатель курсов валют из ЦБ РФ.
/// </summary>
public sealed class CbrRatesReceiver(
    HttpClient httpClient,
    IOptionsMonitor<CbrSettings> settings,
    ILogger<CbrRatesReceiver> logger) : IRatesReceiver
{
    private const string DailyRatesPath = "/scripts/XML_daily.asp";

    private readonly XmlSerializer _serializer = new(typeof(CurrencyRate));

    static CbrRatesReceiver()
    {
        // ЦБ отдаёт XML в windows-1251; без провайдера code pages декодирование недоступно.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<CurrencyRate?> GetRatesAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var requestDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var requestUri =
            $"{DailyRatesPath}?date_req={Uri.EscapeDataString(requestDate.ToString("dd/MM/yyyy"))}";

        try
        {
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(response =>
                    response.StatusCode is HttpStatusCode.InternalServerError
                        or HttpStatusCode.ServiceUnavailable)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    settings.CurrentValue.Retry.Retries,
                    _ => settings.CurrentValue.Retry.Delay,
                    (outcome, delay, retryCount, _) =>
                    {
                        outcome.Result?.Dispose();
                        logger.LogInformation(
                            "Попытка {RetryCount} после задержки {DelaySeconds} секунд",
                            retryCount,
                            delay.TotalSeconds);
                    });

            using var response = await retryPolicy
                .ExecuteAsync(() => httpClient.GetAsync(requestUri, cancellationToken))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogError("Ресурс не найден (404) при запросе курсов ЦБ РФ");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Ошибка HTTP при запросе курсов ЦБ РФ: {StatusCode}",
                    (int)response.StatusCode);

                return null;
            }

            var bytes = await response.Content
                .ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            await using var memoryStream = new MemoryStream(bytes);
            using var xmlReader = XmlReader.Create(memoryStream);

            var result = _serializer.Deserialize(xmlReader) as CurrencyRate;
            if (result is not null)
                return result;

            logger.LogError("Не удалось десериализовать ответ ЦБ РФ");

            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Операция отменена");

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении курсов валют из ЦБ РФ: {Message}", ex.Message);

            return null;
        }
    }
}