# Views

## `IViewBase`

`ViewBase<TViewModel>` is the base class for views (pages). It resolves the view model from the
hosting `WindowShell` service provider and forwards lifecycle events to it.

`IViewBase` is a non-generic interface that exposes the resolved view model as `object?`:

```csharp
public interface IViewBase
{
    object? ViewModel { get; }
}
```

Every `ViewBase<TViewModel>` implements `IViewBase`, so a view can be referenced through the
interface without depending on its concrete view model type.

Use it when:

- A `DataTemplate` (or other loosely-typed code) needs to reach a view's view model without
  knowing its generic argument.
- A test needs to inspect a view's resolved view model without knowing its concrete type at
  compile time.

## Adding a view

XAML can't reference a generic base, so each view uses a non-generic intermediate base that closes
the generic:

```csharp
public partial class SettingsViewBase : ViewBase<SettingsViewModel> { }

[NavigationInfo(NavigationSection.Settings)]
public sealed partial class SettingsView : SettingsViewBase { }
```

The XAML root element is the intermediate base, not the view itself:

```xml
<local:SettingsViewBase x:Class="AppTemplate.Views.SettingsView" ...>
```

Register the pair in `App.RegisterServices`:

```csharp
service.RegisterView(typeof(Views.SettingsView), typeof(SettingsViewModel));
services.AddTransient<SettingsViewModel>();
```

Never `new` a view model or assign `DataContext` by hand — `ViewBase<TViewModel>` resolves it from
the per-window scope. The full recipe, including where the view model belongs, is in
[`.claude/rules/architecture.md`](../.claude/rules/architecture.md).
