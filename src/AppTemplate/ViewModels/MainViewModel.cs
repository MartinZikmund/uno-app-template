using AppTemplate.Core.Infrastructure;
using CommunityToolkit.Mvvm.Input;

namespace AppTemplate.ViewModels;

public partial class MainViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _navigationService.Navigate<SettingsViewModel>();
    }
}
