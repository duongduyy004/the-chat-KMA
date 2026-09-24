# Sprint Mobile UI Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the Sprint minigame's HUD, controls, start flow and player identity so the screen reads as a finished Android landscape mobile game, without changing any gameplay behavior.

**Architecture:** Three new pure, stateless units — `SprintUiTheme` (design tokens), `SprintUiShapes` (procedurally generated rounded 9-slice sprites) and a rewritten `SprintUiLayout` (every rect as a function of the safe-area rect) — are consumed by a thinned `SprintFestivalPresentation` assembler. Control visuals become children of the existing `ScreenTapArea` objects so the button *is* the hit area. `SprintController`, `SprintRules`, `GameplayInputRouter`, `MinigameLifecycle` and `ResultPanel` are untouched.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, uGUI, TextMeshPro, Unity Test Framework `1.6.0` (EditMode + PlayMode), Android landscape.

**Spec:** `docs/superpowers/specs/2026-09-14-sprint-ui-mobile-polish-design.md`

## Global Constraints

- Run every shell command through `rtk`.
- Unity is `6000.3.23f1`. Set `KMA_UNITY_EDITOR=/path/to/Unity` once per shell before any test command.
- Work in the current checkout. Do not create a worktree unless the user explicitly says so.
- Do not change `SprintRules`, `SprintController` pacing/stamina/wind timing, the 14-second limit, pass/fail conditions, score/rank calculation, `MinigameLifecycle` semantics, `GameplayInputRouter`, or `ResultPanel`'s event contract.
- `ScreenTapArea` stays the only component that forwards Sprint input. No presenter may raise `OnLeftTap`, `OnRightTap`, or router events.
- All user-facing copy is Vietnamese. Control labels are exactly `TRÁI` and `PHẢI`. The instruction is exactly `BẤM TRÁI VÀ PHẢI LUÂN PHIÊN ĐỂ CHẠY`. Countdown is `3`, `2`, `1`, `GO!`.
- No new package. No new bitmap, font, or material asset — rounded shapes are generated at runtime.
- Element **sizes** are fractions of safe-area **height** (`H`). Element **positions** may use width (`W`) and height.
- Minimum font size is `24`. Nothing smaller ships.
- Test runs regenerate `Assets/_Project/Fonts/Nunito-Bold.asset` (the dynamic TMP atlas). **Revert it, never commit it:** `rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset`.
- Supported landscape aspects for layout tests: 16:9, 16:10, 18:9, 19.5:9, 20:9, plus a notch inset. 16:10 is the narrowest supported; do not add 4:3.

---

## File Structure

| File | Responsibility |
|---|---|
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs` | Color, type scale, radius, spacing tokens + contrast helper. No scene access. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs` | Generates and caches antialiased rounded-rect `Sprite`s with 9-slice borders. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs` | Every on-screen rect as a pure function of the safe-area rect, plus lane centres and the finish threshold. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs` | Assembles the hierarchy from theme + shapes + layout and wires presenters. Holds no layout constants. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs` | Control visual state machine and press feedback. Owns no input. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs` | Tutorial gate, countdown, persistent instruction. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs` | Populates distance / rank / combo / rail fill from the controller. |
| `Assets/Editor/SprintSceneConfigurator.cs` | Adds a repeatable Editor command that deletes the legacy `SprintMetrics` group from the scene. |
| `Assets/_Project/Scenes/MG_Sprint.unity` | Legacy metrics removed; `SprintHud` references cleared. |
| `Assets/Tests/EditMode/Presentation/SprintUiThemeTests.cs` | Token invariants: font floor, contrast ratios. |
| `Assets/Tests/EditMode/Presentation/SprintUiShapesTests.cs` | Generated sprite geometry and caching. |
| `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs` | Rect geometry, symmetry, containment, and the pairwise overlap matrix. |
| `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs` | Real-scene hierarchy, controls, start flow, player identity, text overflow. |
| `Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs` | Updated Sprint chrome expectations. |

New `.cs` files each need a sibling `.cs.meta`. Unity generates these when the Editor next imports; if working headless, run any test command once and commit the generated `.meta` alongside its `.cs`.

---

### Task 1: Design tokens

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs`
- Create: `Assets/Tests/EditMode/Presentation/SprintUiThemeTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `SprintUiTheme` static class with `Color Surface, TextPrimary, Accent, Player, Energy`; `const float Display=160, Title=54, Headline=48, BodyLarge=40, Body=32, Caption=24, MinimumFontSize=24`; `const float RadiusPanel=24, RadiusControl=36, RadiusPause=20, BorderWidth=3`; `const float SpaceXs=8, SpaceSm=16, SpaceMd=24, SpaceLg=32`; `Color ShadowColor`; `Vector2 ShadowOffset`; `static Color WithAlpha(Color color, float alpha)`; `static float ContrastRatio(Color a, Color b)`; `static float[] AllFontSizes()`; `static Color[] TextColors()`.

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/Presentation/SprintUiThemeTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiThemeTests
    {
        [Test]
        public void EveryFontStep_MeetsTheMinimumSize()
        {
            foreach (float size in SprintUiTheme.AllFontSizes())
                Assert.That(size, Is.GreaterThanOrEqualTo(SprintUiTheme.MinimumFontSize),
                    $"Font step {size} is below the {SprintUiTheme.MinimumFontSize} floor.");
        }

        [Test]
        public void EveryTextColor_ReachesReadableContrastOnSurface()
        {
            foreach (Color color in SprintUiTheme.TextColors())
                Assert.That(SprintUiTheme.ContrastRatio(color, SprintUiTheme.Surface),
                    Is.GreaterThanOrEqualTo(4.5f), $"Colour {color} is unreadable on Surface.");
        }

        [Test]
        public void ContrastRatio_IsSymmetricAndBoundedByTheWcagRange()
        {
            float forward = SprintUiTheme.ContrastRatio(Color.white, Color.black);
            float reverse = SprintUiTheme.ContrastRatio(Color.black, Color.white);
            Assert.That(forward, Is.EqualTo(reverse).Within(.001f));
            Assert.That(forward, Is.EqualTo(21f).Within(.05f));
            Assert.That(SprintUiTheme.ContrastRatio(Color.white, Color.white), Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void PlayerAccent_IsDistinctFromEveryOtherToken()
        {
            Assert.That(SprintUiTheme.Player, Is.Not.EqualTo(SprintUiTheme.Accent));
            Assert.That(SprintUiTheme.Player, Is.Not.EqualTo(SprintUiTheme.Energy));
            Assert.That(SprintUiTheme.Player, Is.Not.EqualTo(SprintUiTheme.TextPrimary));
        }

        [Test]
        public void WithAlpha_ReplacesAlphaAndClamps()
        {
            Assert.That(SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .42f).a, Is.EqualTo(.42f).Within(.001f));
            Assert.That(SprintUiTheme.WithAlpha(SprintUiTheme.Surface, 2f).a, Is.EqualTo(1f).Within(.001f));
            Assert.That(SprintUiTheme.WithAlpha(SprintUiTheme.Surface, -1f).a, Is.EqualTo(0f).Within(.001f));
        }
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Presentation.SprintUiThemeTests' \
  -testResults /tmp/kma-theme-red.xml -logFile /tmp/kma-theme-red.log
```

Expected: compile failure — `SprintUiTheme` does not exist.

- [ ] **Step 3: Write the implementation**

Create `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs`:

```csharp
using UnityEngine;

namespace KMA.Gameplay
{
    /// Design tokens for the Sprint minigame HUD. Pure data: no scene access.
    public static class SprintUiTheme
    {
        public static readonly Color Surface = new Color32(8, 35, 61, 255);
        public static readonly Color TextPrimary = new Color32(255, 249, 231, 255);
        public static readonly Color Accent = new Color32(255, 202, 58, 255);
        public static readonly Color Player = new Color32(58, 230, 255, 255);
        public static readonly Color Energy = new Color32(255, 89, 94, 255);

        public const float Display = 160f;
        public const float Title = 54f;
        public const float Headline = 48f;
        public const float BodyLarge = 40f;
        public const float Body = 32f;
        public const float Caption = 24f;
        public const float MinimumFontSize = 24f;

        public const float RadiusPanel = 24f;
        public const float RadiusControl = 36f;
        public const float RadiusPause = 20f;
        public const float BorderWidth = 3f;

        public const float SpaceXs = 8f;
        public const float SpaceSm = 16f;
        public const float SpaceMd = 24f;
        public const float SpaceLg = 32f;

        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, .35f);
        public static readonly Vector2 ShadowOffset = new Vector2(0f, -4f);

        public static float[] AllFontSizes() =>
            new[] { Display, Title, Headline, BodyLarge, Body, Caption };

        public static Color[] TextColors() =>
            new[] { TextPrimary, Accent, Player, Energy };

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// WCAG 2.1 relative-luminance contrast ratio, from 1 to 21.
        public static float ContrastRatio(Color a, Color b)
        {
            float first = RelativeLuminance(a);
            float second = RelativeLuminance(b);
            float lighter = Mathf.Max(first, second);
            float darker = Mathf.Min(first, second);
            return (lighter + .05f) / (darker + .05f);
        }

        static float RelativeLuminance(Color color) =>
            .2126f * Channel(color.r) + .7152f * Channel(color.g) + .0722f * Channel(color.b);

        static float Channel(float value) =>
            value <= .03928f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
    }
}
```

- [ ] **Step 4: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-theme-green.xml`. Expected: 5 tests, 0 failed. Open the XML and read the `total`/`failed` attributes — do not trust exit status alone.

- [ ] **Step 5: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs* \
  Assets/Tests/EditMode/Presentation/SprintUiThemeTests.cs*
rtk git commit -m "feat: add sprint UI design tokens"
```

---

### Task 2: Procedural rounded shapes

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs`
- Create: `Assets/Tests/EditMode/Presentation/SprintUiShapesTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1 at runtime; radii come from `SprintUiTheme` at call sites.
- Produces: `SprintUiShapes` static class with `static Sprite RoundedRect(int radius)` and `static void ClearCacheForTest()`.

