using System.Text.Json;
using CbrRatesService.Infrastructure.Abstractions.Cache.Settings;
using CbrRatesService.Infrastructure.Redis;
using CbrRatesService.Infrastructure.Redis.Settings;
using CbrRatesService.Infrastructure.Tests.Helpers;
using CbrRatesService.Models;
using Moq;
using StackExchange.Redis;

namespace CbrRatesService.Infrastructure.Tests.Redis;

public sealed class RedisCacheTests
{
    private readonly Mock<IConnectionMultiplexer> _connection = new();
    private readonly Mock<IDatabase> _database = new();

    public RedisCacheTests()
    {
        _connection
            .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_database.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyMissing_ReturnsNull()
    {
        _database
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var sut = CreateSut();

        var result = await sut.GetAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedValue()
    {
        var rates = CurrencyRateFactory.Create("EUR", "100,50");
        var json = JsonSerializer.Serialize(rates);

        _database
            .Setup(d => d.StringGetAsync((RedisKey)"rates", It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        var sut = CreateSut();

        var result = await sut.GetAsync("rates");

        Assert.NotNull(result);
        Assert.Equal(rates.Date, result.Date);
        Assert.Equal("EUR", result.Currencies[0].CharCode);
    }

    [Fact]
    public async Task SetAsync_WritesSerializedValue()
    {
        var rates = CurrencyRateFactory.Create();
        RedisValue written = default;

        _database
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) =>
                written = value)
            .ReturnsAsync(true);

        var sut = CreateSut();

        await sut.SetAsync("rates", rates, TimeSpan.FromMinutes(5));

        Assert.False(written.IsNullOrEmpty);
        var stored = JsonSerializer.Deserialize<CurrencyRate>((string)written!);
        Assert.NotNull(stored);
        Assert.Equal(rates.Currencies[0].CharCode, stored.Currencies[0].CharCode);

        _database.Verify(d => d.StringSetAsync(
            (RedisKey)"rates",
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    private RedisCache<string, CurrencyRate> CreateSut()
    {
        return new RedisCache<string, CurrencyRate>(
            _connection.Object,
            new TestOptionsMonitor<CacheSettings<RedisCacheSettings>>(new CacheSettings<RedisCacheSettings>
            {
                Enabled = true,
                DefaultExpiration = TimeSpan.FromMinutes(5),
                EngineSettings = new RedisCacheSettings()
            }));
    }
}