using AppTemplate.Core.Infrastructure;
using AppTemplate.Shell;
using Microsoft.UI.Xaml.Navigation;

namespace AppTemplate.Views;

public abstract class PageBase<TViewModel> : Page where TViewModel : PageViewModel
{
    private TViewModel? _viewModel;

    protected PageBase()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public TViewModel ViewModel => _viewModel ??= ResolveViewModel();

    private TViewModel ResolveViewModel()
    {
        if (this.FindAncestor<IWindowShell>() is { } shell)
        {
            return shell.ServiceProvider.GetRequiredService<TViewModel>();
        }

        throw new InvalidOperationException("PageBase must be hosted within an IWindowShell.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.ViewNavigatedTo(e.Parameter);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ViewLoaded();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ViewUnloaded();
    }
}

internal static class VisualTreeExtensions
{
    public static T? FindAncestor<T>(this DependencyObject element) where T : class
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent is not null)
        {
            if (parent is T typedParent)
            {
                return typedParent;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