Radius `0` is legal and returns a fully opaque 4×4 square sprite with a zero border, so callers never branch.

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/Presentation/SprintUiShapesTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiShapesTests
    {
        [SetUp]
        public void ResetCache() => SprintUiShapes.ClearCacheForTest();

        [Test]
        public void RoundedRect_CarriesA9SliceBorderEqualToTheRadius()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(24);
            Assert.That(sprite.border, Is.EqualTo(new Vector4(24f, 24f, 24f, 24f)));
        }

        [Test]
        public void RoundedRect_IsTransparentAtTheCornerAndOpaqueAtTheCentre()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(24);
            Texture2D texture = sprite.texture;
            Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(.01f), "corner must be cut away");
            Assert.That(texture.GetPixel(texture.width / 2, texture.height / 2).a,
                Is.EqualTo(1f).Within(.01f), "centre must be solid");
        }

        [Test]
        public void RoundedRect_IsOpaqueAtEachEdgeMidpoint()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(24);
            Texture2D texture = sprite.texture;
            int mid = texture.width / 2;
            Assert.That(texture.GetPixel(mid, 0).a, Is.EqualTo(1f).Within(.01f));
            Assert.That(texture.GetPixel(mid, texture.height - 1).a, Is.EqualTo(1f).Within(.01f));
            Assert.That(texture.GetPixel(0, mid).a, Is.EqualTo(1f).Within(.01f));
            Assert.That(texture.GetPixel(texture.width - 1, mid).a, Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_HasASoftenedCornerRatherThanAHardStep()
        {
            Texture2D texture = SprintUiShapes.RoundedRect(24).texture;
            // Walking the diagonal out of the corner must cross at least one partial pixel.
            bool sawPartial = false;
            for (int i = 0; i < 24; i++)
            {
                float alpha = texture.GetPixel(i, i).a;
                if (alpha > .05f && alpha < .95f)
                    sawPartial = true;
            }
            Assert.That(sawPartial, Is.True, "corner should be antialiased, not a hard step");
        }

        [Test]
        public void RoundedRect_ReturnsTheCachedInstanceForTheSameRadius()
        {
            Sprite first = SprintUiShapes.RoundedRect(36);
            Sprite second = SprintUiShapes.RoundedRect(36);
            Assert.That(second, Is.SameAs(first));
            Assert.That(SprintUiShapes.RoundedRect(24), Is.Not.SameAs(first));
        }

        [Test]
        public void RoundedRect_AcceptsZeroRadiusAsAPlainSquare()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(0);
            Assert.That(sprite.border, Is.EqualTo(Vector4.zero));
            Assert.That(sprite.texture.GetPixel(0, 0).a, Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_RejectsNegativeRadius() =>
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SprintUiShapes.RoundedRect(-1));
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Presentation.SprintUiShapesTests' \
  -testResults /tmp/kma-shapes-red.xml -logFile /tmp/kma-shapes-red.log
```

Expected: compile failure — `SprintUiShapes` does not exist.

- [ ] **Step 3: Write the implementation**

Create `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    /// Generates rounded-rect sprites at runtime so the Sprint HUD needs no bitmap assets.
    /// Every sprite is white; callers tint through Image.color.
    public static class SprintUiShapes
    {
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite RoundedRect(int radius)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Corner radius cannot be negative.");

            if (Cache.TryGetValue(radius, out Sprite cached) && cached != null)
                return cached;

            int size = radius > 0 ? radius * 2 + 2 : 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"SprintRoundedRect{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(Coverage(x, y, size, radius)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[radius] = sprite;
            return sprite;
        }

        public static void ClearCacheForTest() => Cache.Clear();

        /// Signed coverage of one pixel by the rounded rectangle, antialiased over one pixel.
        static float Coverage(int x, int y, int size, int radius)
        {
            if (radius == 0)
                return 1f;

            float pixelX = x + .5f;
            float pixelY = y + .5f;
            float dx = Mathf.Max(radius - pixelX, pixelX - (size - radius), 0f);
            float dy = Mathf.Max(radius - pixelY, pixelY - (size - radius), 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return radius - distance + .5f;
        }
    }
}
```

- [ ] **Step 4: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-shapes-green.xml`. Expected: 7 tests, 0 failed.

- [ ] **Step 5: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs* \
  Assets/Tests/EditMode/Presentation/SprintUiShapesTests.cs*
rtk git commit -m "feat: generate sprint rounded shapes at runtime"
```

---

### Task 3: Responsive layout with a proven overlap matrix

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs` (full rewrite)
- Modify: `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs` (full rewrite)

**Interfaces:**
- Consumes: nothing.
- Produces: `SprintUiLayout` static class with `const float FinishRevealDistance = 70f`; `static float LaneCenter01(int laneIndex, int laneCount)`; `static bool FinishVisible(float distance)`; `static Rect ProgressRailRect(Rect safe)`, `ScoreboardRect(Rect safe)`, `ModeChipRect(Rect safe)`, `PauseRect(Rect safe)`, `ControlRect(Rect safe, bool left)`, `CountdownRect(Rect safe)`, `InstructionRect(Rect safe)`; `readonly struct NamedRect { string Name; Rect Rect; }`; `static NamedRect[] RaceRects(Rect safe)`; `static NamedRect[] StartStateRects(Rect safe)`.

`VisibleControlRect` and `HitAreaRect` are **deleted**. The one rect is `ControlRect`. The existing test `Controls_AreSymmetricSmallerThanHitAreasAndDisjoint` asserted visual < hit area and is replaced — that assertion is the bug this task removes.

- [ ] **Step 1: Write the failing test**

Replace the entire contents of `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiLayoutTests
    {
        static IEnumerable<TestCaseData> LandscapeSafeAreas()
        {
            yield return new TestCaseData(new Rect(0f, 0f, 1920f, 1080f)).SetName("16x9");
            yield return new TestCaseData(new Rect(0f, 0f, 1728f, 1080f)).SetName("16x10");
            yield return new TestCaseData(new Rect(0f, 0f, 2160f, 1080f)).SetName("18x9");
            yield return new TestCaseData(new Rect(0f, 0f, 2340f, 1080f)).SetName("19.5x9");
            yield return new TestCaseData(new Rect(0f, 0f, 2400f, 1080f)).SetName("20x9");
            yield return new TestCaseData(new Rect(120f, 0f, 2232f, 1080f)).SetName("20x9-notch");
        }

        [TestCase(0, 0.125f)]
        [TestCase(1, 0.375f)]
        [TestCase(2, 0.625f)]
        [TestCase(3, 0.875f)]
        public void LaneCenter01_CentersFourEqualLanes(int lane, float expected) =>
            Assert.That(SprintUiLayout.LaneCenter01(lane, 4), Is.EqualTo(expected).Within(.0001f));

        [TestCase(69.9f, false)]
        [TestCase(70f, true)]
        [TestCase(100f, true)]
        public void FinishVisible_UsesApprovedThreshold(float distance, bool expected) =>
            Assert.That(SprintUiLayout.FinishVisible(distance), Is.EqualTo(expected));

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void NoTwoElementsOverlapDuringTheStartState(Rect safe)
        {
            SprintUiLayout.NamedRect[] rects = SprintUiLayout.StartStateRects(safe);
            for (int i = 0; i < rects.Length; i++)
            {
                for (int j = i + 1; j < rects.Length; j++)
                {
                    Assert.That(rects[i].Rect.Overlaps(rects[j].Rect), Is.False,
                        $"{rects[i].Name} overlaps {rects[j].Name} at {safe.width}x{safe.height}");
                }
            }
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void NoTwoElementsOverlapDuringTheRace(Rect safe)
        {
            SprintUiLayout.NamedRect[] rects = SprintUiLayout.RaceRects(safe);
            for (int i = 0; i < rects.Length; i++)
            {
                for (int j = i + 1; j < rects.Length; j++)
                {
                    Assert.That(rects[i].Rect.Overlaps(rects[j].Rect), Is.False,
                        $"{rects[i].Name} overlaps {rects[j].Name} at {safe.width}x{safe.height}");
                }
            }
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void EveryElementStaysInsideTheSafeAreaWithEdgePadding(Rect safe)
        {
            float padX = safe.width * .02f;
            float padY = safe.height * .01f;
            foreach (SprintUiLayout.NamedRect named in SprintUiLayout.StartStateRects(safe))
            {
                Assert.That(named.Rect.xMin, Is.GreaterThanOrEqualTo(safe.xMin + padX - .01f), $"{named.Name} left");
                Assert.That(named.Rect.xMax, Is.LessThanOrEqualTo(safe.xMax - padX + .01f), $"{named.Name} right");
                Assert.That(named.Rect.yMin, Is.GreaterThanOrEqualTo(safe.yMin + padY - .01f), $"{named.Name} bottom");
                Assert.That(named.Rect.yMax, Is.LessThanOrEqualTo(safe.yMax - padY + .01f), $"{named.Name} top");
            }
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void ControlsAreSymmetricEqualSizedAndNeverCrossTheMidpoint(Rect safe)
        {
            Rect left = SprintUiLayout.ControlRect(safe, true);
            Rect right = SprintUiLayout.ControlRect(safe, false);
            Assert.That(left.width, Is.EqualTo(right.width).Within(.01f));
            Assert.That(left.height, Is.EqualTo(right.height).Within(.01f));
            Assert.That(left.yMin, Is.EqualTo(right.yMin).Within(.01f));
            Assert.That(left.xMin - safe.xMin, Is.EqualTo(safe.xMax - right.xMax).Within(.01f));
            Assert.That(left.xMax, Is.LessThan(safe.center.x));
            Assert.That(right.xMin, Is.GreaterThan(safe.center.x));
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void ElementSizesDependOnHeightOnly(Rect safe)
        {
            var reference = new Rect(0f, 0f, 1920f, safe.height);
            Assert.That(SprintUiLayout.ControlRect(safe, true).size,
                Is.EqualTo(SprintUiLayout.ControlRect(reference, true).size));
            Assert.That(SprintUiLayout.ScoreboardRect(safe).size,
                Is.EqualTo(SprintUiLayout.ScoreboardRect(reference).size));
            Assert.That(SprintUiLayout.PauseRect(safe).size,
                Is.EqualTo(SprintUiLayout.PauseRect(reference).size));
        }

        [Test]
        public void PauseIsSquareAndScoreboardSitsAboveTheTopLane()
        {
            var safe = new Rect(0f, 0f, 1920f, 1080f);
            Rect pause = SprintUiLayout.PauseRect(safe);
            Assert.That(pause.width, Is.EqualTo(pause.height).Within(.01f));
            Assert.That(SprintUiLayout.ScoreboardRect(safe).yMin,
                Is.GreaterThan(safe.yMin + safe.height * .76f), "scoreboard must clear the top lane");
        }

        [Test]
        public void RaceRectsAreTheStartStateRectsWithoutCountdownAndInstruction()
        {
            var safe = new Rect(0f, 0f, 1920f, 1080f);
            var raceNames = new List<string>();
            foreach (SprintUiLayout.NamedRect named in SprintUiLayout.RaceRects(safe))
                raceNames.Add(named.Name);
            Assert.That(raceNames, Does.Not.Contain("Countdown"));
            Assert.That(raceNames, Does.Not.Contain("Instruction"));
            Assert.That(raceNames, Has.Count.EqualTo(6));
            Assert.That(SprintUiLayout.StartStateRects(safe), Has.Length.EqualTo(8));
        }
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Presentation.SprintUiLayoutTests' \
  -testResults /tmp/kma-layout-red.xml -logFile /tmp/kma-layout-red.log
```

Expected: compile failure — `NamedRect`, `RaceRects`, `StartStateRects`, `ControlRect` do not exist.

- [ ] **Step 3: Write the implementation**

Replace the entire contents of `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs`:

