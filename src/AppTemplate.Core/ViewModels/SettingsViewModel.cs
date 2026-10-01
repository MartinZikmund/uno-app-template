using AppTemplate.Core.Infrastructure;
using AppTemplate.Services;
using AppTemplate.Services.Dialogs;
using AppTemplate.Services.Logging;
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
    private readonly IFolderLauncher _folderLauncher;
    private readonly ILogFolderProvider _logFolder;
    private readonly IErrorDialogService _errorDialog;
    private bool _isInitializing;

    public SettingsViewModel(
        IStringLocalizer localizer,
        IAppPreferences appPreferences,
        IThemeManager themeManager,
        IPreferences preferences,
        IApplication application,
        IFolderLauncher folderLauncher,
        ILogFolderProvider logFolder,
        IErrorDialogService errorDialog)
    {
        _localizer = localizer;
        _appPreferences = appPreferences;
        _themeManager = themeManager;
        _preferences = preferences;
        _application = application;
        _folderLauncher = folderLauncher;
        _logFolder = logFolder;
        _errorDialog = errorDialog;
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

    /// <summary>
    /// Localized "Worktree: {name}" line shown under the version, or <see langword="null"/> when
    /// this build did not come from a git worktree. Composed here rather than in XAML because
    /// {markup:Localize} takes no format arguments.
    /// </summary>
    public string? WorktreeLabel =>
        _application.WorktreeName is { Length: > 0 } worktree
            ? _localizer["WorktreeFormat", worktree].Value
            : null;

    public bool CanOpenLogsFolder => _folderLauncher.IsSupported;

    [RelayCommand]
    private async Task OpenLogsFolderAsync()
    {
        if (!await _folderLauncher.OpenAsync(_logFolder.FolderPath))
        {
            await _errorDialog.ShowAsync(
                _localizer["OpenLogsFolder"],
                _localizer["OpenLogsFolderFailedFormat", _logFolder.FolderPath]);
        }
    }

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
}
