using UIKit;
using Uno.UI.Hosting;
using UserNotifications;

namespace AppTemplate.iOS;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        // Register the user-notification delegate before building the host so foreground
        // notifications are presented and notification taps are routed to deep-link handling.
        UNUserNotificationCenter.Current.Delegate = new NotificationDelegate();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