```csharp
using UnityEngine;

namespace KMA.Gameplay
{
    /// Pure responsive geometry for the Sprint HUD.
    /// Sizes are fractions of safe-area HEIGHT so every panel and button keeps the same
    /// physical size across landscape aspects; only the gaps between them stretch.
    public static class SprintUiLayout
    {
        public const float FinishRevealDistance = 70f;

        const float EdgeX = .02f;   // horizontal inset, fraction of width
        const float RailInsetX = .03f;
        const float RailTop = .985f;
        const float RailHeight = .03f;
        const float ClusterTop = .94f;
        const float ScoreboardWidth = .50f;
        const float ScoreboardHeight = .15f;
        const float ModeChipWidth = .36f;
        const float ModeChipHeight = .05f;
        const float ModeChipBottom = .895f;
        const float PauseSize = .089f;
        const float ControlWidth = .43f;
        const float ControlHeight = .26f;
        const float ControlBottom = .04f;
        const float CountdownWidth = .53f;
        const float CountdownHeight = .32f;
        const float CountdownCenterY = .62f;
        const float InstructionWidth = 1.07f;
        const float InstructionHeight = .10f;
        const float InstructionCenterY = .38f;

        public readonly struct NamedRect
        {
            public readonly string Name;
            public readonly Rect Rect;

            public NamedRect(string name, Rect rect)
            {
                Name = name;
                Rect = rect;
            }
        }

        public static float LaneCenter01(int laneIndex, int laneCount)
        {
            if (laneCount <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(laneCount));
            return (Mathf.Clamp(laneIndex, 0, laneCount - 1) + .5f) / laneCount;
        }

        public static bool FinishVisible(float distance) => distance >= FinishRevealDistance;

        public static Rect ProgressRailRect(Rect safe) => new Rect(
            safe.xMin + safe.width * RailInsetX,
            safe.yMin + safe.height * (RailTop - RailHeight),
            safe.width * (1f - RailInsetX * 2f),
            safe.height * RailHeight);

        public static Rect ScoreboardRect(Rect safe) => new Rect(
            safe.xMin + safe.width * EdgeX,
            safe.yMin + safe.height * (ClusterTop - ScoreboardHeight),
            safe.height * ScoreboardWidth,
            safe.height * ScoreboardHeight);

        public static Rect ModeChipRect(Rect safe) => new Rect(
            safe.center.x - safe.height * ModeChipWidth * .5f,
            safe.yMin + safe.height * ModeChipBottom,
            safe.height * ModeChipWidth,
            safe.height * ModeChipHeight);

        public static Rect PauseRect(Rect safe) => new Rect(
            safe.xMax - safe.width * EdgeX - safe.height * PauseSize,
            safe.yMin + safe.height * ClusterTop - safe.height * PauseSize,
            safe.height * PauseSize,
            safe.height * PauseSize);

        public static Rect ControlRect(Rect safe, bool left)
        {
            float width = safe.height * ControlWidth;
            float x = left
                ? safe.xMin + safe.width * EdgeX
                : safe.xMax - safe.width * EdgeX - width;
            return new Rect(x, safe.yMin + safe.height * ControlBottom, width, safe.height * ControlHeight);
        }

        public static Rect CountdownRect(Rect safe) => Centered(safe, CountdownWidth, CountdownHeight, CountdownCenterY);

        public static Rect InstructionRect(Rect safe) =>
            Centered(safe, InstructionWidth, InstructionHeight, InstructionCenterY);

        public static NamedRect[] RaceRects(Rect safe) => new[]
        {
            new NamedRect("ProgressRail", ProgressRailRect(safe)),
            new NamedRect("Scoreboard", ScoreboardRect(safe)),
            new NamedRect("ModeChip", ModeChipRect(safe)),
            new NamedRect("Pause", PauseRect(safe)),
            new NamedRect("LeftControl", ControlRect(safe, true)),
            new NamedRect("RightControl", ControlRect(safe, false))
        };

        public static NamedRect[] StartStateRects(Rect safe)
        {
            NamedRect[] race = RaceRects(safe);
            var all = new NamedRect[race.Length + 2];
            System.Array.Copy(race, all, race.Length);
            all[race.Length] = new NamedRect("Countdown", CountdownRect(safe));
            all[race.Length + 1] = new NamedRect("Instruction", InstructionRect(safe));
            return all;
        }

        static Rect Centered(Rect safe, float width01H, float height01H, float centerY01H)
        {
            float width = safe.height * width01H;
            float height = safe.height * height01H;
            return new Rect(
                safe.center.x - width * .5f,
                safe.yMin + safe.height * centerY01H - height * .5f,
                width, height);
        }
    }
}
```

- [ ] **Step 4: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-layout-green.xml`. Expected: 0 failed. The overlap and containment cases run six times each.

If a containment case fails on the notch aspect, the cause is `InstructionWidth = 1.07f` exceeding the narrowed safe width — reduce it until green and record the new value here. Do not widen the safe-area padding to hide it.

- [ ] **Step 5: Confirm no other code still calls the deleted methods**

```bash
rtk grep -rn "VisibleControlRect\|HitAreaRect" Assets --include=*.cs
```

Expected: no results. If `SprintControlPresenter.cs` still references them the project will not compile — that file is rewritten in Task 6; for now delete the offending lines' method bodies only if the compiler blocks this task, otherwise leave them for Task 6.

- [ ] **Step 6: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs \
  Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs
rtk git commit -m "feat: prove sprint layout has no overlaps at any landscape aspect"
```

---

### Task 4: Remove the legacy HUD

**Files:**
- Modify: `Assets/Editor/SprintSceneConfigurator.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs:270-279`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `KMA.EditorTools.SprintSceneConfigurator.RemoveLegacyMetrics()` (Editor-only, menu `KMA/Sprint/Remove Legacy Metrics`); `SprintFestivalPresentation` hides obsolete chrome by canvas-wide name lookup.

Three defects being fixed, all confirmed in the scene and prefab data:

1. `DisableSharedMetrics` looks for `"Time"`, but the object is named **`Timer`** (`HUD_Minigame.prefab`, child of `SafeAreaRoot`). The timer never hides.
2. **`HeartBar`** is a child of `SafeAreaRoot` and is not in the list at all.
3. **`SprintMetrics`** is a *sibling* of `SafeAreaRoot`, added directly under the canvas root (`MG_Sprint.unity:2088`), so `safeArea.Find` can never reach it. Its children `SprintDistance`, `SprintRank`, `SprintCadence`, `SprintDistanceFill` render permanently.

- [ ] **Step 1: Write the failing test**

Add to `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`, inside the existing class, following the file's existing scene-loading pattern:

```csharp
[UnityTest]
public IEnumerator SprintScene_ShowsNoLegacyHudChrome()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    Assert.That(GameObject.Find("SprintMetrics"), Is.Null,
        "The legacy SprintMetrics group must be deleted from MG_Sprint.unity.");
    Assert.That(GameObject.Find("SprintFestivalChrome"), Is.Null);

    foreach (string name in new[] { "Timer", "Phase", "Score", "Status", "Progress", "Stamina", "HeartBar" })
    {
        GameObject shared = GameObject.Find(name);
        Assert.That(shared, Is.Null, $"Shared HUD element {name} must not be active in the Sprint scene.");
    }
}
```

`GameObject.Find` ignores inactive objects, so an element that is merely deactivated passes — which is the intended outcome for the shared prefab's elements, and deletion is the intended outcome for `SprintMetrics`.

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests' \
  -testResults /tmp/kma-legacy-red.xml -logFile /tmp/kma-legacy-red.log
```

Expected: `SprintScene_ShowsNoLegacyHudChrome` fails on `SprintMetrics` and on `Timer`.

- [ ] **Step 3: Fix the name lookup in the presentation builder**

In `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`, replace the `DisableSharedMetrics` method:

```csharp
static void DisableSharedMetrics(Transform safeArea)
{
    Transform canvasRoot = safeArea.parent != null ? safeArea.parent : safeArea;
    string[] obsolete =
    {
        "Timer", "Phase", "Score", "Status", "Progress", "Stamina", "HeartBar", "SprintMetrics"
    };

    for (int i = 0; i < obsolete.Length; i++)
    {
        Transform found = FindDescendant(canvasRoot, obsolete[i]);
        if (found != null)
            found.gameObject.SetActive(false);
    }
}

static Transform FindDescendant(Transform root, string name)
{
    if (root.name == name)
        return root;

    for (int i = 0; i < root.childCount; i++)
    {
        Transform found = FindDescendant(root.GetChild(i), name);
        if (found != null)
            return found;
    }

    return null;
}
```

This searches the whole canvas rather than one transform's direct children, so siblings of `SafeAreaRoot` are reachable.

- [ ] **Step 4: Add the scene cleanup command**

Append to `KMA.EditorTools.SprintSceneConfigurator` in `Assets/Editor/SprintSceneConfigurator.cs`, before the closing braces:

```csharp
[MenuItem("KMA/Sprint/Remove Legacy Metrics")]
public static void RemoveLegacyMetrics()
{
    var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    var removed = 0;

    foreach (var root in scene.GetRootGameObjects())
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform == null || transform.name != "SprintMetrics")
                continue;

            UnityEngine.Object.DestroyImmediate(transform.gameObject);
            removed++;
            break;
        }
    }

    if (removed == 0)
        Debug.Log("SprintMetrics was already absent from MG_Sprint.");

    EditorSceneManager.MarkSceneDirty(scene);
    EditorSceneManager.SaveScene(scene);
    Debug.Log($"Removed {removed} legacy SprintMetrics group(s) from {ScenePath}.");
}
```

- [ ] **Step 5: Run the cleanup command**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -quit -projectPath . \
  -executeMethod KMA.EditorTools.SprintSceneConfigurator.RemoveLegacyMetrics \
  -logFile /tmp/kma-remove-metrics.log
```

Expected: the log contains `Removed 1 legacy SprintMetrics group(s)`. Then confirm the scene no longer contains it:

```bash
rtk grep -c "SprintMetrics\|SprintDistanceFill" Assets/_Project/Scenes/MG_Sprint.unity
```

Expected: `0`.

- [ ] **Step 6: Clear the stale SprintHud references**

Destroying the group leaves `SprintHud`'s serialized fields pointing at nothing, which Unity writes as `{fileID: 0}`. Confirm the scene diff shows `metricsRoot`, `distanceLabel`, `rankLabel`, `cadenceLabel` and `distanceFill` are now `{fileID: 0}`:

```bash
rtk git diff Assets/_Project/Scenes/MG_Sprint.unity | rtk grep -n "metricsRoot\|distanceLabel\|rankLabel\|cadenceLabel\|distanceFill"
```

Expected: the removed lines carry real fileIDs, the added lines carry `{fileID: 0}`. `SprintHud.CacheVisuals()` rebinds from the built chrome at `Awake`, so no code change is needed here.

- [ ] **Step 7: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-legacy-green.xml`. Expected: `SprintScene_ShowsNoLegacyHudChrome` passes.

- [ ] **Step 8: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/Editor/SprintSceneConfigurator.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs \
  Assets/_Project/Scenes/MG_Sprint.unity \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
rtk git commit -m "fix: remove legacy sprint HUD chrome"
```

---

