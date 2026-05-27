using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace AppTemplate.Droid;

/// <summary>
/// Receives the <see cref="Intent.ActionBootCompleted"/> broadcast and re-schedules any
/// future-dated notifications via <see cref="AlarmManager"/>.
/// </summary>
/// <remarks>
/// Android wipes all <see cref="AlarmManager"/> alarms when the device reboots. Any app that
/// relies on exact/inexact alarms to post scheduled notifications must therefore listen for
/// <see cref="Intent.ActionBootCompleted"/> and re-register its pending alarms after boot,
/// otherwise previously-scheduled notifications will silently never fire.
/// </remarks>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootReceiver : BroadcastReceiver
{
    private const string LogTag = nameof(BootReceiver);

    public override void OnReceive(Context? context, Intent? intent)
    {
        // Only react to the boot-completed broadcast. The intent filter already narrows this
        // down, but Android can deliver other actions to an exported receiver, so guard anyway.
        if (context is null || intent?.Action != Intent.ActionBootCompleted)
        {
            return;
        }

        Log.Info(LogTag, "Device boot completed; re-scheduling persisted notifications.");

        try
        {
            RescheduleNotifications(context);
        }
        catch (Exception ex)
        {
            // Never let an exception escape OnReceive: an unhandled exception here crashes the
            // broadcast and can mark the receiver as misbehaving on some OEM builds.
            Log.Error(LogTag, $"Failed to re-schedule notifications after boot: {ex}");
        }
    }

    /// <summary>
    /// Re-reads the persisted scheduled notifications and re-registers a pending alarm for each
    /// future-dated entry with <see cref="AlarmManager"/>.
    /// </summary>
    private static void RescheduleNotifications(Context context)
    {
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager is null)
        {
            Log.Warn(LogTag, "AlarmManager is unavailable; cannot re-schedule notifications.");
            return;
        }

        // TODO: Wire this up to the app's scheduled-notification service once it ships.
        //
        // The expected flow is:
        //   1. Resolve the scheduled-notification service (e.g. IScheduledNotificationService)
        //      from the app's DI container or a shared service locator.
        //   2. Read the persisted notifications (SQLite / preferences / file) that were saved
        //      when they were originally scheduled.
        //   3. For every notification whose trigger time is still in the future, build a
        //      PendingIntent that targets NotificationAlarmReceiver and register it, e.g.:
        //
        //      foreach (var scheduled in persistedNotifications)
        //      {
        //          if (scheduled.TriggerTimeUtc <= DateTimeOffset.UtcNow)
        //          {
        //              continue;
        //          }
        //
        //          var intent = new Intent(context, typeof(NotificationAlarmReceiver));
        //          intent.PutExtra(NotificationAlarmReceiver.ExtraNotificationId, scheduled.Id);
        //          intent.PutExtra(NotificationAlarmReceiver.ExtraTitle, scheduled.Title);
        //          intent.PutExtra(NotificationAlarmReceiver.ExtraMessage, scheduled.Message);
        //
        //          var pendingIntent = PendingIntent.GetBroadcast(
        //              context,
        //              scheduled.Id,
        //              intent,
        //              PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        //
        //          var triggerAtMillis = scheduled.TriggerTimeUtc.ToUnixTimeMilliseconds();
        //          alarmManager.SetExactAndAllowWhileIdle(
        //              AlarmType.RtcWakeup,
        //              triggerAtMillis,
        //              pendingIntent);
        //      }
        //
        // Note: on Android 12 (API 31)+ exact alarms require the SCHEDULE_EXACT_ALARM /
        // USE_EXACT_ALARM permission; fall back to SetAndAllowWhileIdle when it is not granted.

        Log.Info(LogTag, "No scheduled-notification service is wired up yet; nothing to re-schedule.");
    }
}
