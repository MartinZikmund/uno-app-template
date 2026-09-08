using AppTemplate.Services.Theming;
using Microsoft.UI.Xaml;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeThemeManager : IThemeManager
{
    public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;

    public ApplicationTheme ActualTheme => ApplicationTheme.Light;

    public bool IsDisposed { get; private set; }

    public void SetTheme(ElementTheme theme) => CurrentTheme = theme;

    public void Dispose() => IsDisposed = true;
}
