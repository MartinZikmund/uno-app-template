namespace AppTemplate.Core.Messages;

/// <summary>
/// Sent when a deep link has been received and stored in <see cref="Services.DeepLink.IDeepLinkService"/>
/// while the app is already running. Recipients (typically a view model) consume the pending
/// navigation and route to the appropriate destination.
/// </summary>
public record DeepLinkReceivedMessage;
