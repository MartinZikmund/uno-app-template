namespace AppTemplate.Services.Store;

/// <summary>
/// Fallback <see cref="IStoreService"/> registered for non-DEBUG builds until a real,
/// platform-specific store implementation is added. All operations no-op and log a warning
/// rather than crash, keeping the GetPro page in a graceful "unavailable" state.
/// </summary>
/// <remarks>
/// Registered outside <c>#if DEBUG</c> in <c>App.xaml.cs</c> so <c>GetProViewModel</c> always
/// has an <see cref="IStoreService"/> to resolve, satisfying <c>ValidateOnBuild</c>.
/// </remarks>
public sealed class UnsupportedStoreService : IStoreService
{
    private readonly ILogger<UnsupportedStoreService> _logger;

    public UnsupportedStoreService(ILogger<UnsupportedStoreService> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetPriceAsync()
    {
        _logger.LogWarning("Store purchases are not supported on this build; no price is available.");
        return Task.FromResult<string?>(null);
    }

    public Task<bool> HasProAsync()
    {
        _logger.LogWarning("Store purchases are not supported on this build; Pro is unavailable.");
        return Task.FromResult(false);
    }

    public Task<bool> TryPurchaseProAsync()
    {
        _logger.LogWarning("Store purchases are not supported on this build; purchase cannot be completed.");
        return Task.FromResult(false);
    }

    public Task<bool> TryRestorePurchasesAsync()
    {
        _logger.LogWarning("Store purchases are not supported on this build; nothing to restore.");
        return Task.FromResult(false);
    }
}
