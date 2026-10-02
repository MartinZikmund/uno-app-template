using AppTemplate.Core.Services.Tips;

namespace AppTemplate.Core.Tests.Fakes;

/// <summary>Every anchor is ready unless a test removes it.</summary>
internal sealed class FakeTipAnchorHost : ITipAnchorHost
{
    public HashSet<TipId> MissingAnchors { get; } = [];

    public bool IsAnchorReady(TipId tip) => !MissingAnchors.Contains(tip);
}