### Task 5: Scoreboard, progress rail, mode chip and pause

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs`

**Interfaces:**
- Consumes: `SprintUiTheme`, `SprintUiShapes.RoundedRect(int)`, `SprintUiLayout.ProgressRailRect/ScoreboardRect/ModeChipRect/PauseRect`, `SprintController.Snapshot`, `SprintController.RankText`, `SprintController.CadenceCombo`.
- Produces: hierarchy `SprintBroadcastChrome/{ProgressRail/RailFill, ProgressRail/PlayerPip, Scoreboard/{Distance,RankBadge/RankLabel,Combo}, ModeLabel, PausePanel}`; `SprintHud.HasBoundVisuals`; `SprintHud.DistanceText/RankText/CadenceText`.

The scoreboard is **two rows**: `Distance` (Title, left) and `RankBadge` (Headline, gold pill, right) share row 1; `Combo` (Body) is row 2. Three stacked rows would need ~182 units against the 162 available and would clip.

- [ ] **Step 1: Write the failing test**

Add to `SprintPresentationGateTests`:

```csharp
[UnityTest]
public IEnumerator SprintScene_BuildsTheApprovedScoreboardAndRail()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
    Assert.That(chrome.GetComponentInParent<KMA.Gameplay.UI.SafeAreaFitter>(), Is.Not.Null);

    Assert.That(chrome.Find("Scoreboard/Distance").GetComponent<TMP_Text>().text, Is.EqualTo("0 / 100 m"));
    Assert.That(chrome.Find("Scoreboard/RankBadge/RankLabel").GetComponent<TMP_Text>().text, Is.EqualTo("1st"));
    Assert.That(chrome.Find("Scoreboard/Combo").GetComponent<TMP_Text>().text, Is.EqualTo("COMBO ×0"));
    Assert.That(chrome.Find("ModeLabel").GetComponent<TMP_Text>().text, Is.EqualTo("CHẠY NƯỚC RÚT · 100M"));

    Image railFill = chrome.Find("ProgressRail/RailFill").GetComponent<Image>();
    Assert.That(railFill.type, Is.EqualTo(Image.Type.Filled));
    Assert.That(railFill.fillAmount, Is.EqualTo(0f).Within(.001f));
    Assert.That(railFill.color, Is.EqualTo(SprintUiTheme.Accent));

    Image pip = chrome.Find("ProgressRail/PlayerPip").GetComponent<Image>();
    Assert.That(pip.color, Is.EqualTo(SprintUiTheme.Player));

    Assert.That(chrome.Find("Scoreboard").GetComponent<Image>().sprite, Is.Not.Null,
        "The scoreboard must use a generated rounded sprite, not the default square.");
    Assert.That(chrome.Find("Scoreboard").GetComponent<Outline>(), Is.Null,
        "Outline is replaced by Shadow.");
    Assert.That(chrome.Find("Scoreboard").GetComponent<Shadow>(), Is.Not.Null);
}

[UnityTest]
public IEnumerator SprintHud_ReflectsDistanceRankAndCombo()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    var hud = SceneObjects<SprintHud>(scene)[0];
    Assert.That(hud.HasBoundVisuals, Is.True);

    controller.AdvanceToDistance(42f);
    hud.Refresh();

    Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
    Assert.That(chrome.Find("Scoreboard/Distance").GetComponent<TMP_Text>().text, Is.EqualTo("42 / 100 m"));
    Assert.That(chrome.Find("ProgressRail/RailFill").GetComponent<Image>().fillAmount,
        Is.EqualTo(.42f).Within(.001f));
    Assert.That(chrome.Find("ProgressRail/PlayerPip").GetComponent<RectTransform>().anchorMin.x,
        Is.EqualTo(.42f).Within(.001f));
}
```

Add `using KMA.Gameplay;` and `using UnityEngine.UI;` to the file if absent.

In `FestivalUiExperienceTests.cs`, update the Sprint expectation at line 101 from `"CHẠY NƯỚC RÚT · 100 M"` to `"CHẠY NƯỚC RÚT · 100M"` and re-point any lookup of `SprintFestivalChrome` to `SprintBroadcastChrome`. Preserve the user's other existing edits in that file — read it first and change only Sprint chrome assertions.

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode \
  -testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests|KMA.Tests.Presentation.FestivalUiExperienceTests' \
  -testResults /tmp/kma-scoreboard-red.xml -logFile /tmp/kma-scoreboard-red.log
```

Expected: missing `ProgressRail`, `RankBadge`, and `Shadow`; `Outline` still present.

- [ ] **Step 3: Add layout and styling helpers to the builder**

In `SprintFestivalPresentation.cs`, delete the `Navy`, `Coral`, `Gold`, `Cream`, `ControlNavy` fields and replace every use with `SprintUiTheme`. Add these helpers:

```csharp
/// Positions a RectTransform from an absolute rect expressed in the safe area's own space.
static void ApplyRect(RectTransform rect, Rect safe, Rect target)
{
    rect.anchorMin = new Vector2(
        Mathf.InverseLerp(safe.xMin, safe.xMax, target.xMin),
        Mathf.InverseLerp(safe.yMin, safe.yMax, target.yMin));
    rect.anchorMax = new Vector2(
        Mathf.InverseLerp(safe.xMin, safe.xMax, target.xMax),
        Mathf.InverseLerp(safe.yMin, safe.yMax, target.yMax));
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
}

static Rect SafeRect(RectTransform safeArea) =>
    new Rect(0f, 0f, safeArea.rect.width, safeArea.rect.height);

static Image Panel(Transform parent, string name, Color color, int radius)
{
    RectTransform root = Rect(parent, name);
    Image image = root.gameObject.AddComponent<Image>();
    image.sprite = SprintUiShapes.RoundedRect(radius);
    image.type = Image.Type.Sliced;
    image.pixelsPerUnitMultiplier = 1f;
    image.color = color;
    image.raycastTarget = false;
    return image;
}

static void AddShadow(Component target)
{
    var shadow = target.gameObject.AddComponent<Shadow>();
    shadow.effectColor = SprintUiTheme.ShadowColor;
    shadow.effectDistance = SprintUiTheme.ShadowOffset;
}
```

- [ ] **Step 4: Build the rail, scoreboard, mode chip and pause**

Replace the scoreboard/mode/pause section of `Build()` (currently lines 41-86) with:

```csharp
RectTransform safeRect = (RectTransform)safeArea;
Rect safe = SafeRect(safeRect);

// Progress rail
Image railTrack = Panel(root, "ProgressRail", SprintUiTheme.WithAlpha(Color.white, .22f),
    Mathf.RoundToInt(safe.height * .015f));
ApplyRect(railTrack.rectTransform, safe, SprintUiLayout.ProgressRailRect(safe));
Image railFill = Panel(railTrack.transform, "RailFill", SprintUiTheme.Accent,
    Mathf.RoundToInt(safe.height * .015f));
Stretch(railFill.rectTransform);
railFill.type = Image.Type.Filled;
railFill.fillMethod = Image.FillMethod.Horizontal;
railFill.fillOrigin = (int)Image.OriginHorizontal.Left;
railFill.fillAmount = 0f;
Image pip = Panel(railTrack.transform, "PlayerPip", SprintUiTheme.Player, 4);
pip.rectTransform.anchorMin = new Vector2(0f, -.35f);
pip.rectTransform.anchorMax = new Vector2(0f, 1.35f);
pip.rectTransform.pivot = new Vector2(.5f, .5f);
pip.rectTransform.sizeDelta = new Vector2(safe.height * .014f, 0f);
pip.rectTransform.anchoredPosition = Vector2.zero;

// Scoreboard
Image scoreboard = Panel(root, "Scoreboard", SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .92f),
    Mathf.RoundToInt(SprintUiTheme.RadiusPanel));
ApplyRect(scoreboard.rectTransform, safe, SprintUiLayout.ScoreboardRect(safe));
AddShadow(scoreboard);

TMP_Text distance = Metric(scoreboard.transform, "Distance", font, SprintUiTheme.Title,
    SprintUiTheme.TextPrimary, new Vector2(.05f, .46f), new Vector2(.62f, .92f));
distance.alignment = TextAlignmentOptions.Left;
distance.text = "0 / 100 m";

Image rankBadge = Panel(scoreboard.transform, "RankBadge",
    SprintUiTheme.WithAlpha(SprintUiTheme.Accent, .22f), Mathf.RoundToInt(SprintUiTheme.RadiusPanel));
rankBadge.rectTransform.anchorMin = new Vector2(.66f, .46f);
rankBadge.rectTransform.anchorMax = new Vector2(.95f, .92f);
rankBadge.rectTransform.offsetMin = Vector2.zero;
rankBadge.rectTransform.offsetMax = Vector2.zero;
TMP_Text rank = Text(rankBadge.transform, "RankLabel", "1st", font, SprintUiTheme.Headline,
    SprintUiTheme.Accent, TextAlignmentOptions.Center);
Stretch(rank.rectTransform);

TMP_Text combo = Metric(scoreboard.transform, "Combo", font, SprintUiTheme.Body,
    SprintUiTheme.Energy, new Vector2(.05f, .10f), new Vector2(.62f, .42f));
combo.alignment = TextAlignmentOptions.Left;
combo.text = "COMBO ×0";

// Mode chip
TMP_Text mode = Text(root, "ModeLabel", "CHẠY NƯỚC RÚT · 100M", font, SprintUiTheme.Caption,
    SprintUiTheme.WithAlpha(SprintUiTheme.TextPrimary, .6f), TextAlignmentOptions.Center);
ApplyRect(mode.rectTransform, safe, SprintUiLayout.ModeChipRect(safe));

EnsurePause(root, font, safe);
```

Rewrite `EnsurePause` to take the safe rect and use the layout:

```csharp
static void EnsurePause(RectTransform parent, TMP_FontAsset font, Rect safe)
{
    if (Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>() != null)
        return;

    Image button = Panel(parent, "PausePanel",
        SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .92f), Mathf.RoundToInt(SprintUiTheme.RadiusPause));
    button.raycastTarget = true;
    ApplyRect(button.rectTransform, safe, SprintUiLayout.PauseRect(safe));
    AddShadow(button);
    button.gameObject.AddComponent<Button>();
    button.gameObject.AddComponent<KMA.Gameplay.UI.PausePanel>();

    PauseBar(button.transform, "BarLeft", .28f, .44f);
    PauseBar(button.transform, "BarRight", .56f, .72f);
}

static void PauseBar(Transform parent, string name, float minX, float maxX)
{
    Image bar = Panel(parent, name, SprintUiTheme.TextPrimary, 2);
    bar.rectTransform.anchorMin = new Vector2(minX, .26f);
    bar.rectTransform.anchorMax = new Vector2(maxX, .74f);
    bar.rectTransform.offsetMin = Vector2.zero;
    bar.rectTransform.offsetMax = Vector2.zero;
}
```

Delete the `TouchPrompts` / `Prompt` block (current lines 90-100) and the now-unused `Prompt` method — the control buttons carry their own labels from Task 6. Remove all remaining `Outline` usage in this file.

- [ ] **Step 5: Update SprintHud to drive the rail and rank badge**

In `SprintHud.cs`, add a `RectTransform playerPip` field, and change `Refresh()` and `CacheVisuals()`:

```csharp
public void Refresh()
{
    if (controller == null)
        return;

    var snapshot = controller.Snapshot;
    float progress = Mathf.Clamp01(snapshot.Distance / 100f);

    DistanceText = $"{Mathf.RoundToInt(snapshot.Distance)} / 100 m";
    RankText = controller.RankText;
    CadenceText = $"COMBO ×{controller.CadenceCombo}";

    if (distanceLabel != null) distanceLabel.text = DistanceText;
    if (rankLabel != null) rankLabel.text = RankText;
    if (cadenceLabel != null) cadenceLabel.text = CadenceText;
    if (distanceFill != null) distanceFill.fillAmount = progress;
    if (playerPip != null)
    {
        Vector2 min = playerPip.anchorMin;
        Vector2 max = playerPip.anchorMax;
        playerPip.anchorMin = new Vector2(progress, min.y);
        playerPip.anchorMax = new Vector2(progress, max.y);
    }
}

void CacheVisuals()
{
    var hud = GameObject.Find("S2_HUD_Minigame");
    if (hud == null)
        return;

    var chrome = hud.transform.Find("SafeAreaRoot/SprintBroadcastChrome");
    if (chrome == null)
        return;

    metricsRoot = chrome.Find("Scoreboard");
    if (metricsRoot == null)
        return;

    distanceLabel = metricsRoot.Find("Distance")?.GetComponent<TMP_Text>();
    rankLabel = metricsRoot.Find("RankBadge/RankLabel")?.GetComponent<TMP_Text>();
    cadenceLabel = metricsRoot.Find("Combo")?.GetComponent<TMP_Text>();
    distanceFill = chrome.Find("ProgressRail/RailFill")?.GetComponent<Image>();
    playerPip = chrome.Find("ProgressRail/PlayerPip") as RectTransform;
}

public bool HasBoundVisuals => metricsRoot != null && distanceLabel != null && rankLabel != null &&
    cadenceLabel != null && distanceFill != null && playerPip != null;
```

