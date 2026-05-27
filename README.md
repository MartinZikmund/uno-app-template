# App Template

A production-shaped starting point for a cross-platform [Uno Platform](https://platform.uno/) app.
Five platform heads from a single project, plain WinUI/XAML with CommunityToolkit.Mvvm, and the
plumbing you would otherwise rebuild every time: navigation, dependency injection, theming,
localization, dialogs, versioning, and release pipelines.

Copy it, rename it, delete what you don't need.

## What's in the box

| | |
|---|---|
| **Five heads, one project** | Android, iOS, Windows (WinAppSDK), Desktop (Skia), and WebAssembly from `src/AppTemplate`. See [docs/building.md](./docs/building.md). |
| **MVVM with CommunityToolkit.Mvvm** | `ObservableObject`, `[ObservableProperty]` partial properties, and `[RelayCommand]`. View models live in `AppTemplate.Core` so they unit-test without a UI head. See [docs/views.md](./docs/views.md). |
| **Type-driven navigation** | `INavigationService.Navigate<TViewModel>()`, with views registered explicitly rather than by reflection. |
| **DI with the guardrails on** | Scope validation enabled, so a captive dependency fails at startup instead of in production. Per-window scopes for window-bound services. |
| **Services already wired** | Theming, preferences, dialogs and confirmations, app rating, share, launcher, display-request, and app-update checks. |
| **Localization from the start** | `{markup:Localize Key=...}` in XAML, `IStringLocalizer` in code, English and Czech resources included. |
| **Side-by-side Dev builds** | Nerdbank.GitVersioning with Dev and Prod channels that install alongside each other, distinct icons included. See [docs/versioning.md](./docs/versioning.md). |
| **CI that packages** | Build and smoke-test workflows plus Windows, Android, iOS packaging and WebAssembly deployment. XAML formatting is enforced on every PR — see [docs/xaml-styler.md](./docs/xaml-styler.md). |
| **Written for coding agents** | [`AGENTS.md`](./AGENTS.md) and [`.claude/rules/`](./.claude/rules/) carry the conventions an agent needs before it writes a line. |

## Using this template

There is no rename script — the steps below are the whole job, and doing them by hand once is
clearer than debugging a script that half-worked.

1. **Start your repo.** Use this repository as a GitHub template, or clone it and point `origin`
   at your own remote.

2. **Rename `AppTemplate` to your app.** It appears in roughly 59 C# namespace declarations plus:

   ```text
   src/AppTemplate/                          folder + AppTemplate.csproj
   src/AppTemplate.Core/                     folder + AppTemplate.Core.csproj
   tests/AppTemplate.Core.Tests/             folder + .csproj
   src/AppTemplate.slnx
   src/.run/AppTemplate.run.xml
   src/.vscode/launch.json, tasks.json
   src/AppTemplate/Properties/launchSettings.json
   src/AppTemplate/Platforms/WebAssembly/LinkerConfig.xml
   ```

   A find-and-replace of `AppTemplate` → `YourApp` across the repo, then renaming the folders and
   project files, covers all of it.

3. **Claim your identity.** In `src/AppTemplate/AppTemplate.csproj`, set `ApplicationPublisher`,
   and set `ApplicationTitle` and `ApplicationId` for **both** the `Prod` and `Dev` channel
   property groups — they must differ, that's what lets Dev install side by side. Then update the
   display names in `src/AppTemplate/Platforms/Android/Resources/values*/Strings.xml`.

4. **Replace the artwork.** Drop your own SVGs into `src/AppTemplate/Assets/Icons` and
   `src/AppTemplate/Assets/Splash`. Keep `icon_transparent.svg` and `icon.svg` as the background
   filenames unless you also update the `UnoIcon*` properties — the generated Android
   `@mipmap/icon` resource name is derived from them.

5. **Reset the version.** `version.json` starts at `0.1`. Set it to whatever your first release
   should be; git height supplies the rest.

6. **Translate or trim.** Keep both `src/AppTemplate/Strings/en` and `.../cs`, or delete the `cs`
   folder and its Android `values-cs` counterpart if you only ship one language.

7. **Delete what you don't need.** Sample views, the Czech resources, the rating service — none of
   it is load-bearing. Removing a service means deleting its files and its registration in
   `App.RegisterServices`.

### If you're a coding agent

Read [`AGENTS.md`](./AGENTS.md) first — it points at [`.claude/rules/`](./.claude/rules/), which
carries the conventions this repo actually enforces: code style, the Core/head split, testing, git,
and documentation. Adding a feature means adding a page under [`docs/`](./docs/), never appending
prose to this file.

## Quickstart

```bash
dotnet tool restore                                                   # XAML Styler, once per clone
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop    # fastest head, no workloads
```

Other heads, per-platform prerequisites, and how to run the packaged Windows app live in
[docs/building.md](./docs/building.md).

## Docs

[`docs/`](./docs/) holds a page per topic — start at [docs/README.md](./docs/README.md).

An Uno Platform (WinUI) cross-platform app template targeting .NET 10.

## Android: scheduled notifications & boot rescheduling

Android wipes every `AlarmManager` alarm when the device reboots. Any app that posts
notifications via `AlarmManager` therefore has to re-register its pending alarms after boot,
otherwise previously-scheduled notifications silently never fire.

The template ships the receiver pair needed for this pattern under
`src/AppTemplate/Platforms/Android/`:

- **`BootReceiver`** — an exported `[BroadcastReceiver]` listening for
  `Intent.ActionBootCompleted`. On boot it re-reads the persisted scheduled notifications and
  re-registers a pending alarm for each future-dated entry.
- **`NotificationAlarmReceiver`** — a non-exported `[BroadcastReceiver]` that is the target of
  each scheduled `PendingIntent`. When the alarm fires it builds and posts the notification via
  `NotificationManager`.

The manifest already declares the required permission:

```xml
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
```

### Wiring to a scheduled-notifications service

Both receivers currently contain placeholder logic marked with `TODO` comments. To make them
functional, provide a scheduled-notifications service (not shipped in this template) and wire it
up as follows:

1. **Persist scheduled notifications.** When the app schedules a notification, store its id,
   title, message and trigger time somewhere durable (SQLite via `sqlite-net-e`, preferences, or
   a file). Persistence is what allows `BootReceiver` to rebuild the alarms after a reboot.

2. **Schedule an alarm.** Build an `Intent` targeting `NotificationAlarmReceiver`, attach the
   payload through the `ExtraNotificationId` / `ExtraTitle` / `ExtraMessage` extras, wrap it in a
   `PendingIntent`, and register it with `AlarmManager` (use `SetExactAndAllowWhileIdle` for exact
   timing). On Android 12 (API 31)+ exact alarms require the `SCHEDULE_EXACT_ALARM` /
   `USE_EXACT_ALARM` permission — fall back to `SetAndAllowWhileIdle` when it is not granted.

   ```csharp
   var intent = new Intent(context, typeof(NotificationAlarmReceiver));
   intent.PutExtra(NotificationAlarmReceiver.ExtraNotificationId, scheduled.Id);
   intent.PutExtra(NotificationAlarmReceiver.ExtraTitle, scheduled.Title);
   intent.PutExtra(NotificationAlarmReceiver.ExtraMessage, scheduled.Message);

   var pendingIntent = PendingIntent.GetBroadcast(
       context,
       scheduled.Id,
       intent,
       PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

   var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService)!;
   alarmManager.SetExactAndAllowWhileIdle(
       AlarmType.RtcWakeup,
       scheduled.TriggerTimeUtc.ToUnixTimeMilliseconds(),
       pendingIntent);
   ```

3. **Re-schedule on boot.** Implement the `TODO` in `BootReceiver.RescheduleNotifications`: read
   the persisted notifications, skip any whose trigger time is in the past, and re-register an
   alarm for the rest using the same code path as step 2.

4. **Post the notification.** `NotificationAlarmReceiver` already creates the
   `scheduled_notifications` channel (required on Android 8.0+) and posts a placeholder
   notification. Replace the placeholder title/message/icon with the real persisted payload.

Keep the actual scheduling/persistence logic in a platform-agnostic service so it stays testable,
and call into these Android receivers only for the platform-specific alarm + notification plumbing.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
