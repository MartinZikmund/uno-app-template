using Microsoft.UI.Xaml;

namespace AppTemplate.Services.Theming;

public interface IThemeManager
{
    ElementTheme CurrentTheme { get; }

    ElementTheme ActualTheme { get; }

    void SetTheme(ElementTheme theme);
}
