using AppTemplate.Core.Navigation;
using AppTemplate.Core.Services;

namespace AppTemplate.Core.Tests.Fakes;

/// <summary>
/// Minimal hand-written <see cref="INavigationService"/> fake for view model tests.
/// </summary>
internal sealed class FakeNavigationService : INavigationService
{
    public bool CanGoBack { get; set; }

    public NavigationSection? CurrentSection { get; set; }

    public event EventHandler? CanGoBackChanged;

    public void Initialize()
    {
    }

    public void Navigate<TViewModel>()
    {
    }

    public void Navigate<TViewModel>(object? parameter)
    {
    }

    public bool GoBack() => CanGoBack;

    public void ClearBackStack()
    {
    }

    public void RaiseCanGoBackChanged() => CanGoBackChanged?.Invoke(this, EventArgs.Empty);
}
