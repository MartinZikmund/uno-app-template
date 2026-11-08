using AppTemplate.Core.Navigation;

namespace AppTemplate.Core.Services;

public interface INavigationService
{
    bool CanGoBack { get; }

    NavigationSection? CurrentSection { get; }

    /// <summary>
    /// Raised whenever the back stack may have changed (after Navigate, GoBack, or
    /// ClearBackStack), so listeners can refresh <see cref="CanGoBack"/>-derived state.
    /// </summary>
    event EventHandler? CanGoBackChanged;

    void Initialize();

    void Navigate<TViewModel>();

    void Navigate<TViewModel>(object? parameter);

    bool GoBack();

    void ClearBackStack();
}
