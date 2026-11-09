using UIKit;
using Uno.UI.Hosting;
using UserNotifications;

namespace AppTemplate.iOS;

public class EntryPoint
{
    // UNUserNotificationCenter.Delegate is held weakly on iOS, so a static reference keeps
    // the instance alive for the app's lifetime.
    private static NotificationDelegate? _notificationDelegate;

    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        // Register the user-notification delegate before building the host so foreground
        // notifications are presented and notification taps are routed to deep-link handling.
        _notificationDelegate = new NotificationDelegate();
        UNUserNotificationCenter.Current.Delegate = _notificationDelegate;

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
