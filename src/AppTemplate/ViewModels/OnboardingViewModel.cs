using AppTemplate.Core.Services;
using AppTemplate.Core.ViewModels;
using AppTemplate.Services.Settings;

namespace AppTemplate.ViewModels;

public partial class OnboardingViewModel : ViewModelBase
{
    private readonly IStringLocalizer _localizer;
    private readonly IAppPreferences _appPreferences;
    private readonly INavigationService _navigationService;

    public OnboardingViewModel(
        IStringLocalizer localizer,
        IAppPreferences appPreferences,
        INavigationService navigationService)
    {
        _localizer = localizer;
        _appPreferences = appPreferences;
        _navigationService = navigationService;
        PageTitle = _localizer["OnboardingTitle"];
    }

    /// <summary>
    /// Number of slides in the onboarding flow. Update this when adding or
    /// removing slides in <c>OnboardingView.xaml</c>.
    /// </summary>
    public int PageCount { get; } = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOnLastPage))]
    [NotifyPropertyChangedFor(nameof(IsNotOnLastPage))]
    public partial int CurrentPageIndex { get; set; }

    public bool IsOnLastPage => CurrentPageIndex >= PageCount - 1;

    public bool IsNotOnLastPage => !IsOnLastPage;

    [RelayCommand]
    private void Next()
    {
        if (CurrentPageIndex < PageCount - 1)
        {
            CurrentPageIndex++;
        }
    }

    [RelayCommand]
    private void Skip() => Complete();

    [RelayCommand]
    private void GetStarted() => Complete();

    private void Complete()
    {
        _appPreferences.HasSeenOnboarding = true;
        _navigationService.Navigate<MainViewModel>();
        _navigationService.ClearBackStack();
    }
}
