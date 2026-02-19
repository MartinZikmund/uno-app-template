using AppTemplate.ViewModels;

namespace AppTemplate.Views;

public abstract class MainViewBase : PageBase<MainViewModel>
{
}

public sealed partial class MainView : MainViewBase
{
    public MainView()
    {
        InitializeComponent();
    }
}
