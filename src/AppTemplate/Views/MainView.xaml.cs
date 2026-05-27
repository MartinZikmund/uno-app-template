using AppTemplate.Core.Navigation;
using AppTemplate.Core.ViewModels;

namespace AppTemplate.Views;

public partial class MainViewBase : ViewBase<MainViewModel> { }

[NavigationInfo(NavigationSection.Main)]
public sealed partial class MainView : MainViewBase
{
    public MainView()
    {
        this.InitializeComponent();
    }

    // Sample usage of AppTemplate.Controls.InAppToastQueueControl:
    //
    // 1. Add the control as the last child of a root Grid so it overlays page content:
    //
    //    xmlns:controls="using:AppTemplate.Controls"
    //    <Grid>
    //        <!-- page content -->
    //        <controls:InAppToastQueueControl x:Name="Toasts" />
    //    </Grid>
    //
    // 2. Enqueue toasts from code-behind (they queue and show one at a time):
    //
    //    Toasts.Enqueue("Saved", "Your changes were saved.", "#2E7D32");
    //    Toasts.Enqueue("Sync completed", "All changes saved to the cloud.");
}