Delete the now-unused `TimerText` and `StaminaText` properties and the `ReadHudState()` call that fed them — nothing renders them any more. Check for other readers first:

```bash
rtk grep -rn "TimerText\|StaminaText" Assets --include=*.cs
```

If any test reads them, keep the properties and leave `ReadHudState()` in place rather than breaking that test.

- [ ] **Step 6: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-scoreboard-green.xml`. Expected: 0 failed.

- [ ] **Step 7: Inspect the scene diff**

```bash
rtk git diff Assets/_Project/Scenes/MG_Sprint.unity
```

Expected: no diff at all — this task builds everything at runtime. If the scene changed, something was authored by accident; revert it.

- [ ] **Step 8: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs \
  Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs
rtk git commit -m "feat: rebuild sprint scoreboard and progress rail"
```

---

### Task 6: LEFT and RIGHT as real buttons

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs` (full rewrite)
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs` (`EnsureControls`, `Control`, `PrepareTapArea`)
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: `SprintController.ExpectedSide`, `KMA.Input.ScreenTapArea` objects named `LeftTap` and `RightTap`, `SprintUiLayout.ControlRect`, `SprintUiTheme`, `SprintUiShapes`, `KMA.Gameplay.Core.HapticsService`.
- Produces: `SprintControlPresenter.Configure(SprintController controller, RectTransform leftVisual, RectTransform rightVisual, Image leftBackground, Image rightBackground, Image leftBorder, Image rightBorder)`; `BindPressFeedback(ScreenTapArea left, ScreenTapArea right)`; read-only `Side HighlightedSide`, `float LeftScale`, `float RightScale`; `RefreshForTest()`, `PressForTest(Side side)`, `TickForTest(float deltaTime)`.

Structure built under each tap area:

```
LeftTap                 ScreenTapArea + Image(alpha 0, raycastTarget) — rect = SprintUiLayout.ControlRect
└ Visual                stretched to fill, raycast off; this is what scales
  ├ Border              rounded, Accent, fills Visual
  ├ Background          rounded, Surface, inset by SprintUiTheme.BorderWidth
  ├ Arrow               ← / → glyph, BodyLarge * 1.6
  └ Label               "TRÁI" / "PHẢI", BodyLarge
```

The border is the outer image and the background is inset over it — that gives a frame without needing a hollow ring sprite.

Delete `SyncLayout`, `SetVisualLayout`, `HasSafeAreaInsets` and `ScreenRect` (current lines 119-186). They exist only to reconcile two rects that are now one.

- [ ] **Step 1: Write the failing test**

Add to `SprintPresentationGateTests`:

```csharp
[UnityTest]
public IEnumerator SprintControls_AreRealButtonsMatchingTheirHitAreas()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    foreach (string tapName in new[] { "LeftTap", "RightTap" })
    {
        var tap = GameObject.Find(tapName).GetComponent<RectTransform>();
        var visual = tap.Find("Visual") as RectTransform;
        Assert.That(visual, Is.Not.Null, $"{tapName} must own a Visual child.");

        Assert.That(visual.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(visual.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(visual.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(visual.offsetMax, Is.EqualTo(Vector2.zero));

        Assert.That(tap.GetComponent<Image>().raycastTarget, Is.True, $"{tapName} must receive taps.");
        Assert.That(visual.Find("Border").GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(visual.Find("Background").GetComponent<Image>().sprite, Is.Not.Null);
    }

    Assert.That(GameObject.Find("LeftTap").transform.Find("Visual/Label").GetComponent<TMP_Text>().text,
        Is.EqualTo("TRÁI"));
    Assert.That(GameObject.Find("RightTap").transform.Find("Visual/Label").GetComponent<TMP_Text>().text,
        Is.EqualTo("PHẢI"));
    Assert.That(GameObject.Find("LeftTap").transform.Find("Visual/Arrow").GetComponent<TMP_Text>().text,
        Is.EqualTo("←"));
    Assert.That(GameObject.Find("RightTap").transform.Find("Visual/Arrow").GetComponent<TMP_Text>().text,
        Is.EqualTo("→"));

    Rect leftRect = RectOf(GameObject.Find("LeftTap").GetComponent<RectTransform>());
    Rect rightRect = RectOf(GameObject.Find("RightTap").GetComponent<RectTransform>());
    Assert.That(leftRect.width, Is.EqualTo(rightRect.width).Within(1f));
    Assert.That(leftRect.height, Is.EqualTo(rightRect.height).Within(1f));
    Assert.That(leftRect.Overlaps(rightRect), Is.False);
}

static Rect RectOf(RectTransform rect)
{
    var corners = new Vector3[4];
    rect.GetWorldCorners(corners);
    return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
}

[UnityTest]
public IEnumerator SprintControls_HighlightTheExpectedSideWithoutTouchingGameplay()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    var presenter = SceneObjects<SprintControlPresenter>(scene)[0];
    controller.Simulate(1f);
    controller.Simulate(1f);
    controller.Simulate(1f);
    presenter.RefreshForTest();

    Assert.That(presenter.HighlightedSide, Is.EqualTo(controller.ExpectedSide));

    int comboBefore = controller.CadenceCombo;
    presenter.RefreshForTest();
    Assert.That(controller.CadenceCombo, Is.EqualTo(comboBefore),
        "The presenter must never advance gameplay state.");
}

[UnityTest]
public IEnumerator SprintControls_ShrinkOnPressAndRecover()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    var presenter = SceneObjects<SprintControlPresenter>(scene)[0];
    float distanceBefore = controller.Snapshot.Distance;

    presenter.PressForTest(Side.Left);
    Assert.That(presenter.LeftScale, Is.EqualTo(.94f).Within(.001f));
    Assert.That(presenter.RightScale, Is.EqualTo(1f).Within(.001f));

    presenter.TickForTest(.091f);
    Assert.That(presenter.LeftScale, Is.EqualTo(1f).Within(.001f));
    Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBefore).Within(.0001f));
}
```

Reaching `MinigamePhase.Play` uses the seam the file already uses elsewhere: `start.TickForTest(1.5f)` to release the gate, then three `controller.Simulate(1f)` calls. Do not add a new production seam.

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests' \
  -testResults /tmp/kma-controls-red.xml -logFile /tmp/kma-controls-red.log
```

Expected: no `Visual` child under `LeftTap`.

- [ ] **Step 3: Rewrite the control presenter**

Replace the entire contents of `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.Core;
using KMA.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    /// Visual state for the Sprint LEFT/RIGHT buttons. Owns no input: ScreenTapArea
    /// remains the only component that forwards taps to gameplay.
    public sealed class SprintControlPresenter : MonoBehaviour
    {
        const float PressScale = .94f;
        const float PressDuration = .09f;
        const float BreatheHz = 1.2f;
        const float BreatheAmount = .03f;

        SprintController controller;
        HapticsService haptics;
        RectTransform leftVisual;
        RectTransform rightVisual;
        Image leftBackground;
        Image rightBackground;
        Image leftBorder;
        Image rightBorder;
        Side pressedSide;
        float pressRemaining;
        float breathePhase;

        public Side HighlightedSide { get; private set; } = Side.Left;
        public float LeftScale { get; private set; } = 1f;
        public float RightScale { get; private set; } = 1f;

        public void Configure(SprintController sprintController, RectTransform left, RectTransform right,
            Image leftFill, Image rightFill, Image leftFrame, Image rightFrame)
        {
            controller = sprintController;
            leftVisual = left;
            rightVisual = right;
            leftBackground = leftFill;
            rightBackground = rightFill;
            leftBorder = leftFrame;
            rightBorder = rightFrame;
            haptics = Object.FindFirstObjectByType<HapticsService>();
            SetScale(Side.Left, 1f);
            SetScale(Side.Right, 1f);
            RefreshForTest();
        }

        public void BindPressFeedback(ScreenTapArea left, ScreenTapArea right)
        {
            Bind(left, Side.Left);
            Bind(right, Side.Right);
        }

        public void RefreshForTest()
        {
            if (controller == null)
                return;

            HighlightedSide = controller.ExpectedSide;
            ApplyState(Side.Left, leftBackground, leftBorder);
            ApplyState(Side.Right, rightBackground, rightBorder);
        }

        public void PressForTest(Side side)
        {
            if (pressRemaining > 0f)
                SetScale(pressedSide, 1f);

            pressedSide = side;
            pressRemaining = PressDuration;
            SetScale(side, PressScale);
            RefreshForTest();

            if (haptics != null && controller != null && side == controller.ExpectedSide)
                haptics.Light();
        }

        public void TickForTest(float deltaTime)
        {
            breathePhase += deltaTime * BreatheHz * Mathf.PI * 2f;

            if (pressRemaining <= 0f)
                return;

            pressRemaining = Mathf.Max(0f, pressRemaining - deltaTime);
            if (pressRemaining <= 0f)
                SetScale(pressedSide, 1f);
        }

        void Update()
        {
            RefreshForTest();
            TickForTest(Time.unscaledDeltaTime);
            ApplyBreathe();
        }

        void Bind(ScreenTapArea tapArea, Side side)
        {
            if (tapArea == null)
                return;

            EventTrigger trigger = tapArea.GetComponent<EventTrigger>()
                ?? tapArea.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            for (int i = trigger.triggers.Count - 1; i >= 0; i--)
            {
                if (trigger.triggers[i].eventID == EventTriggerType.PointerDown)
                    trigger.triggers.RemoveAt(i);
            }

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => PressForTest(side));
            trigger.triggers.Add(entry);
        }

        void ApplyState(Side side, Image background, Image border)
        {
            if (background == null || border == null)
                return;

            bool pressed = pressRemaining > 0f && pressedSide == side;
            bool expected = HighlightedSide == side;
            bool finished = controller != null && controller.PresentationPhase != MinigamePhase.Play;

            if (pressed)
            {
                background.color = SprintUiTheme.WithAlpha(SprintUiTheme.Energy, .55f);
                border.color = SprintUiTheme.WithAlpha(SprintUiTheme.Accent, .75f);
                return;
            }

            if (finished)
            {
                background.color = SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .30f);
                border.color = SprintUiTheme.WithAlpha(SprintUiTheme.Accent, .15f);
                return;
            }

            background.color = SprintUiTheme.WithAlpha(SprintUiTheme.Surface, expected ? .55f : .42f);
            border.color = SprintUiTheme.WithAlpha(SprintUiTheme.Accent, expected ? .75f : .25f);
        }

        void ApplyBreathe()
        {
            if (pressRemaining > 0f)
                return;

            float pulse = 1f + Mathf.Sin(breathePhase) * .5f * BreatheAmount + .5f * BreatheAmount;
            SetScale(HighlightedSide, pulse);
            SetScale(HighlightedSide == Side.Left ? Side.Right : Side.Left, 1f);
        }

        void SetScale(Side side, float scale)
        {
            RectTransform visual = side == Side.Left ? leftVisual : rightVisual;
            if (visual != null)
                visual.localScale = new Vector3(scale, scale, 1f);

            if (side == Side.Left)
                LeftScale = scale;
            else
                RightScale = scale;
        }
    }
}
```

`ApplyBreathe` never runs during a press, so `PressForTest` followed by `TickForTest` returns exactly `1f` as the test asserts.

Add `KMA.Gameplay.Core` to the `references` array in `Assets/_Project/Scripts/Gameplay/Sprint/KMA.Gameplay.Sprint.asmdef` so `HapticsService` resolves.

- [ ] **Step 4: Build the controls in the assembler**

In `SprintFestivalPresentation.cs`, replace `EnsureControls`, `Control` and `PrepareTapArea`:

```csharp
static void EnsureControls(RectTransform root, Rect safe)
{
    KMA.Input.ScreenTapArea leftTap = FindTapArea("LeftTap");
    KMA.Input.ScreenTapArea rightTap = FindTapArea("RightTap");
    if (leftTap == null || rightTap == null)
        return;

    ControlVisual left = BuildControl(leftTap, safe, true);
    ControlVisual right = BuildControl(rightTap, safe, false);

    var presenter = root.GetComponent<SprintControlPresenter>()
        ?? root.gameObject.AddComponent<SprintControlPresenter>();
    presenter.Configure(Object.FindFirstObjectByType<SprintController>(),
        left.Visual, right.Visual, left.Background, right.Background, left.Border, right.Border);
    presenter.BindPressFeedback(leftTap, rightTap);
}

