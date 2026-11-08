# App Template

A cross-platform [Uno Platform](https://platform.uno/) app, created from the
[Uno App template](https://github.com/MartinZikmund/uno-app-template).

## Getting started

```bash
<!--#if (xamlStyler)-->
dotnet tool restore                                  # XAML Styler, once per clone
<!--#endif-->
dotnet build src/AppTemplate/AppTemplate.csproj      # add -f <tfm> to build one platform
<!--#if (tests)-->
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj
<!--#endif-->
```

Target frameworks, prerequisites and run commands are in [docs/building.md](./docs/building.md).
The rest of the documentation starts at [docs/README.md](./docs/README.md).

## Before your first release

1. In `src/AppTemplate/AppTemplate.csproj`, check `ApplicationPublisher`, `ApplicationTitle` and
   `ApplicationId` for both the `Prod` and `Dev` channels. They must differ between channels, which
   is what lets a Dev build install next to the released app.
2. Drop your own artwork into `src/AppTemplate/Assets/Icons` and `src/AppTemplate/Assets/Splash`.
3. Set the first version in `version.json`.
4. Choose a license for your app. `docs/TEMPLATE-LICENSE.txt` is the template's own MIT notice, kept
   because this code started from it. It doesn't license your app.

## Staying up to date

`.template-state.json` records the template version and features this app was created with. Keep it
committed: it's how you pick up later template improvements. See
[docs/template-upgrades.md](./docs/template-upgrades.md).
