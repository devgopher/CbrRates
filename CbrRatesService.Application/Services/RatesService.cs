using CbrRatesService.Infrastructure.Abstractions.Integrations;
using CbrRatesService.Models;

namespace CbrRatesService.Application.Services;

/// <summary>
///     <inheritdoc cref="IRatesService"/>
/// </summary>
public sealed class RatesService(IRatesReceiver ratesReceiver) : IRatesService
{
    /// <inheritdoc />
    public async Task<CurrencyRate?> GetRatesAsync(
        DateOnly? date = null,
        string? currency = null,
        CancellationToken cancellationToken = default)
    {
        date ??= DateOnly.FromDateTime(DateTime.Today);

        var rates = await ratesReceiver.GetRatesAsync(date, cancellationToken).ConfigureAwait(false);

        if (rates is null)
            return null;

        if (string.IsNullOrWhiteSpace(currency))
            return rates;

        var match = rates.Currencies
            .FirstOrDefault(c => c.CharCode.Equals(currency, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return null;

        return new CurrencyRate
        {
            Date = rates.Date,
            Name = rates.Name,
            Currencies = [match]
        };
    }
}
