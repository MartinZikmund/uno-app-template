# Android: scheduled notifications & boot rescheduling

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

## Wiring up the notification source

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
