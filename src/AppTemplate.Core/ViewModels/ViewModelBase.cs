namespace AppTemplate.Core.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether the view model is performing its
    /// initial data load. This represents the busy state triggered when the view is
    /// entered/navigated to (for example, fetching the data needed to render the page).
    /// Use this to drive a full-page loading indicator while the view is being populated.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the view model is busy running a
    /// user-initiated action or command on an already-loaded view (for example,
    /// saving changes or submitting a form). Use this to drive an inline busy
    /// indicator and to disable command-bound controls while the action runs.
    /// </summary>
    [ObservableProperty]
    public partial bool IsWorking { get; set; }

    [ObservableProperty]
    public partial string? PageTitle { get; set; }

    public virtual void ViewCreated() { }

    public virtual void ViewLoading() { }

    public virtual void ViewLoaded() { }

    public virtual void ViewUnloaded() { }

    public virtual void OnNavigatedTo(object? parameter) { }

    public virtual void OnNavigatedFrom() { }
}
