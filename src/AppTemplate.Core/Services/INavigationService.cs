using System.Reflection;

namespace AppTemplate.Core.Services;

public interface INavigationService
{
    event EventHandler? CanGoBackChanged;

    bool CanGoBack { get; }

    void Initialize(object frame);

    void RegisterViewsFromAssembly(Assembly assembly);

    void ClearBackStack();

    void Navigate<TViewModel>();

    void Navigate<TViewModel>(object? parameter);

    bool GoBack();
}
