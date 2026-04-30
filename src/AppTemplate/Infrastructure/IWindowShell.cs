using AppTemplate.Core.ViewModels;
using Microsoft.UI.Dispatching;

namespace AppTemplate.Infrastructure;

public interface IWindowShell
{
	WindowShellViewModel ViewModel { get; }

	XamlRoot? XamlRoot { get; }

	IServiceProvider ServiceProvider { get; }

	DispatcherQueue DispatcherQueue { get; }

	Frame RootFrame { get; }

	void SetTitleBar(UIElement? titleBar);
}
