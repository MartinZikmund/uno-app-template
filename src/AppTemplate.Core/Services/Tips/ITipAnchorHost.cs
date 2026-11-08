namespace AppTemplate.Core.Services.Tips;

/// <summary>
/// Implemented by a view that hosts teaching tips, so its view model can ask whether a tip has
/// something on screen to point at without touching the visual tree.
/// </summary>
public interface ITipAnchorHost
{
    /// <summary>Whether the target of <paramref name="tip"/> is loaded and laid out.</summary>
    bool IsAnchorReady(TipId tip);
}
