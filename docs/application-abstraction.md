# Infrastructure abstractions

A cross-platform [Uno Platform](https://platform.uno/) (WinUI) application targeting .NET 10.

## `IApplication`

`AppTemplate.Core.Infrastructure.IApplication` abstracts the application-level surface that
Core code depends on, decoupling it from the concrete `Microsoft.UI.Xaml.Application` type.

```csharp
public interface IApplication
{
    ApplicationTheme RequestedTheme { get; }

    ResourceDictionary Resources { get; }

    void Exit();
}
```

The head project's `App` class implements the interface (it already derives from
`Application`, which supplies these members), and it is registered in DI as a singleton:

```csharp
services.AddSingleton<IApplication>(sp => App.Current);
```

Core services then take a dependency on `IApplication` rather than reaching for the
`Application.Current` singleton directly.

**Why it exists / use cases:**

- **Testability** &mdash; Core logic can be exercised without a live WinUI application by
  injecting a fake/mock `IApplication`.
- **Server / headless hosting** &mdash; Core can run in environments that never instantiate a
  real `Microsoft.UI.Xaml.Application`.
- **Alternative renderers** &mdash; the same Core can be hosted by a different UI shell that
  provides its own `IApplication` implementation.
