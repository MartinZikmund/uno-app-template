using AppTemplate.ViewModels;

namespace AppTemplate.Views;

public abstract class SettingsViewBase : PageBase<SettingsViewModel>
{
}

public sealed partial class SettingsView : SettingsViewBase
{
    public SettingsView()
    {
        InitializeComponent();
    }
}
