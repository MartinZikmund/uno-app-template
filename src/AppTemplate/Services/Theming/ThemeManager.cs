using AppTemplate.Shell;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace AppTemplate.Services.Theming;

public class ThemeManager : IThemeManager
{
    private readonly IWindowShellProvider _windowShellProvider;
    private readonly UISettings _uiSettings = new();

    private FrameworkElement? _rootElement;

    public ThemeManager(IWindowShellProvider windowShellProvider)
    {
        _windowShellProvider = windowShellProvider;
        _uiSettings.ColorValuesChanged += OnSystemColorValuesChanged;
    }

    public ElementTheme CurrentTheme => _rootElement?.RequestedTheme ?? ElementTheme.Default;

    public ElementTheme ActualTheme => _rootElement?.ActualTheme ?? ElementTheme.Light;

    public void SetTheme(ElementTheme theme)
    {
        _rootElement = _windowShellProvider.Shell as FrameworkElement;
        if (_rootElement is null)
        {
            return;
        }

        _rootElement.RequestedTheme = theme;
        UpdateTitleBarColors();
    }

    private void OnSystemColorValuesChanged(UISettings sender, object args)
    {
        _windowShellProvider.Shell?.DispatcherQueue.TryEnqueue(UpdateTitleBarColors);
    }

    private void UpdateTitleBarColors()
    {
#if WINDOWS && !HAS_UNO
        var window = _windowShellProvider.Window;
        if (window?.AppWindow?.TitleBar is { } titleBar)
        {
            var isDark = ActualTheme == ElementTheme.Dark;
            titleBar.ForegroundColor = isDark ? Colors.White : Colors.Black;
            titleBar.ButtonForegroundColor = isDark ? Colors.White : Colors.Black;
            titleBar.ButtonHoverForegroundColor = isDark ? Colors.White : Colors.Black;
            titleBar.ButtonHoverBackgroundColor = isDark ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x33, 0x00, 0x00, 0x00);
        }
#endif
    }
}
