using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.MemoryCache;
using CbrRatesService.Infrastructure.MemoryCache.Settings;
using CbrRatesService.Infrastructure.Tests.Helpers;
using CbrRatesService.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CbrRatesService.Infrastructure.Tests.MemoryCache;

public sealed class MemoryCacheTests : IDisposable
{
    private readonly IMemoryCache _memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
        new MemoryCacheOptions());

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task GetAsync_WhenKeyMissing_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        var sut = CreateSut();
        var rates = CurrencyRateFactory.Create();

        await sut.SetAsync("rates", rates);
        var result = await sut.GetAsync("rates");

        Assert.NotNull(result);
        Assert.Equal(rates.Date, result.Date);
        Assert.Equal(rates.Currencies[0].CharCode, result.Currencies[0].CharCode);
        Assert.Equal(rates.Currencies[0].Value, result.Currencies[0].Value);
    }

    [Fact]
    public async Task GetAsync_AfterExpiration_ReturnsNull()
    {
        var sut = CreateSut();
        var rates = CurrencyRateFactory.Create();

        await sut.SetAsync("rates", rates, TimeSpan.FromMilliseconds(50));
        await Task.Delay(100);

        var result = await sut.GetAsync("rates");

        Assert.Null(result);
    }

    private MemoryCache<string, CurrencyRate> CreateSut()
    {
        return new MemoryCache<string, CurrencyRate>(
            _memoryCache,
            new TestOptionsMonitor<CacheSettings<MemoryCacheSettings>>(new CacheSettings<MemoryCacheSettings>
            {
                Enabled = true,
                DefaultExpiration = TimeSpan.FromMinutes(5),
                EngineSettings = new MemoryCacheSettings()
            }));
    }
}