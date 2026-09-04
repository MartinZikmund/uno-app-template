using AppTemplate.Core.Infrastructure;

namespace AppTemplate.Core.Tests.Infrastructure;

[TestClass]
[DoNotParallelize]
public class IoCTests
{
    [TestCleanup]
    public void TestCleanup() => IoC.Reset();

    [TestMethod]
    public void GetService_AfterSetProvider_ReturnsServiceFromProvider()
    {
        ISampleService expected = new SampleService("first");
        StubServiceProvider provider = new(expected);

        IoC.SetProvider(provider);

        Assert.AreSame(expected, IoC.GetService<ISampleService>());
    }

    [TestMethod]
    public void GetRequiredService_AfterSetProvider_ReturnsServiceFromProvider()
    {
        ISampleService expected = new SampleService("first");
        StubServiceProvider provider = new(expected);

        IoC.SetProvider(provider);

        Assert.AreSame(expected, IoC.GetRequiredService<ISampleService>());
    }

    [TestMethod]
    public void Reset_ClearsProvider_GetServiceThrows()
    {
        IoC.SetProvider(new StubServiceProvider(new SampleService("first")));

        IoC.Reset();

        Assert.ThrowsExactly<InvalidOperationException>(() => IoC.GetService<ISampleService>());
    }

    [TestMethod]
    public void Reset_ClearsProvider_GetRequiredServiceThrows()
    {
        IoC.SetProvider(new StubServiceProvider(new SampleService("first")));

        IoC.Reset();

        Assert.ThrowsExactly<InvalidOperationException>(() => IoC.GetRequiredService<ISampleService>());
    }

    [TestMethod]
    public void SetProvider_AfterReset_ResolvesFromNewProvider()
    {
        // Arrange: first provider resolves the "first" instance.
        ISampleService first = new SampleService("first");
        IoC.SetProvider(new StubServiceProvider(first));
        Assert.AreSame(first, IoC.GetRequiredService<ISampleService>());

        // Act: reset and swap in a different provider, mimicking per-test isolation.
        IoC.Reset();
        ISampleService second = new SampleService("second");
        IoC.SetProvider(new StubServiceProvider(second));

        // Assert: the new provider's instance is resolved, proving isolation between providers.
        ISampleService resolved = IoC.GetRequiredService<ISampleService>();
        Assert.AreSame(second, resolved);
        Assert.AreNotSame(first, resolved);
        Assert.AreEqual("second", resolved.Name);
    }

    [TestMethod]
    public void SetProvider_WithNull_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => IoC.SetProvider(null!));
    }

    [TestMethod]
    public void GetService_WithoutProvider_ThrowsInvalidOperationException()
    {
        IoC.Reset();

        Assert.ThrowsExactly<InvalidOperationException>(() => IoC.GetService<ISampleService>());
    }

    [TestMethod]
    public void GetRequiredService_WhenServiceMissing_ThrowsInvalidOperationException()
    {
        // Provider resolves nothing for the requested type.
        IoC.SetProvider(new StubServiceProvider(null));

        Assert.ThrowsExactly<InvalidOperationException>(() => IoC.GetRequiredService<ISampleService>());
    }

    private interface ISampleService
    {
        string Name { get; }
    }

    private sealed class SampleService(string name) : ISampleService
    {
        public string Name => name;
    }

    /// <summary>
    /// Minimal <see cref="IServiceProvider"/> that returns a single registered
    /// <see cref="ISampleService"/> instance (or <see langword="null"/>) without pulling in a
    /// DI container dependency.
    /// </summary>
    private sealed class StubServiceProvider(ISampleService? service) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(ISampleService) ? service : null;
    }
}
