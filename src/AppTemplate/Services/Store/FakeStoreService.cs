#if DEBUG
namespace AppTemplate.Services.Store;

/// <summary>
/// A fake, in-memory <see cref="IStoreService"/> used during development so the
/// <c>GetPro</c> scaffold compiles and can be exercised without a real store backend.
/// </summary>
/// <remarks>
/// Replace with the real implementation (e.g. the one from <c>MZikmund.Toolkit.WinUI</c>)
/// before shipping. Registered under <c>#if DEBUG</c> in <c>App.xaml.cs</c>.
/// </remarks>
public sealed class FakeStoreService : IStoreService
{
    private static readonly TimeSpan SimulatedDelay = TimeSpan.FromSeconds(1);

    private bool _isPurchased;

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

    public async Task<string?> GetPriceAsync()
    {
        await Task.Delay(SimulatedDelay);
        return "$4.99";
    }
}
#endif
