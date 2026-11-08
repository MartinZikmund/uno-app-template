# Generated Dev Icon and Splash Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dev-channel builds stamp a top-right "DEV" badge onto the prod `icon_foreground.svg` and `splash_screen.svg` at build time, so an app only ever supplies prod artwork.

**Architecture:** A C# composer (`src/DevAssets.Task.cs`) nests the original SVG inside a wrapper and draws the badge, pulled inside the platform mask. `RoslynCodeTaskFactory` compiles it into an MSBuild task, and `src/DevAssets.targets` runs that task `BeforeTargets="UnoResizetizeCollectItems"`. The task re-points `UnoIcon.ForegroundFile` and `UnoSplashScreen` at `obj/…/devassets/<hash>/<original name>`. The test project compiles the same source file for unit tests.

**Tech Stack:** MSBuild (`RoslynCodeTaskFactory`), C# (`System.Xml.Linq`), Uno.Resizetizer 1.13 (via Uno.Sdk 6.7.0-dev.64), MSTest on MTP + FluentAssertions, PowerShell 7 + `System.Drawing` for build verification.

**Spec:** [`docs/superpowers/specs/2026-09-22-dev-assets-design.md`](../specs/2026-09-22-dev-assets-design.md). Read §2, §3 and §7.1 (spike results) before starting.

> **Executed 2026-09-22.** Where the build diverged from this plan (28% badge, centre fallback, Android stale-icon guard, the
> `Template.SelfTests` project, MSBuild 18.9.6, the desktop splash path), the spec's §9 records what and why. This plan is kept as written.

## Global Constraints

