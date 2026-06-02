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
}
