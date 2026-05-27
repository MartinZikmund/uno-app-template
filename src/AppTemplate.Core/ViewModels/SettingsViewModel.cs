using AppTemplate.Core.Infrastructure;
using AppTemplate.Services.Settings;
using AppTemplate.Services.Theming;
using MZikmund.Toolkit.WinUI.Services;

namespace AppTemplate.Core.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IStringLocalizer _localizer;
    private readonly IAppPreferences _appPreferences;
    private readonly IThemeManager _themeManager;
    private readonly IPreferences _preferences;
    private readonly IApplication _application;
    private bool _isInitializing;

    public SettingsViewModel(
        IStringLocalizer localizer,
        IAppPreferences appPreferences,
        IThemeManager themeManager,
        IPreferences preferences,
        IApplication application)
    {
        _localizer = localizer;
        _appPreferences = appPreferences;
        _themeManager = themeManager;
        _preferences = preferences;
        _application = application;
        PageTitle = _localizer["Settings"];
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        try
        {
            _isInitializing = true;
            Theme = _appPreferences.Theme;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public ElementTheme[] ThemeOptions { get; } = [ElementTheme.Default, ElementTheme.Light, ElementTheme.Dark];

    [ObservableProperty]
    public partial ElementTheme Theme { get; set; }

    partial void OnThemeChanged(ElementTheme value)
    {
        if (_isInitializing)
        {
            return;
        }

        _themeManager.SetTheme(value);
        _appPreferences.Theme = value;
    }

    public string AppVersion => _application.AppVersion;

    public bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    [RelayCommand]
    private void ClearPreferences()
    {
        _preferences.Clear();
    }

    // Sample usage of ColorPickerDialog (see AppTemplate.Dialogs.ColorPickerDialog).
    // Inject IDialogService into the constructor, then show the dialog from a command:
    //
    //     [RelayCommand]
    //     private async Task PickColor()
    //     {
    //         var dialog = new ColorPickerDialog(Colors.SteelBlue);
    //         if (await _dialogService.ShowAsync(dialog) == ContentDialogResult.Primary)
    //         {
    //             var chosenColor = dialog.SelectedColor;
    //             // Apply the chosen color...
    //         }
    //     }
}
