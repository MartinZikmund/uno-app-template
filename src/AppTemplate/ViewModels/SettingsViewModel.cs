using AppTemplate.Core.Infrastructure;
using AppTemplate.Services.Settings;
using AppTemplate.Services.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace AppTemplate.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    private readonly IAppPreferences _appPreferences;
    private readonly IThemeManager _themeManager;

    [ObservableProperty]
    private int _selectedThemeIndex;

    public SettingsViewModel(IAppPreferences appPreferences, IThemeManager themeManager)
    {
        _appPreferences = appPreferences;
        _themeManager = themeManager;
    }

    public override void ViewNavigatedTo(object? parameter)
    {
        base.ViewNavigatedTo(parameter);
        SelectedThemeIndex = (int)_appPreferences.Theme;
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = (ElementTheme)value;
        _appPreferences.Theme = theme;
        _themeManager.SetTheme(theme);
    }
}