readonly struct ControlVisual
{
    public readonly RectTransform Visual;
    public readonly Image Background;
    public readonly Image Border;

    public ControlVisual(RectTransform visual, Image background, Image border)
    {
        Visual = visual;
        Background = background;
        Border = border;
    }
}

static ControlVisual BuildControl(KMA.Input.ScreenTapArea tapArea, Rect safe, bool left)
{
    var tapRect = tapArea.GetComponent<RectTransform>();
    ApplyRect(tapRect, safe, SprintUiLayout.ControlRect(safe, left));

    Image tapImage = tapArea.GetComponent<Image>() ?? tapArea.gameObject.AddComponent<Image>();
    tapImage.color = new Color(1f, 1f, 1f, 0f);
    tapImage.raycastTarget = true;

    Transform existing = tapRect.Find("Visual");
    if (existing != null)
        Object.DestroyImmediate(existing.gameObject);

    RectTransform visual = Rect(tapRect, "Visual");
    Stretch(visual);

    int radius = Mathf.RoundToInt(SprintUiTheme.RadiusControl);
    Image border = Panel(visual, "Border", SprintUiTheme.WithAlpha(SprintUiTheme.Accent, .25f), radius);
    Stretch(border.rectTransform);
    AddShadow(border);

    Image background = Panel(visual, "Background",
        SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .42f), radius);
    float inset = SprintUiTheme.BorderWidth;
    Stretch(background.rectTransform, new Vector2(inset, inset), new Vector2(-inset, -inset));

    TMP_FontAsset font = Object.FindFirstObjectByType<TMP_Text>()?.font;
    TMP_Text arrow = Text(visual, "Arrow", left ? "←" : "→", font,
        SprintUiTheme.BodyLarge * 1.6f, SprintUiTheme.TextPrimary, TextAlignmentOptions.Center);
    arrow.rectTransform.anchorMin = new Vector2(.1f, .44f);
    arrow.rectTransform.anchorMax = new Vector2(.9f, .88f);
    arrow.rectTransform.offsetMin = Vector2.zero;
    arrow.rectTransform.offsetMax = Vector2.zero;

    TMP_Text label = Text(visual, "Label", left ? "TRÁI" : "PHẢI", font,
        SprintUiTheme.BodyLarge, SprintUiTheme.TextPrimary, TextAlignmentOptions.Center);
    label.rectTransform.anchorMin = new Vector2(.1f, .12f);
    label.rectTransform.anchorMax = new Vector2(.9f, .46f);
    label.rectTransform.offsetMin = Vector2.zero;
    label.rectTransform.offsetMax = Vector2.zero;

    return new ControlVisual(visual, background, border);
}
```

Update the `Build()` call site to `EnsureControls(root, safe);` and delete the `PrepareTapArea("LeftTap")` / `PrepareTapArea("RightTap")` calls and the `PrepareTapArea` method — `BuildControl` now owns the tap area's image.

- [ ] **Step 5: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-controls-green.xml`. Expected: 0 failed.

- [ ] **Step 6: Run the input regression**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode \
  -testFilter 'KMA.Tests.Gameplay.Running.SprintRuntimeInputTests|KMA.Tests.Gameplay.Running.SprintControllerTests' \
  -testResults /tmp/kma-input-green.xml -logFile /tmp/kma-input-green.log
```

Expected: 0 failed. One physical or UI tap still reaches gameplay exactly once. If a tap now registers twice, the cause is the `EventTrigger` being added on top of an existing forwarder — check that `Bind` removed prior `PointerDown` entries.

- [ ] **Step 7: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/KMA.Gameplay.Sprint.asmdef \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
rtk git commit -m "feat: make sprint controls real buttons"
```

---

### Task 7: Continuous start instruction and countdown

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs` (`EnsureStartPresentation`)
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: `SprintController.SetTutorialGate(bool)`, `SprintController.PresentationPhase`, `SprintController.PhaseChanged`, `SprintUiLayout.CountdownRect/InstructionRect`, `SprintUiTheme`.
- Produces: `SprintStartPresentation.Configure(GameObject countdownRoot, TMP_Text countdownLabel, GameObject instructionRoot, TMP_Text instructionLabel)`; `Bind(SprintController)`; `TickForTest(float)`; read-only `bool InstructionVisible`, `string CountdownText`, `string InstructionText`, `float CountdownScale`; `const string InstructionCopy = "BẤM TRÁI VÀ PHẢI LUÂN PHIÊN ĐỂ CHẠY"`.

The separate tutorial banner is removed. The gate still holds for 1.5 s, but the instruction plate is what shows during it and it stays visible through the countdown, fading 0.4 s after `GO!`. `GoDuration` rises from `.25f` to `.5f`.

`TutorialVisible`, `TutorialText`, `TutorialCopy` and the `tutorialRoot`/`tutorialLabel` fields are deleted — this also removes the `TutorialCopy` vs `TutorialMessage` mismatch at lines 37 and 209. Check for external readers first:

```bash
rtk grep -rn "TutorialVisible\|TutorialCopy\|TutorialText" Assets --include=*.cs
```

Update every hit, including `PhaseFlowTests` if it reads them.

- [ ] **Step 1: Write the failing test**

Add to `SprintPresentationGateTests`:

```csharp
[UnityTest]
public IEnumerator SprintStart_ShowsOneInstructionAcrossTutorialAndCountdown()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    var start = SceneObjects<SprintStartPresentation>(scene)[0];

    Assert.That(start.InstructionVisible, Is.True, "instruction shows immediately");
    Assert.That(start.InstructionText, Is.EqualTo(SprintStartPresentation.InstructionCopy));
    Assert.That(start.InstructionText, Is.EqualTo("BẤM TRÁI VÀ PHẢI LUÂN PHIÊN ĐỂ CHẠY"));

    start.TickForTest(1.49f);
    Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial));
    Assert.That(start.InstructionVisible, Is.True, "instruction persists through the gate");

    start.TickForTest(.02f);
    Assert.That(controller.PresentationPhase, Is.Not.EqualTo(MinigamePhase.Tutorial),
        "the gate auto-releases at 1.5 s");
}

[UnityTest]
public IEnumerator SprintStart_CountsDownThreeTwoOneThenGo()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    var start = SceneObjects<SprintStartPresentation>(scene)[0];
    start.TickForTest(1.51f);

    Assert.That(start.CountdownText, Is.EqualTo("3"));
    start.TickForTest(1f);
    Assert.That(start.CountdownText, Is.EqualTo("2"));
    start.TickForTest(1f);
    Assert.That(start.CountdownText, Is.EqualTo("1"));

    controller.Simulate(1f);
    controller.Simulate(1f);
    controller.Simulate(1f);
    Assert.That(start.CountdownText, Is.EqualTo("GO!"));
    Assert.That(start.CountdownScale, Is.GreaterThan(1f), "GO! pops before settling");

    start.TickForTest(.51f);
    Assert.That(start.CountdownText, Is.Empty.Or.Null, "GO! clears after 0.5 s");

    start.TickForTest(.41f);
    Assert.That(start.InstructionVisible, Is.False, "instruction fades 0.4 s after GO");
}

[UnityTest]
public IEnumerator SprintStart_BlocksInputUntilPlay()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    controller.OnLeftTap();
    Assert.That(controller.Snapshot.Distance, Is.EqualTo(0f).Within(.0001f),
        "taps before Play must not move the runner");
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode \
  -testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests|KMA.Tests.Presentation.PhaseFlowTests' \
  -testResults /tmp/kma-start-red.xml -logFile /tmp/kma-start-red.log