- Badge: top-right, height 24% of the image's shorter side, corner radius 5.52%, horizontal padding 8.5%, text size 13.92%; fill `#FFB900`, text `#141414`.
- "DEV" is drawn as outlines from Selawik Semibold 1.01 (SIL OFL 1.1), never `<text>`.
- Output path: `$(IntermediateOutputPath)devassets/<first 12 hex of SHA-256 of the composed SVG>/<original file name>`.
- Runs only when `'$(AppChannel)' == 'Dev' and '$(GenerateDevAssets)' != 'false'`, **including CI**.
- Never fails a build: `warning DEVASSETS001` (unreadable/invalid SVG, falls back to the prod artwork), `warning DEVASSETS002` (the badge can't fit the mask).
- `src/DevAssets.Task.cs` is compiled by `RoslynCodeTaskFactory`: **explicit usings only, netstandard2.0 APIs only (no `Math.Clamp`, ranges `[..^n]`, `SHA256.HashData`, `Convert.ToHexString`, `File.Move(a, b, true)`), no records or `init` accessors.** Modern *syntax* (file-scoped namespace, primary constructors, collection expressions, pattern matching) is fine: it was verified under both the .NET SDK and Visual Studio 18 MSBuild.
- Code style: `.claude/rules/code-style.md`. Always use braces. Keep comments to a line or two, explaining *why*.
- Tests: `Method_Scenario_ExpectedResult`, Arrange/Act/Assert, FluentAssertions, hand-written fakes.
- Commits: Conventional Commits, ending with the two attribution lines from the session (`Co-Authored-By: …` / `Claude-Session: …`).
- If a `dotnet build`/`dotnet test` hangs with no output, set `MSBUILDDISABLENODEREUSE=1` and rerun. Never pass `-nodeReuse:false` to `dotnet test`.

Test command used throughout:

```bash
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"
```

---

### Task 1: Badge geometry and mask placement

**Files:**
- Create: `src/DevAssets.Task.cs`
- Create: `tests/AppTemplate.Core.Tests/DevAssets/DevBadgePlacementTests.cs`
- Modify: `tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`

**Interfaces:**
- Produces:
  - `namespace AppTemplate.Build`
  - `enum DevBadgeMask { None, AndroidAdaptive, AndroidSplash, IosIcon }`
  - `sealed class BadgeBox(double x, double y, double width, double height, double radius)` with `X, Y, Width, Height, Radius, Right, Bottom`, `BadgeBox Offset(double dx, double dy)`, and `BadgeBox Normalize(double originX, double originY, double size)`
  - `static class DevBadge` with the public consts `HeightRatio`, `CornerRadiusRatio`, `PaddingRatio`, `TextSizeRatio`, `Fill`, `TextFill`, plus:
    - `static BadgeBox Place(double viewBoxX, double viewBoxY, double viewBoxWidth, double viewBoxHeight, DevBadgeMask mask, double scale, out bool fits)` returns the badge in viewBox units.
    - `static bool IsVisible(BadgeBox badge, DevBadgeMask mask, double scale)` takes the badge in frame units (0..1).

- [ ] **Step 1: Link the source into the test project**

Add to `tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`, after the `ProjectReference` item group:

```xml
  <!-- The Dev-asset composer is an MSBuild inline task (src/DevAssets.targets); compile the same source to test it. -->
  <ItemGroup>
    <Compile Include="..\..\src\DevAssets.Task.cs" Link="DevAssets\DevAssets.Task.cs" />
  </ItemGroup>
```

- [ ] **Step 2: Write the failing tests**

Create `tests/AppTemplate.Core.Tests/DevAssets/DevBadgePlacementTests.cs`:

```csharp
using AppTemplate.Build;
using FluentAssertions;

namespace AppTemplate.Core.Tests.DevAssets;

[TestClass]
public sealed class DevBadgePlacementTests
{
    [TestMethod]
    public void Place_NoMask_IsFlushTopRight()
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.None, 1, out bool fits);

        fits.Should().BeTrue();
        badge.Right.Should().BeApproximately(100, 1e-9);
        badge.Y.Should().Be(0);
        badge.Height.Should().BeApproximately(24, 1e-9);
        badge.Radius.Should().BeApproximately(5.52, 1e-9);
    }

    [TestMethod]
    public void Place_NoMask_WidthIsTextAdvancePlusPadding()
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.None, 1, out _);

        // 13.92 * 3892 / 2048 (the "DEV" advance) + 2 * 8.5
        badge.Width.Should().BeApproximately(43.4534, 1e-3);
    }

    [TestMethod]
    public void Place_OffsetViewBox_AnchorsToViewBoxCorner()
    {
        BadgeBox badge = DevBadge.Place(-10, -10, 120, 120, DevBadgeMask.None, 1, out _);

        badge.Right.Should().BeApproximately(110, 1e-9);
        badge.Y.Should().Be(-10);
        badge.Height.Should().BeApproximately(28.8, 1e-9);
    }

    [TestMethod]
    public void Place_NonSquareViewBox_SizesFromShorterSide()
    {
        BadgeBox badge = DevBadge.Place(0, 0, 200, 100, DevBadgeMask.None, 1, out _);

        badge.Height.Should().BeApproximately(24, 1e-9);
        badge.Right.Should().BeApproximately(200, 1e-9);
    }

    [TestMethod]
    [DataRow(0.6)]
    [DataRow(0.65)]
    [DataRow(0.8)]
    public void Place_AndroidAdaptive_StaysInsideSafeZone(double scale)
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.AndroidAdaptive, scale, out bool fits);

        fits.Should().BeTrue();
        FarthestReach(badge).Should().BeLessThanOrEqualTo(100 * (33.0 / 108.0) / scale);
    }

    [TestMethod]
    [DataRow(0.85)]
    [DataRow(1.0)]
    public void Place_AndroidSplash_StaysInsideSplashCircle(double scale)
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.AndroidSplash, scale, out bool fits);

        fits.Should().BeTrue();
        FarthestReach(badge).Should().BeLessThanOrEqualTo(100 * (36.0 / 108.0) / scale);
    }

    [TestMethod]
    [DataRow(1.0)]
    [DataRow(0.8)]
    public void Place_IosIcon_ClearsRoundedCorner(double scale)
    {
        BadgeBox badge = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.IosIcon, scale, out bool fits);

        fits.Should().BeTrue();
        double half = 50 / scale;
        double corner = 22.37 / scale;
        double arcX = 50 + half - corner;
        double arcY = 50 - half + corner;
        double discX = badge.Right - badge.Radius;
        double discY = badge.Y + badge.Radius;
        if (discX > arcX && discY < arcY)
        {
            (Distance(discX - arcX, discY - arcY) + badge.Radius).Should().BeLessThanOrEqualTo(corner);
        }

        badge.Right.Should().BeLessThanOrEqualTo(50 + half);
        badge.Y.Should().BeGreaterThanOrEqualTo(50 - half);
    }

    [TestMethod]
    [DataRow(DevBadgeMask.AndroidAdaptive, 0.6)]
    [DataRow(DevBadgeMask.AndroidSplash, 0.85)]
    [DataRow(DevBadgeMask.IosIcon, 1.0)]
    public void Place_MaskClipsFlushBadge_PullsDiagonallyTowardsCentre(DevBadgeMask mask, double scale)
    {
        BadgeBox flush = DevBadge.Place(0, 0, 100, 100, DevBadgeMask.None, 1, out _);

        BadgeBox pulled = DevBadge.Place(0, 0, 100, 100, mask, scale, out _);

        double dx = pulled.X - flush.X;
        double dy = pulled.Y - flush.Y;
        dx.Should().BeNegative();
        dy.Should().BeApproximately(-dx, 1e-9);
    }

    [TestMethod]
    public void Place_MaskSmallerThanBadge_ReportsNotFitting()
    {
        DevBadge.Place(0, 0, 100, 100, DevBadgeMask.AndroidAdaptive, 3.0, out bool fits);

        fits.Should().BeFalse();
    }

    [TestMethod]
    public void IsVisible_NoMask_IsAlwaysTrue()
    {
        DevBadge.IsVisible(new BadgeBox(-1, -1, 3, 3, 0), DevBadgeMask.None, 1).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(DevBadgeMask.AndroidAdaptive)]
    [DataRow(DevBadgeMask.AndroidSplash)]
    [DataRow(DevBadgeMask.IosIcon)]
    public void IsVisible_SmallBadgeAtCentre_IsTrue(DevBadgeMask mask)
    {
        DevBadge.IsVisible(new BadgeBox(0.45, 0.45, 0.1, 0.1, 0.01), mask, 1).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(DevBadgeMask.AndroidAdaptive)]
    [DataRow(DevBadgeMask.AndroidSplash)]
    [DataRow(DevBadgeMask.IosIcon)]
    public void IsVisible_SquareBadgeInCorner_IsFalse(DevBadgeMask mask)
    {
        DevBadge.IsVisible(new BadgeBox(0.9, 0, 0.1, 0.1, 0), mask, 1).Should().BeFalse();
    }

    // Farthest point of the rounded badge from the image centre: a corner disc's centre distance plus its radius.
    static double FarthestReach(BadgeBox b) => new[]
    {
        (X: b.X + b.Radius, Y: b.Y + b.Radius),
        (X: b.Right - b.Radius, Y: b.Y + b.Radius),
        (X: b.X + b.Radius, Y: b.Bottom - b.Radius),
        (X: b.Right - b.Radius, Y: b.Bottom - b.Radius),
    }.Max(c => Distance(c.X - 50, c.Y - 50) + b.Radius);

    static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
}
```

- [ ] **Step 3: Run the tests and confirm they fail**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"`
Expected: FAIL, because the build can't find `src/DevAssets.Task.cs` / `AppTemplate.Build`.

- [ ] **Step 4: Implement the geometry**

Create `src/DevAssets.Task.cs`:

```csharp
// Compiled by RoslynCodeTaskFactory (src/DevAssets.targets) and by AppTemplate.Core.Tests. The factory has no implicit
// usings and targets netstandard2.0 under Visual Studio's MSBuild: explicit usings, netstandard2.0 APIs, no records.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace AppTemplate.Build;

/// <summary>The platform mask an image is shown through.</summary>
public enum DevBadgeMask
{
    None,
    AndroidAdaptive,
    AndroidSplash,
    IosIcon,
}

/// <summary>A rounded rectangle.</summary>
public sealed class BadgeBox(double x, double y, double width, double height, double radius)
{
    public double X { get; } = x;

    public double Y { get; } = y;

    public double Width { get; } = width;

    public double Height { get; } = height;

    public double Radius { get; } = radius;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public BadgeBox Offset(double dx, double dy) => new(X + dx, Y + dy, Width, Height, Radius);

    /// <summary>Maps the box into a unit square whose top-left corner is (<paramref name="originX"/>, <paramref name="originY"/>).</summary>
    public BadgeBox Normalize(double originX, double originY, double size) =>
        new((X - originX) / size, (Y - originY) / size, Width / size, Height / size, Radius / size);
}

/// <summary>Stamps the Dev-channel badge onto an SVG.</summary>
public static class DevBadge
{
    // The in-app DevChannelBadge (Padding 6,2, 10px SemiBold text, CornerRadius 4, ~17.3px tall) scaled to 24% of
    // the image. Fractions of the image's shorter side.
    public const double HeightRatio = 0.24;
    public const double CornerRadiusRatio = 0.0552;
    public const double PaddingRatio = 0.085;
    public const double TextSizeRatio = 0.1392;
    public const string Fill = "#FFB900";
    public const string TextFill = "#141414";

    // Visible regions in frame units: the square the image is fitted into, before ForegroundScale / Scale.
    const double AndroidAdaptiveSafeRadius = 33.0 / 108.0; // 66dp safe zone of the 108dp adaptive layer
    const double AndroidSplashRadius = 36.0 / 108.0;       // 72dp visible circle of the 108dp Android 12+ splash drawable
    const double IosCornerRatio = 0.2237;
    const double Margin = 0.01;
    const double PullStep = 0.0025;
    const int MaxPullSteps = 200;

    // "DEV" in Selawik Semibold 1.01 (© Microsoft, SIL Open Font License 1.1), as outlines so the badge never depends
    // on the build machine's fonts. Font units, baseline at y = 0, y pointing down.
    const double UnitsPerEm = 2048;
    const double CapHeight = 1434;
    const double TextAdvance = 3892;
    const string TextPath = "M172 -1433H671Q1034 -1433 1219 -1261Q1404 -1089 1404 -734Q1404 -510 1311 -344Q1218 -179 1048 -90Q879 0 656 0L172 2ZM658 -225Q808 -225 914 -284Q1021 -343 1076 -456Q1132 -568 1132 -728Q1132 -971 1014 -1090Q897 -1210 667 -1209L437 -1207V-225ZM1657 -1434H2452V-1215H1922V-837H2390V-616H1922V-219H2484V0H1657ZM2569 -1433H2857L3191 -380Q3215 -308 3221 -253H3225Q3229 -281 3238 -316Q3248 -350 3259 -383L3603 -1434L3881 -1433L3373 1H3070Z";

    /// <summary>
    /// Returns the badge in viewBox units: flush top-right, then pulled towards the centre until the mask shows all of it.
    /// </summary>
    public static BadgeBox Place(double viewBoxX, double viewBoxY, double viewBoxWidth, double viewBoxHeight, DevBadgeMask mask, double scale, out bool fits)
    {
        double side = Math.Min(viewBoxWidth, viewBoxHeight);
        double width = (TextSizeRatio * TextAdvance / UnitsPerEm + 2 * PaddingRatio) * side;
        BadgeBox flush = new(viewBoxX + viewBoxWidth - width, viewBoxY, width, HeightRatio * side, CornerRadiusRatio * side);

        // Resizetizer fits the image into a square canvas and scales it about the centre, so masks live on that square.
        double frame = Math.Max(viewBoxWidth, viewBoxHeight);
        double frameX = viewBoxX + (viewBoxWidth - frame) / 2;
        double frameY = viewBoxY + (viewBoxHeight - frame) / 2;

        // Diagonal pull keeps the badge in its corner as far as the mask allows.
        double dx = Math.Sign(Math.Round(frameX + frame / 2 - (flush.X + flush.Right) / 2, 9));
        double dy = Math.Sign(Math.Round(frameY + frame / 2 - (flush.Y + flush.Bottom) / 2, 9));
        double length = Math.Sqrt(dx * dx + dy * dy);

        BadgeBox badge = flush;
        for (int step = 0; step <= MaxPullSteps; step++)
        {
            double distance = length == 0 ? 0 : step * PullStep * frame / length;
            badge = flush.Offset(dx * distance, dy * distance);
            if (IsVisible(badge.Normalize(frameX, frameY, frame), mask, scale))
            {
                fits = true;
                return badge;
            }
        }

        fits = false;
        return badge;
    }

    /// <summary>Whether a badge, in frame units (0..1), is fully visible through the mask.</summary>
    public static bool IsVisible(BadgeBox badge, DevBadgeMask mask, double scale) => mask switch
    {
        DevBadgeMask.AndroidAdaptive => InsideCircle(badge, AndroidAdaptiveSafeRadius / scale - Margin),
        DevBadgeMask.AndroidSplash => InsideCircle(badge, AndroidSplashRadius / scale - Margin),
        DevBadgeMask.IosIcon => InsideRoundedSquare(badge, 0.5 / scale - Margin, Math.Max(0, IosCornerRatio / scale - Margin)),
        _ => true,
    };

    // Every mask is convex, so the rounded badge is inside it exactly when its four corner discs are.
    static bool InsideCircle(BadgeBox badge, double radius) =>
        CornerCentres(badge).All(c => Distance(c.X - 0.5, c.Y - 0.5) + badge.Radius <= radius);

    static bool InsideRoundedSquare(BadgeBox badge, double halfSide, double cornerRadius)
    {
        // A disc fits a rounded square when its centre fits the square shrunk by the disc's radius.
        double half = halfSide - badge.Radius;
        double corner = Math.Max(0, cornerRadius - badge.Radius);
        double straight = half - corner;
        return CornerCentres(badge).All(c =>
        {
            double dx = Math.Abs(c.X - 0.5);
            double dy = Math.Abs(c.Y - 0.5);
            return dx <= half && dy <= half && (dx <= straight || dy <= straight || Distance(dx - straight, dy - straight) <= corner);
        });
    }

    static IEnumerable<(double X, double Y)> CornerCentres(BadgeBox badge)
    {
        yield return (badge.X + badge.Radius, badge.Y + badge.Radius);
        yield return (badge.Right - badge.Radius, badge.Y + badge.Radius);
        yield return (badge.X + badge.Radius, badge.Bottom - badge.Radius);
        yield return (badge.Right - badge.Radius, badge.Bottom - badge.Radius);
    }

    static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
}
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"`
Expected: PASS. The build must be free of new warnings from these files.

- [ ] **Step 6: Commit**

```bash
git add src/DevAssets.Task.cs tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj tests/AppTemplate.Core.Tests/DevAssets/DevBadgePlacementTests.cs
git commit -m "feat(build): place the Dev badge inside each platform's icon mask"
```

---

### Task 2: SVG composition

**Files:**
- Modify: `src/DevAssets.Task.cs`
- Create: `tests/AppTemplate.Core.Tests/DevAssets/DevBadgeComposeTests.cs`

**Interfaces:**
- Consumes: `DevBadge.Place`, `BadgeBox`, `DevBadgeMask` (Task 1).
- Produces: `static string DevBadge.Compose(string svg, DevBadgeMask mask, double scale, out bool fits)`. It throws `FormatException` when there's no usable `viewBox`/`width`/`height` or the root isn't `<svg>`, and `XmlException` when the XML is malformed. The badge group is `<g id="dev-badge">` holding one `<rect>` and one `<path>`.

- [ ] **Step 1: Write the failing tests**

Create `tests/AppTemplate.Core.Tests/DevAssets/DevBadgeComposeTests.cs`:

```csharp
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using AppTemplate.Build;
using FluentAssertions;

namespace AppTemplate.Core.Tests.DevAssets;

[TestClass]
public sealed class DevBadgeComposeTests
{
    static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    static readonly XNamespace XLink = "http://www.w3.org/1999/xlink";

    const string Logo = """
        <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="450" height="450" viewBox="0 0 50 50" id="logo">
          <defs><path id="p" d="M0 0H10V10Z" /></defs>
          <use xlink:href="#p" fill="#7a67f8" />
        </svg>
        """;

    [TestMethod]
    public void Compose_Logo_NestsOriginalWithSameViewBox()
    {
        XElement result = XElement.Parse(DevBadge.Compose(Logo, DevBadgeMask.None, 1, out _));

        result.Attribute("viewBox")!.Value.Should().Be("0 0 50 50");
        result.Attribute("width")!.Value.Should().Be("450");
        XElement nested = result.Elements(Svg + "svg").Single();
        nested.Attribute("id")!.Value.Should().Be("logo");
        nested.Attribute("width")!.Value.Should().Be("50");
        nested.Attribute("height")!.Value.Should().Be("50");
        nested.Descendants(Svg + "use").Single().Attribute(XLink + "href")!.Value.Should().Be("#p");
    }

    [TestMethod]
    public void Compose_Logo_DrawsBadgeTopRightInViewBoxUnits()
    {
        XElement rect = Badge(DevBadge.Compose(Logo, DevBadgeMask.None, 1, out _)).Element(Svg + "rect")!;

        (Number(rect, "x") + Number(rect, "width")).Should().BeApproximately(50, 1e-3);
        Number(rect, "y").Should().Be(0);
        Number(rect, "height").Should().BeApproximately(12, 1e-3);
        rect.Attribute("fill")!.Value.Should().Be(DevBadge.Fill);
    }

    [TestMethod]
    public void Compose_Logo_DrawsDevAsOutlinesNotText()
    {
        XElement badge = Badge(DevBadge.Compose(Logo, DevBadgeMask.None, 1, out _));

        badge.Descendants(Svg + "text").Should().BeEmpty();
        XElement path = badge.Element(Svg + "path")!;
        path.Attribute("fill")!.Value.Should().Be(DevBadge.TextFill);
        path.Attribute("d")!.Value.Should().StartWith("M172 -1433");
    }

    [TestMethod]
    public void Compose_NoViewBox_UsesPixelWidthAndHeight()
    {
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg" width="200px" height="100"><rect width="10" height="10" /></svg>""";

        XElement result = XElement.Parse(DevBadge.Compose(svg, DevBadgeMask.None, 1, out _));

        result.Attribute("viewBox")!.Value.Should().Be("0 0 200 100");
        Number(Badge(result).Element(Svg + "rect")!, "height").Should().BeApproximately(24, 1e-3);
    }

    [TestMethod]
    [DataRow("""<svg xmlns="http://www.w3.org/2000/svg" />""")]
    [DataRow("""<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%" />""")]
    [DataRow("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 0 10" />""")]
    [DataRow("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ten 10" />""")]
    [DataRow("""<html />""")]
    public void Compose_NoUsableSvg_ThrowsFormatException(string svg)
    {
        Action compose = () => DevBadge.Compose(svg, DevBadgeMask.None, 1, out _);

        compose.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Compose_MalformedXml_ThrowsXmlException()
    {
        Action compose = () => DevBadge.Compose("<svg", DevBadgeMask.None, 1, out _);

        compose.Should().Throw<XmlException>();
    }

    [TestMethod]
    public void Compose_WithoutSvgNamespace_MovesElementsIntoIt()
    {
        const string svg = """<svg viewBox="0 0 10 10"><rect width="5" height="5" /></svg>""";

        XElement result = XElement.Parse(DevBadge.Compose(svg, DevBadgeMask.None, 1, out _));

        result.DescendantsAndSelf().Should().OnlyContain(e => e.Name.Namespace == Svg);
    }

    [TestMethod]
    public void Compose_WithDoctype_IgnoresIt()
    {
        const string svg = """<!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" />""";

        Action compose = () => DevBadge.Compose(svg, DevBadgeMask.None, 1, out _);

        compose.Should().NotThrow();
    }

    [TestMethod]
    public void Compose_SameInputUnderDifferentCultures_IsIdentical()
    {
        string english = WithCulture("en-US", () => DevBadge.Compose(Logo, DevBadgeMask.AndroidAdaptive, 0.6, out _));
        string czech = WithCulture("cs-CZ", () => DevBadge.Compose(Logo, DevBadgeMask.AndroidAdaptive, 0.6, out _));

        czech.Should().Be(english);
    }

    [TestMethod]
    [DataRow(0.6, true)]
    [DataRow(3.0, false)]
    public void Compose_AndroidAdaptive_ReportsWhetherBadgeFits(double scale, bool expected)
    {
        DevBadge.Compose(Logo, DevBadgeMask.AndroidAdaptive, scale, out bool fits);

        fits.Should().Be(expected);
    }

    static XElement Badge(string svg) => Badge(XElement.Parse(svg));

    static XElement Badge(XElement root) => root.Elements(Svg + "g").Single(g => (string?)g.Attribute("id") == "dev-badge");

    static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    static T WithCulture<T>(string name, Func<T> action)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(name);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"`
Expected: FAIL to compile. `DevBadge` doesn't contain a definition for `Compose`.

- [ ] **Step 3: Implement composition**

In `src/DevAssets.Task.cs`, extend the usings to:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
```

Add this field to `DevBadge`, directly after the `TextPath` const:

```csharp
    static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
```

Add these members to `DevBadge`, between `Place` and `IsVisible`:

```csharp
    /// <summary>Returns <paramref name="svg"/> with the badge drawn on top; the original drawing is nested unchanged.</summary>
    /// <exception cref="FormatException">The root isn't &lt;svg&gt; or has no usable viewBox, width or height.</exception>
    /// <exception cref="XmlException">The SVG isn't well-formed.</exception>
    public static string Compose(string svg, DevBadgeMask mask, double scale, out bool fits)
    {
        XElement original = Load(svg);
        double[] viewBox = ReadViewBox(original);
        double x = viewBox[0];
        double y = viewBox[1];
        double width = viewBox[2];
        double height = viewBox[3];
        BadgeBox badge = Place(x, y, width, height, mask, scale, out fits);

        XElement wrapper = new(Svg + "svg", new XAttribute("viewBox", string.Join(" ", viewBox.Select(v => Format(v)))));
        foreach (string size in new[] { "width", "height" })
        {
            if (original.Attribute(size) is { } attribute)
            {
                wrapper.SetAttributeValue(size, attribute.Value);
            }
        }

        // With an identical viewBox the original maps 1:1 and keeps its own namespaces, defs and ids.
        original.SetAttributeValue("x", Format(x));
        original.SetAttributeValue("y", Format(y));
        original.SetAttributeValue("width", Format(width));
        original.SetAttributeValue("height", Format(height));
        wrapper.Add(original);
        wrapper.Add(BadgeElement(badge, Math.Min(width, height)));
        return wrapper.ToString(SaveOptions.DisableFormatting);
    }

    static XElement BadgeElement(BadgeBox badge, double side)
    {
        double glyphScale = TextSizeRatio * side / UnitsPerEm;
        double textX = badge.X + (badge.Width - TextAdvance * glyphScale) / 2;
        double baseline = badge.Y + (badge.Height + CapHeight * glyphScale) / 2;
        return new XElement(Svg + "g",
            new XAttribute("id", "dev-badge"),
            new XElement(Svg + "rect",
                new XAttribute("x", Format(badge.X)),
                new XAttribute("y", Format(badge.Y)),
                new XAttribute("width", Format(badge.Width)),
                new XAttribute("height", Format(badge.Height)),
                new XAttribute("rx", Format(badge.Radius)),
                new XAttribute("fill", Fill)),
            new XElement(Svg + "path",
                new XAttribute("transform", $"translate({Format(textX)} {Format(baseline)}) scale({Format(glyphScale, "0.#########")})"),
                new XAttribute("fill", TextFill),
                new XAttribute("d", TextPath)));
    }

    static XElement Load(string svg)
    {
        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        using XmlReader reader = XmlReader.Create(new StringReader(svg), settings);
        XElement root = XDocument.Load(reader).Root ?? throw new FormatException("The SVG has no root element.");
        if (root.Name.LocalName != "svg")
        {
            throw new FormatException($"The root element is <{root.Name.LocalName}>, not <svg>.");
        }

        // Without this, a nested SVG that omits xmlns serialises as xmlns="" and renderers skip it.
        foreach (XElement element in root.DescendantsAndSelf().Where(e => e.Name.Namespace == XNamespace.None).ToList())
        {
            element.Name = Svg + element.Name.LocalName;
        }

        return root;
    }

    static double[] ReadViewBox(XElement root)
    {
        string? viewBox = (string?)root.Attribute("viewBox");
        if (!string.IsNullOrWhiteSpace(viewBox))
        {
            double[] parts = viewBox!.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseNumber).ToArray();
            if (parts.Length == 4 && parts[2] > 0 && parts[3] > 0)
            {
                return parts;
            }

            throw new FormatException($"The viewBox '{viewBox}' is not four numbers with a positive size.");
        }

        return [0, 0, ReadLength(root, "width"), ReadLength(root, "height")];
    }

    static double ReadLength(XElement root, string name)
    {
        string raw = ((string?)root.Attribute(name) ?? "").Trim();
        if (raw.EndsWith("px", StringComparison.Ordinal))
        {
            raw = raw.Substring(0, raw.Length - 2);
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0)
        {
            return value;
        }

        throw new FormatException($"The SVG has no viewBox and its {name} '{raw}' is not a plain number.");
    }

    static double ParseNumber(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    static string Format(double value, string pattern = "0.####") => value.ToString(pattern, CultureInfo.InvariantCulture);
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"`
Expected: PASS (all placement and compose tests).

- [ ] **Step 5: Commit**

```bash
git add src/DevAssets.Task.cs tests/AppTemplate.Core.Tests/DevAssets/DevBadgeComposeTests.cs
git commit -m "feat(build): compose the Dev badge onto an SVG"
```

---

### Task 3: The MSBuild task

**Files:**
- Modify: `src/DevAssets.Task.cs`
- Modify: `tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`
- Create: `tests/AppTemplate.Core.Tests/Fakes/FakeBuildEngine.cs`
- Create: `tests/AppTemplate.Core.Tests/DevAssets/DevAssetPlacementTests.cs`
- Create: `tests/AppTemplate.Core.Tests/DevAssets/ComposeDevAssetTests.cs`

**Interfaces:**
- Consumes: `DevBadge.Compose`, `DevBadgeMask` (Tasks 1–2).
- Produces:
  - `sealed class DevAssetPlacement(DevBadgeMask mask, double scale)` with `Mask`, `Scale`, and `static DevAssetPlacement Resolve(bool isIcon, string targetPlatform, ITaskItem asset)`.
  - `sealed class ComposeDevAsset : Microsoft.Build.Utilities.Task`. Inputs: `[Required] ITaskItem[] Assets`, `[Required] string Kind` (`"Icon"` or `"Splash"`), `string TargetPlatform`, `[Required] string ProjectDirectory`, `[Required] string OutputRoot`. Output: `[Output] ITaskItem[] Result`. `src/DevAssets.targets` (Task 4) calls it by these names.

- [ ] **Step 1: Add the MSBuild package to the test project**

In `tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`, extend the item group added in Task 1:

```xml
  <!-- The Dev-asset composer is an MSBuild inline task (src/DevAssets.targets); compile the same source to test it. -->
  <ItemGroup>
    <Compile Include="..\..\src\DevAssets.Task.cs" Link="DevAssets\DevAssets.Task.cs" />
    <PackageReference Include="Microsoft.Build.Utilities.Core" Version="18.10.1" />
  </ItemGroup>
```

(Explicit version: the test project sits outside Central Package Management, as its existing comment explains.)

- [ ] **Step 2: Write the fake build engine**

Create `tests/AppTemplate.Core.Tests/Fakes/FakeBuildEngine.cs`:

```csharp
using System.Collections;
using Microsoft.Build.Framework;

namespace AppTemplate.Core.Tests.Fakes;

internal sealed class FakeBuildEngine : IBuildEngine
{
    public List<BuildWarningEventArgs> Warnings { get; } = new();

    public bool ContinueOnError => false;

    public int LineNumberOfTaskNode => 0;

    public int ColumnNumberOfTaskNode => 0;

    public string ProjectFileOfTaskNode => "test.proj";

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

    public void LogCustomEvent(CustomBuildEventArgs e)
    {
    }

    public void LogErrorEvent(BuildErrorEventArgs e)
    {
    }

    public void LogMessageEvent(BuildMessageEventArgs e)
    {
    }

    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
}
```

If `CustomBuildEventArgs` is marked `[Obsolete]` in 18.10.1 and triggers CS0618, the interface still requires the member. Keep it and tell the reviewer. Don't suppress the warning project-wide.

- [ ] **Step 3: Write the failing tests**

Create `tests/AppTemplate.Core.Tests/DevAssets/DevAssetPlacementTests.cs`:

```csharp
using AppTemplate.Build;
using FluentAssertions;
using Microsoft.Build.Utilities;

namespace AppTemplate.Core.Tests.DevAssets;

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
```

Create `tests/AppTemplate.Core.Tests/DevAssets/ComposeDevAssetTests.cs`:

```csharp
using AppTemplate.Build;
using AppTemplate.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace AppTemplate.Core.Tests.DevAssets;

[TestClass]
public sealed class ComposeDevAssetTests
{
    const string Logo = """<svg xmlns="http://www.w3.org/2000/svg" width="450" height="450" viewBox="0 0 50 50"><rect width="50" height="50" fill="#7a67f8" /></svg>""";

    string _project = null!;
    FakeBuildEngine _engine = null!;

    [TestInitialize]
    public void Setup()
    {
        _project = Path.Combine(Path.GetTempPath(), "devassets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_project, "Assets"));
        File.WriteAllText(Path.Combine(_project, "Assets", "icon_foreground.svg"), Logo);
        File.WriteAllText(Path.Combine(_project, "Assets", "splash_screen.svg"), Logo);
        _engine = new FakeBuildEngine();
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_project, recursive: true);

    [TestMethod]
    public void Execute_Icon_PointsForegroundFileAtHashedCopy()
    {
        TaskItem icon = new("Assets/icon.svg");
        icon.SetMetadata("ForegroundFile", "Assets/icon_foreground.svg");
        icon.SetMetadata("ForegroundScale", "0.6");

        ITaskItem result = Run("Icon", "windows", icon).Single();

        result.ItemSpec.Should().Be("Assets/icon.svg");
        result.GetMetadata("ForegroundScale").Should().Be("0.6");
        string badged = result.GetMetadata("ForegroundFile");
        Path.GetFileName(badged).Should().Be("icon_foreground.svg");
        Path.GetFileName(Path.GetDirectoryName(badged)).Should().MatchRegex("^[0-9a-f]{12}$");
        Path.GetDirectoryName(Path.GetDirectoryName(badged)).Should().Be(Path.Combine(_project, "obj", "devassets"));
        File.ReadAllText(badged).Should().Contain("dev-badge");
    }

    [TestMethod]
    public void Execute_Splash_ReplacesItemSpecAndKeepsMetadata()
    {
        TaskItem splash = new("Assets/splash_screen.svg");
        splash.SetMetadata("BaseSize", "300,300");
        splash.SetMetadata("Scale", "0.85");

        ITaskItem result = Run("Splash", "android", splash).Single();

        Path.IsPathRooted(result.ItemSpec).Should().BeTrue();
        Path.GetFileName(result.ItemSpec).Should().Be("splash_screen.svg");
        result.GetMetadata("BaseSize").Should().Be("300,300");
        result.GetMetadata("Scale").Should().Be("0.85");
    }

    [TestMethod]
    public void Execute_SameInputTwice_ReusesTheFile()
    {
        TaskItem splash = new("Assets/splash_screen.svg");
        string first = Run("Splash", "windows", splash).Single().ItemSpec;
        DateTime written = File.GetLastWriteTimeUtc(first);

        string second = Run("Splash", "windows", splash).Single().ItemSpec;

        second.Should().Be(first);
        File.GetLastWriteTimeUtc(second).Should().Be(written);
    }

    [TestMethod]
    public void Execute_DifferentPlacement_GetsDifferentHash()
    {
        TaskItem splash = new("Assets/splash_screen.svg");

        string windows = Run("Splash", "windows", splash).Single().ItemSpec;
        string android = Run("Splash", "android", splash).Single().ItemSpec;

        android.Should().NotBe(windows);
    }

    [TestMethod]
    public void Execute_MissingSource_WarnsAndKeepsTheItem()
    {
        TaskItem splash = new("Assets/missing.svg");

        ITaskItem result = Run("Splash", "windows", splash).Single();

        result.Should().BeSameAs(splash);
        _engine.Warnings.Should().ContainSingle(w => w.Code == "DEVASSETS001");
    }

    [TestMethod]
    public void Execute_MalformedSource_WarnsAndKeepsTheItem()
    {
        File.WriteAllText(Path.Combine(_project, "Assets", "splash_screen.svg"), "<svg");
        TaskItem splash = new("Assets/splash_screen.svg");

        ITaskItem result = Run("Splash", "windows", splash).Single();

        result.Should().BeSameAs(splash);
        _engine.Warnings.Should().ContainSingle(w => w.Code == "DEVASSETS001");
    }

    [TestMethod]
    public void Execute_SourceAlreadyBadged_LeavesItemUntouched()
    {
        ITaskItem badged = Run("Splash", "windows", new TaskItem("Assets/splash_screen.svg")).Single();

        ITaskItem again = Run("Splash", "windows", badged).Single();

        again.Should().BeSameAs(badged);
    }

    [TestMethod]
    public void Execute_IconWithoutForegroundFile_LeavesItemUntouched()
    {
        TaskItem icon = new("Assets/icon_foreground.svg");

        Run("Icon", "windows", icon).Single().Should().BeSameAs(icon);
    }

    [TestMethod]
    public void Execute_BadgeCannotFitMask_WarnsDevAssets002()
    {
        TaskItem icon = new("Assets/icon.svg");
        icon.SetMetadata("ForegroundFile", "Assets/icon_foreground.svg");
        icon.SetMetadata("ForegroundScale", "3");

        Run("Icon", "android", icon);

        _engine.Warnings.Should().ContainSingle(w => w.Code == "DEVASSETS002");
    }

    ITaskItem[] Run(string kind, string platform, params ITaskItem[] assets)
    {
        ComposeDevAsset task = new()
        {
            BuildEngine = _engine,
            Assets = assets,
            Kind = kind,
            TargetPlatform = platform,
            ProjectDirectory = _project,
            OutputRoot = Path.Combine("obj", "devassets"),
        };

        task.Execute().Should().BeTrue();
        return task.Result;
    }
}
```

- [ ] **Step 4: Run the tests and confirm they fail**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"`
Expected: FAIL to compile. `DevAssetPlacement` and `ComposeDevAsset` don't exist.

- [ ] **Step 5: Implement the task**

In `src/DevAssets.Task.cs`, extend the usings to:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
```

Append at the end of the file:

```csharp
/// <summary>Which mask and scale an asset is rendered with, resolved the way Uno.Resizetizer reads them.</summary>
public sealed class DevAssetPlacement(DevBadgeMask mask, double scale)
{
    public DevBadgeMask Mask { get; } = mask;

    public double Scale { get; } = scale;

    public static DevAssetPlacement Resolve(bool isIcon, string targetPlatform, ITaskItem asset)
    {
        string platform = targetPlatform.ToLowerInvariant();
        string metadata = isIcon ? "ForegroundScale" : "Scale";

        // A platform override (e.g. AndroidForegroundScale) wins, as in Resizetizer's ResizeImageInfo.
        double scale = ReadScale(asset, PlatformPrefix(platform) + metadata) ?? ReadScale(asset, metadata) ?? 1.0;
        DevBadgeMask mask = (platform, isIcon) switch
        {
            ("android", true) => DevBadgeMask.AndroidAdaptive,
            ("android", false) => DevBadgeMask.AndroidSplash,
            ("ios", true) => DevBadgeMask.IosIcon,
            _ => DevBadgeMask.None,
        };
        return new DevAssetPlacement(mask, scale);
    }

    static string PlatformPrefix(string platform) => platform switch
    {
        "android" => "Android",
        "ios" => "IOS",
        "windows" => "Windows",
        "browserwasm" => "Wasm",
        _ => "Skia",
    };

    static double? ReadScale(ITaskItem asset, string name) =>
        double.TryParse(asset.GetMetadata(name), NumberStyles.Number, CultureInfo.InvariantCulture, out double value) && value > 0 ? value : null;
}

/// <summary>
/// Replaces each UnoIcon foreground (Kind=Icon) or UnoSplashScreen image (Kind=Splash) with a badged copy at
/// OutputRoot/&lt;content hash&gt;/&lt;original file name&gt;, so every generated resource name stays the same.
/// </summary>
// Fully qualified: the test project's implicit usings also bring System.Threading.Tasks.Task into scope.
public sealed class ComposeDevAsset : Microsoft.Build.Utilities.Task
{
    static readonly UTF8Encoding Utf8 = new(false);

    [Required]
    public ITaskItem[] Assets { get; set; } = [];

    /// <summary>"Icon" rewrites ForegroundFile; "Splash" rewrites the item spec.</summary>
    [Required]
    public string Kind { get; set; } = "";

    public string TargetPlatform { get; set; } = "";

    [Required]
    public string ProjectDirectory { get; set; } = "";

    [Required]
    public string OutputRoot { get; set; } = "";

    [Output]
    public ITaskItem[] Result { get; set; } = [];

    public override bool Execute()
    {
        bool isIcon = string.Equals(Kind, "Icon", StringComparison.OrdinalIgnoreCase);
        string root = Path.GetFullPath(Path.Combine(ProjectDirectory, OutputRoot));
        Result = Assets.Select(asset => Compose(asset, isIcon, root)).ToArray();
        return !Log.HasLoggedErrors;
    }

    ITaskItem Compose(ITaskItem asset, bool isIcon, string root)
    {
        string file = isIcon ? asset.GetMetadata("ForegroundFile") : asset.ItemSpec;
        if (file.Length == 0)
        {
            return asset;
        }

        string source = Path.GetFullPath(Path.Combine(ProjectDirectory, file));
        if (source.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return asset;
        }

        DevAssetPlacement placement = DevAssetPlacement.Resolve(isIcon, TargetPlatform, asset);
        string composed;
        bool fits;
        try
        {
            composed = DevBadge.Compose(File.ReadAllText(source), placement.Mask, placement.Scale, out fits);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException or FormatException)
        {
            Log.LogWarning(null, "DEVASSETS001", null, source, 0, 0, 0, 0, "Dev badge skipped, the original artwork is used instead: {0}", ex.Message);
            return asset;
        }

        if (!fits)
        {
            Log.LogWarning(null, "DEVASSETS002", null, source, 0, 0, 0, 0, "The Dev badge doesn't fit inside the {0} mask at scale {1} and may be clipped.", placement.Mask, placement.Scale.ToString(CultureInfo.InvariantCulture));
        }

        string badged = Publish(composed, root, Path.GetFileName(source));
        if (isIcon)
        {
            TaskItem icon = new(asset);
            icon.SetMetadata("ForegroundFile", badged);
            return icon;
        }

        return new TaskItem(badged, asset.CloneCustomMetadata());
    }

    static string Publish(string content, string root, string fileName)
    {
        byte[] bytes = Utf8.GetBytes(content);
        string hash;
        using (SHA256 sha = SHA256.Create())
        {
            hash = string.Concat(sha.ComputeHash(bytes).Take(6).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        string directory = Path.Combine(root, hash);
        string path = Path.Combine(directory, fileName);
        if (File.Exists(path))
        {
            return path;
        }

        // Android's inner builds compose the same asset from several project instances at once, so publish atomically.
        Directory.CreateDirectory(directory);
        string temp = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllBytes(temp, bytes);
        try
        {
            File.Move(temp, path);
        }
        catch (IOException)
        {
            // Another instance won the race with identical content.
            File.Delete(temp);
        }

        return path;
    }
}
```

- [ ] **Step 6: Run the tests and confirm they pass**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"`
Expected: PASS (all DevAssets tests).

- [ ] **Step 7: Run the whole suite**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`
Expected: PASS. Existing tests are unaffected.

- [ ] **Step 8: Commit**

```bash
git add src/DevAssets.Task.cs tests/AppTemplate.Core.Tests
git commit -m "feat(build): add the ComposeDevAsset MSBuild task"
```

---

### Task 4: Wire it into the build

**Files:**
- Create: `src/DevAssets.targets`
- Modify: `src/Directory.Build.targets`
- Modify: `src/AppTemplate/AppTemplate.csproj:38-40`
- Delete: `src/AppTemplate/Assets/Icons/icon_foreground_dev.svg`

**Interfaces:**
- Consumes: `ComposeDevAsset` (Task 3) with `Assets`, `Kind`, `TargetPlatform`, `ProjectDirectory`, `OutputRoot` → `Result`.
- Produces: the MSBuild target `GenerateDevAssets` and the opt-out property `GenerateDevAssets=false`.

- [ ] **Step 1: Create the targets file**

Create `src/DevAssets.targets`:

```xml
<Project>

    <!--
        Dev-channel icon and splash: the prod artwork with a DEV badge stamped on at build time, so an app only ever
        supplies prod artwork. Opt out with -p:GenerateDevAssets=false.

        Full documentation: docs/dev-assets.md
    -->

    <UsingTask TaskName="ComposeDevAsset"
               TaskFactory="RoslynCodeTaskFactory"
               AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll">
        <Task>
            <Code Type="Class" Language="cs" Source="$(MSBuildThisFileDirectory)DevAssets.Task.cs" />
        </Task>
    </UsingTask>

    <!--
        Evaluation is left alone: Uno.Sdk only creates UnoIcon / UnoSplashScreen when their files exist at evaluation
        time, which a build-generated file never does. The items are re-pointed here instead, just before Resizetizer
        collects them.

        Copies land in devassets/<content hash>/<original file name>. The hash because Resizetizer doesn't notice a
        changed file at an unchanged path; the original name because Resizetizer derives resource names from it.
    -->
    <Target Name="GenerateDevAssets"
            BeforeTargets="UnoResizetizeCollectItems"
            Condition="'$(AppChannel)' == 'Dev' and '$(GenerateDevAssets)' != 'false'">

        <ComposeDevAsset Condition="'@(UnoIcon)' != ''"
                         Assets="@(UnoIcon)"
                         Kind="Icon"
                         TargetPlatform="$(TargetPlatformIdentifier)"
                         ProjectDirectory="$(MSBuildProjectDirectory)"
                         OutputRoot="$(IntermediateOutputPath)devassets">
            <Output TaskParameter="Result" ItemName="_DevAssetIcon" />
        </ComposeDevAsset>

        <ComposeDevAsset Condition="'@(UnoSplashScreen)' != ''"
                         Assets="@(UnoSplashScreen)"
                         Kind="Splash"
                         TargetPlatform="$(TargetPlatformIdentifier)"
                         ProjectDirectory="$(MSBuildProjectDirectory)"
                         OutputRoot="$(IntermediateOutputPath)devassets">
            <Output TaskParameter="Result" ItemName="_DevAssetSplash" />
        </ComposeDevAsset>

        <ItemGroup Condition="'@(_DevAssetIcon)' != ''">
            <UnoIcon Remove="@(UnoIcon)" />
            <UnoIcon Include="@(_DevAssetIcon)" />
        </ItemGroup>

        <ItemGroup Condition="'@(_DevAssetSplash)' != ''">
            <UnoSplashScreen Remove="@(UnoSplashScreen)" />
            <UnoSplashScreen Include="@(_DevAssetSplash)" />
        </ItemGroup>
    </Target>

</Project>
```

- [ ] **Step 2: Import it**

In `src/Directory.Build.targets`, after the `WorktreeIdentity.targets` import, add:

```xml

    <!-- Dev-channel icon and splash, badged at build time. See docs/dev-assets.md. -->
    <Import Project="$(MSBuildThisFileDirectory)DevAssets.targets" />
```

- [ ] **Step 3: Remove the hand-made Dev icon**

In `src/AppTemplate/AppTemplate.csproj`, replace:

```xml
        <!-- Dev channel: override only the foreground (the logo). This keeps the
             generated icon resource name stable while giving Dev a distinct look. -->
        <UnoIconForegroundFile Condition="'$(AppChannel)' == 'Dev'">Assets/Icons/icon_foreground_dev.svg</UnoIconForegroundFile>
```

with:

```xml
        <!-- Dev builds badge the icon and splash below at build time (src/DevAssets.targets). -->
```

Then delete the file:

```bash
git rm src/AppTemplate/Assets/Icons/icon_foreground_dev.svg
```

- [ ] **Step 4: Build the desktop head (Dev) and check the output**

Run:

```bash
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop -c Debug -p:AppChannel=Dev
```

Expected: `Build succeeded` with no `DEVASSETS` warnings. `src/AppTemplate/obj/Debug/net10.0-desktop/devassets/` holds `icon_foreground.svg` and `splash_screen.svg` under folders named with 12 hex characters. The template's two SVGs are identical and desktop applies no mask, so both files may share one folder. Open `src/AppTemplate/obj/Debug/net10.0-desktop/unoresizetizer/r/Assets/Icons/icon_transparentLogo.targetsize-256.png` and `…/unoresizetizer/sp/splash_screen.scale-100.png` with the Read tool. **Both show a gold DEV pill top-right, and the logo is otherwise unchanged.**

- [ ] **Step 5: Build Prod and the opt-out, and check neither is badged**

Run:

```bash
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop -c Debug -p:AppChannel=Prod
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-desktop -c Debug -p:AppChannel=Dev -p:GenerateDevAssets=false
```

Expected: both succeed. After each, `icon_transparentLogo.targetsize-256.png` shows the plain logo with no badge.

- [ ] **Step 6: Build the Windows and Android heads**

Run:

```bash
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-windows10.0.26100 -c Debug -p:AppChannel=Dev
dotnet build src/AppTemplate/AppTemplate.csproj -f net10.0-android -c Debug -p:AppChannel=Dev
```

Expected: both succeed with no `DEVASSETS` warnings. Read `src/AppTemplate/obj/Debug/net10.0-android/unoresizetizer/AppIcons/mipmap-xxxhdpi/icon_foreground.png`. The badge sits **inside** the central circle, pulled down-left from the logo's corner.

- [ ] **Step 7: Commit**

```bash
git add src/DevAssets.targets src/Directory.Build.targets src/AppTemplate/AppTemplate.csproj
git commit -m "feat(build): generate the Dev icon and splash from the prod artwork"
```

(The `git rm` from Step 3 is already staged.)

---

### Task 5: Build verification script and self-test job

**Files:**
- Create: `scripts/verify-dev-assets.ps1`
- Modify: `.github/workflows/template-selftest.yml`

**Interfaces:**
- Consumes: the `GenerateDevAssets` target and property (Task 4), plus Resizetizer's output layout `obj/Debug/<tfm>/unoresizetizer/{r/Assets/Icons,sp,AppIcons}`.

- [ ] **Step 1: Write the script**

Create `scripts/verify-dev-assets.ps1`:

```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies the build-time DEV badge on the app icon and splash screen.

.DESCRIPTION
    Builds the app head as Dev, as Dev with GenerateDevAssets=false, and as Prod, and compares the PNGs
    Uno.Resizetizer renders. See docs/dev-assets.md.

    Runs in CI on the template repository only (.github/workflows/template-selftest.yml). Run it by hand after
    touching DevAssets.targets or DevAssets.Task.cs. -IncludeAndroid adds D5 and needs the Android workload.

.EXAMPLE
    pwsh scripts/verify-dev-assets.ps1 -IncludeAndroid
#>
[CmdletBinding()]
param(
    [string]$Project = 'src/AppTemplate/AppTemplate.csproj',
    [string]$TargetFramework = 'net10.0-desktop',
    [switch]$IncludeAndroid
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
Add-Type -AssemblyName System.Drawing

$script:failures = 0
$projectDir = Split-Path -Parent (Join-Path $repoRoot $Project)
$snapshots = Join-Path ([IO.Path]::GetTempPath()) "verify-dev-assets-$PID"
$badgeColor = 'FFB900'

function Get-ObjDir([string]$Tfm) {
    Join-Path $projectDir "obj/Debug/$Tfm"
}

function Invoke-Build {
    param([string]$Tfm, [string[]]$MSBuildArgs)

    $output = & dotnet build $Project -f $Tfm -c Debug -nologo @MSBuildArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $Tfm $($MSBuildArgs -join ' '):`n$($output | Out-String)"
    }
}

# Builds, then copies the named Resizetizer outputs aside so the next build can't overwrite them.
function Get-Render {
    param([string]$Label, [string]$Tfm, [string[]]$MSBuildArgs, [string[]]$Files)

    Invoke-Build -Tfm $Tfm -MSBuildArgs $MSBuildArgs
    $root = Join-Path (Get-ObjDir $Tfm) 'unoresizetizer'
    $dest = Join-Path $snapshots $Label
    New-Item -ItemType Directory -Force $dest | Out-Null
    $copies = @{}
    foreach ($file in $Files) {
        $copy = Join-Path $dest ($file -replace '[\\/]', '_')
        Copy-Item (Join-Path $root $file) $copy
        $copies[$file] = $copy
    }

    $names = Get-ChildItem $root -Recurse -File | ForEach-Object { [IO.Path]::GetRelativePath($root, $_.FullName) } | Sort-Object
    [pscustomobject]@{ Files = $copies; Names = $names }
}

# Pixels that differ between two renders: how many, whether their centroid is top-right, and their commonest colour.
function Compare-Render {
    param([string]$Badged, [string]$Plain)

    $a = [System.Drawing.Bitmap]::new($Badged)
    $b = [System.Drawing.Bitmap]::new($Plain)
    try {
        $count = 0
        $sumX = 0.0
        $sumY = 0.0
        $colors = @{}
        for ($y = 0; $y -lt $a.Height; $y++) {
            for ($x = 0; $x -lt $a.Width; $x++) {
                $p = $a.GetPixel($x, $y)
                if ($p.ToArgb() -ne $b.GetPixel($x, $y).ToArgb()) {
                    $count++
                    $sumX += $x
                    $sumY += $y
                    $key = '{0:X2}{1:X2}{2:X2}' -f $p.R, $p.G, $p.B
                    $colors[$key] = 1 + [int]$colors[$key]
                }
            }
        }

        $dominant = ''
        if ($count -gt 0) {
            $dominant = ($colors.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key
        }

        [pscustomobject]@{
            Changed  = $count
            TopRight = $count -gt 0 -and ($sumX / $count) -gt ($a.Width / 2) -and ($sumY / $count) -lt ($a.Height / 2)
            Dominant = $dominant
        }
    }
    finally {
        $a.Dispose()
        $b.Dispose()
    }
}

function Assert-That {
    param([string]$Label, [bool]$Condition, [string]$Detail = '')

    if ($Condition) {
        Write-Host ('  PASS  {0}' -f $Label) -ForegroundColor Green
    }
    else {
        Write-Host ('  FAIL  {0}' -f $Label) -ForegroundColor Red
        if ($Detail) {
            Write-Host ('          {0}' -f $Detail) -ForegroundColor Red
        }
        $script:failures++
    }
}

function Test-SameBytes([string]$Left, [string]$Right) {
    (Get-FileHash $Left).Hash -eq (Get-FileHash $Right).Hash
}

function Test-Badged($Diff) {
    $Diff.Changed -gt 0 -and $Diff.TopRight -and $Diff.Dominant -eq $badgeColor
}

try {
    $icon = 'r/Assets/Icons/icon_transparentLogo.targetsize-256.png'
    $splash = 'sp/splash_screen.scale-100.png'
    $files = @($icon, $splash)
    $obj = Get-ObjDir $TargetFramework

    # Start clean so D4 compares resource names from this run only.
    Remove-Item (Join-Path $obj 'unoresizetizer'), (Join-Path $obj 'devassets') -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host "Rendering $TargetFramework as Dev (opted out), Dev and Prod..."
    $plain = Get-Render -Label 'dev-optout' -Tfm $TargetFramework -MSBuildArgs @('-p:AppChannel=Dev', '-p:GenerateDevAssets=false') -Files $files
    $dev = Get-Render -Label 'dev' -Tfm $TargetFramework -MSBuildArgs @('-p:AppChannel=Dev') -Files $files
    Remove-Item (Join-Path $obj 'devassets') -Recurse -Force -ErrorAction SilentlyContinue
    $prod = Get-Render -Label 'prod' -Tfm $TargetFramework -MSBuildArgs @('-p:AppChannel=Prod') -Files $files

    foreach ($file in $files) {
        $diff = Compare-Render -Badged $dev.Files[$file] -Plain $plain.Files[$file]
        Assert-That "D1  Dev badges $file top-right" (Test-Badged $diff) "changed=$($diff.Changed) topRight=$($diff.TopRight) dominant=$($diff.Dominant)"
    }

    Assert-That 'D2  Prod writes nothing under devassets' (-not (Test-Path (Join-Path $obj 'devassets')))

    $renamed = Compare-Object @($plain.Names) @($dev.Names)
    Assert-That 'D4  Dev keeps every generated resource name' ($null -eq $renamed) ($renamed | Out-String)

    foreach ($file in $files) {
        Assert-That "D6  GenerateDevAssets=false renders $file exactly like Prod" (Test-SameBytes $plain.Files[$file] $prod.Files[$file])
    }

    # D3: change the foreground in place (a half-transparent veil over everything); an incremental build must re-render.
    New-Item -ItemType Directory -Force $snapshots | Out-Null
    $foreground = Join-Path $snapshots 'icon_foreground.svg'
    Copy-Item (Join-Path $projectDir 'Assets/Icons/icon_foreground.svg') $foreground
    $override = @('-p:AppChannel=Dev', "-p:UnoIconForegroundFile=$foreground")
    $before = Get-Render -Label 'd3-before' -Tfm $TargetFramework -MSBuildArgs $override -Files @($icon)
    $svg = Get-Content -Raw $foreground
    $veil = '<circle cx="0" cy="0" r="1000000" fill="#000000" fill-opacity="0.5" /></svg>'
    $svg.Substring(0, $svg.LastIndexOf('</svg>')) + $veil | Set-Content -NoNewline $foreground
    $after = Get-Render -Label 'd3-after' -Tfm $TargetFramework -MSBuildArgs ($override + '--no-restore') -Files @($icon)
    Assert-That 'D3  Editing the prod icon re-renders the Dev icon on an incremental build' (-not (Test-SameBytes $before.Files[$icon] $after.Files[$icon]))

    if ($IncludeAndroid) {
        Write-Host 'Rendering net10.0-android...'
        $foregroundPng = 'AppIcons/mipmap-xxxhdpi/icon_foreground.png'
        $splashXml = 'sp/drawable-v31/uno_splash_image.xml'
        $androidPlain = Get-Render -Label 'android-optout' -Tfm 'net10.0-android' -MSBuildArgs @('-p:AppChannel=Dev', '-p:GenerateDevAssets=false') -Files @($foregroundPng)
        $androidDev = Get-Render -Label 'android-dev' -Tfm 'net10.0-android' -MSBuildArgs @('-p:AppChannel=Dev') -Files @($foregroundPng, $splashXml)
        $diff = Compare-Render -Badged $androidDev.Files[$foregroundPng] -Plain $androidPlain.Files[$foregroundPng]
        $keepsSplash = (Get-Content -Raw $androidDev.Files[$splashXml]).Contains('@drawable/splash_screen')
        Assert-That 'D5  Android badges the adaptive icon and keeps @drawable/splash_screen' ((Test-Badged $diff) -and $keepsSplash) "changed=$($diff.Changed) topRight=$($diff.TopRight) dominant=$($diff.Dominant) keepsSplash=$keepsSplash"
    }
    else {
        Write-Host '  SKIP  D5  Android (pass -IncludeAndroid)' -ForegroundColor DarkGray
    }
}
finally {
    Remove-Item $snapshots -Recurse -Force -ErrorAction SilentlyContinue
    Pop-Location
}

if ($script:failures -gt 0) {
    Write-Host "$($script:failures) check(s) failed." -ForegroundColor Red
    exit 1
}

Write-Host 'All Dev asset checks passed.' -ForegroundColor Green
```

- [ ] **Step 2: Run it locally, including Android**

Run: `pwsh scripts/verify-dev-assets.ps1 -IncludeAndroid`
Expected: every line `PASS` (D1 ×2, D2, D4, D6 ×2, D3, D5), ending with `All Dev asset checks passed.` If a check fails, fix the cause in Task 1–4 code, not the check.

- [ ] **Step 3: Add the self-test job**

In `.github/workflows/template-selftest.yml`, add these entries to **both** `paths:` lists (push and pull_request), after `"src/WorktreeIdentity.targets"`:

```yaml
      - "src/DevAssets.targets"
      - "src/DevAssets.Task.cs"
      - "src/AppTemplate/Assets/Icons/**"
      - "src/AppTemplate/Assets/Splash/**"
      - "tests/AppTemplate.Core.Tests/DevAssets/**"
      - "scripts/verify-dev-assets.ps1"
```

Then append this job after `worktree_identity`:

```yaml

  dev_assets:
    name: Dev icon and splash badge
    if: github.repository == 'MartinZikmund/uno-app-template'
    runs-on: windows-latest
    timeout-minutes: 20
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Unit tests
        shell: pwsh
        run: dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"

      # Desktop head only: it needs no workload and renders the same icon and splash outputs as the Windows head.
      - name: Verify rendered assets
        shell: pwsh
        run: ./scripts/verify-dev-assets.ps1
```

- [ ] **Step 4: Commit**

```bash
git add scripts/verify-dev-assets.ps1 .github/workflows/template-selftest.yml
git commit -m "ci: verify the generated Dev icon and splash"
```

---

### Task 6: Documentation

**Files:**
- Create: `docs/dev-assets.md`
- Modify: `docs/README.md`
- Modify: `docs/versioning.md:23`
- Modify: `docs/versioning-migration.md:66-68`
- Modify: `docs/worktree-identity.md` (the "Icons are identical across worktrees" limitation)
- Modify: `README.md` (the "Side-by-side Dev builds" row and step 4)

- [ ] **Step 1: Write `docs/dev-assets.md`**

```markdown
# Generated Dev icon and splash

Replace `src/AppTemplate/Assets/Icons/icon_foreground.svg` and `src/AppTemplate/Assets/Splash/splash_screen.svg` with your
own artwork, and you're done. Every Dev-channel build stamps a **DEV** badge onto both, and Prod builds use them untouched.
There is no Dev artwork to draw, commit, or keep in sync.

## What you see

The badge copies the in-app `DevChannelBadge` from the title bar: a gold (`#FFB900`) pill with dark "DEV" lettering,
24% of the image tall, in the top-right corner.

| Where | Dev build |
|---|---|
| Windows Start menu, taskbar, Alt-Tab | flush in the top-right corner |
| Desktop window icon, WebAssembly favicon | flush in the top-right corner |
| Android launcher | pulled towards the centre, into the 66dp safe zone that every launcher mask keeps |
| Android 12+ splash | pulled inside the circle the system crops the splash icon to |
| iOS home screen | pulled clear of the rounded corner |
| Splash on Windows, Desktop, WebAssembly, iOS | flush in the top-right corner of the logo |

At 16–24px the word is unreadable, but the gold corner still marks the build, the way a notification dot does.

The worktree name is deliberately **not** on the icon: at taskbar sizes it would be 1–3px tall. Worktrees show their name
as text instead. See [worktree-identity.md](./worktree-identity.md).

## How it works

[`src/DevAssets.targets`](../src/DevAssets.targets) runs `GenerateDevAssets` just before Uno.Resizetizer collects its
items. It runs on the Dev channel only, **including CI**, so Dev packages built from `main` carry the badge.

1. It takes the `UnoIcon` foreground and the `UnoSplashScreen` image exactly as Uno.Sdk created them from
   `UnoIconForegroundFile` and `UnoSplashScreenFile`.
2. `ComposeDevAsset` ([`src/DevAssets.Task.cs`](../src/DevAssets.Task.cs)) nests each SVG unchanged inside a wrapper and
   draws the badge on top. MSBuild's `RoslynCodeTaskFactory` compiles it, so no extra tool is needed.
3. The result goes to `obj/<config>/<tfm>/devassets/<content hash>/<original file name>`, and the items are pointed at it.

Two details hold this together:

- **The content hash is in the folder name** because Resizetizer doesn't notice when a file changes at the same path. It
  records the foreground's *path*, not its timestamp. A new folder is a new path.
- **The file name is unchanged** because Resizetizer derives resource names from it (`@drawable/splash_screen`, the
  Windows manifest's splash entry). Nothing downstream sees a difference.

"DEV" is drawn with outlines from Selawik Semibold, Microsoft's open-source (SIL OFL 1.1) stand-in for Segoe UI. It
therefore looks the same whichever machine builds it; the Linux and macOS CI runners don't have Segoe UI.

The badge's position comes from each image's own scale metadata, read the way Resizetizer reads it:
`AndroidForegroundScale` over `ForegroundScale`, `AndroidScale` over `Scale`, and so on. Changing
`UnoIconForegroundScale` or `UnoSplashScreenScale` moves it correctly.

## Turning it off

| You want | Do this |
|---|---|
| The prod artwork on a Dev build | `dotnet build … -p:GenerateDevAssets=false`, or set the property in the csproj |
| A hand-drawn Dev icon | set `GenerateDevAssets` to `false` and add `<UnoIconForegroundFile Condition="'$(AppChannel)' == 'Dev'">Assets/Icons/my_dev_icon.svg</UnoIconForegroundFile>` |

With generation left on, a Dev-specific foreground gets badged too.

## Warnings

| Code | Meaning |
|---|---|
| `DEVASSETS001` | The SVG is missing, malformed, or has no usable `viewBox`, `width` or `height`. The build uses the prod artwork. |
| `DEVASSETS002` | The badge can't fit inside the platform mask at this scale (e.g. a very large `UnoIconForegroundScale`), so it may be clipped. |

A cosmetic badge never fails the build.

## Verifying

```powershell
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj --filter "FullyQualifiedName~DevAssets"
pwsh scripts/verify-dev-assets.ps1                   # desktop head
pwsh scripts/verify-dev-assets.ps1 -IncludeAndroid   # adds D5; needs the Android workload
```

On the template repository, [`template-selftest.yml`](../.github/workflows/template-selftest.yml) runs both whenever the
Dev asset files or the artwork change.

| # | Guarantee |
|---|---|
| D1 | A Dev build badges the icon and splash, top-right, in `#FFB900` |
| D2 | A Prod build writes nothing under `devassets` |
| D3 | Editing the prod icon re-renders the Dev icon on an incremental build |
| D4 | Every generated resource keeps its name |
| D5 | Android badges the adaptive icon and still references `@drawable/splash_screen` |
| D6 | `GenerateDevAssets=false` renders exactly what Prod renders |

## Limitations

- **iOS placement is covered by unit tests only.** The iOS head can't be built on Windows; check it on a Mac.
- **The Android 12+ splash circle is derived, not observed.** It comes from Resizetizer's 108dp splash drawable and
  Android's 72dp visible circle. Confirm it on a device if you change `UnoSplashScreenScale` a lot.

## See also

- [versioning.md](./versioning.md): the Dev/Prod `AppChannel` model this hooks into.
- [worktree-identity.md](./worktree-identity.md): why the worktree name stays out of the icon.
- [Design spec](./superpowers/specs/2026-09-22-dev-assets-design.md): the decisions and the spike behind them.
```

- [ ] **Step 2: Index it**

In `docs/README.md`, under `## Building & tooling`, insert after the `building.md` line:

```markdown
- [dev-assets.md](./dev-assets.md) — the Dev-channel icon and splash, badged at build time from your prod artwork.
```

- [ ] **Step 3: Update `docs/versioning.md`**

Replace the `- **App icon:** …` bullet (line 23) with:

```markdown
- **App icon and splash:** Dev builds stamp a DEV badge onto the prod artwork at build time, so there is no Dev artwork to maintain. See [dev-assets.md](./dev-assets.md).
```

- [ ] **Step 4: Update `docs/versioning-migration.md` step 5**

Replace the heading and first paragraph of step 5 (keep the `> **Do not** override UnoIconBackgroundFile…` note that follows):

```markdown
### 5. Get a Dev icon

Copy `src/DevAssets.targets` and `src/DevAssets.Task.cs` from the template and import the targets from your `src/Directory.Build.targets` (`<Import Project="$(MSBuildThisFileDirectory)DevAssets.targets" />`). Dev builds then badge your existing icon and splash automatically. See [dev-assets.md](./dev-assets.md).
```

- [ ] **Step 5: Update the worktree limitation**

In `docs/worktree-identity.md`, replace the bullet that starts `- **Icons are identical across worktrees.**` (and its continuation lines) with:

```markdown
- **Icons are identical across worktrees.** Every Dev build gets the same generated DEV badge
  ([dev-assets.md](./dev-assets.md)). A worktree name would be 1–3px tall at taskbar sizes, so it stays in the text labels.
```

- [ ] **Step 6: Update `README.md`**

In the "What's in the box" table, in the **Side-by-side Dev builds** row, replace `distinct icons included` with
`with a DEV badge generated onto your icon and splash ([docs/dev-assets.md](./docs/dev-assets.md))`.

In "Using this template", step 4, append this sentence to the end of the paragraph:

```markdown
   Dev builds badge them automatically, so there's no Dev variant to draw. See [docs/dev-assets.md](./docs/dev-assets.md).
```

- [ ] **Step 7: Commit**

```bash
git add docs README.md
git commit -m "docs: document the generated Dev icon and splash"
```

---

### Task 7: End-to-end check

**Files:** none (verification only)

- [ ] **Step 1: Full test suite**

Run: `dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`
Expected: all tests pass.

- [ ] **Step 2: Packaged Windows app**

Follow `.claude/skills/run-winui-app/SKILL.md`: build `net10.0-windows10.0.26100` (Dev), launch it with `winapp run … --detach --json`, and confirm it starts. Then find the installed package's logo:

```powershell
$pkg = Get-AppxPackage | Where-Object { $_.InstallLocation -eq (Resolve-Path src\AppTemplate\bin\Debug\net10.0-windows10.0.26100).Path }
Get-ChildItem $pkg.InstallLocation\Assets\Icons -Filter 'icon_transparentLogo.targetsize-256.png'
```

Read that PNG. It shows the badge. Screenshot the window with `winapp ui screenshot -a <pid> --output .screenshots\dev-assets.png`, then stop the PID and `winapp unregister` as the skill describes.

- [ ] **Step 3: Android PNGs**

Read `src/AppTemplate/obj/Debug/net10.0-android/unoresizetizer/AppIcons/mipmap-xxxhdpi/icon_foreground.png` and `…/mipmap-xxxhdpi/icon.png`. The badge is inside the central circle.

- [ ] **Step 4: Mark the spec implemented**

In `docs/superpowers/specs/2026-09-22-dev-assets-design.md`, change `**Status:** Proposed` to `**Status:** Implemented`.

```bash
git add docs/superpowers/specs/2026-09-22-dev-assets-design.md
git commit -m "docs: mark the Dev assets design implemented"
```
