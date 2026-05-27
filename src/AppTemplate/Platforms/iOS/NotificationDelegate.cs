using AppTemplate.Core.Infrastructure;
using Foundation;
using UserNotifications;

namespace AppTemplate.iOS;

/// <summary>
/// Handles user-notification interactions on iOS:
/// presents notifications while the app is in the foreground and routes a
/// notification tap to the application's deep-link handling.
/// </summary>
/// <remarks>
/// Registered as <see cref="UNUserNotificationCenter.Delegate"/> from
/// <see cref="AppDelegate.FinishedLaunching"/>.
/// </remarks>
public class NotificationDelegate : UNUserNotificationCenterDelegate
{
    /// <summary>
    /// The key used to carry a deep link inside a notification's
    /// <see cref="UNNotificationContent.UserInfo"/> payload.
    /// </summary>
    public const string DeepLinkUserInfoKey = "deepLink";

    /// <summary>
    /// Called when a notification is delivered while the app is in the foreground.
    /// Returning <see cref="UNNotificationPresentationOptions.Banner"/> |
    /// <see cref="UNNotificationPresentationOptions.Sound"/> |
    /// <see cref="UNNotificationPresentationOptions.List"/> ensures the notification
    /// is shown to the user even when the app is active.
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
    /// Called when the user taps (or otherwise acts on) a delivered notification.
    /// Extracts the deep link from the notification payload and enqueues it for
    /// the application to handle.
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
                EnqueueDeepLink(deepLink!);
            }
        }
        finally
        {
            // Always signal completion so iOS does not keep the app awake waiting on us.
            completionHandler();
        }
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

        var key = new NSString(DeepLinkUserInfoKey);
        if (userInfo.TryGetValue(key, out var value) && value is not null)
        {
            return value.ToString();
        }

        return null;
    }

    /// <summary>
    /// Hands the extracted deep link to the application's deep-link service.
    /// </summary>
    private static void EnqueueDeepLink(string deepLink)
    {
        // TODO: MZikmund.Toolkit.WinUI does not yet expose an IDeepLinkService
        // (verified against v0.1.13-dev.43). Once it does, replace the reflective/guarded
        // resolve below with a strongly-typed reference, e.g.:
        //
        //     var deepLinkService = IoC.GetService<MZikmund.Toolkit.WinUI.Services.IDeepLinkService>();
        //     deepLinkService?.Enqueue(deepLink);
        //
        // Until then we resolve defensively so this file stays self-consistent and the
        // notification-tap plumbing is ready the moment the service becomes available.
        var deepLinkService = IoC.GetService<IDeepLinkService>();
        deepLinkService?.Enqueue(deepLink);
    }
}

/// <summary>
/// Placeholder contract for the application's deep-link handling.
/// </summary>
/// <remarks>
/// TODO: This is a temporary, local stand-in. <c>MZikmund.Toolkit.WinUI</c> is expected to
/// provide an <c>IDeepLinkService</c> (not available as of v0.1.13-dev.43). When the toolkit
/// ships it, delete this interface and resolve the toolkit type from <see cref="IoC"/> instead.
/// No implementation is registered yet, so <see cref="IoC.GetService{T}"/> returns
/// <see langword="null"/> and the tap is safely ignored.
/// </remarks>
public interface IDeepLinkService
{
    /// <summary>
    /// Enqueues a deep link for the application to process.
    /// </summary>
    /// <param name="deepLink">The deep link extracted from the notification payload.</param>
    void Enqueue(string deepLink);
}
