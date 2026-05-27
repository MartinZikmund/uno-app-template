#if WINDOWS
namespace AppTemplate.Services.Store;

/// <summary>
/// Placeholder <see cref="IStoreService"/> for the Windows (WinAppSDK) target.
/// </summary>
/// <remarks>
/// Compiled only for the Windows head (<c>#if WINDOWS</c>). This is intentionally a stub: a real
/// Windows implementation would back onto the Microsoft Store via
/// <c>Windows.Services.Store.StoreContext</c>. It exists so the Windows build resolves
/// <see cref="IStoreService"/> through DI. Replace with a concrete Store-backed implementation
/// when Windows monetization is wired up.
/// </remarks>
public sealed class WindowsStoreService : IStoreService
{
    /// <inheritdoc />
    public Task<bool> TryPurchaseProAsync() => Task.FromResult(false);

    /// <inheritdoc />
    public Task<bool> TryRestorePurchasesAsync() => Task.FromResult(false);

    /// <inheritdoc />
    public Task<string?> GetPriceAsync() => Task.FromResult<string?>(null);
}
#endif
