using Microsoft.UI.Dispatching;

namespace AppTemplate.Shell;

public interface IWindowShell
{
    WindowShellViewModel ViewModel { get; }

    XamlRoot? XamlRoot { get; }

    IServiceProvider ServiceProvider { get; }

    DispatcherQueue DispatcherQueue { get; }

    Frame RootFrame { get; }
}
