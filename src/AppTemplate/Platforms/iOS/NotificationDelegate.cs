#if __IOS__
using AppTemplate.Core.Infrastructure;
using AppTemplate.Core.Messages;
using AppTemplate.Core.Services.DeepLink;
using CommunityToolkit.Mvvm.Messaging;
using Foundation;
using UserNotifications;

namespace AppTemplate.iOS;

/// <summary>
/// Handles user-notification interactions on iOS: presents notifications while the app
/// is in the foreground and routes a notification tap to the app's deep-link handling.
/// </summary>
/// <remarks>
/// Registered as <see cref="UNUserNotificationCenter.Delegate"/> from the iOS entry point
/// (<c>Main.iOS.cs</c>) before the Uno Platform host is built.
/// </remarks>
public class NotificationDelegate : UNUserNotificationCenterDelegate
{
    /// <summary>
    /// The key used to carry a deep link inside a notification's
    /// <see cref="UNNotificationContent.UserInfo"/> payload.
    /// </summary>
    public const string DeepLinkUserInfoKey = "deepLink";

    /// <summary>
    /// Holds a deep link received during a cold start, when the dependency-injection
    /// container is not yet available. Consumed once the app has finished initializing.
    /// </summary>
    public static string? PendingDeepLink { get; set; }

    /// <summary>
    /// Called when the user taps (or otherwise acts on) a delivered notification.
    /// Extracts the deep link from the notification payload and either hands it to the
    /// running app's deep-link service or stores it for a cold start.
    /// </summary>
    public override void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        try
        {
            var deepLink = ExtractDeepLink(response.Notification.Request.Content.UserInfo);
            if (!string.IsNullOrEmpty(deepLink))
            {
                // DidReceiveNotificationResponse may be called on a background queue,
                // so dispatch to the main thread for UI-bound operations.
                NSRunLoop.Main.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var deepLinkService = IoC.GetService<IDeepLinkService>();
                        deepLinkService?.SetPendingNavigation(deepLink!);

                        // If the app is already running, notify recipients to handle the deep link.
                        var messenger = IoC.GetService<IMessenger>();
                        messenger?.Send(new DeepLinkReceivedMessage());
                    }
                    catch
                    {
                        // IoC may not be initialized during a cold start; store the deep link
                        // in a static field to be picked up once the app has initialized.
                        PendingDeepLink = deepLink;
                    }
                });
            }
        }
        finally
        {
            // Always signal completion so iOS does not keep the app awake waiting on us.
            completionHandler();
        }
    }

    /// <summary>
    /// Called when a notification is delivered while the app is in the foreground.
    /// Returning <see cref="UNNotificationPresentationOptions.Banner"/> |
    /// <see cref="UNNotificationPresentationOptions.Sound"/> |
    /// <see cref="UNNotificationPresentationOptions.List"/> ensures the notification is
    /// shown to the user even when the app is active.
    /// </summary>
    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        completionHandler(
            UNNotificationPresentationOptions.Banner |
            UNNotificationPresentationOptions.Sound |
            UNNotificationPresentationOptions.List);
    }

    /// <summary>
    /// Reads the deep link from a notification's <paramref name="userInfo"/> payload.
    /// </summary>
    private static string? ExtractDeepLink(NSDictionary userInfo)
    {
        if (userInfo is null)
        {
            return null;
        }

        var value = userInfo.ObjectForKey(new NSString(DeepLinkUserInfoKey));
        return value?.ToString();
    }
}
#endif
