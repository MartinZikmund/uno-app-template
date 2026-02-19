using AppTemplate.Core.ViewModels;
using AppTemplate.Infrastructure;
using Microsoft.UI.Dispatching;

namespace AppTemplate.Services.Navigation;

public interface IWindowShellProvider
{
	Window Window { get; }

	IWindowShell Shell { get; }

	WindowShellViewModel ViewModel { get; }

	DispatcherQueue DispatcherQueue { get; }

	Frame RootFrame { get; }
}
