using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AppTemplate.Core.Services;
using Uno.Disposables;

#if HAS_UNO
using Windows.UI.Core;
#endif

namespace AppTemplate.Services;

public class NavigationService : INavigationService
{
    private readonly Dictionary<Type, Type> _viewModelToViewMapping = new();
    private readonly SerialDisposable _backRequestedSubscription = new();

    private Frame? _frame;
    private bool _lastCanGoBack;
    private bool _isSubscribedToBackRequested;

    public NavigationService()
    {
    }

    public event EventHandler? CanGoBackChanged;

    public bool CanGoBack
    {
        get
        {
            if (_frame is null)
            {
                return false;
            }

            return _frame.CanGoBack;
        }
    }

    public void Initialize(object frame)
    {
        if (frame is not Frame typedFrame)
        {
            throw new ArgumentException("Frame must be of type Microsoft.UI.Xaml.Controls.Frame", nameof(frame));
        }

        _frame = typedFrame;
        _frame.Navigated += OnNavigated;
    }

    public void RegisterViewsFromAssembly(Assembly assembly)
    {
        var viewTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("View") && !t.IsAbstract && t.IsClass);

        foreach (var viewType in viewTypes)
        {
            var viewModelTypeName = viewType.FullName?.Replace("Views.", "ViewModels.").Replace("View", "ViewModel");
            if (viewModelTypeName is null)
            {
                continue;
            }

            var viewModelType = assembly.GetType(viewModelTypeName);
            if (viewModelType is not null)
            {
                _viewModelToViewMapping[viewModelType] = viewType;
            }
        }
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
        UpdateBackRequestedSubscription();
        RaiseCanGoBackChangedIfNeeded();
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
        UpdateBackRequestedSubscription();
        RaiseCanGoBackChangedIfNeeded();
    }

    /// <summary>
    /// Updates the BackRequested subscription based on current navigation state.
    /// For Android 16+, the subscription state determines whether the app handles back navigation,
    /// not the Handled property. Subscribe when we can go back, unsubscribe when we can't.
    /// </summary>
    private void UpdateBackRequestedSubscription()
    {
#if HAS_UNO
        var shouldSubscribe = _frame?.CanGoBack ?? false;

        if (shouldSubscribe && !_isSubscribedToBackRequested)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            _isSubscribedToBackRequested = true;
        }
        else if (!shouldSubscribe && _isSubscribedToBackRequested)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
            _isSubscribedToBackRequested = false;
        }
#endif
    }

    private void RaiseCanGoBackChangedIfNeeded()
    {
        var currentCanGoBack = _frame?.CanGoBack ?? false;
        if (currentCanGoBack != _lastCanGoBack)
        {
            _lastCanGoBack = currentCanGoBack;
            CanGoBackChanged?.Invoke(this, EventArgs.Empty);
        }
    }

#if HAS_UNO
    private void OnBackRequested(object? sender, BackRequestedEventArgs e)
    {
        // On Android 16+, the fact that we're subscribed indicates we want to handle back.
        // We perform the navigation directly - subscription state controls behavior.
        GoBack();
    }
#endif
}
