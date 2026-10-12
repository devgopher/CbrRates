using System.Net;
using CbrRatesService.Infrastructure.Cbr;
using CbrRatesService.Infrastructure.Cbr.Settings;
using CbrRatesService.Infrastructure.Tests.Helpers;
using CbrRatesService.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace CbrRatesService.Infrastructure.Tests.Cbr;

public sealed class CbrRatesReceiverTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CbrMockServer _mockServer = new();

    public CbrRatesReceiverTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_mockServer.Url.TrimEnd('/') + "/")
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _mockServer.Dispose();
    }

    [Fact]
    public async Task GetRatesAsync_WhenResponseOk_ReturnsDeserializedRates()
    {
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("28.02.2026", result.Date);
        Assert.NotEmpty(result.Currencies);
        Assert.Contains(result.Currencies, c => c.CharCode == "USD");
    }

    [Fact]
    public async Task GetRatesAsync_WhenDateProvided_ReturnsRates()
    {
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 2, 28), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("28.02.2026", result.Date);
    }

    [Fact]
    public async Task GetRatesAsync_WhenNotFound_ReturnsNull()
    {
        _mockServer.SetupStatusCode(HttpStatusCode.NotFound);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRatesAsync_WhenInternalServerError_ReturnsNull()
    {
        _mockServer.SetupStatusCode(HttpStatusCode.InternalServerError);
        var sut = CreateSut(1, TimeSpan.FromMilliseconds(1));

        var result = await sut.GetRatesAsync(null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRatesAsync_WhenServiceUnavailable_ReturnsNull()
    {
        _mockServer.SetupStatusCode(HttpStatusCode.ServiceUnavailable);
        var sut = CreateSut(1, TimeSpan.FromMilliseconds(1));

        var result = await sut.GetRatesAsync(null, CancellationToken.None);

        Assert.Null(result);
    }

    private CbrRatesReceiver CreateSut(int retries = 0, TimeSpan? delay = null)
    {
        var settings = new CbrSettings
        {
            BaseUrl = _mockServer.Url,
            Timeout = 10,
            Retry = new RetrySettings
            {
                Retries = retries,
                Delay = delay ?? TimeSpan.Zero
            }
        };

        return new CbrRatesReceiver(
            _httpClient,
            new TestOptionsMonitor<CbrSettings>(settings),
            NullLogger<CbrRatesReceiver>.Instance);
    }
}