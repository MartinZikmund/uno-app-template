using AppTemplate.Core.Infrastructure;
using Microsoft.UI.Xaml;

namespace AppTemplate.Core.Tests.Fakes;

/// <summary>Hand-written <see cref="IApplication"/> stand-in; no DI container or UI head needed.</summary>
internal sealed class FakeApplication : IApplication
{
    public ApplicationTheme RequestedTheme { get; set; } = ApplicationTheme.Light;

    /// <summary>
    /// Not available under test. Uno's <c>net10.0</c> build is a reference assembly, so
    /// constructing a <see cref="ResourceDictionary"/> throws <see cref="NotSupportedException"/>
    /// ("Ref assembly"). Kept as a throwing property rather than an eager field so that view
    /// models which never touch it stay testable.
    /// </summary>
    public ResourceDictionary Resources =>
        throw new NotSupportedException(
            "FakeApplication does not provide Resources: Uno's net10.0 reference assembly cannot instantiate ResourceDictionary.");

    public string AppVersion { get; set; } = "1.0.0";

    public string? WorktreeName { get; set; }

    public bool ExitCalled { get; private set; }

    public void Exit() => ExitCalled = true;
}
