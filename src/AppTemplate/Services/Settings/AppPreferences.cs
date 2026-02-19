using Microsoft.UI.Xaml;
using MZikmund.Toolkit.WinUI.Services;

namespace AppTemplate.Services.Settings;

public class AppPreferences : IAppPreferences
{
    private const string ThemeKey = "Theme";
    private const string FirstStartKey = "FirstStart";
    private const string LaunchCountKey = "LaunchCount";

    private readonly IPreferences _preferences;

    public AppPreferences(IPreferences preferences)
    {
        _preferences = preferences;
    }

    public ElementTheme Theme
    {
        get => _preferences.GetComplex<ElementTheme>(ThemeKey, ElementTheme.Default);
        set => _preferences.SetComplex(ThemeKey, value);
    }

    public bool FirstStart
    {
        get => _preferences.Get(FirstStartKey, true);
        set => _preferences.Set(FirstStartKey, value);
    }

    public int LaunchCount
    {
        get => _preferences.Get(LaunchCountKey, 0);
        set => _preferences.Set(LaunchCountKey, value);
    }
}
