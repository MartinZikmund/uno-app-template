using AppTemplate.Core.Navigation;
using AppTemplate.Core.ViewModels;

namespace AppTemplate.Views;

public partial class SettingsViewBase : ViewBase<SettingsViewModel> { }

[NavigationInfo(NavigationSection.Settings)]
public sealed partial class SettingsView : SettingsViewBase
{
    public SettingsView()
    {
        this.InitializeComponent();
    }

    private async void OnReportProblemClick(object sender, RoutedEventArgs e) =>
        await LaunchAsync(ViewModel.RepositoryLinks.ReportProblemUri);

    private async void OnSourceCodeClick(object sender, RoutedEventArgs e) =>
        await LaunchAsync(ViewModel.RepositoryLinks.RepositoryUri);

    private static async Task LaunchAsync(Uri? uri)
    {
        if (uri is not null)
        {
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
