using AppTemplate.Core.ViewModels;

namespace AppTemplate.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	private readonly IStringLocalizer _localizer;

	public MainViewModel(IStringLocalizer localizer)
	{
		_localizer = localizer;
		PageTitle = _localizer["ApplicationName"];
	}
}
