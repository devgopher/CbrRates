using Microsoft.Extensions.Options;

namespace CbrRatesService.Infrastructure.Tests.Helpers;

internal sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
{
    public T CurrentValue { get; private set; } = currentValue;

    public T Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return null;
    }

    public void Set(T value)
    {
        CurrentValue = value;
    }
}