using CbrRatesService.Models;

namespace CbrRatesService.Infrastructure.Abstractions.Integrations;

public interface IRatesReceiver
{
    Task<CurrencyRate?> GetRatesAsync(DateOnly? date, CancellationToken cancellationToken);
}