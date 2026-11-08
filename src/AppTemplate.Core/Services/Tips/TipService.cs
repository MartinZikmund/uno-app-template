using MZikmund.Toolkit.WinUI.Services;

namespace AppTemplate.Core.Services.Tips;

/// <summary>
/// Stores seen tips as one comma-separated preference of stable string ids, so adding a tip
/// never means adding a preference key.
/// </summary>
public sealed class TipService(IPreferences preferences) : ITipService
{
    public const string SeenTipsKey = "SeenTips";

    public bool ShouldShow(TipId tip) => !ReadSeen().Contains(tip.ToStorageId());

    public void MarkSeen(params TipId[] tips)
    {
        var seen = ReadSeen();
        foreach (var tip in tips)
        {
            var id = tip.ToStorageId();
            if (!seen.Contains(id))
            {
                seen.Add(id);
            }
        }

        preferences.Set(SeenTipsKey, string.Join(',', seen));
    }

    public void Reset() => preferences.Remove(SeenTipsKey);

    // Ids this build doesn't know are kept on write: after a downgrade, replaying a tip a newer
    // build already showed would be worse than carrying a value we can't interpret.
    private List<string> ReadSeen() =>
    [
        .. preferences.Get(SeenTipsKey, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
    ];
}
