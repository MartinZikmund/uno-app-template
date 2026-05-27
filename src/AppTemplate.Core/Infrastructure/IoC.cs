using System.Diagnostics.CodeAnalysis;

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
    /// via <see cref="SetProvider"/> for each test. Because <see cref="SetProvider"/> rejects
    /// <see langword="null"/>, calling <see cref="Reset"/> is the supported way to clear the provider.
    /// </remarks>
    public static void Reset() => _serviceProvider = null;

    public static T? GetService<T>() where T : class
    {
        EnsureServiceProvider();
        return (T?)_serviceProvider.GetService(typeof(T));
    }

    public static T GetRequiredService<T>() where T : class
    {
        EnsureServiceProvider();
        return (T?)_serviceProvider.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    [MemberNotNull(nameof(_serviceProvider))]
    private static void EnsureServiceProvider()
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service provider was not yet initialized. Call IoC.SetProvider() first.");
        }
    }
}
