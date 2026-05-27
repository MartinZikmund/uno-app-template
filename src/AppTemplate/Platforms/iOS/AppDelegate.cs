using Foundation;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;
using UserNotifications;

namespace AppTemplate.iOS;

/// <summary>
/// Custom iOS application delegate used to hook into the app lifecycle.
/// </summary>
/// <remarks>
/// This app uses the Skia-based Apple UIKit host, so the delegate derives from
/// <see cref="UnoUIApplicationDelegate"/> rather than from <c>UIApplicationDelegate</c>
/// directly. It is wired up in <c>Main.iOS.cs</c> via
/// <c>UseAppleUIKit(builder =&gt; builder.UseUIApplicationDelegate&lt;AppDelegate&gt;())</c>.
/// </remarks>
public class AppDelegate : UnoUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Register the notification delegate so foreground notifications are presented
        // and notification taps can be routed to the app's deep-link handling.
        UNUserNotificationCenter.Current.Delegate = new NotificationDelegate();

        // Always call the base implementation so Uno Platform's internal startup runs.
        return base.FinishedLaunching(application, launchOptions);
    }
}