```

Expected: `InstructionCopy` and `CountdownScale` do not exist.

- [ ] **Step 3: Rewrite the start presentation**

Replace the entire contents of `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs`:

```csharp
using TMPro;
using UnityEngine;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    /// Sprint-only start flow: a 1.5 s input gate, a 3-2-1-GO! countdown, and one
    /// instruction line that persists across both and fades shortly after GO!.
    public sealed class SprintStartPresentation : MonoBehaviour, ISprintStartPresentation
    {
        public const string InstructionCopy = "BẤM TRÁI VÀ PHẢI LUÂN PHIÊN ĐỂ CHẠY";

        const float GateDuration = 1.5f;
        const float GoDuration = .5f;
        const float InstructionFadeDuration = .4f;
        const float DigitPopDuration = .15f;
        const float DigitPopScale = 1.25f;
        const float GoPopScale = 1.4f;
        const float GoPopDuration = .2f;

        [SerializeField] GameObject countdownRoot;
        [SerializeField] TMP_Text countdownLabel;
        [SerializeField] GameObject instructionRoot;
        [SerializeField] TMP_Text instructionLabel;

        SprintController controller;
        CanvasGroup instructionCanvasGroup;
        float gateElapsed;
        float countdownElapsed;
        float goRemaining;
        float fadeRemaining;
        bool gateReleased;
        bool fading;
        string lastDigit = string.Empty;
        float popRemaining;
        float popFrom = 1f;

        public string CountdownText => countdownLabel == null ? string.Empty : countdownLabel.text;
        public string InstructionText => instructionLabel == null ? string.Empty : instructionLabel.text;
        public bool InstructionVisible =>
            instructionRoot != null && instructionRoot.activeSelf &&
            (instructionCanvasGroup == null || instructionCanvasGroup.alpha > .001f);
        public float CountdownScale =>
            countdownRoot == null ? 1f : countdownRoot.transform.localScale.x;

        void OnDisable() => Unsubscribe();
        void OnDestroy() => Unsubscribe();
        void Update() => Tick(Time.unscaledDeltaTime);

        public void Configure(GameObject countdown, TMP_Text countdownText,
            GameObject instruction, TMP_Text instructionText)
        {
            countdownRoot = countdown;
            countdownLabel = countdownText;
            instructionRoot = instruction;
            instructionLabel = instructionText;
            instructionCanvasGroup = instructionRoot == null ? null :
                instructionRoot.GetComponent<CanvasGroup>() ?? instructionRoot.AddComponent<CanvasGroup>();

            if (instructionLabel != null)
                instructionLabel.text = InstructionCopy;
            SetActive(countdownRoot, false);
        }

        public void Bind(SprintController source)
        {
            Unsubscribe();
            controller = source;
            gateElapsed = 0f;
            countdownElapsed = 0f;
            goRemaining = 0f;
            fadeRemaining = 0f;
            gateReleased = false;
            fading = false;
            lastDigit = string.Empty;
            popRemaining = 0f;

            if (controller == null)
            {
                SetActive(countdownRoot, false);
                SetInstructionAlpha(0f);
                return;
            }

            controller.PhaseChanged += ApplyPhase;
            controller.SetTutorialGate(true);
            SetActive(instructionRoot, true);
            SetInstructionAlpha(1f);
            ApplyPhase(controller.PresentationPhase);
        }

        void ISprintStartPresentation.Bind(MinigameBase source) => Bind(source as SprintController);

        public void TickForTest(float deltaTime) => Tick(deltaTime);

        void Tick(float deltaTime)
        {
            if (controller == null)
                return;

            float elapsed = Mathf.Max(0f, deltaTime);

            if (!gateReleased)
            {
                gateElapsed += elapsed;
                if (gateElapsed >= GateDuration)
                {
                    gateReleased = true;
                    controller.SetTutorialGate(false);
                }
            }

            if (controller.PresentationPhase == MinigamePhase.Countdown)
            {
                countdownElapsed += elapsed;
                RefreshCountdown();
            }

            if (goRemaining > 0f)
            {
                goRemaining = Mathf.Max(0f, goRemaining - elapsed);
                if (goRemaining <= 0f)
                {
                    if (countdownLabel != null)
                        countdownLabel.text = string.Empty;
                    SetActive(countdownRoot, false);
                }
            }

            if (fading)
            {
                fadeRemaining = Mathf.Max(0f, fadeRemaining - elapsed);
                SetInstructionAlpha(fadeRemaining / InstructionFadeDuration);
                if (fadeRemaining <= 0f)
                {
                    fading = false;
                    SetActive(instructionRoot, false);
                }
            }

            TickPop(elapsed);
        }

        void ApplyPhase(MinigamePhase phase)
        {
            if (phase == MinigamePhase.Countdown)
            {
                countdownElapsed = 0f;
                SetActive(countdownRoot, true);
                RefreshCountdown();
                return;
            }

            if (phase == MinigamePhase.Play)
            {
                if (countdownLabel != null)
                    countdownLabel.text = "GO!";
                SetActive(countdownRoot, true);
                goRemaining = GoDuration;
                Pop(GoPopScale, GoPopDuration);
                fading = true;
                fadeRemaining = InstructionFadeDuration;
                return;
            }

            if (phase != MinigamePhase.Tutorial)
            {
                SetActive(countdownRoot, false);
                SetActive(instructionRoot, false);
            }
        }

        void RefreshCountdown()
        {
            string digit = Mathf.Clamp(Mathf.CeilToInt(3f - countdownElapsed), 1, 3).ToString();
            if (digit == lastDigit)
                return;

            lastDigit = digit;
            if (countdownLabel != null)
                countdownLabel.text = digit;
            Pop(DigitPopScale, DigitPopDuration);
        }

        void Pop(float fromScale, float duration)
        {
            popFrom = fromScale;
            popRemaining = duration;
            if (countdownRoot != null)
                countdownRoot.transform.localScale = Vector3.one * fromScale;
        }

        void TickPop(float deltaTime)
        {
            if (popRemaining <= 0f || countdownRoot == null)
                return;

            popRemaining = Mathf.Max(0f, popRemaining - deltaTime);
            float duration = popFrom == GoPopScale ? GoPopDuration : DigitPopDuration;
            float t = duration <= 0f ? 1f : 1f - popRemaining / duration;
            countdownRoot.transform.localScale = Vector3.one * Mathf.Lerp(popFrom, 1f, t);
        }

        void Unsubscribe()
        {
            if (controller != null)
                controller.PhaseChanged -= ApplyPhase;
            controller = null;
        }

        void SetInstructionAlpha(float alpha)
        {
            if (instructionCanvasGroup != null)
                instructionCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
```

- [ ] **Step 4: Rebuild the start visuals in the assembler**

Replace `EnsureStartPresentation` in `SprintFestivalPresentation.cs`:

```csharp
static void EnsureStartPresentation(RectTransform parent, TMP_FontAsset font, Rect safe)
{
    RectTransform root = Rect(parent, "StartPresentation");
    Stretch(root);
    root.SetAsLastSibling();

    TMP_Text countdown = Text(root, "CountdownLabel", string.Empty, font, SprintUiTheme.Display,
        SprintUiTheme.Accent, TextAlignmentOptions.Center);
    ApplyRect(countdown.rectTransform, safe, SprintUiLayout.CountdownRect(safe));
    countdown.outlineWidth = .2f;
    countdown.outlineColor = new Color32(3, 18, 33, 255);

    RectTransform instructionRoot = Rect(root, "InstructionPlate");
    ApplyRect(instructionRoot, safe, SprintUiLayout.InstructionRect(safe));
    Image plate = instructionRoot.gameObject.AddComponent<Image>();
    plate.sprite = SprintUiShapes.RoundedRect(Mathf.RoundToInt(SprintUiTheme.RadiusPanel));
    plate.type = Image.Type.Sliced;
    plate.pixelsPerUnitMultiplier = 1f;
    plate.color = SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .82f);
    plate.raycastTarget = false;
    AddShadow(plate);

    TMP_Text instruction = Text(instructionRoot, "InstructionLabel",
        SprintStartPresentation.InstructionCopy, font, SprintUiTheme.BodyLarge,
        SprintUiTheme.TextPrimary, TextAlignmentOptions.Center);
    Stretch(instruction.rectTransform, new Vector2(SprintUiTheme.SpaceMd, SprintUiTheme.SpaceXs),
        new Vector2(-SprintUiTheme.SpaceMd, -SprintUiTheme.SpaceXs));

    SprintStartPresentation presenter = SceneObjects<SprintStartPresentation>(scene)[0]
        ?? root.gameObject.AddComponent<SprintStartPresentation>();
    presenter.Configure(countdown.gameObject, countdown, instructionRoot.gameObject, instruction);
    presenter.Bind(Object.FindFirstObjectByType<SprintController>());
}
```

Update the `Build()` call site to `EnsureStartPresentation(root, font, safe);`. Delete the `Arrow` and `CreateArrowTip` methods and their call sites — the tutorial banner they decorated is gone, and the control buttons carry their own arrow glyphs.

- [ ] **Step 5: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-start-green.xml`. Expected: 0 failed, including `PhaseFlowTests`.

- [ ] **Step 6: Confirm other minigames are unaffected**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Presentation' \
  -testResults /tmp/kma-presentation-green.xml -logFile /tmp/kma-presentation-green.log
```

- [ ] **Step 7: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs \
  Assets/Tests/PlayMode/Presentation/PhaseFlowTests.cs
rtk git commit -m "feat: give sprint one continuous start instruction"
```

---

### Task 8: Player identity

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs` (`EnsurePlayerIdentity`, `SprintPlayerIdentityOutline`)
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: the `Player` GameObject, `RunnerVisualPresenter`, `SprintUiTheme.Player`, `SprintUiShapes.RoundedRect(int)`.
- Produces: hierarchy `Player/<presentation>/PlayerMarker/{Plate,Label,Chevron}`; `SprintPlayerIdentityOutline.OutlineScale` constant `1.12f`.

- [ ] **Step 1: Write the failing test**

Add to `SprintPresentationGateTests`:

```csharp
[UnityTest]
public IEnumerator SprintPlayer_IsMarkedWithLabelPlateAndChevron()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    GameObject player = GameObject.Find("Player");
    var label = player.GetComponentInChildren<TextMesh>(true);
    Assert.That(label, Is.Not.Null, "player must carry a marker");
    Assert.That(label.name, Is.EqualTo("Label"));

    Transform markerRoot = label.transform.parent;
    Assert.That(markerRoot.name, Is.EqualTo("PlayerMarker"));
    Assert.That(markerRoot.localPosition.y, Is.GreaterThan(2f), "marker must clear the sprite's head");
    Assert.That(markerRoot.Find("Plate"), Is.Not.Null, "marker needs a plate so cyan reads over sky");
    Assert.That(markerRoot.Find("Chevron"), Is.Not.Null, "identity must not rely on colour alone");

    Assert.That(label.text, Is.EqualTo("PLAYER"));
    Assert.That(label.color, Is.EqualTo(SprintUiTheme.Player));

    var outline = player.GetComponentInChildren<SprintPlayerIdentityOutline>(true);
    Assert.That(outline, Is.Not.Null);
    Assert.That(outline.OutlineColor, Is.EqualTo(SprintUiTheme.Player));
    Assert.That(outline.Outline.transform.localScale.x,
        Is.EqualTo(SprintPlayerIdentityOutline.OutlineScale).Within(.001f));
}

[UnityTest]
public IEnumerator SprintRivals_DoNotUseThePlayerAccentColour()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    foreach (var rival in Object.FindObjectsByType<RivalRunnerAI>(FindObjectsSortMode.None))
    {
        Assert.That(rival.GetComponentInChildren<SprintPlayerIdentityOutline>(true), Is.Null,
            "cyan is reserved for the player");
        Assert.That(rival.GetComponentInChildren<TextMesh>(true), Is.Null,
            "rivals carry no PLAYER marker");
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests' \
  -testResults /tmp/kma-identity-red.xml -logFile /tmp/kma-identity-red.log
```

Expected: no `Plate` or `Chevron`; marker Y is `1.65`.

- [ ] **Step 3: Rebuild the marker**

Replace `EnsurePlayerIdentity` in `SprintFestivalPresentation.cs`:

```csharp
static void EnsurePlayerIdentity(Transform chrome)
{
    Transform chromeMarker = chrome.Find("PlayerMarker");
    if (chromeMarker != null)
        Object.Destroy(chromeMarker.gameObject);

    Transform player = GameObject.Find("Player")?.transform;
    if (player == null)
        return;

    Transform presentation = player.GetComponentInChildren<RunnerVisualPresenter>(true)?.transform ?? player;

    Transform stale = presentation.Find("PlayerLabel");
    if (stale != null)
        Object.DestroyImmediate(stale.gameObject);
    Transform existing = presentation.Find("PlayerMarker");
    if (existing != null)
        Object.DestroyImmediate(existing.gameObject);

    var marker = new GameObject("PlayerMarker").transform;
    marker.SetParent(presentation, false);
    marker.localPosition = new Vector3(0f, 2.15f, 0f);

    var plate = new GameObject("Plate", typeof(SpriteRenderer)).transform;
    plate.SetParent(marker, false);
    plate.localScale = new Vector3(1.35f, .42f, 1f);
    var plateRenderer = plate.GetComponent<SpriteRenderer>();
    plateRenderer.sprite = SprintUiShapes.RoundedRect(8);
    plateRenderer.color = SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .85f);
    plateRenderer.sortingOrder = 19;

    var labelObject = new GameObject("Label");
    labelObject.transform.SetParent(marker, false);
    var label = labelObject.AddComponent<TextMesh>();
    label.text = "PLAYER";
    label.fontSize = 48;
    label.characterSize = .055f;
    label.anchor = TextAnchor.MiddleCenter;
    label.color = SprintUiTheme.Player;
    var labelRenderer = labelObject.GetComponent<MeshRenderer>();
    if (labelRenderer != null)
        labelRenderer.sortingOrder = 20;

    var chevron = new GameObject("Chevron", typeof(SpriteRenderer)).transform;
    chevron.SetParent(marker, false);
    chevron.localPosition = new Vector3(0f, -.30f, 0f);
    chevron.localScale = new Vector3(.22f, .22f, 1f);
    chevron.localRotation = Quaternion.Euler(0f, 0f, 45f);
    var chevronRenderer = chevron.GetComponent<SpriteRenderer>();
    chevronRenderer.sprite = SprintUiShapes.RoundedRect(2);
    chevronRenderer.color = SprintUiTheme.Player;
    chevronRenderer.sortingOrder = 20;

    SpriteRenderer playerVisual = presentation.GetComponentInChildren<SpriteRenderer>(true);
    if (playerVisual != null && playerVisual.transform != plate && playerVisual.transform != chevron)
    {
        var identity = presentation.GetComponent<SprintPlayerIdentityOutline>()
            ?? presentation.gameObject.AddComponent<SprintPlayerIdentityOutline>();
        identity.Bind(playerVisual, SprintUiTheme.Player);
    }
}
```

`GetComponentInChildren<SpriteRenderer>` would otherwise pick up the plate; call `EnsurePlayerIdentity` **before** the marker is created, or keep the guard above. The guard above is the safer of the two — keep it.

- [ ] **Step 4: Soften the outline**

In `SprintPlayerIdentityOutline`, add the constant and use it:

```csharp
public const float OutlineScale = 1.12f;
```

and in `EnsureOutlineRenderer`, change:

```csharp
outlineObject.transform.localScale = new Vector3(OutlineScale, OutlineScale, 1f);
```

- [ ] **Step 5: Run the test and verify it passes**

Re-run the Step 2 command with `/tmp/kma-identity-green.xml`. Expected: 0 failed.

- [ ] **Step 6: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
rtk git commit -m "feat: make the sprint player unmistakable"
```

