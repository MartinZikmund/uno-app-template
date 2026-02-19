using AppTemplate.Core.Navigation;
using AppTemplate.ViewModels;

namespace AppTemplate.Views;

[NavigationInfo(NavigationSection.Settings)]
public partial class SettingsViewBase : ViewBase<SettingsViewModel> { }

public sealed partial class SettingsView : SettingsViewBase
{
	public SettingsView()
	{
		this.InitializeComponent();
	}
}
