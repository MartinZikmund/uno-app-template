using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppTemplate.Shell;

public partial class WindowShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _title = "App Template";

    [ObservableProperty]
    private bool _canGoBack;

    public WindowShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.CanGoBackChanged += OnCanGoBackChanged;
        UpdateCanGoBack();
    }

    private void OnCanGoBackChanged(object? sender, EventArgs e)
    {
        UpdateCanGoBack();
    }

    private void UpdateCanGoBack()
    {
        CanGoBack = _navigationService.CanGoBack;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
