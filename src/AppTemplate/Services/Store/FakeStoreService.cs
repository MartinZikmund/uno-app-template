#if DEBUG
namespace AppTemplate.Services.Store;

/// <summary>
/// In-memory <see cref="IStoreService"/> used in development builds so the GetPro page can be
/// exercised without a real store backend. Purchases are simulated and reset on every launch.
/// </summary>
/// <remarks>
/// Registered under <c>#if DEBUG</c> in <c>App.xaml.cs</c>. Add a real, platform-specific
/// implementation for release builds.
/// </remarks>
public sealed class FakeStoreService : IStoreService
{
    private static readonly TimeSpan SimulatedDelay = TimeSpan.FromSeconds(1);

    private bool _isPurchased;

    public async Task<string?> GetPriceAsync()
    {
        await Task.Delay(SimulatedDelay);
        return "$4.99";
    }

    public async Task<bool> HasProAsync()
    {
        await Task.Delay(SimulatedDelay);
        return _isPurchased;
    }

    public async Task<bool> TryPurchaseProAsync()
    {
        await Task.Delay(SimulatedDelay);
        _isPurchased = true;
        return _isPurchased;
    }

    public async Task<bool> TryRestorePurchasesAsync()
    {
        await Task.Delay(SimulatedDelay);
        return _isPurchased;
    }
}
#endif
