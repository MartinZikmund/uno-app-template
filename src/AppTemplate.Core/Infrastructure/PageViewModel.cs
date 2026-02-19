using CommunityToolkit.Mvvm.ComponentModel;

namespace AppTemplate.Core.Infrastructure;

public abstract class PageViewModel : ObservableObject
{
    public virtual void ViewNavigatedTo(object? parameter)
    {
    }

    public virtual void ViewLoaded()
    {
    }

    public virtual void ViewUnloaded()
    {
    }
}
