using AppTemplate.Build;
using FluentAssertions;
using Microsoft.Build.Utilities;

namespace Template.SelfTests.DevAssets;

[TestClass]
public sealed class DevAssetPlacementTests
{
    [TestMethod]
    [DataRow(true, "android", DevBadgeMask.AndroidAdaptive)]
    [DataRow(false, "android", DevBadgeMask.AndroidSplash)]
    [DataRow(true, "ios", DevBadgeMask.IosIcon)]
    [DataRow(false, "ios", DevBadgeMask.None)]
    [DataRow(true, "windows", DevBadgeMask.None)]
    [DataRow(true, "browserwasm", DevBadgeMask.None)]
    [DataRow(true, "", DevBadgeMask.None)]
    public void Resolve_PlatformAndKind_PicksMask(bool isIcon, string platform, DevBadgeMask expected)
    {
        DevAssetPlacement.Resolve(isIcon, platform, new TaskItem("a.svg")).Mask.Should().Be(expected);
    }

    [TestMethod]
    public void Resolve_IconWithForegroundScale_UsesIt()
    {
        TaskItem icon = new("a.svg");
        icon.SetMetadata("ForegroundScale", "0.6");

        DevAssetPlacement.Resolve(true, "android", icon).Scale.Should().Be(0.6);
    }

    [TestMethod]
    public void Resolve_IconWithPlatformOverride_OverrideWins()
    {
        TaskItem icon = new("a.svg");
        icon.SetMetadata("ForegroundScale", "1");
        icon.SetMetadata("AndroidForegroundScale", "0.65");

        DevAssetPlacement.Resolve(true, "android", icon).Scale.Should().Be(0.65);
    }

    [TestMethod]
    public void Resolve_SplashWithScaleAndAndroidScale_AndroidScaleWins()
    {
        TaskItem splash = new("a.svg");
        splash.SetMetadata("Scale", "0.85");
        splash.SetMetadata("AndroidScale", "0.7");

        DevAssetPlacement.Resolve(false, "android", splash).Scale.Should().Be(0.7);
        DevAssetPlacement.Resolve(false, "ios", splash).Scale.Should().Be(0.85);
    }

    [TestMethod]
    public void Resolve_NoScaleMetadata_DefaultsToOne()
    {
        DevAssetPlacement.Resolve(true, "ios", new TaskItem("a.svg")).Scale.Should().Be(1.0);
    }
}
