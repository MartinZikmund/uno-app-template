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

Android wipes every `AlarmManager` alarm when the device reboots. Anything that posts
notifications via `AlarmManager` therefore has to re-register its pending alarms after boot,
otherwise previously-scheduled notifications silently never fire.

The receiver pair that implements this pattern lives under `src/AppTemplate/Platforms/Android/`:

- **`BootReceiver`** — an exported `[BroadcastReceiver]` listening for
  `Intent.ActionBootCompleted`, guarded with the `RECEIVE_BOOT_COMPLETED` permission so only the
  system can trigger it. On boot it hands off to a background thread via `goAsync()` and re-reads
  the persisted scheduled notifications there, re-registering a pending alarm for each future-dated
  entry. The actual `AlarmManager` scheduling (including the exact-alarm fallback below) lives in
  its `ScheduleAlarm` helper, which is the single code path scheduling should reuse so it never
  diverges from boot-rescheduling.
- **`NotificationAlarmReceiver`** — a non-exported `[BroadcastReceiver]` that is the target of
  each scheduled `PendingIntent`. When the alarm fires it creates the `scheduled_notifications`
  channel (required on Android 8.0+), builds the notification with a tap-to-open content intent,
  and posts it via `NotificationManagerCompat`. The accompanying `ScheduledNotification` record is
  a minimal generic payload (`Id`, `Title`, `Message`, `TriggerTimeUtc`) — replace it with, or map
  it onto, the app's own persisted model.

The manifest declares the permissions the pattern needs:

```xml
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
<uses-permission android:name="android.permission.SCHEDULE_EXACT_ALARM" />
<uses-permission android:name="android.permission.USE_EXACT_ALARM" />
```

`POST_NOTIFICATIONS` is a runtime permission on Android 13 (API 33)+ — declaring it in the manifest
isn't enough, the app must also request it at runtime before scheduling, or notifications are
silently suppressed.

`SCHEDULE_EXACT_ALARM` / `USE_EXACT_ALARM` are special-access permissions, not required for this
pattern to work: the receivers already fall back to an inexact (but Doze-friendly) alarm when exact
scheduling isn't available. Most apps should leave these out of the manifest by default and only add
them if they genuinely need exact-time delivery — and even then, declaring the permission isn't
sufficient on its own; on Android 12+ the user (or, pre-13, the OS) must also grant exact-alarm
access, which `AlarmManager.CanScheduleExactAlarms()` checks at runtime.

### Wiring up the notification source

`BootReceiver.RescheduleNotifications` reads an empty notification set marked with a `TODO`, and
`NotificationAlarmReceiver` posts a placeholder title/message/icon. To make them functional:

1. **Persist scheduled notifications.** When a notification is scheduled, store its id, title,
   message and trigger time somewhere durable (SQLite via `sqlite-net-e`, preferences, or a file).
   Persistence is what allows `BootReceiver` to rebuild the alarms after a reboot, so the
   scheduling code must save every notification it registers.

2. **Schedule an alarm.** Reuse `BootReceiver.ScheduleAlarm` (or mirror it). It builds an `Intent`
   targeting `NotificationAlarmReceiver`, attaches the payload through the `ExtraNotificationId` /
   `ExtraTitle` / `ExtraMessage` extras, wraps it in a `PendingIntent`, and registers it with
   `AlarmManager` using the tiered exact-alarm handling:

   ```csharp
   var intent = new Intent(context, typeof(NotificationAlarmReceiver));
   intent.PutExtra(NotificationAlarmReceiver.ExtraNotificationId, scheduled.Id);
   intent.PutExtra(NotificationAlarmReceiver.ExtraTitle, scheduled.Title);
   intent.PutExtra(NotificationAlarmReceiver.ExtraMessage, scheduled.Message);

   // The request code must be stable across process restarts so a reboot rebuilds the same
   // alarm. An int primary key works directly; for string/GUID ids derive a deterministic hash.
   var pendingIntent = PendingIntent.GetBroadcast(
       context,
       scheduled.Id,
       intent,
       PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

   var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService)!;
   var triggerAtMillis = scheduled.TriggerTimeUtc.ToUnixTimeMilliseconds();

   if (Build.VERSION.SdkInt >= BuildVersionCodes.S && !alarmManager.CanScheduleExactAlarms())
   {
       alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
   }
   else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
   {
       alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
   }
   else
   {
       alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
   }
   ```

3. **Re-schedule on boot.** Replace the empty set in `BootReceiver.RescheduleNotifications` with
   the persisted notifications; it already skips past-due entries and re-registers the rest via
   `ScheduleAlarm`.

4. **Post the notification.** Replace the placeholder title/message in `NotificationAlarmReceiver`
   with the real persisted payload, and swap the placeholder system icon for the app's own
   notification icon (e.g. `Resource.Mipmap.icon_foreground`, generated by the Uno SDK during the
   Android build).

Keep the scheduling/persistence logic in a platform-agnostic service so it stays testable, and
call into these Android receivers only for the platform-specific alarm + notification plumbing.

## Versioning

This template uses Nerdbank.GitVersioning. `main` produces `0.X.0-dev.{height}` prerelease builds with a Dev-channel identity that installs side-by-side with the Store version. Stable releases come from `release/v{minor}` branches. See [docs/versioning.md](./docs/versioning.md) for the full model and [docs/versioning-migration.md](./docs/versioning-migration.md) to apply it to an existing app.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
