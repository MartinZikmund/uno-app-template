using AppTemplate.Core.Services.Tips;
using AppTemplate.Core.Tests.Fakes;
using FluentAssertions;

namespace AppTemplate.Core.Tests.Services;

[TestClass]
public class TipServiceTests
{
    private FakePreferences _preferences = null!;
    private TipService _tips = null!;

    [TestInitialize]
    public void Initialize()
    {
        _preferences = new();
        _tips = new(_preferences);
    }

    private string SeenTips => _preferences.Get(TipService.SeenTipsKey, string.Empty);

    [TestMethod]
    public void ShouldShow_TipNeverSeen_ReturnsTrue() =>
        _tips.ShouldShow(TipId.HomeWelcome).Should().BeTrue();

    [TestMethod]
    public void ShouldShow_TipAlreadySeen_ReturnsFalse()
    {
        _tips.MarkSeen(TipId.HomeWelcome);

        _tips.ShouldShow(TipId.HomeWelcome).Should().BeFalse();
    }

    [TestMethod]
    public void MarkSeen_OneTip_LeavesTheOthersEligible()
    {
        _tips.MarkSeen(TipId.HomeWelcome);

        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeTrue();
        _tips.ShouldShow(TipId.HomeCounter).Should().BeTrue();
    }

    [TestMethod]
    public void MarkSeen_Always_StoresTheStableStringId()
    {
        _tips.MarkSeen(TipId.HomeWelcome);

        SeenTips.Should().Be("home-welcome", "enum ordinals must never reach storage");
    }

    [TestMethod]
    public void MarkSeen_SeveralTips_RecordsAllOfThem()
    {
        _tips.MarkSeen(TipId.HomeWelcome, TipId.HomeSampleAction);

        _tips.ShouldShow(TipId.HomeWelcome).Should().BeFalse();
        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeFalse();
    }

    [TestMethod]
    public void MarkSeen_CalledTwice_DoesNotRepeatTheId()
    {
        _tips.MarkSeen(TipId.HomeWelcome);
        _tips.MarkSeen(TipId.HomeWelcome);

        SeenTips.Split(',').Should().ContainSingle();
    }

    [TestMethod]
    public void MarkSeen_UnknownIdsInStorage_ArePreserved()
    {
        _preferences.Set(TipService.SeenTipsKey, "from-a-newer-build");

        _tips.MarkSeen(TipId.HomeWelcome);

        SeenTips.Split(',').Should().BeEquivalentTo(
            ["from-a-newer-build", "home-welcome"],
            "dropping it would replay that tip after a downgrade");
    }

    [TestMethod]
    public void ShouldShow_BlankAndPaddedSegments_AreTolerated()
    {
        _preferences.Set(TipService.SeenTipsKey, ",, , home-welcome ,");

        _tips.ShouldShow(TipId.HomeWelcome).Should().BeFalse();
        _tips.ShouldShow(TipId.HomeSampleAction).Should().BeTrue();
    }

    [TestMethod]
    public void Reset_AfterTipsWereSeen_MakesEveryTipEligibleAgain()
    {
        _tips.MarkSeen(TipId.HomeWelcome, TipId.HomeSampleAction, TipId.HomeCounter);

        _tips.Reset();

        Enum.GetValues<TipId>().Should().OnlyContain(tip => _tips.ShouldShow(tip));
    }

    [TestMethod]
    [DataRow(TipId.HomeWelcome, "home-welcome")]
    [DataRow(TipId.HomeSampleAction, "home-sample-action")]
    [DataRow(TipId.HomeCounter, "home-counter")]
    public void ToStorageId_EveryTip_HasAStableId(TipId tip, string expected) =>
        tip.ToStorageId().Should().Be(expected);

    [TestMethod]
    public void ToStorageId_EveryTip_IsUnique() =>
        Enum.GetValues<TipId>().Select(tip => tip.ToStorageId()).Should().OnlyHaveUniqueItems();
}