---

### Task 9: Text overflow guard and full regression

**Files:**
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: everything built in Tasks 5-8.
- Produces: no new runtime API.

- [ ] **Step 1: Write the failing test**

Add to `SprintPresentationGateTests`:

```csharp
[UnityTest]
public IEnumerator SprintLabels_AreNeverClipped()
{
    yield return LoadSprint();
    var scene = SceneManager.GetActiveScene();

    var controller = SceneObjects<SprintController>(scene)[0];
    var hud = SceneObjects<SprintHud>(scene)[0];

    // 100 / 100 m is the widest distance string the scoreboard ever renders.
    controller.AdvanceToDistance(100f);
    hud.Refresh();
    Canvas.ForceUpdateCanvases();
    yield return null;

    Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
    foreach (TMP_Text text in chrome.GetComponentsInChildren<TMP_Text>(true))
    {
        if (string.IsNullOrEmpty(text.text))
            continue;

        text.ForceMeshUpdate();
        Assert.That(text.isTextOverflowing, Is.False,
            $"'{text.text}' overflows {text.name} ({text.rectTransform.rect.size}).");
    }

    foreach (string tapName in new[] { "LeftTap", "RightTap" })
    {
        foreach (TMP_Text text in GameObject.Find(tapName).GetComponentsInChildren<TMP_Text>(true))
        {
            text.ForceMeshUpdate();
            Assert.That(text.isTextOverflowing, Is.False, $"'{text.text}' overflows in {tapName}.");
        }
    }
}
```

- [ ] **Step 2: Run the test and verify it fails or passes honestly**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests' \
  -testResults /tmp/kma-overflow.xml -logFile /tmp/kma-overflow.log
```

If it passes immediately, that is a legitimate result — the guard is a regression net, not a bug report. If it fails, the likely culprit is `100 / 100 m` against the scoreboard's 540-unit width; shrink `SprintUiTheme.Title` to `50` rather than widening the panel, and re-run Task 1's theme tests.

- [ ] **Step 3: Run the complete EditMode suite**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults /tmp/kma-editmode-final.xml -logFile /tmp/kma-editmode-final.log
```

Expected: 0 failed, 0 inconclusive. Read the `total` and `failed` attributes in the XML. The baseline before this work was `258/258`; the new EditMode tests raise the total.

- [ ] **Step 4: Run the complete PlayMode suite**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/kma-playmode-final.xml -logFile /tmp/kma-playmode-final.log
```

Expected: 0 failed, 0 inconclusive. Baseline was `243/243`. If the environment cannot run tests, record the exact blocker — do not report them as passing.

- [ ] **Step 5: Commit**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git add Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
rtk git commit -m "test: guard sprint labels against clipping"
```

---

### Task 10: Visual verification

**Files:**
- Produce evidence under: `Builds/Screenshots/`
- Create: `docs/qa/sprint-ui-polish-gate.md`

**Interfaces:** No new runtime API. This task produces verification evidence.

**Prerequisite:** a GUI Unity Editor must be open with this project loaded; `tools/qa-screenshot.sh` talks to a running Editor via `Builds/Screenshots/request.json`. Ask the user to open it if it is not already running.

- [ ] **Step 1: Capture the seven 16:9 states**

Set the Game View to `1920x1080`, then:

```bash
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_instruction.png Assets/_Project/Scenes/MG_Sprint.unity 1 true
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_countdown.png Assets/_Project/Scenes/MG_Sprint.unity 2
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_go.png Assets/_Project/Scenes/MG_Sprint.unity 5
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_race_early.png Assets/_Project/Scenes/MG_Sprint.unity 6 false 25
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_race_finish.png Assets/_Project/Scenes/MG_Sprint.unity 6 false 82
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_result_pass.png Assets/_Project/Scenes/MG_Sprint.unity 6 false -1 pass
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_16x9_result_fail.png Assets/_Project/Scenes/MG_Sprint.unity 6 false -1 fail
```

- [ ] **Step 2: Prove the captures are not stale**

This is the guard for how the previous round went wrong: `task6_countdown.png` and `task6_gameplay.png` were byte-identical despite being different forced states, and the work was signed off against them.

```bash
rtk sha256sum Builds/Screenshots/ui_16x9_*.png | rtk sort | rtk uniq -w64 -d
```

Expected: **no output**. Any duplicate hash means the Editor served a stale capture — recompile, restart the Editor, and retake the entire set before inspecting any image.

- [ ] **Step 3: Inspect every 16:9 capture**

Open each PNG and check, recording a pass or fail per image:

| Check | Applies to |
|---|---|
| Instruction plate is centred, fully readable, not covering a runner | instruction, countdown |
| Countdown digit is large and centred; GO! is visibly larger | countdown, go |
| Scoreboard shows `NN / 100 m` and a gold rank badge on one row, combo below | all race + result |
| Progress rail is visible with a gold fill and a cyan pip at the player's position | race_early, race_finish |
| PLAYER marker sits above the head on its plate, chevron visible, nothing overlapping the sprite | all race |
| LEFT and RIGHT read as buttons with an arrow and a label, one visibly highlighted | all race |
| Finish line absent at 25 m, present at 82 m | race_early vs race_finish |
| No legacy timer, hearts, or loose metric text anywhere | every image |
| Nothing clipped at any screen edge | every image |

- [ ] **Step 4: Capture and inspect 20:9**

Set the Game View to `2400x1080`, then:

```bash
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_20x9_race.png Assets/_Project/Scenes/MG_Sprint.unity 6 false 55
rtk bash tools/qa-screenshot.sh Builds/Screenshots/ui_20x9_result_pass.png Assets/_Project/Scenes/MG_Sprint.unity 6 false -1 pass
rtk sha256sum Builds/Screenshots/ui_20x9_*.png Builds/Screenshots/ui_16x9_race_early.png | rtk sort | rtk uniq -w64 -d
```

Expected: no duplicate hashes. Then confirm on the images that the scoreboard, rail, pause and both controls stay inside the safe area, the buttons have **not** stretched toward the centre, and the gap between them is wider than at 16:9.

- [ ] **Step 5: Record the gate**

Create `docs/qa/sprint-ui-polish-gate.md` with: the Unity version, the EditMode and PlayMode totals from Task 9 Steps 3-4, the list of captured PNGs with the staleness-hash result, the per-image checklist outcomes from Steps 3-4, and any accepted deviation. State plainly what was not verified — an Android device run is explicitly **not** part of this gate.

- [ ] **Step 6: Repository hygiene**

```bash
rtk git checkout -- Assets/_Project/Fonts/Nunito-Bold.asset
rtk git diff --check
rtk git status --short --untracked-files=all
rtk git log --oneline --decorate -12
```

Expected: no whitespace errors; one focused commit per task; `Nunito-Bold.asset` is clean; no `.superpowers/` artifacts staged. Screenshots under `Builds/Screenshots/` are evidence — check whether `Builds/` is gitignored and, if it is not, do not commit the PNGs.

- [ ] **Step 7: Commit**

```bash
rtk git add docs/qa/sprint-ui-polish-gate.md
rtk git commit -m "docs: record sprint UI polish verification gate"
```

---

## Self-Review Checklist

**Spec coverage:**

| Spec section | Task |
|---|---|
| Architecture: `SprintUiTheme` | 1 |
| Architecture: `SprintUiShapes` | 2 |
| Architecture: `SprintUiLayout`, sizing rule, all rects | 3 |
| Cleanup: `Timer`, `HeartBar`, `SprintMetrics` | 4 |
| Layout: rail, scoreboard rows, mode chip, pause | 5 |
| Visual system: colors, type scale, radii, `Shadow` over `Outline` | 1, 5 |
| Controls: structure, states, haptics, deleted sync math | 6 |
| Start flow: continuous instruction, countdown pops, `GoDuration` | 7 |
| Player identity: outline, marker plate + chevron, rail pip | 5 (pip), 8 |
| Out of scope: rival pips | not implemented, asserted absent in Task 8 |
| Verification: EditMode geometry, PlayMode contracts, overflow, screenshots, staleness guard | 3, 9, 10 |
| Acceptance criteria 1-8 | 3, 4, 8, 9, 10 |

**Placeholder scan:** no `TBD`, no "add error handling", no "similar to Task N". Every code step carries the actual code. Two steps carry conditional fallbacks with named remedies (Task 3 Step 4 instruction width, Task 9 Step 2 title size) rather than open-ended instructions.

**Type consistency:** `SprintUiTheme.WithAlpha`, `SprintUiShapes.RoundedRect(int)`, `SprintUiLayout.NamedRect`/`RaceRects`/`StartStateRects`/`ControlRect`, `SprintControlPresenter.Configure(7 args)`/`RefreshForTest`/`PressForTest`/`TickForTest`/`LeftScale`/`RightScale`/`HighlightedSide`, `SprintStartPresentation.Configure(4 args)`/`InstructionCopy`/`CountdownScale`/`InstructionVisible`, `SprintPlayerIdentityOutline.OutlineScale` — each defined once and consumed with the same name and arity everywhere.

**State ownership:** every new component is presentation-only. `SprintController`, `SprintRules`, `GameplayInputRouter`, `MinigameLifecycle`, `ResultPanel` and the progression route are untouched. `ScreenTapArea` remains the only input forwarder; Task 6 Step 6 proves a tap still reaches gameplay exactly once.

**Scope:** all runtime changes are isolated to `MG_Sprint` and Sprint-namespaced files. The one shared file touched is `SprintFestivalPresentation`'s canvas-wide name lookup, which only deactivates objects inside the Sprint scene's own HUD instance. Task 7 Step 6 proves other minigames' tutorials are unchanged.
