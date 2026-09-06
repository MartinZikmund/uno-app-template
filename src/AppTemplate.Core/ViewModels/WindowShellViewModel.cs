using AppTemplate.Core.Infrastructure;
using AppTemplate.Core.Services;

namespace AppTemplate.Core.ViewModels;

public partial class WindowShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IStringLocalizer _localizer;
    private readonly IApplication _application;

    public WindowShellViewModel(
        INavigationService navigationService,
        IStringLocalizer localizer,
        IApplication application)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    /// <summary>
    /// App name for the title bar, carrying the worktree when this build came from one. The window
    /// chrome is a custom control (<c>ExtendsContentIntoTitleBar</c>), so this — not the OS window
    /// title — is what you actually read when two worktree builds are open side by side.
    /// </summary>
    public string AppTitle =>
        _application.WorktreeName is { Length: > 0 } worktree
            ? $"{_localizer["ApplicationName"].Value} ({worktree})"
            : _localizer["ApplicationName"].Value;

    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string LoadingStatusMessage { get; set; } = "";

    public bool CanGoBack => _navigationService.CanGoBack;

    public void NotifyCanGoBackChanged() => OnPropertyChanged(nameof(CanGoBack));

    [RelayCommand]
    public void GoBack()
    {
        _navigationService.GoBack();
        NotifyCanGoBackChanged();
    }
}
