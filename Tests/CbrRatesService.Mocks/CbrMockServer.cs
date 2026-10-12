using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CbrRatesService.Mocks;

/// <summary>
///     Мок-сервер для ЦБ РФ
/// </summary>
public sealed class CbrMockServer : IDisposable
{
    public const string DailyRatesPath = "/scripts/XML_daily.asp";

    private readonly WireMockServer _server;

    public CbrMockServer()
    {
        _server = WireMockServer.Start();
        SetupSuccess();
    }

    public string Url => _server.Urls[0];

    public string DailyRatesUrl => $"{Url.TrimEnd('/')}{DailyRatesPath}";

    public void Dispose()
    {
        _server.Stop();
    }

    public void SetupSuccess()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "Responses", "XML_daily.xml");

        _server.ResetMappings();
        _server
            .Given(Request.Create()
                .WithPath(DailyRatesPath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/xml; charset=utf-8")
                .WithBodyFromFile(xmlPath));
    }

    public void SetupStatusCode(HttpStatusCode statusCode)
    {
        _server.ResetMappings();
        _server
            .Given(Request.Create()
                .WithPath(DailyRatesPath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode));
    }
}