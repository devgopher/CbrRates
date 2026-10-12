using CbrRatesService.Application.Services;
using CbrRatesService.Application.Tests.Helpers;
using CbrRatesService.Infrastructure.Abstractions.Integrations;
using CbrRatesService.Models;
using Moq;

namespace CbrRatesService.Application.Tests.Services;

public sealed class RatesServiceTests
{
    private readonly Mock<IRatesReceiver> _ratesReceiver = new();

    [Fact]
    public async Task GetRatesAsync_WhenCharCodeOmitted_ReturnsAllCurrencies()
    {
        var rates = CurrencyRateFactory.Create();
        SetupReceiver(rates);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 10, 11));

        Assert.NotNull(result);
        Assert.Equal(2, result.Currencies.Count);
        Assert.Contains(result.Currencies, c => c.CharCode == "USD");
        Assert.Contains(result.Currencies, c => c.CharCode == "EUR");
    }

    [Fact]
    public async Task GetRatesAsync_WhenCharCodeProvided_ReturnsFilteredCurrency()
    {
        var rates = CurrencyRateFactory.Create();
        SetupReceiver(rates);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 10, 11), "EUR");

        Assert.NotNull(result);
        Assert.Single(result.Currencies);
        Assert.Equal("EUR", result.Currencies[0].CharCode);
        Assert.Equal(rates.Date, result.Date);
        Assert.Equal(rates.Name, result.Name);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("USD")]
    public async Task GetRatesAsync_WhenCharCodeCasingDiffers_FiltersCaseInsensitively(string charCode)
    {
        var rates = CurrencyRateFactory.Create();
        SetupReceiver(rates);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 10, 11), charCode);

        Assert.NotNull(result);
        Assert.Single(result.Currencies);
        Assert.Equal("USD", result.Currencies[0].CharCode);
    }

    [Fact]
    public async Task GetRatesAsync_WhenCurrencyMissing_ReturnsNull()
    {
        var rates = CurrencyRateFactory.Create();
        SetupReceiver(rates);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 10, 11), "JPY");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRatesAsync_WhenReceiverReturnsNull_ReturnsNull()
    {
        SetupReceiver(null);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 10, 11));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRatesAsync_WhenDateOmitted_PassesTodayToReceiver()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly? capturedDate = null;

        _ratesReceiver
            .Setup(r => r.GetRatesAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly?, CancellationToken>((date, _) => capturedDate = date)
            .ReturnsAsync(CurrencyRateFactory.Create());

        var sut = CreateSut();

        await sut.GetRatesAsync();

        Assert.Equal(today, capturedDate);
        _ratesReceiver.Verify(
            r => r.GetRatesAsync(today, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRatesAsync_WhenDateProvided_PassesSameDateToReceiver()
    {
        var date = new DateOnly(2026, 2, 28);
        SetupReceiver(CurrencyRateFactory.Create());
        var sut = CreateSut();

        await sut.GetRatesAsync(date);

        _ratesReceiver.Verify(
            r => r.GetRatesAsync(date, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRatesAsync_WhenWhitespaceCharCode_ReturnsAllCurrencies()
    {
        var rates = CurrencyRateFactory.Create();
        SetupReceiver(rates);
        var sut = CreateSut();

        var result = await sut.GetRatesAsync(new DateOnly(2026, 10, 11), "   ");

        Assert.NotNull(result);
        Assert.Equal(2, result.Currencies.Count);
    }

    private RatesService CreateSut() => new(_ratesReceiver.Object);

    private void SetupReceiver(CurrencyRate? rates)
    {
        _ratesReceiver
            .Setup(r => r.GetRatesAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);
    }
}
