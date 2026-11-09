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
/// Android wipes all <see cref="AlarmManager"/> alarms when the device reboots. Any feature that
/// relies on exact/inexact alarms to post scheduled notifications must therefore listen for
/// <see cref="Intent.ActionBootCompleted"/> and re-register its pending alarms after boot,
/// otherwise previously-scheduled notifications will silently never fire.
/// </remarks>
// Exported (required for the OS to deliver ACTION_BOOT_COMPLETED) but guarded with the
// RECEIVE_BOOT_COMPLETED permission - only the system holds it, so third-party apps can't spoof
// the broadcast via an explicit intent naming this component.
[BroadcastReceiver(Enabled = true, Exported = true, Permission = "android.permission.RECEIVE_BOOT_COMPLETED")]
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

        // Re-reading persisted notifications (SQLite/file/preferences) can be slow enough to risk
        // an ANR on the broadcast thread, so hand off to a background thread via goAsync() rather
        // than blocking OnReceive.
        var pendingResult = GoAsync();
        Task.Run(() =>
        {
            try
            {
                RescheduleNotifications(context);
            }
            catch (Exception ex)
            {
                // Never let an exception escape: an unhandled exception here can mark the receiver
                // as misbehaving on some OEM builds. The alarms will be re-scheduled the next time
                // the app is opened.
                Log.Error(LogTag, $"Failed to re-schedule notifications after boot: {ex}");
            }
            finally
            {
                pendingResult?.Finish();
            }
        });
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

        // TODO: Read the notifications that were persisted when they were originally scheduled
        // (SQLite, preferences, a JSON file - whatever store the app uses) and replace this
        // empty set. Persistence is what allows the alarms to be rebuilt after a reboot, so the
        // scheduling code path must save every notification it registers.
        //
        // Keep the persistence/read logic in a platform-agnostic service so it stays testable,
        // and call into it from here - this receiver only owns the AlarmManager plumbing.
        IReadOnlyList<ScheduledNotification> persistedNotifications = Array.Empty<ScheduledNotification>();

        var now = DateTimeOffset.UtcNow;
        foreach (var scheduled in persistedNotifications)
        {
            // Skip anything that should already have fired - the alarm would fire immediately.
            if (scheduled.TriggerTimeUtc <= now)
            {
                continue;
            }

            ScheduleAlarm(context, alarmManager, scheduled);
        }
    }

    /// <summary>
    /// Registers a single notification alarm with <see cref="AlarmManager"/>, mirroring the same
    /// <see cref="PendingIntent"/> and exact-alarm handling used when the notification is first
    /// scheduled. Re-using identical request codes means a reboot rebuilds the exact same alarms.
    /// </summary>
    /// <remarks>
    /// This is the canonical scheduling code path: the app's notification-scheduling service
    /// should call into the same logic so that scheduling and boot-rescheduling never diverge.
    /// </remarks>
    private static void ScheduleAlarm(Context context, AlarmManager alarmManager, ScheduledNotification scheduled)
    {
        var triggerAtMillis = scheduled.TriggerTimeUtc.ToUnixTimeMilliseconds();

        var intent = new Intent(context, typeof(NotificationAlarmReceiver));
        intent.PutExtra(NotificationAlarmReceiver.ExtraNotificationId, scheduled.Id);
        intent.PutExtra(NotificationAlarmReceiver.ExtraTitle, scheduled.Title);
        intent.PutExtra(NotificationAlarmReceiver.ExtraMessage, scheduled.Message);

        // The request code must be stable across process restarts so the rebuilt PendingIntent
        // matches the original (and so duplicate scheduling updates rather than stacks). An int
        // primary key works directly; for string/GUID ids derive a deterministic int hash.
        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            scheduled.Id,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S &&
            !alarmManager.CanScheduleExactAlarms())
        {
            // Android 12 (API 31)+ withholds exact alarms unless SCHEDULE_EXACT_ALARM /
            // USE_EXACT_ALARM is granted; fall back to an inexact alarm that still fires in Doze.
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            // Android 6.0 (API 23)+ exact alarm that is allowed to fire while the device is idle.
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else
        {
            alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
    }
}
