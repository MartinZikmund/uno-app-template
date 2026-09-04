namespace AppTemplate.Core.Infrastructure;

public static class IoC
{
    private static IServiceProvider? _serviceProvider;

    public static void SetProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Clears the stored service provider so that subsequent <see cref="GetService{T}"/> and
    /// <see cref="GetRequiredService{T}"/> calls behave as if no provider was configured.
    /// </summary>
    /// <remarks>
    /// This is primarily intended for test isolation, allowing a fresh provider to be supplied
    /// via <see cref="SetProvider"/> for each test.
    /// </remarks>
    public static void Reset() => _serviceProvider = null;

    public static T? GetService<T>() where T : class
    {
        IServiceProvider provider = EnsureServiceProvider();
        return (T?)provider.GetService(typeof(T));
    }

    public static T GetRequiredService<T>() where T : class
    {
        IServiceProvider provider = EnsureServiceProvider();
        return (T?)provider.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    // Reads the field once into a local so a concurrent Reset() can't null it out between the
    // check and the subsequent GetService call (which would otherwise throw NullReferenceException
    // instead of the intended InvalidOperationException).
    private static IServiceProvider EnsureServiceProvider() =>
        _serviceProvider ?? throw new InvalidOperationException("Service provider was not yet initialized. Call IoC.SetProvider() first.");
}
