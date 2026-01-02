using System.Diagnostics.CodeAnalysis;
using AppTemplate.Core.Services;
using Uno.Disposables;

#if HAS_UNO
using Windows.UI.Core;
#endif

namespace AppTemplate.Services;

public class NavigationService : INavigationService
{
    private readonly Dictionary<Type, Type> _viewModelToViewMapping = new();
    private readonly SerialDisposable _canGoBackDisposable = new();

    private Frame? _frame;

    public NavigationService()
    {
    }

    public bool CanGoBack
    {
        get
        {
            EnsureInitialized();

            return _frame.CanGoBack;
        }
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
        _frame.Navigated += OnNavigated;
    }

    public bool GoBack()
    {
        EnsureInitialized();

        if (_frame.CanGoBack)
        {
            _frame.GoBack();
            return true;
        }

        return false;
    }

    public void Navigate<TViewModel>() => Navigate<TViewModel>(null);

    public void Navigate<TViewModel>(object? parameter)
    {
        EnsureInitialized();

        if (!TryFindViewForViewModel(typeof(TViewModel), out var viewType))
        {
            throw new InvalidOperationException($"ViewModel type {typeof(TViewModel).Name} is not registered for navigation.");
        }

        _frame.Navigate(viewType, parameter);
    }

    public void ClearBackStack()
    {
        EnsureInitialized();

        _frame.BackStack.Clear();
    }

    private bool TryFindViewForViewModel(Type viewModelType, out Type? viewType)
    {
        EnsureInitialized();

        return _viewModelToViewMapping.TryGetValue(viewModelType, out viewType);
    }

    [MemberNotNull(nameof(_frame))]
    private void EnsureInitialized()
    {
        if (_frame is null)
        {
            throw new InvalidOperationException("NavigationService is not initialized. Call Initialize(Frame) before using navigation methods.");
        }
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        _canGoBackDisposable.Disposable = null;
        if (CanGoBack)
        {

#if HAS_UNO
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnNavigationManagerBackRequested;
            _canGoBackDisposable.Disposable = Disposable.Create(() =>
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested -= OnNavigationManagerBackRequested;
            });
#endif
        }
    }

#if HAS_UNO
    private void OnNavigationManagerBackRequested(object? sender, BackRequestedEventArgs e)
    {
        if (GoBack())
        {
            e.Handled = true;
        }
    }
#endif
}
