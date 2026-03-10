using AppTemplate.Core.Services;

namespace AppTemplate.Core.ViewModels;

public partial class WindowShellViewModel : ObservableObject
{
	private readonly INavigationService _navigationService;

	public WindowShellViewModel(INavigationService navigationService)
	{
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
	}

	[ObservableProperty]
	public partial string Title { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial string LoadingStatusMessage { get; set; } = "";

	public bool CanGoBack => _navigationService.CanGoBack;

	[RelayCommand]
	public void GoBack()
	{
		_navigationService.GoBack();
		OnPropertyChanged(nameof(CanGoBack));
	}
}
