using AppTemplate.Core.Navigation;
using AppTemplate.Core.Services;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeNavigationService : INavigationService
{
    public bool CanGoBack { get; set; }

    public NavigationSection? CurrentSection { get; set; }

    public bool GoBackCalled { get; private set; }

    public void Initialize()
    {
    }

    public void Navigate<TViewModel>()
    {
    }

    public void Navigate<TViewModel>(object? parameter)
    {
    }

    public bool GoBack()
    {
        GoBackCalled = true;
        return CanGoBack;
    }

    public void ClearBackStack()
    {
    }
}
