# Logging & crash handling

You want failures to leave a trail on the device, and you want users to be able to hand you that
trail with a bug report. This page covers where the log goes, what gets caught automatically, and
how to show an error that points at the log.

## Where the log lives

The app writes a rolling file log with [Serilog](https://serilog.net/) (the `LoggingSerilog` Uno
feature in `src/Directory.Build.props`):

| Head | Folder |
| --- | --- |
| Windows (packaged) | `%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalState\Logs` |
| Desktop (Skia) | `ApplicationData.Current.LocalFolder` + `Logs` — on Windows `%LOCALAPPDATA%\<Publisher>\<ApplicationId>\LocalState\Logs` |
| Android / iOS / WebAssembly | the app's private local folder + `Logs` (not reachable by users) |

The file is `app.log`. It rolls on **size, not date** (5 MB per file, last 5 files kept), so the
name never carries a timestamp. Once `app.log` fills up, Serilog continues in `app_001.log`,
`app_002.log`, … — the highest number is the newest. Constants live in
`AppTemplate.Core/Services/Logging/LogPaths.cs`.

## How it's wired

`App`'s constructor builds the Serilog logger **before** the host exists, because the global
exception handlers are registered right there and can fire before `OnLaunched` has built anything.
The host then receives that same logger (`ConfigureLogging` → `AddSerilog(_fileLogger)`), so there's
a single handle on the file and everything that goes through `ILogger<T>` lands in it, filtered by
the host's minimum level (Information in Development, Warning otherwise). Uno's own internal
logging goes there too.

Global handlers (all in `App.RegisterGlobalExceptionHandlers`), none of which mark the exception
handled — the point is diagnosis, not hiding faults:

- `Application.UnhandledException` — UI-thread exceptions.
- `AppDomain.CurrentDomain.UnhandledException` — anything that's about to take the process down.
- `TaskScheduler.UnobservedTaskException` — faulted tasks nobody awaited.
- `AndroidEnvironment.UnhandledExceptionRaiser` — Android only.

> On Uno's Skia heads, an exception thrown in a dispatcher callback (for example after an `await` in
> an `async void` handler) is logged by Uno's dispatcher instead of raising
> `Application.UnhandledException` ([unoplatform/uno#6569](https://github.com/unoplatform/uno/issues/6569)).
> It still reaches the log file, but it's one more reason every `async void` handler should catch
> and log — see [`.claude/rules/error-handling.md`](../.claude/rules/error-handling.md).

## Showing an error to the user

Inject `IErrorDialogService` (defined in Core, so view models can use it):

```csharp
using AppTemplate.Services.Dialogs;

try
{
    await SaveAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Saving failed");
    await _errorDialog.ShowAsync(_localizer["SaveFailedTitle"], _localizer["SaveFailedMessage"], ex);
}
```

(`SaveAsync` and the two string keys are placeholders — add your own.) The dialog shows your
message, then the exception message, then — on Windows, macOS and Linux — where the logs are, with
an **Open logs folder** button. Composition lives in `ErrorMessage.Compose` (Core, unit-tested).

## Opening the logs folder

`IFolderLauncher` (Core) opens a folder in the system file manager. `IsSupported` is `false` on
Android, iOS and WebAssembly, where app data is private; gate UI on it. The Windows head uses
`Launcher.LaunchFolderPathAsync`; Skia desktop shells out to the OS (`explorer` / `open` /
`xdg-open`). The log folder path comes from `ILogFolderProvider`.

Settings → About has an **Open logs folder** card that does exactly this; it's hidden where the
launcher isn't supported.
