# One UI Kit Across Minigames Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sprint, Football and Volleyball draw every button, joystick, slider, bar, panel, chip, countdown, pause and result screen with one navy UI kit, so the three minigames look like one game.

**Architecture:** A kit in the existing `KMA.Gameplay.UI` assembly (namespace `KMA.UI.Kit`) holds the design tokens (`MinigameUiTheme`), a pure shape rasteriser, an asset (`UiKitAssets`, loaded from `Resources`) pointing at sprites baked once by an editor menu, and `UiKit`, a set of builders that run both at runtime (Sprint, pause menu, tutorial) and in editor configurators (Football, Volleyball). An editor styler restyles the shared prefabs in place without renaming nodes. Football drops its own canvas and result panel and moves onto the shared `S2_HUD_Minigame`, `PhaseOverlay`, `ResultPanel` and `PausePanel`. A shared audit (`MinigameStyleAudit`) pins "every minigame graphic uses the kit".

**Tech Stack:** Unity 6000.3.23f1, C#, uGUI + TextMeshPro, Unity Test Framework (NUnit, EditMode and PlayMode), Unity editor scripting (`TextureImporter`, `AssetDatabase`, `PrefabUtility`, `EditorSceneManager`), Git Bash on Windows.

**Spec:** `docs/superpowers/specs/2026-09-29-minigame-ui-kit-design.md`

## Global Constraints

- Target style is the Sprint navy style. Tokens (exact values):
  - Colours: `Surface` #08233D, `TextPrimary` #FFF9E7, `Accent` #FFCA3A, `Player` #3AE6FF, `Energy` #FF595E, `TextOutline` #031221, `Success` #5EDE8C, `Track` = white at 22% alpha, `Scrim` = #031221 at 70% alpha.
  - Surface alphas: `SurfaceOpaque` .92, `SurfaceSoft` .82, `SurfaceControl` .42, `SurfaceControlActive` .55. Border alphas: rest .25, hint .75. Disabled: fill .30, border .15, disabled button alpha .45.
  - Font sizes: `Display` 160, `Title` 54, `Headline` 48, `BodyLarge` 40, `Body` 32, `Caption` 24. **No text below 24**, including auto-size minimums.
  - Radii: `RadiusPanel` 24, `RadiusControl` 36, `RadiusPause` 20, bars use half their height, `BorderWidth` 3.
  - Spacing: 8 / 16 / 24 / 32. Shadow: black at 35% alpha, offset (0,-4). Press: scale 0.94, lighten 15%, ease back over 0.1 s.
  - Sizes (reference px, 1080 high): `ButtonHeight` 88, `RoundButton` 220, `JoystickBase` 224, `JoystickKnob` 112, `BarHeight` 32, `SliderKnob` 56.
- Button variants: **Primary** `Accent` face + `Surface` text; **Secondary** `Accent` 3 px border around an opaque `Surface` fill + `TextPrimary` text; **Danger** `Energy` face + `Surface` text.
- Identical tokens in all three games: no per-game accent colour.
- Font: `Assets/_Project/Fonts/Baloo2-ExtraBold.asset`; stroke material `Assets/_Project/Fonts/Baloo2-ExtraBold-TextStrokeDark.mat`. No legacy `UnityEngine.UI.Text` in minigame UI.
- Baked sprites live in `Assets/_Project/Art/UI/Kit/` (`RoundRect20.png`, `RoundRect24.png`, `RoundRect36.png`, `Circle.png` 128 px, `Ring.png` 128 px with a band of 3/64 of the diameter). Import: Sprite, bilinear, no mipmaps, clamp, 100 PPU, `FullRect` mesh, border equal to the radius.
- `UiKitAssets` lives at `Assets/_Project/Settings/UI/Resources/UiKitAssets.asset` and is loaded with `UiKitAssets.Load()`.
- Menu and Map keep the brutalist `UITheme`; do not touch them.
- Shared prefabs are restyled in place: node names and serialized references stay, so the router, tests, `GameOver` and `Punishment` keep working. Do **not** run `MinigameUIAssembler.AssembleTask5Presentation` or assemble `GameOver` (its rule removes the shared prefabs from non-gameplay scenes).
- `ResultPanel.Continue()` keeps raising the **preview route**; `Retry()` raises `ResultPanelActions.Retry`. `SceneRouter` is not changed.
- The Unity Editor must be closed while running batch-mode commands. Batch mode cannot open a project that is already open.
- Python is not installed on this machine, so `tools/run-unity-tests.sh` cannot parse results. Run tests with this command and read the summary line it prints:

  ```bash
  UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
  mkdir -p Builds/TestResults
  "$UNITY" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform <EditMode|PlayMode> \
    -testFilter "<filter>" -testResults Builds/TestResults/<name>.xml -logFile Builds/TestResults/<name>.log
  grep -m1 -o '<test-run[^>]*' Builds/TestResults/<name>.xml || grep -n "error CS\|Exception" Builds/TestResults/<name>.log | head -20
  grep -o 'fullname="[^"]*" [^>]*result="Failed"' Builds/TestResults/<name>.xml | head -20
  ```

  In this plan, "Run tests: `<Platform>` `<filter>` `<name>`" means that command with those three values. A filter may list several names separated by `;`. When a step expects a compile failure, the XML is missing and the log shows `error CS…`.
- Run an editor method with:

  ```bash
  "$UNITY" -batchmode -projectPath "$(pwd -W)" -executeMethod <Method> -logFile Builds/<name>.log -quit; echo "exit=$?"
  grep -n "error CS\|Exception\|\[KMA\]" Builds/<name>.log | head -20
  ```

- Git: commit directly on `master`. Commit messages carry **no** `Co-Authored-By` trailer.
- Commit the `.meta` files Unity creates next to new or changed assets and scripts. When deleting a file, delete its `.meta` too.

## Review Focus

1. **Sprint and Volleyball "Tiếp tục" must still leave the result screen.** The panel now implements the retry interface and the router calls `SetActionPending` on it. Expected: one tap raises the preview route exactly once and a second tap does nothing. This is pinned in Task 4, `PanelsWithoutRetryKeepTheirSingleContinueAction`, and by the existing `CoreLoopTests` and Sprint gate tests.
2. **A failed scene load must not leave the result screen dead.** Expected: after `SetActionPending(false, error)` the error shows and both buttons work again. This is pinned in Task 4, `ExhaustedFailureHidesRetryAndPendingActionCanRecoverAfterError`.
3. **Disabled buttons must look disabled.** Kit buttons use `Transition.None`, so the old ColorTint dimming is gone. Expected: a non-interactable kit button fades to 45% and ignores presses (Football's SHOOT before BẮT ĐẦU, result buttons while a load is pending). This is pinned in Task 3, `DisabledButtonsFadeAndIgnorePresses`.
4. **Football must not start before the player picks a difficulty.** `PhaseOverlay` used to open the start gate of every non-Sprint game. Expected: a minigame that owns its gate stays in `Tutorial` until it opens the gate itself; then the shared 3-2-1 shows. This is pinned in Task 5, `OwnedGateStaysClosedUntilTheMinigameOpensIt`.
5. **Re-running the baker, the styler or a configurator must not duplicate parts or change asset GUIDs.** Expected: one `Fill`, one pair of pause bars, stable sprite GUIDs. This is pinned in Task 2, `BakingTwiceKeepsTheSameSpriteGuids`, in Task 3, `StyleButton_ReplacesBrutalistPartsAndIsIdempotent` and `StylePauseButton_DrawsTwoBarsOnceAndDropsLegacyText`, and in Task 9, by building the Football scene twice.

---

## File Structure

| Path | Change | Responsibility |
|---|---|---|
| `Assets/_Project/Scripts/UI/Kit/MinigameUiTheme.cs` | Create (Task 1) | Tokens and colour maths |
| `Assets/_Project/Scripts/UI/Kit/UiShapeRaster.cs` | Create (Task 1) | Pure anti-aliased shape pixels |
| `Assets/_Project/Scripts/UI/Kit/UiKitAssets.cs` | Create (Task 2) | Baked sprites, font, stroke material; `Load()` |
| `Assets/Editor/UiKitSpriteBaker.cs` | Create (Task 2) | `KMA/UI/Bake UI Kit Sprites` |
| `Assets/_Project/Art/UI/Kit/*.png` | Create (Task 2) | Baked sprites |
| `Assets/_Project/Settings/UI/Resources/UiKitAssets.asset` | Create (Task 2) | The asset |
| `Assets/_Project/Scripts/UI/Kit/UiKit.cs` | Create (Task 3) | Widget builders and restylers |
| `Assets/_Project/Scripts/UI/Kit/UiKitHandles.cs` | Create (Task 3) | `ButtonVariant` and handle structs |
| `Assets/_Project/Scripts/UI/Kit/KitPressFeedback.cs` | Create (Task 3) | Shared press / disabled response |
| `Assets/_Project/Scripts/UI/Kit/KitBar.cs` | Create (Task 3) | Rounded bar with fill, pip, label |
| `Assets/_Project/Scripts/UI/Kit/KitControlState.cs` | Create (Task 3) | Rest / Hint / Pressed / Disabled colours |
| `Assets/_Project/Scripts/UI/ResultPanel.cs` | Rewrite (Task 4) | Shared result screen with retry, detail, pending, reveal |
| `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs` | Modify (Task 5) | Three presentation flags |
| `Assets/_Project/Scripts/UI/PhaseOverlay.cs` | Modify (Task 5) | Uses the flags instead of class names |
| `Assets/_Project/Scripts/UI/PausePanel.cs`, `TutorialOverlay.cs`, `MinigameHUD.cs`, `HeartBar.cs` | Modify (Task 6) | Kit styling at runtime |
| `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs` | Modify (Tasks 6, 9) | Kit pause button; no theme refs; no owner branch |
| `Assets/Editor/MinigamePrefabStyler.cs` | Create (Task 6) | Restyles the three shared prefabs and Punishment's pause |
| `Assets/_Project/Scripts/Gameplay/Sprint/*` | Modify (Task 7) | Sprint on the kit; `SprintUiShapes`, `SprintResultPresentation` deleted |
| `Assets/Editor/VolleyballSceneConfigurator.cs`, `Scripts/Gameplay/Volleyball/{VirtualJoystick,ActionButton}.cs` | Modify (Task 8) | Volleyball on the kit |
| `Assets/Editor/FootballSceneConfigurator.cs`, `Scripts/Gameplay/Football/{FootballHud,FootballController}.cs` | Modify (Task 9) | Football on the kit and the shared screens |
| `Assets/_Project/Scripts/Gameplay/Football/FootballResultPanel.cs`, `Scripts/Gameplay/Common/MinigamePresentationOwner.cs` | Delete (Task 9) | Replaced by `ResultPanel` / no longer needed |
| `Assets/_Project/Scripts/UI/Kit/MinigameStyleAudit.cs` | Create (Task 10) | Shared style rules for guard tests |
| `Assets/Editor/PlayModeScreenshot.cs`, `tools/qa-screenshot.sh` | Modify (Task 11) | QA seam to open the pause menu |

---

### Task 1: Shared tokens and shape rasteriser

**Files:**
- Create: `Assets/_Project/Scripts/UI/Kit/MinigameUiTheme.cs`
- Create: `Assets/_Project/Scripts/UI/Kit/UiShapeRaster.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs` (use the rasteriser; deleted in Task 7)
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`, `SprintControlPresenter.cs`, `SprintResultPresentation.cs` (token rename)
- Delete: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs` (+ `.meta`)
- Move: `Assets/Tests/EditMode/Presentation/SprintUiThemeTests.cs` → `MinigameUiThemeTests.cs`; `SprintUiShapesTests.cs` → `UiShapeRasterTests.cs` (keep each `.meta` with its file so GUIDs survive)
- Modify: `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs`, `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs` (token rename)

**Interfaces:**
- Produces: `KMA.UI.Kit.MinigameUiTheme` (every constant listed in Global Constraints, plus `WithAlpha(Color, float)`, `Lighten(Color, float)`, `ContrastRatio(Color, Color)`, `RgbEquals(Color, Color)`, `AllFontSizes()`, `TextColors()`, `Palette()`); `KMA.UI.Kit.UiShapeRaster` (`RoundedRectSize(int)`, `RoundedRect(int)`, `Circle(int)`, `Ring(int, float)`), all returning `Color32[]` of white pixels.

- [ ] **Step 1: Move the two test files so their history and GUIDs follow**

```bash
cd Assets/Tests/EditMode/Presentation
git mv SprintUiThemeTests.cs MinigameUiThemeTests.cs
git mv SprintUiThemeTests.cs.meta MinigameUiThemeTests.cs.meta
git mv SprintUiShapesTests.cs UiShapeRasterTests.cs
git mv SprintUiShapesTests.cs.meta UiShapeRasterTests.cs.meta
cd -
```

- [ ] **Step 2: Write the failing theme tests**

Replace the whole content of `Assets/Tests/EditMode/Presentation/MinigameUiThemeTests.cs`:

```csharp
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameUiThemeTests
    {
        [Test]
        public void EveryFontStep_MeetsTheMinimumSize()
        {
            foreach (float size in MinigameUiTheme.AllFontSizes())
                Assert.That(size, Is.GreaterThanOrEqualTo(MinigameUiTheme.MinimumFontSize),
                    $"Font step {size} is below the {MinigameUiTheme.MinimumFontSize} floor.");
        }

        [Test]
        public void EveryTextColor_ReachesReadableContrastOnSurface()
        {
            foreach (Color color in MinigameUiTheme.TextColors())
                Assert.That(MinigameUiTheme.ContrastRatio(color, MinigameUiTheme.Surface),
                    Is.GreaterThanOrEqualTo(4.5f), $"Colour {color} is unreadable on Surface.");
        }

        [Test]
        public void SurfaceText_IsReadableOnTheAccentAndEnergyFaces()
        {
            Assert.That(MinigameUiTheme.ContrastRatio(MinigameUiTheme.Surface, MinigameUiTheme.Accent),
                Is.GreaterThanOrEqualTo(4.5f), "Primary button text");
            Assert.That(MinigameUiTheme.ContrastRatio(MinigameUiTheme.Surface, MinigameUiTheme.Energy),
                Is.GreaterThanOrEqualTo(4.5f), "Danger button text");
        }

        [Test]
        public void ContrastRatio_IsSymmetricAndBoundedByTheWcagRange()
        {
            float forward = MinigameUiTheme.ContrastRatio(Color.white, Color.black);
            float reverse = MinigameUiTheme.ContrastRatio(Color.black, Color.white);
            Assert.That(forward, Is.EqualTo(reverse).Within(.001f));
            Assert.That(forward, Is.EqualTo(21f).Within(.05f));
            Assert.That(MinigameUiTheme.ContrastRatio(Color.white, Color.white), Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void PlayerAccent_IsDistinctFromEveryOtherToken()
        {
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.Accent));
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.Energy));
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.TextPrimary));
            Assert.That(MinigameUiTheme.Player, Is.Not.EqualTo(MinigameUiTheme.Success));
        }

        [Test]
        public void WithAlpha_ReplacesAlphaAndClamps()
        {
            Assert.That(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, .42f).a, Is.EqualTo(.42f).Within(.001f));
            Assert.That(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, 2f).a, Is.EqualTo(1f).Within(.001f));
            Assert.That(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, -1f).a, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void Lighten_MovesTowardWhiteAndKeepsAlpha()
        {
            Color source = MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, .5f);
            Color lighter = MinigameUiTheme.Lighten(source, .15f);
            Assert.That(lighter.a, Is.EqualTo(.5f).Within(.001f));
            Assert.That(lighter.b, Is.GreaterThan(source.b));
            Assert.That(lighter, Is.EqualTo(Color.Lerp(source, new Color(1f, 1f, 1f, .5f), .15f)));
        }

        [Test]
        public void Palette_HoldsEveryColourTokenAndMatchesIgnoringAlpha()
        {
            Color[] palette = MinigameUiTheme.Palette();
            foreach (Color token in new[]
                     {
                         MinigameUiTheme.Surface, MinigameUiTheme.TextPrimary, MinigameUiTheme.Accent,
                         MinigameUiTheme.Player, MinigameUiTheme.Energy, MinigameUiTheme.TextOutline,
                         MinigameUiTheme.Success, MinigameUiTheme.Track, MinigameUiTheme.Scrim
                     })
                Assert.That(System.Array.Exists(palette, entry => MinigameUiTheme.RgbEquals(entry, token)), Is.True,
                    token.ToString());
            Assert.That(MinigameUiTheme.RgbEquals(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, .3f),
                MinigameUiTheme.Surface), Is.True);
            Assert.That(MinigameUiTheme.RgbEquals(new Color32(255, 152, 0, 255), MinigameUiTheme.Accent), Is.False,
                "Volleyball's old orange is not a token.");
        }
    }
}
```

- [ ] **Step 3: Write the failing rasteriser tests**

Replace the whole content of `Assets/Tests/EditMode/Presentation/UiShapeRasterTests.cs`:

```csharp
using System;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class UiShapeRasterTests
    {
        static float Alpha(Color32[] pixels, int size, int x, int y) => pixels[y * size + x].a / 255f;

        [Test]
        public void RoundedRect_IsTransparentAtTheCornerAndOpaqueAtTheCentre()
        {
            int size = UiShapeRaster.RoundedRectSize(24);
            Color32[] pixels = UiShapeRaster.RoundedRect(24);
            Assert.That(size, Is.EqualTo(50));
            Assert.That(pixels, Has.Length.EqualTo(size * size));
            Assert.That(Alpha(pixels, size, 0, 0), Is.EqualTo(0f).Within(.01f), "corner must be cut away");
            Assert.That(Alpha(pixels, size, size / 2, size / 2), Is.EqualTo(1f).Within(.01f), "centre must be solid");
        }

        [Test]
        public void RoundedRect_IsOpaqueAtEachEdgeMidpoint()
        {
            int size = UiShapeRaster.RoundedRectSize(24);
            Color32[] pixels = UiShapeRaster.RoundedRect(24);
            int mid = size / 2;
            Assert.That(Alpha(pixels, size, mid, 0), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, size, mid, size - 1), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, size, 0, mid), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, size, size - 1, mid), Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_HasASoftenedCornerRatherThanAHardStep()
        {
            int size = UiShapeRaster.RoundedRectSize(24);
            Color32[] pixels = UiShapeRaster.RoundedRect(24);
            int partial = 0;
            for (int y = 0; y <= 24; y++)
                for (int x = 0; x <= 24; x++)
                {
                    float alpha = Alpha(pixels, size, x, y);
                    if (alpha > .05f && alpha < .95f)
                        partial++;
                }
            Assert.That(partial, Is.GreaterThanOrEqualTo(4), "corner should be antialiased along the arc");
        }

        [Test]
        public void RoundedRect_AcceptsZeroRadiusAsAPlainSquare()
        {
            Assert.That(UiShapeRaster.RoundedRectSize(0), Is.EqualTo(4));
            Assert.That(Alpha(UiShapeRaster.RoundedRect(0), 4, 0, 0), Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_RejectsNegativeRadius() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => UiShapeRaster.RoundedRect(-1));

        [Test]
        public void Circle_IsSolidInsideAndClearAtTheCorners()
        {
            Color32[] pixels = UiShapeRaster.Circle(128);
            Assert.That(Alpha(pixels, 128, 64, 64), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, 128, 0, 0), Is.EqualTo(0f).Within(.01f));
            Assert.That(Alpha(pixels, 128, 64, 0), Is.GreaterThan(.5f), "the circle touches its edge midpoints");
        }

        [Test]
        public void Ring_IsHollowInTheMiddleAndSolidOnItsBand()
        {
            Color32[] pixels = UiShapeRaster.Ring(128, 6f);
            Assert.That(Alpha(pixels, 128, 64, 64), Is.EqualTo(0f).Within(.01f), "hollow centre");
            Assert.That(Alpha(pixels, 128, 64, 2), Is.EqualTo(1f).Within(.01f), "solid band");
            Assert.That(Alpha(pixels, 128, 64, 10), Is.EqualTo(0f).Within(.01f), "inside the band");
            Assert.That(Alpha(pixels, 128, 0, 0), Is.EqualTo(0f).Within(.01f), "outside the ring");
        }

        [Test]
        public void Ring_RejectsABandWiderThanItsRadius() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => UiShapeRaster.Ring(128, 64f));
    }
}
```

- [ ] **Step 4: Run the tests and confirm they fail to compile**

Run tests: `EditMode` `KMA.Tests.Presentation.MinigameUiThemeTests;KMA.Tests.Presentation.UiShapeRasterTests` `kit-tokens-red`
Expected: no XML; the log shows `error CS0246` / `CS0103` for `MinigameUiTheme` and `UiShapeRaster`.

- [ ] **Step 5: Create `MinigameUiTheme`**

`Assets/_Project/Scripts/UI/Kit/MinigameUiTheme.cs`:

```csharp
using UnityEngine;

namespace KMA.UI.Kit
{
    /// Design tokens shared by every minigame's UI. Pure data: no scene access.
    public static class MinigameUiTheme
    {
        public static readonly Color Surface = new Color32(8, 35, 61, 255);
        public static readonly Color TextPrimary = new Color32(255, 249, 231, 255);
        public static readonly Color Accent = new Color32(255, 202, 58, 255);
        public static readonly Color Player = new Color32(58, 230, 255, 255);
        public static readonly Color Energy = new Color32(255, 89, 94, 255);
        public static readonly Color TextOutline = new Color32(3, 18, 33, 255);
        public static readonly Color Success = new Color32(94, 222, 140, 255);
        public static readonly Color Track = new Color(1f, 1f, 1f, .22f);
        public static readonly Color Scrim = new Color(3f / 255f, 18f / 255f, 33f / 255f, .7f);

        public const float SurfaceOpaque = .92f;
        public const float SurfaceSoft = .82f;
        public const float SurfaceControl = .42f;
        public const float SurfaceControlActive = .55f;
        public const float SurfaceDisabled = .30f;
        public const float BorderRest = .25f;
        public const float BorderHint = .75f;
        public const float BorderDisabled = .15f;
        public const float DisabledAlpha = .45f;

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

        public const float PressScale = .94f;
        public const float PressLighten = .15f;
        public const float PressRestoreSeconds = .1f;

        public const float ButtonHeight = 88f;
        public const float RoundButton = 220f;
        public const float JoystickBase = 224f;
        public const float JoystickKnob = 112f;
        public const float BarHeight = 32f;
        public const float SliderKnob = 56f;

        public static float[] AllFontSizes() =>
            new[] { Display, Title, Headline, BodyLarge, Body, Caption };

        public static Color[] TextColors() =>
            new[] { TextPrimary, Accent, Player, Energy, Success };

        /// Every colour a minigame UI may draw with, compared without alpha (Track is white).
        public static Color[] Palette() =>
            new[] { Surface, TextPrimary, Accent, Player, Energy, TextOutline, Success, Track };

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// Moves a colour toward white by amount (0..1) and keeps its alpha.
        public static Color Lighten(Color color, float amount)
        {
            Color lighter = Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
            lighter.a = color.a;
            return lighter;
        }

        /// True when both colours have the same 8-bit RGB, whatever their alpha.
        public static bool RgbEquals(Color a, Color b)
        {
            Color32 first = a;
            Color32 second = b;
            return first.r == second.r && first.g == second.g && first.b == second.b;
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

- [ ] **Step 6: Create `UiShapeRaster`**

`Assets/_Project/Scripts/UI/Kit/UiShapeRaster.cs`:

```csharp
using System;
using UnityEngine;

namespace KMA.UI.Kit
{
    /// Anti-aliased white shapes as pixel arrays, row by row from the bottom. Callers tint
    /// through Image.color. Pure: used by the editor baker and by tests.
    public static class UiShapeRaster
    {
        public static int RoundedRectSize(int radius) => radius > 0 ? radius * 2 + 2 : 4;

        public static Color32[] RoundedRect(int radius)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Corner radius cannot be negative.");

            int size = RoundedRectSize(radius);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = White(RoundedRectCoverage(x, y, size, radius));
            return pixels;
        }

        public static Color32[] Circle(int diameter) => Disc(diameter, 0f);

        public static Color32[] Ring(int diameter, float bandWidth)
        {
            if (bandWidth <= 0f || bandWidth * 2f >= diameter)
                throw new ArgumentOutOfRangeException(nameof(bandWidth), "The band must be thinner than the radius.");
            return Disc(diameter, bandWidth);
        }

        static Color32[] Disc(int diameter, float bandWidth)
        {
            if (diameter < 2)
                throw new ArgumentOutOfRangeException(nameof(diameter), "A disc needs at least 2 pixels.");

            float radius = diameter * .5f;
            var pixels = new Color32[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + .5f - radius;
                    float dy = y + .5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01(radius - distance + .5f);
                    if (bandWidth > 0f)
                        coverage *= 1f - Mathf.Clamp01(radius - bandWidth - distance + .5f);
                    pixels[y * diameter + x] = White(coverage);
                }
            }
            return pixels;
        }

        static Color32 White(float coverage) =>
            new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage) * 255f));

        /// Coverage of one pixel by the rounded rectangle, antialiased over one pixel.
        static float RoundedRectCoverage(int x, int y, int size, int radius)
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

- [ ] **Step 7: Point Sprint at the shared tokens and rasteriser**

Delete the old token class:

```bash
git rm Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs Assets/_Project/Scripts/Gameplay/Sprint/SprintUiTheme.cs.meta
```

In `SprintFestivalPresentation.cs`, `SprintControlPresenter.cs`, `SprintResultPresentation.cs`, `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs` and `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`, replace every `SprintUiTheme.` with `MinigameUiTheme.` and add `using KMA.UI.Kit;` to the usings:

```bash
for f in Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs \
         Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs \
         Assets/_Project/Scripts/Gameplay/Sprint/SprintResultPresentation.cs \
         Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs \
         Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs; do
  sed -i 's/SprintUiTheme\./MinigameUiTheme./g' "$f"
  grep -q '^using KMA.UI.Kit;' "$f" || sed -i '0,/^using /s//using KMA.UI.Kit;\nusing /' "$f"
done
grep -rn "SprintUiTheme" Assets --include=*.cs
```

Expected: the last grep prints nothing.

In `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs`, replace the texture fill and delete the private `Coverage` method so the class delegates to the rasteriser (the class itself is deleted in Task 7):

```csharp
            int size = UiShapeRaster.RoundedRectSize(radius);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"SprintRoundedRect{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixels32(UiShapeRaster.RoundedRect(radius));
            texture.Apply(false, false);
```

and add `using KMA.UI.Kit;` at the top. The negative-radius guard stays as the first statement.

- [ ] **Step 8: Run the tests and confirm they pass**

Run tests: `EditMode` `KMA.Tests.Presentation.MinigameUiThemeTests;KMA.Tests.Presentation.UiShapeRasterTests;KMA.Tests.Presentation.SprintUiLayoutTests` `kit-tokens`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Scripts/UI/Kit Assets/_Project/Scripts/Gameplay/Sprint Assets/Tests/EditMode/Presentation Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
git commit -m "feat(ui): shared minigame tokens and shape rasteriser"
```

---

### Task 2: Baked kit sprites and `UiKitAssets`

**Files:**
- Create: `Assets/_Project/Scripts/UI/Kit/UiKitAssets.cs`
- Create: `Assets/Editor/UiKitSpriteBaker.cs`
- Create (by running the baker): `Assets/_Project/Art/UI/Kit/{RoundRect20,RoundRect24,RoundRect36,Circle,Ring}.png`, `Assets/_Project/Settings/UI/Resources/UiKitAssets.asset`
- Test: `Assets/Tests/EditMode/EditorTools/UiKitAssetsTests.cs`

**Interfaces:**
- Consumes: `UiShapeRaster` (Task 1).
- Produces: `KMA.UI.Kit.UiKitAssets` with `Load()`, `RoundRect20`, `RoundRect24`, `RoundRect36`, `Circle`, `Ring`, `Font` (`TMP_FontAsset`), `OutlineMaterial`, `RoundRectFor(float radius)`, `AllSprites()`, `ResourcePath`, `AssetPath`; `KMA.EditorTools.UiKitSpriteBaker.Bake()` (static void), `SpriteDir`, `CircleDiameter`, `RingWidth`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/EditorTools/UiKitAssetsTests.cs`:

```csharp
#if UNITY_EDITOR
using System.IO;
using KMA.EditorTools;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class UiKitAssetsTests
    {
        [OneTimeSetUp]
        public void Bake() => UiKitSpriteBaker.Bake();

        [Test]
        public void LoadReturnsTheBakedAssetWithFiveSpritesTheFontAndTheStroke()
        {
            UiKitAssets assets = UiKitAssets.Load();
            Assert.That(assets, Is.SameAs(AssetDatabase.LoadAssetAtPath<UiKitAssets>(UiKitAssets.AssetPath)));
            Assert.That(assets.AllSprites(), Has.Length.EqualTo(5));
            Assert.That(assets.AllSprites(), Has.All.Not.Null);
            Assert.That(assets.Font, Is.Not.Null);
            Assert.That(assets.Font.name, Is.EqualTo("Baloo2-ExtraBold"));
            Assert.That(assets.OutlineMaterial, Is.Not.Null);
            Assert.That(assets.OutlineMaterial.name, Is.EqualTo("Baloo2-ExtraBold-TextStrokeDark"));
        }

        [TestCase(20)]
        [TestCase(24)]
        [TestCase(36)]
        public void EachRoundRectCarriesABorderEqualToItsRadius(int radius)
        {
            Sprite sprite = UiKitAssets.Load().RoundRectFor(radius);
            Assert.That(sprite.name, Is.EqualTo("RoundRect" + radius));
            Assert.That(sprite.border, Is.EqualTo(new Vector4(radius, radius, radius, radius)));
            Assert.That(sprite.rect.width, Is.EqualTo(radius * 2 + 2));
        }

        [Test]
        public void OtherRadiiFallBackToRoundRect24() =>
            Assert.That(UiKitAssets.Load().RoundRectFor(16f), Is.SameAs(UiKitAssets.Load().RoundRect24));

        [Test]
        public void EverySpriteImportsBilinearWithoutMipmapsAt100PixelsPerUnit()
        {
            foreach (Sprite sprite in UiKitAssets.Load().AllSprites())
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), sprite.name);
                Assert.That(importer.mipmapEnabled, Is.False, sprite.name);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), sprite.name);
                Assert.That(sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), sprite.name);
            }
        }

        [Test]
        public void CircleIsSolidAndRingIsHollow()
        {
            UiKitAssets assets = UiKitAssets.Load();
            Assert.That(CentreAlpha(assets.Circle), Is.EqualTo(1f).Within(.01f));
            Assert.That(CentreAlpha(assets.Ring), Is.EqualTo(0f).Within(.01f));
            Assert.That(assets.Circle.rect.width, Is.EqualTo(UiKitSpriteBaker.CircleDiameter));
        }

        [Test]
        public void BakingTwiceKeepsTheSameSpriteGuids()
        {
            string before = AssetDatabase.AssetPathToGUID(UiKitSpriteBaker.SpriteDir + "/Circle.png");
            UiKitSpriteBaker.Bake();
            Assert.That(AssetDatabase.AssetPathToGUID(UiKitSpriteBaker.SpriteDir + "/Circle.png"), Is.EqualTo(before));
            Assert.That(UiKitAssets.Load().Circle, Is.Not.Null);
        }

        static float CentreAlpha(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            float alpha = texture.GetPixel(texture.width / 2, texture.height / 2).a;
            Object.DestroyImmediate(texture);
            return alpha;
        }
    }
}
#endif
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run tests: `EditMode` `KMA.Tests.EditorTools.UiKitAssetsTests` `kit-assets-red`
Expected: no XML; `error CS0246` for `UiKitAssets` / `UiKitSpriteBaker`.

- [ ] **Step 3: Create `UiKitAssets`**

`Assets/_Project/Scripts/UI/Kit/UiKitAssets.cs`:

```csharp
using System;
using TMPro;
using UnityEngine;

namespace KMA.UI.Kit
{
    /// The baked sprites, font and stroke material every minigame UI is drawn with. It lives in a
    /// Resources folder so runtime builders (Sprint, the pause menu, the tutorial) and editor
    /// configurators load the same asset without a scene reference.
    public sealed class UiKitAssets : ScriptableObject
    {
        public const string ResourcePath = "UiKitAssets";
        public const string AssetPath = "Assets/_Project/Settings/UI/Resources/UiKitAssets.asset";

        [SerializeField] Sprite roundRect20;
        [SerializeField] Sprite roundRect24;
        [SerializeField] Sprite roundRect36;
        [SerializeField] Sprite circle;
        [SerializeField] Sprite ring;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material outlineMaterial;

        static UiKitAssets cached;

        public Sprite RoundRect20 => roundRect20;
        public Sprite RoundRect24 => roundRect24;
        public Sprite RoundRect36 => roundRect36;
        public Sprite Circle => circle;
        public Sprite Ring => ring;
        public TMP_FontAsset Font => font;
        public Material OutlineMaterial => outlineMaterial;

        public static UiKitAssets Load()
        {
            if (cached == null)
                cached = Resources.Load<UiKitAssets>(ResourcePath);
            if (cached == null)
                throw new InvalidOperationException($"Missing {AssetPath}; run KMA/UI/Bake UI Kit Sprites.");
            return cached;
        }

        /// The baked rounded rect with this radius, or RoundRect24 for any other radius; callers
        /// scale its corners with Image.pixelsPerUnitMultiplier.
        public Sprite RoundRectFor(float radius)
        {
            if (Mathf.Approximately(radius, 20f))
                return roundRect20;
            if (Mathf.Approximately(radius, 36f))
                return roundRect36;
            return roundRect24;
        }

        public Sprite[] AllSprites() => new[] { roundRect20, roundRect24, roundRect36, circle, ring };

        public void Configure(Sprite roundRect20Sprite, Sprite roundRect24Sprite, Sprite roundRect36Sprite,
            Sprite circleSprite, Sprite ringSprite, TMP_FontAsset fontAsset, Material strokeMaterial)
        {
            roundRect20 = roundRect20Sprite;
            roundRect24 = roundRect24Sprite;
            roundRect36 = roundRect36Sprite;
            circle = circleSprite;
            ring = ringSprite;
            font = fontAsset;
            outlineMaterial = strokeMaterial;
        }
    }
}
```

- [ ] **Step 4: Create the baker**

`Assets/Editor/UiKitSpriteBaker.cs`:

```csharp
#if UNITY_EDITOR
using System;
using System.IO;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    /// Writes the UI kit's white, anti-aliased sprites once and points UiKitAssets at them.
    /// Re-running rewrites the pixels in place, so GUIDs and scene references survive.
    public static class UiKitSpriteBaker
    {
        public const string SpriteDir = "Assets/_Project/Art/UI/Kit";
        public const int CircleDiameter = 128;
        public const float RingWidth = CircleDiameter * 3f / 64f;
        const string FontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";
        const string OutlineMaterialPath = "Assets/_Project/Fonts/Baloo2-ExtraBold-TextStrokeDark.mat";

        [MenuItem("KMA/UI/Bake UI Kit Sprites")]
        public static void Bake()
        {
            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(Path.GetDirectoryName(UiKitAssets.AssetPath));

            Sprite roundRect20 = WriteRoundRect(20);
            Sprite roundRect24 = WriteRoundRect(24);
            Sprite roundRect36 = WriteRoundRect(36);
            Sprite circle = Write("Circle", CircleDiameter, UiShapeRaster.Circle(CircleDiameter), Vector4.zero);
            Sprite ring = Write("Ring", CircleDiameter, UiShapeRaster.Ring(CircleDiameter, RingWidth), Vector4.zero);

            var assets = AssetDatabase.LoadAssetAtPath<UiKitAssets>(UiKitAssets.AssetPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<UiKitAssets>();
                AssetDatabase.CreateAsset(assets, UiKitAssets.AssetPath);
            }

            assets.Configure(roundRect20, roundRect24, roundRect36, circle, ring,
                Load<TMP_FontAsset>(FontPath), Load<Material>(OutlineMaterialPath));
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] UI kit sprites baked.");
        }

        static Sprite WriteRoundRect(int radius) =>
            Write("RoundRect" + radius, UiShapeRaster.RoundedRectSize(radius), UiShapeRaster.RoundedRect(radius),
                new Vector4(radius, radius, radius, radius));

        static Sprite Write(string name, int size, Color32[] pixels, Vector4 border)
        {
            string path = $"{SpriteDir}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return Load<Sprite>(path);
        }

        static T Load<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("[KMA] Missing " + path);
    }
}
#endif
```

- [ ] **Step 5: Bake the sprites and the asset**

Run an editor method: `KMA.EditorTools.UiKitSpriteBaker.Bake`, log `bake-ui-kit`.
Expected: `exit=0`, the log shows `[KMA] UI kit sprites baked.`, and these files exist:

```bash
ls Assets/_Project/Art/UI/Kit/*.png Assets/_Project/Settings/UI/Resources/UiKitAssets.asset
```

- [ ] **Step 6: Run the tests and confirm they pass**

Run tests: `EditMode` `KMA.Tests.EditorTools.UiKitAssetsTests` `kit-assets`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/UI/Kit/UiKitAssets.cs* Assets/Editor/UiKitSpriteBaker.cs* Assets/_Project/Art/UI/Kit Assets/_Project/Art/UI/Kit.meta \
  Assets/_Project/Settings/UI/Resources Assets/Tests/EditMode/EditorTools/UiKitAssetsTests.cs*
git commit -m "feat(ui): bake the UI kit sprites into a shared asset"
```

---

### Task 3: Kit widgets, press feedback, bar and control states

**Files:**
- Create: `Assets/_Project/Scripts/UI/Kit/UiKitHandles.cs`, `UiKit.cs`, `KitPressFeedback.cs`, `KitBar.cs`, `KitControlState.cs`
- Modify: `Assets/Tests/EditMode/Presentation/KMA.Gameplay.UI.EditMode.Tests.asmdef` (add `"Unity.TextMeshPro"` to `references`)
- Test: `Assets/Tests/EditMode/Presentation/UiKitTests.cs`, `KitPressFeedbackTests.cs`

**Interfaces:**
- Consumes: `MinigameUiTheme`, `UiKitAssets` (Tasks 1–2).
- Produces (all in `KMA.UI.Kit`):
  - `enum ButtonVariant { Primary, Secondary, Danger }`; `enum ControlState { Rest, Hint, Pressed, Disabled }`
  - handles: `ButtonHandle(Button Button, Image Face, Image Fill, TMP_Text Label, KitPressFeedback Feedback)`, `ChipHandle(Image Background, TMP_Text Label)`, `RoundButtonHandle(RectTransform Root, Image Shadow, Image Rim, Image Face, TMP_Text Label, KitPressFeedback Feedback)`, `ControlPlateHandle(RectTransform Visual, Image Border, Image Background, TMP_Text Arrow, TMP_Text Label)`, `SliderHandle(Slider Slider, Image Track, Image Knob)`, `JoystickHandle(Image Base, Image Rim, Image Knob)`
  - `UiKit`: `Rect`, `Stretch(rect)`, `Stretch(rect, offsetMin, offsetMax)`, `Anchor(rect, min, max)`, `Place(rect, anchor, pivot, position, size)`, `SetRadius(Image, float)`, `Shape(parent, name, radius, color)`, `Disc(parent, name, ring, color)`, `AddShadow(Graphic)`, `Panel(parent, name, radius = 24, alpha = .92, shadow = true)`, `StylePanel(Image, radius = 24, alpha = .92)`, `Label(parent, name, text, size, color, alignment = Center, outline = false)`, `StyleLabel(TMP_Text, size, color, outline = false)`, `FitLabel(TMP_Text, maxSize)`, `Chip(parent, name, text)`, `Button(parent, name, text, variant)`, `StyleButton(Button, variant, text = null)`, `ApplyVariant(ButtonHandle, variant)`, `ButtonParts(Button)`, `RoundButton(RectTransform hitArea, string text, float size = 220)`, `ControlPlate(RectTransform visual, string arrow, string text)`, `Bar(parent, name, pip = false, label = false)` → `KitBar`, `Slider(parent, name)`, `Joystick(RectTransform area)`, `StylePauseButton(RectTransform)`, `Countdown(parent, name)`, `StyleCountdown(TMP_Text)`
  - `KitPressFeedback`: `Configure(Graphic face, RectTransform target)`, `SetRestColor(Color)`, `Press()`, `Release()`, `Tick(float)`, `IsPressed`, `Scale`, `RestColor`
  - `KitBar`: `Configure(Image track, Image fill, Image pip, TMP_Text label)`, `SetValue(float)`, `SetFillColor(Color)`, `Value`, `Track`, `Fill`, `Pip`, `Label`
  - `KitControlState`: `Fill(ControlState)`, `Border(ControlState)`, `Apply(Image fill, Image border, ControlState)`

- [ ] **Step 1: Add TextMeshPro to the EditMode presentation test assembly**

In `Assets/Tests/EditMode/Presentation/KMA.Gameplay.UI.EditMode.Tests.asmdef`, add `"Unity.TextMeshPro"` to the `references` array (after `"UnityEngine.UI"`).

- [ ] **Step 2: Write the failing widget tests**

`Assets/Tests/EditMode/Presentation/UiKitTests.cs`:

```csharp
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class UiKitTests
    {
        GameObject root;
        UiKitAssets assets;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("KitRoot", typeof(RectTransform));
            assets = UiKitAssets.Load();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void Panel_UsesTheBakedRoundRectTheSurfaceTokenAndASoftShadow()
        {
            Image panel = UiKit.Panel(root.transform, "Panel");
            Assert.That(panel.sprite, Is.SameAs(assets.RoundRect24));
            Assert.That(panel.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(panel.color, Is.EqualTo(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceOpaque)));
            var shadow = panel.GetComponent<Shadow>();
            Assert.That(shadow, Is.Not.Null);
            Assert.That(shadow, Is.Not.InstanceOf<Outline>());
            Assert.That(shadow.effectDistance, Is.EqualTo(MinigameUiTheme.ShadowOffset));
        }

        [Test]
        public void SetRadius_ScalesTheBakedCornerToTheRequestedRadius()
        {
            Image image = UiKit.Shape(root.transform, "Shape", 12f, Color.white);
            Assert.That(image.sprite, Is.SameAs(assets.RoundRect24));
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(2f).Within(.001f));
            UiKit.SetRadius(image, 36f);
            Assert.That(image.sprite, Is.SameAs(assets.RoundRect36));
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1f).Within(.001f));
        }

        [TestCase(ButtonVariant.Primary)]
        [TestCase(ButtonVariant.Secondary)]
        [TestCase(ButtonVariant.Danger)]
        public void ButtonVariants_KeepReadableKitTextOnTheirFace(ButtonVariant variant)
        {
            ButtonHandle handle = UiKit.Button(root.transform, "Button", "TIẾP TỤC", variant);
            Color background = variant == ButtonVariant.Secondary ? handle.Fill.color : handle.Face.color;
            Assert.That(MinigameUiTheme.ContrastRatio(handle.Label.color, background), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(handle.Label.font, Is.SameAs(assets.Font));
            Assert.That(handle.Label.fontSizeMin, Is.GreaterThanOrEqualTo(MinigameUiTheme.MinimumFontSize));
            Assert.That(handle.Face.sprite, Is.SameAs(assets.RoundRect36));
            Assert.That(((RectTransform)handle.Button.transform).sizeDelta.y, Is.EqualTo(MinigameUiTheme.ButtonHeight));
            Assert.That(handle.Button.transition, Is.EqualTo(Selectable.Transition.None));
        }

        [Test]
        public void ApplyVariant_SwitchesFaceFillAndLabelTogether()
        {
            ButtonHandle handle = UiKit.Button(root.transform, "Button", "DỄ", ButtonVariant.Secondary);
            Assert.That(handle.Fill.gameObject.activeSelf, Is.True);
            UiKit.ApplyVariant(handle, ButtonVariant.Primary);
            Assert.That(handle.Fill.gameObject.activeSelf, Is.False);
            Assert.That(handle.Face.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(handle.Label.color, Is.EqualTo(MinigameUiTheme.Surface));
            Assert.That(handle.Feedback.RestColor, Is.EqualTo(MinigameUiTheme.Accent));
            UiKit.ApplyVariant(handle, ButtonVariant.Danger);
            Assert.That(handle.Face.color, Is.EqualTo(MinigameUiTheme.Energy));
        }

        [Test]
        public void StyleButton_ReplacesBrutalistPartsAndIsIdempotent()
        {
            var legacy = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            legacy.transform.SetParent(root.transform, false);
            legacy.AddComponent<BrutalButton>();
            new GameObject("Shadow", typeof(RectTransform), typeof(Image)).transform.SetParent(legacy.transform, false);
            var visual = new GameObject("Visual", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(legacy.transform, false);
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(visual.transform, false);

            var button = legacy.GetComponent<Button>();
            UiKit.StyleButton(button, ButtonVariant.Primary, "TIẾP");
            UiKit.StyleButton(button, ButtonVariant.Primary, "TIẾP");

            Assert.That(legacy.transform.Find("Shadow"), Is.Null);
            Assert.That(visual.GetComponent<Image>(), Is.Null);
            Assert.That(legacy.GetComponent<Outline>(), Is.Null);
            Assert.That(legacy.GetComponent<BrutalButton>(), Is.Null);
            Assert.That(legacy.GetComponents<KitPressFeedback>(), Has.Length.EqualTo(1));
            int fills = 0;
            foreach (Transform child in legacy.transform)
                if (child.name == "Fill") fills++;
            Assert.That(fills, Is.EqualTo(1));
            Assert.That(legacy.GetComponent<Image>().sprite, Is.SameAs(assets.RoundRect36));
            Assert.That(label.text, Is.EqualTo("TIẾP"));
            Assert.That(label.font, Is.SameAs(assets.Font));
        }

        [Test]
        public void Bar_FillGrowsWithValueAndThePipFollows()
        {
            KitBar bar = UiKit.Bar(root.transform, "Bar", pip: true, label: true);
            bar.SetValue(.42f);
            Assert.That(bar.Value, Is.EqualTo(.42f).Within(.001f));
            Assert.That(bar.Fill.rectTransform.anchorMax.x, Is.EqualTo(.42f).Within(.001f));
            Assert.That(bar.Pip.rectTransform.anchorMin.x, Is.EqualTo(.42f).Within(.001f));
            Assert.That(bar.Fill.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(bar.Track.color, Is.EqualTo(MinigameUiTheme.Track));
            bar.SetValue(2f);
            Assert.That(bar.Value, Is.EqualTo(1f));
            bar.SetValue(0f);
            Assert.That(bar.Fill.enabled, Is.False, "an empty bar hides its fill instead of drawing a sliver");
            Assert.That(bar.Label, Is.Not.Null);
        }

        [Test]
        public void Joystick_RestsInTheSharedControlColours()
        {
            var area = UiKit.Rect(root.transform, "Area");
            JoystickHandle stick = UiKit.Joystick(area);
            Assert.That(stick.Base.sprite, Is.SameAs(assets.Circle));
            Assert.That(stick.Rim.sprite, Is.SameAs(assets.Ring));
            Assert.That(stick.Base.color, Is.EqualTo(KitControlState.Fill(ControlState.Rest)));
            Assert.That(stick.Rim.color, Is.EqualTo(KitControlState.Border(ControlState.Rest)));
            Assert.That(stick.Base.rectTransform.sizeDelta.x, Is.EqualTo(MinigameUiTheme.JoystickBase));
            Assert.That(stick.Knob.rectTransform.sizeDelta.x, Is.EqualTo(MinigameUiTheme.JoystickKnob));
        }

        [Test]
        public void RoundButton_DrawsRimFaceAndLabelAndPutsFeedbackOnTheHitArea()
        {
            var hitArea = UiKit.Rect(root.transform, "ActionButton");
            RoundButtonHandle round = UiKit.RoundButton(hitArea, "ĐÁNH");
            Assert.That(round.Rim.sprite, Is.SameAs(assets.Ring));
            Assert.That(round.Face.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(round.Label.color, Is.EqualTo(MinigameUiTheme.Surface));
            Assert.That(round.Feedback.gameObject, Is.SameAs(hitArea.gameObject));
            Assert.That(round.Root.sizeDelta.x, Is.EqualTo(MinigameUiTheme.RoundButton));
        }

        [Test]
        public void StylePauseButton_DrawsTwoBarsOnceAndDropsLegacyText()
        {
            var pause = new GameObject("PausePanel", typeof(RectTransform), typeof(Image), typeof(Button));
            pause.transform.SetParent(root.transform, false);
            var legacyLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
            legacyLabel.transform.SetParent(pause.transform, false);

            UiKit.StylePauseButton((RectTransform)pause.transform);
            UiKit.StylePauseButton((RectTransform)pause.transform);

            Assert.That(pause.GetComponentsInChildren<Text>(true), Is.Empty);
            int bars = 0;
            foreach (Transform child in pause.transform)
                if (child.name == "BarLeft" || child.name == "BarRight") bars++;
            Assert.That(bars, Is.EqualTo(2));
            Assert.That(pause.GetComponent<Image>().sprite, Is.SameAs(assets.RoundRect20));
            Assert.That(pause.GetComponents<Button>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void Countdown_UsesDisplaySizeAccentAndTheStroke()
        {
            TMP_Text countdown = UiKit.Countdown(root.transform, "Countdown");
            Assert.That(countdown.fontSize, Is.EqualTo(MinigameUiTheme.Display));
            Assert.That(countdown.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(countdown.fontSharedMaterial, Is.SameAs(assets.OutlineMaterial));
        }

        [Test]
        public void ControlPlate_NamesItsPartsForTheSprintPresenter()
        {
            var visual = UiKit.Rect(root.transform, "Visual");
            ControlPlateHandle plate = UiKit.ControlPlate(visual, "←", "TRÁI");
            Assert.That(visual.Find("Border"), Is.SameAs(plate.Border.transform));
            Assert.That(visual.Find("Background"), Is.SameAs(plate.Background.transform));
            Assert.That(visual.Find("Arrow").GetComponent<TMP_Text>().text, Is.EqualTo("←"));
            Assert.That(visual.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("TRÁI"));
            Assert.That(plate.Background.color, Is.EqualTo(KitControlState.Fill(ControlState.Rest)));
        }

        [Test]
        public void Slider_UsesAKitKnobAndATransparentHitArea()
        {
            SliderHandle slider = UiKit.Slider(root.transform, "Slider");
            Assert.That(slider.Knob.sprite, Is.SameAs(assets.Circle));
            Assert.That(slider.Knob.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(slider.Slider.handleRect, Is.SameAs(slider.Knob.rectTransform));
            Assert.That(slider.Slider.GetComponent<Image>().color.a, Is.LessThan(.01f));
        }

        [Test]
        public void Chip_TextShrinksButNeverBelowTheMinimum()
        {
            ChipHandle chip = UiKit.Chip(root.transform, "Chip", "Di chuyển bằng joystick");
            Assert.That(chip.Background.color,
                Is.EqualTo(MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceSoft)));
            Assert.That(chip.Label.enableAutoSizing, Is.True);
            Assert.That(chip.Label.fontSizeMin, Is.EqualTo(MinigameUiTheme.MinimumFontSize));
        }
    }
}
```

- [ ] **Step 3: Write the failing feedback and state tests**

`Assets/Tests/EditMode/Presentation/KitPressFeedbackTests.cs`:

```csharp
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class KitPressFeedbackTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("FeedbackRoot", typeof(RectTransform));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void PressLightensAndShrinksThenReleaseEasesBackOverTheRestoreTime()
        {
            var face = root.AddComponent<Image>();
            face.color = MinigameUiTheme.Accent;
            var feedback = root.AddComponent<KitPressFeedback>();
            feedback.Configure(face, (RectTransform)root.transform);

            feedback.Press();
            Assert.That(feedback.IsPressed, Is.True);
            Assert.That(feedback.Scale, Is.EqualTo(MinigameUiTheme.PressScale).Within(.001f));
            Assert.That(face.color, Is.EqualTo(MinigameUiTheme.Lighten(MinigameUiTheme.Accent, MinigameUiTheme.PressLighten)));

            feedback.Tick(1f);
            Assert.That(feedback.Scale, Is.EqualTo(MinigameUiTheme.PressScale).Within(.001f), "held presses stay pressed");

            feedback.Release();
            feedback.Tick(MinigameUiTheme.PressRestoreSeconds * .5f);
            Assert.That(feedback.Scale, Is.GreaterThan(MinigameUiTheme.PressScale).And.LessThan(1f));
            feedback.Tick(MinigameUiTheme.PressRestoreSeconds);
            Assert.That(feedback.Scale, Is.EqualTo(1f).Within(.001f));
            Assert.That(face.color, Is.EqualTo(MinigameUiTheme.Accent));
        }

        [Test]
        public void SetRestColorRecoloursAnIdleFace()
        {
            var face = root.AddComponent<Image>();
            var feedback = root.AddComponent<KitPressFeedback>();
            feedback.Configure(face, (RectTransform)root.transform);
            feedback.SetRestColor(MinigameUiTheme.Energy);
            Assert.That(face.color, Is.EqualTo(MinigameUiTheme.Energy));
        }

        [Test]
        public void DisabledButtonsFadeAndIgnorePresses()
        {
            ButtonHandle handle = UiKit.Button(root.transform, "Shoot", "GIỮ ĐỂ SÚT", ButtonVariant.Primary);
            handle.Button.interactable = false;
            handle.Feedback.Tick(0f);
            Assert.That(handle.Button.GetComponent<CanvasGroup>().alpha, Is.EqualTo(MinigameUiTheme.DisabledAlpha).Within(.001f));
            handle.Feedback.Press();
            Assert.That(handle.Feedback.IsPressed, Is.False);
            Assert.That(handle.Feedback.Scale, Is.EqualTo(1f).Within(.001f));

            handle.Button.interactable = true;
            handle.Feedback.Tick(0f);
            Assert.That(handle.Button.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void ControlStates_MakeTheHintMoreProminentThanRest()
        {
            Assert.That(KitControlState.Border(ControlState.Hint).a, Is.GreaterThan(KitControlState.Border(ControlState.Rest).a));
            Assert.That(KitControlState.Fill(ControlState.Hint).a, Is.GreaterThan(KitControlState.Fill(ControlState.Rest).a));
            Assert.That(KitControlState.Fill(ControlState.Pressed), Is.Not.EqualTo(KitControlState.Fill(ControlState.Hint)));
            Assert.That(KitControlState.Fill(ControlState.Disabled).a, Is.LessThan(KitControlState.Fill(ControlState.Rest).a));
            Assert.That(MinigameUiTheme.RgbEquals(KitControlState.Fill(ControlState.Rest), MinigameUiTheme.Surface), Is.True);
            Assert.That(MinigameUiTheme.RgbEquals(KitControlState.Border(ControlState.Rest), MinigameUiTheme.Accent), Is.True);
        }
    }
}
```

- [ ] **Step 4: Run the tests and confirm they fail to compile**

Run tests: `EditMode` `KMA.Tests.Presentation.UiKitTests;KMA.Tests.Presentation.KitPressFeedbackTests` `kit-widgets-red`
Expected: no XML; `error CS0246` for `UiKit`, `ButtonHandle`, `KitPressFeedback`, …

- [ ] **Step 5: Create the handles and enums**

`Assets/_Project/Scripts/UI/Kit/UiKitHandles.cs`:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    public enum ButtonVariant { Primary, Secondary, Danger }

    public readonly struct ButtonHandle
    {
        public readonly Button Button;
        public readonly Image Face;
        public readonly Image Fill;
        public readonly TMP_Text Label;
        public readonly KitPressFeedback Feedback;

        public ButtonHandle(Button button, Image face, Image fill, TMP_Text label, KitPressFeedback feedback)
        {
            Button = button;
            Face = face;
            Fill = fill;
            Label = label;
            Feedback = feedback;
        }
    }

    public readonly struct ChipHandle
    {
        public readonly Image Background;
        public readonly TMP_Text Label;

        public ChipHandle(Image background, TMP_Text label)
        {
            Background = background;
            Label = label;
        }
    }

    public readonly struct RoundButtonHandle
    {
        public readonly RectTransform Root;
        public readonly Image Shadow;
        public readonly Image Rim;
        public readonly Image Face;
        public readonly TMP_Text Label;
        public readonly KitPressFeedback Feedback;

        public RoundButtonHandle(RectTransform root, Image shadow, Image rim, Image face, TMP_Text label,
            KitPressFeedback feedback)
        {
            Root = root;
            Shadow = shadow;
            Rim = rim;
            Face = face;
            Label = label;
            Feedback = feedback;
        }
    }

    public readonly struct ControlPlateHandle
    {
        public readonly RectTransform Visual;
        public readonly Image Border;
        public readonly Image Background;
        public readonly TMP_Text Arrow;
        public readonly TMP_Text Label;

        public ControlPlateHandle(RectTransform visual, Image border, Image background, TMP_Text arrow, TMP_Text label)
        {
            Visual = visual;
            Border = border;
            Background = background;
            Arrow = arrow;
            Label = label;
        }
    }

    public readonly struct SliderHandle
    {
        public readonly Slider Slider;
        public readonly Image Track;
        public readonly Image Knob;

        public SliderHandle(Slider slider, Image track, Image knob)
        {
            Slider = slider;
            Track = track;
            Knob = knob;
        }
    }

    public readonly struct JoystickHandle
    {
        public readonly Image Base;
        public readonly Image Rim;
        public readonly Image Knob;

        public JoystickHandle(Image stickBase, Image rim, Image knob)
        {
            Base = stickBase;
            Rim = rim;
            Knob = knob;
        }
    }
}
```

- [ ] **Step 6: Create the control states**

`Assets/_Project/Scripts/UI/Kit/KitControlState.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    public enum ControlState { Rest, Hint, Pressed, Disabled }

    /// Fill and border colours for touch controls drawn as a Surface plate inside an Accent
    /// border (Sprint tap zones, the Volleyball joystick).
    public static class KitControlState
    {
        public static Color Fill(ControlState state) => state switch
        {
            ControlState.Hint => MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceControlActive),
            ControlState.Pressed => MinigameUiTheme.Lighten(
                MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceControlActive),
                MinigameUiTheme.PressLighten),
            ControlState.Disabled => MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceDisabled),
            _ => MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceControl)
        };

        public static Color Border(ControlState state) => state switch
        {
            ControlState.Hint or ControlState.Pressed =>
                MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, MinigameUiTheme.BorderHint),
            ControlState.Disabled => MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, MinigameUiTheme.BorderDisabled),
            _ => MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, MinigameUiTheme.BorderRest)
        };

        public static void Apply(Image fill, Image border, ControlState state)
        {
            if (fill != null)
                fill.color = Fill(state);
            if (border != null)
                border.color = Border(state);
        }
    }
}
```

- [ ] **Step 7: Create `KitPressFeedback`**

`Assets/_Project/Scripts/UI/Kit/KitPressFeedback.cs`:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    /// Shared press response: while held, the face lightens and the target shrinks; after release
    /// both ease back over PressRestoreSeconds. A non-interactable Selectable on the same object
    /// fades to DisabledAlpha and ignores presses. Owns no input.
    public sealed class KitPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Graphic face;
        [SerializeField] RectTransform target;
        [SerializeField] Color restColor = Color.white;
        [SerializeField] Selectable selectable;

        CanvasGroup group;
        bool pressed;
        float restoreRemaining;

        public bool IsPressed => pressed;
        public float Scale => target == null ? 1f : target.localScale.x;
        public Color RestColor => restColor;

        public void Configure(Graphic faceGraphic, RectTransform scaleTarget)
        {
            face = faceGraphic;
            target = scaleTarget;
            selectable = GetComponent<Selectable>();
            if (face != null)
                restColor = face.color;
        }

        public void SetRestColor(Color color)
        {
            restColor = color;
            if (!pressed && restoreRemaining <= 0f && face != null)
                face.color = color;
        }

        public void OnPointerDown(PointerEventData eventData) => Press();
        public void OnPointerUp(PointerEventData eventData) => Release();

        public void Press()
        {
            if (selectable != null && !selectable.interactable)
                return;
            pressed = true;
            restoreRemaining = 0f;
            if (face != null)
                face.color = MinigameUiTheme.Lighten(restColor, MinigameUiTheme.PressLighten);
            SetScale(MinigameUiTheme.PressScale);
        }

        public void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            restoreRemaining = MinigameUiTheme.PressRestoreSeconds;
        }

        public void Tick(float deltaTime)
        {
            ApplyInteractable();
            if (pressed || restoreRemaining <= 0f)
                return;

            restoreRemaining = Mathf.Max(0f, restoreRemaining - deltaTime);
            float t = 1f - restoreRemaining / MinigameUiTheme.PressRestoreSeconds;
            if (face != null)
                face.color = Color.Lerp(MinigameUiTheme.Lighten(restColor, MinigameUiTheme.PressLighten), restColor, t);
            SetScale(Mathf.Lerp(MinigameUiTheme.PressScale, 1f, t));
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        void OnDisable()
        {
            pressed = false;
            restoreRemaining = 0f;
            if (face != null)
                face.color = restColor;
            SetScale(1f);
        }

        void ApplyInteractable()
        {
            if (selectable == null)
                return;
            if (group == null)
                group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            group.alpha = selectable.interactable ? 1f : MinigameUiTheme.DisabledAlpha;
            if (!selectable.interactable && pressed)
            {
                pressed = false;
                restoreRemaining = 0f;
                if (face != null)
                    face.color = restColor;
                SetScale(1f);
            }
        }

        void SetScale(float scale)
        {
            if (target != null)
                target.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
```

- [ ] **Step 8: Create `KitBar`**

`Assets/_Project/Scripts/UI/Kit/KitBar.cs`:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    /// A rounded progress bar. The fill is a sliced child that grows to the value, so both of its
    /// ends stay round at any width, and the corner radius follows half the bar's height.
    public sealed class KitBar : MonoBehaviour
    {
        [SerializeField] Image track;
        [SerializeField] Image fill;
        [SerializeField] Image pip;
        [SerializeField] TMP_Text label;

        public float Value { get; private set; }
        public Image Track => track;
        public Image Fill => fill;
        public Image Pip => pip;
        public TMP_Text Label => label;

        public void Configure(Image trackImage, Image fillImage, Image pipImage, TMP_Text labelText)
        {
            track = trackImage;
            fill = fillImage;
            pip = pipImage;
            label = labelText;
            ApplyRadius();
        }

        public void SetValue(float value)
        {
            Value = Mathf.Clamp01(value);
            if (fill != null)
            {
                fill.rectTransform.anchorMax = new Vector2(Value, 1f);
                fill.enabled = Value > .001f;
            }
            if (pip != null)
            {
                RectTransform rect = pip.rectTransform;
                rect.anchorMin = new Vector2(Value, rect.anchorMin.y);
                rect.anchorMax = new Vector2(Value, rect.anchorMax.y);
            }
        }

        public void SetFillColor(Color color)
        {
            if (fill != null)
                fill.color = color;
        }

        void OnRectTransformDimensionsChange() => ApplyRadius();

        void ApplyRadius()
        {
            if (track == null)
                return;
            float height = track.rectTransform.rect.height;
            if (height <= 0f)
                return;
            float radius = height * .5f;
            UiKit.SetRadius(track, radius);
            if (fill != null)
                UiKit.SetRadius(fill, radius);
        }
    }
}
```

- [ ] **Step 9: Create `UiKit`**

`Assets/_Project/Scripts/UI/Kit/UiKit.cs`:

```csharp
using KMA.Gameplay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    /// Builds and restyles the shared minigame widgets. The kit owns look and press feedback;
    /// callers own position and size. Runs at runtime and in editor configurators alike.
    public static class UiKit
    {
        static UiKitAssets Assets => UiKitAssets.Load();

        public static RectTransform Rect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        public static void Stretch(RectTransform rect) => Stretch(rect, Vector2.zero, Vector2.zero);

        public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// Gives an Image a baked rounded-rect sprite and scales its corners to the radius.
        public static void SetRadius(Image image, float radius)
        {
            Sprite sprite = Assets.RoundRectFor(radius);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = radius > 0f ? sprite.border.x / radius : 1f;
        }

        public static Image Shape(Transform parent, string name, float radius, Color color)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            SetRadius(image, radius);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image Disc(Transform parent, string name, bool ring, Color color)
        {
            RectTransform rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = ring ? Assets.Ring : Assets.Circle;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void AddShadow(Graphic graphic)
        {
            foreach (Outline outline in graphic.GetComponents<Outline>())
                DestroyObject(outline);
            Shadow shadow = null;
            foreach (Shadow existing in graphic.GetComponents<Shadow>())
                if (!(existing is Outline))
                    shadow = existing;
            shadow ??= graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = MinigameUiTheme.ShadowColor;
            shadow.effectDistance = MinigameUiTheme.ShadowOffset;
        }

        public static Image Panel(Transform parent, string name, float radius = MinigameUiTheme.RadiusPanel,
            float alpha = MinigameUiTheme.SurfaceOpaque, bool shadow = true)
        {
            Image image = Shape(parent, name, radius, MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, alpha));
            if (shadow)
                AddShadow(image);
            return image;
        }

        /// Restyles an existing Image as a kit panel (prefab styler, tutorial card).
        public static void StylePanel(Image image, float radius = MinigameUiTheme.RadiusPanel,
            float alpha = MinigameUiTheme.SurfaceOpaque)
        {
            SetRadius(image, radius);
            image.color = MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, alpha);
            AddShadow(image);
        }

        public static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool outline = false)
        {
            var label = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            StyleLabel(label, size, color, outline);
            label.alignment = alignment;
            label.text = text;
            return label;
        }

        public static void StyleLabel(TMP_Text label, float size, Color color, bool outline = false)
        {
            label.font = Assets.Font;
            label.fontSharedMaterial = outline ? Assets.OutlineMaterial : Assets.Font.material;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false;
            label.fontSize = Mathf.Max(size, MinigameUiTheme.MinimumFontSize);
            label.color = color;
            label.raycastTarget = false;
        }

        /// Lets a label shrink to fit its rect, never below the minimum font size.
        public static void FitLabel(TMP_Text label, float maxSize)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = MinigameUiTheme.MinimumFontSize;
            label.fontSizeMax = Mathf.Max(maxSize, MinigameUiTheme.MinimumFontSize);
        }

        public static ChipHandle Chip(Transform parent, string name, string text)
        {
            Image background = Panel(parent, name, MinigameUiTheme.RadiusPanel, MinigameUiTheme.SurfaceSoft);
            TMP_Text label = Label(background.transform, "Label", text, MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            Stretch(label.rectTransform, new Vector2(MinigameUiTheme.SpaceMd, MinigameUiTheme.SpaceXs),
                new Vector2(-MinigameUiTheme.SpaceMd, -MinigameUiTheme.SpaceXs));
            FitLabel(label, MinigameUiTheme.Body);
            return new ChipHandle(background, label);
        }

        public static ButtonHandle Button(Transform parent, string name, string text, ButtonVariant variant)
        {
            RectTransform rect = Rect(parent, name);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, MinigameUiTheme.ButtonHeight);
            rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            TMP_Text label = Label(rect, "Label", text, MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            Stretch(label.rectTransform, new Vector2(MinigameUiTheme.SpaceSm, 0f), new Vector2(-MinigameUiTheme.SpaceSm, 0f));
            return StyleButton(button, variant);
        }

        /// Restyles any uGUI Button, kit-built or prefab-authored, as a kit button. Idempotent.
        public static ButtonHandle StyleButton(UnityEngine.UI.Button button, ButtonVariant variant, string text = null)
        {
            GameObject root = button.gameObject;
            RemoveLegacyButtonParts(root.transform);

            Image face = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            SetRadius(face, MinigameUiTheme.RadiusControl);
            face.raycastTarget = true;
            AddShadow(face);
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;

            Transform existingFill = root.transform.Find("Fill");
            Image fill = existingFill != null
                ? existingFill.GetComponent<Image>()
                : Shape(root.transform, "Fill", MinigameUiTheme.RadiusControl - MinigameUiTheme.BorderWidth, MinigameUiTheme.Surface);
            fill.rectTransform.SetAsFirstSibling();
            Stretch(fill.rectTransform, Vector2.one * MinigameUiTheme.BorderWidth, -Vector2.one * MinigameUiTheme.BorderWidth);

            TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                StyleLabel(label, MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
                FitLabel(label, MinigameUiTheme.Body);
                label.alignment = TextAlignmentOptions.Center;
                if (text != null)
                    label.text = text;
            }

            var feedback = root.GetComponent<KitPressFeedback>() ?? root.AddComponent<KitPressFeedback>();
            feedback.Configure(face, (RectTransform)root.transform);
            var handle = new ButtonHandle(button, face, fill, label, feedback);
            ApplyVariant(handle, variant);
            return handle;
        }

        public static void ApplyVariant(ButtonHandle handle, ButtonVariant variant)
        {
            Color faceColor = variant == ButtonVariant.Danger ? MinigameUiTheme.Energy : MinigameUiTheme.Accent;
            if (handle.Face != null)
                handle.Face.color = faceColor;
            if (handle.Feedback != null)
                handle.Feedback.SetRestColor(faceColor);
            if (handle.Fill != null)
                handle.Fill.gameObject.SetActive(variant == ButtonVariant.Secondary);
            if (handle.Label != null)
                handle.Label.color = variant == ButtonVariant.Secondary ? MinigameUiTheme.TextPrimary : MinigameUiTheme.Surface;
        }

        /// The parts StyleButton added, for callers that switch a button's variant later.
        public static ButtonHandle ButtonParts(UnityEngine.UI.Button button)
        {
            Transform fill = button.transform.Find("Fill");
            return new ButtonHandle(button, button.GetComponent<Image>(), fill != null ? fill.GetComponent<Image>() : null,
                button.GetComponentInChildren<TMP_Text>(true), button.GetComponent<KitPressFeedback>());
        }

        /// Draws a round action button centred in a larger hit area; the hit area owns the feedback.
        public static RoundButtonHandle RoundButton(RectTransform hitArea, string text,
            float size = MinigameUiTheme.RoundButton)
        {
            RectTransform root = Rect(hitArea, "RoundButton");
            Place(root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.one * size);
            float drop = MinigameUiTheme.ShadowOffset.y * 2f;
            Image shadow = Disc(root, "Shadow", false, MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, .35f));
            Stretch(shadow.rectTransform, new Vector2(0f, drop), new Vector2(0f, drop));
            Image rim = Disc(root, "Rim", true, MinigameUiTheme.TextPrimary);
            Stretch(rim.rectTransform);
            float inset = size * 3f / 64f;
            Image face = Disc(root, "Face", false, MinigameUiTheme.Accent);
            Stretch(face.rectTransform, Vector2.one * inset, -Vector2.one * inset);
            TMP_Text label = Label(face.transform, "Label", text, MinigameUiTheme.Headline, MinigameUiTheme.Surface);
            Stretch(label.rectTransform);
            FitLabel(label, MinigameUiTheme.Headline);

            var feedback = hitArea.GetComponent<KitPressFeedback>() ?? hitArea.gameObject.AddComponent<KitPressFeedback>();
            feedback.Configure(face, root);
            return new RoundButtonHandle(root, shadow, rim, face, label, feedback);
        }

        public static ControlPlateHandle ControlPlate(RectTransform visual, string arrow, string text)
        {
            Image border = Shape(visual, "Border", MinigameUiTheme.RadiusControl, KitControlState.Border(ControlState.Rest));
            Stretch(border.rectTransform);
            AddShadow(border);
            Image background = Shape(visual, "Background", MinigameUiTheme.RadiusControl - MinigameUiTheme.BorderWidth,
                KitControlState.Fill(ControlState.Rest));
            Stretch(background.rectTransform, Vector2.one * MinigameUiTheme.BorderWidth, -Vector2.one * MinigameUiTheme.BorderWidth);
            TMP_Text arrowLabel = Label(visual, "Arrow", arrow, MinigameUiTheme.BodyLarge * 1.6f, MinigameUiTheme.TextPrimary);
            Anchor(arrowLabel.rectTransform, new Vector2(.1f, .44f), new Vector2(.9f, .88f));
            TMP_Text label = Label(visual, "Label", text, MinigameUiTheme.BodyLarge, MinigameUiTheme.TextPrimary);
            Anchor(label.rectTransform, new Vector2(.1f, .12f), new Vector2(.9f, .46f));
            return new ControlPlateHandle(visual, border, background, arrowLabel, label);
        }

        public static KitBar Bar(Transform parent, string name, bool pip = false, bool label = false)
        {
            float radius = MinigameUiTheme.BarHeight * .5f;
            Image track = Shape(parent, name, radius, MinigameUiTheme.Track);
            track.rectTransform.sizeDelta = new Vector2(track.rectTransform.sizeDelta.x, MinigameUiTheme.BarHeight);
            Image fill = Shape(track.transform, "Fill", radius, MinigameUiTheme.Accent);
            fill.rectTransform.pivot = new Vector2(0f, .5f);
            Anchor(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f));

            Image pipImage = null;
            if (pip)
            {
                pipImage = Shape(track.transform, "Pip", 4f, MinigameUiTheme.Player);
                pipImage.rectTransform.anchorMin = new Vector2(0f, -.35f);
                pipImage.rectTransform.anchorMax = new Vector2(0f, 1.35f);
                pipImage.rectTransform.pivot = new Vector2(.5f, .5f);
                pipImage.rectTransform.sizeDelta = new Vector2(MinigameUiTheme.BarHeight * .45f, 0f);
                pipImage.rectTransform.anchoredPosition = Vector2.zero;
            }

            TMP_Text text = null;
            if (label)
            {
                text = Label(track.transform, "Label", "0%", MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary,
                    TextAlignmentOptions.Center, outline: true);
                Stretch(text.rectTransform);
            }

            var bar = track.gameObject.AddComponent<KitBar>();
            bar.Configure(track, fill, pipImage, text);
            bar.SetValue(0f);
            return bar;
        }

        public static SliderHandle Slider(Transform parent, string name)
        {
            RectTransform root = Rect(parent, name);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            Image track = Shape(root, "Track", MinigameUiTheme.BarHeight * .25f, MinigameUiTheme.Track);
            Anchor(track.rectTransform, new Vector2(0f, .40f), new Vector2(1f, .60f));

            RectTransform area = Rect(root, "HandleSlideArea");
            area.anchorMin = new Vector2(0f, .5f);
            area.anchorMax = new Vector2(1f, .5f);
            area.sizeDelta = new Vector2(-MinigameUiTheme.SliderKnob, MinigameUiTheme.SliderKnob);
            Image knob = Disc(area, "Handle", false, MinigameUiTheme.Accent);
            knob.rectTransform.sizeDelta = new Vector2(MinigameUiTheme.SliderKnob, 0f);
            Image ring = Disc(knob.transform, "Ring", true, MinigameUiTheme.Surface);
            Stretch(ring.rectTransform);

            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.handleRect = knob.rectTransform;
            slider.targetGraphic = knob;
            slider.transition = Selectable.Transition.None;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return new SliderHandle(slider, track, knob);
        }

        public static JoystickHandle Joystick(RectTransform area)
        {
            Image stickBase = Disc(area, "JoystickBase", false, KitControlState.Fill(ControlState.Rest));
            Place(stickBase.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero,
                Vector2.one * MinigameUiTheme.JoystickBase);
            Image rim = Disc(stickBase.transform, "JoystickRim", true, KitControlState.Border(ControlState.Rest));
            Stretch(rim.rectTransform);
            Image knob = Disc(area, "JoystickKnob", false, MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .9f));
            Place(knob.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero,
                Vector2.one * MinigameUiTheme.JoystickKnob);
            return new JoystickHandle(stickBase, rim, knob);
        }

        /// Turns a rect into the shared pause button: rounded Surface square with two bars.
        /// Idempotent, and removes any legacy text label.
        public static void StylePauseButton(RectTransform root)
        {
            foreach (Text legacy in root.GetComponentsInChildren<Text>(true))
                DestroyObject(legacy.gameObject);

            Image face = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            SetRadius(face, MinigameUiTheme.RadiusPause);
            face.color = MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceOpaque);
            face.raycastTarget = true;
            AddShadow(face);

            var button = root.GetComponent<UnityEngine.UI.Button>() ?? root.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;

            PauseBar(root, "BarLeft", .28f, .44f);
            PauseBar(root, "BarRight", .56f, .72f);

            var feedback = root.GetComponent<KitPressFeedback>() ?? root.gameObject.AddComponent<KitPressFeedback>();
            feedback.Configure(face, root);
        }

        public static TMP_Text Countdown(Transform parent, string name) =>
            Label(parent, name, string.Empty, MinigameUiTheme.Display, MinigameUiTheme.Accent,
                TextAlignmentOptions.Center, outline: true);

        public static void StyleCountdown(TMP_Text label) =>
            StyleLabel(label, MinigameUiTheme.Display, MinigameUiTheme.Accent, outline: true);

        static void PauseBar(RectTransform root, string name, float minX, float maxX)
        {
            if (root.Find(name) != null)
                return;
            Image bar = Shape(root, name, 2f, MinigameUiTheme.TextPrimary);
            Anchor(bar.rectTransform, new Vector2(minX, .26f), new Vector2(maxX, .74f));
        }

        static void RemoveLegacyButtonParts(Transform root)
        {
            Transform shadow = root.Find("Shadow");
            if (shadow != null)
                DestroyObject(shadow.gameObject);

            Transform visual = root.Find("Visual");
            if (visual != null)
            {
                Image visualImage = visual.GetComponent<Image>();
                if (visualImage != null)
                    DestroyObject(visualImage);
                foreach (Shadow effect in visual.GetComponents<Shadow>())
                    DestroyObject(effect);
                Stretch((RectTransform)visual);
            }

            BrutalButton brutal = root.GetComponent<BrutalButton>();
            if (brutal != null)
                DestroyObject(brutal);
        }

        static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
```

- [ ] **Step 10: Run the tests and confirm they pass**

Run tests: `EditMode` `KMA.Tests.Presentation.UiKitTests;KMA.Tests.Presentation.KitPressFeedbackTests` `kit-widgets`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 11: Commit**

```bash
git add Assets/_Project/Scripts/UI/Kit Assets/Tests/EditMode/Presentation
git commit -m "feat(ui): shared kit widgets, press feedback, bar and control states"
```

---

### Task 4: Shared `ResultPanel` with retry, detail, pending state and reveal

**Files:**
- Rewrite: `Assets/_Project/Scripts/UI/ResultPanel.cs`
- Modify: `Assets/_Project/Scripts/UI/KMA.Gameplay.UI.asmdef` (add `"Unity.InputSystem"` to `references`)
- Test: `Assets/Tests/EditMode/Presentation/ResultPanelRetryTests.cs`

**Interfaces:**
- Consumes: `UiKit.ApplyVariant`, `UiKit.ButtonParts`, `ButtonVariant`, `KitPressFeedback`, `MinigameUiTheme` (Task 3); `IRetryResultPreviewPanel`, `ResultPanelActions` (existing, `KMA.Gameplay`).
- Produces: `KMA.Gameplay.UI.ResultPanel : IRetryResultPreviewPanel` with `Configure(GameObject content, TMP_Text status, TMP_Text detail, TMP_Text score, TMP_Text rank, TMP_Text lives, Button continueAction, Button retryAction, TMP_Text error = null)`, `ValidateReferences()`, `SupportsRetry`, `SetTitles(string success, string failure)`, `SetDetail(string)`, `ConfigureRetry(int)`, `Show(MinigameResult, string)`, `Continue()`, `Retry()`, `SetActionPending(bool, string)`, `CurrentResult`, `PreviewRoute`, `HasContinued`, `RetryAvailable`, `IsActionPending`, `IsVisible`, `ContinueInteractable`, event `ActionRequested`. Serialized field names (used by the Task 6 styler): `contentRoot`, `statusLabel`, `scoreLabel`, `rankLabel`, `detailLabel`, `livesLabel`, `errorLabel`, `actionButton`, `retryButton`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Presentation/ResultPanelRetryTests.cs`:

```csharp
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class ResultPanelRetryTests
    {
        GameObject root;
        ResultPanel panel;
        Button continueButton, retryButton;
        TMP_Text status, detail, score, rank, lives, error;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ResultPanel", typeof(RectTransform));
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            continueButton = UiKit.Button(content.transform, "ActionButton", "TIẾP TỤC", ButtonVariant.Primary).Button;
            retryButton = UiKit.Button(content.transform, "RetryButton", "CHƠI LẠI", ButtonVariant.Primary).Button;
            status = Text(content, "StatusLabel"); detail = Text(content, "DetailLabel");
            score = Text(content, "ScoreLabel"); rank = Text(content, "RankLabel");
            lives = Text(content, "LivesLabel"); error = Text(content, "ErrorLabel");
            panel = root.AddComponent<ResultPanel>();
            panel.Configure(content, status, detail, score, rank, lives, continueButton, retryButton, error);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void FailureShowsRetryOnlyWhenLivesRemainAndReportsDetailAndLives()
        {
            panel.SetDetail("2/5 BÀN");
            panel.ConfigureRetry(2);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            Assert.That(panel.IsVisible, Is.True);
            Assert.That(detail.text, Is.EqualTo("2/5 BÀN"));
            Assert.That(lives.text, Is.EqualTo("CÒN 2 MẠNG"));
            Assert.That(retryButton.gameObject.activeSelf, Is.True);
            Assert.That(continueButton.gameObject.activeSelf, Is.True);
            Assert.That(panel.RetryAvailable, Is.True);
        }

        [Test]
        public void ExhaustedFailureHidesRetryAndPendingActionCanRecoverAfterError()
        {
            panel.ConfigureRetry(0);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "GameOver");
            Assert.That(retryButton.gameObject.activeSelf, Is.False);

            int actions = 0;
            panel.ActionRequested += _ => actions++;
            panel.SetActionPending(true, null);
            panel.Retry();
            panel.Continue();
            Assert.That(actions, Is.Zero);
            Assert.That(panel.ContinueInteractable, Is.False);

            panel.SetActionPending(false, "Could not load scene");
            Assert.That(error.text, Is.EqualTo("Could not load scene"));
            Assert.That(panel.ContinueInteractable, Is.True);
            panel.Continue();
            Assert.That(actions, Is.EqualTo(1));
        }

        [Test]
        public void RetryRaisesTheRetryActionAndContinueRaisesThePreviewRouteOnlyOnce()
        {
            panel.ConfigureRetry(3);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            string action = null;
            panel.ActionRequested += requested => action = requested;
            panel.Retry();
            panel.Retry();
            Assert.That(action, Is.EqualTo(ResultPanelActions.Retry));

            panel.Show(new MinigameResult(true, 8f, Rank.A), "Map");
            action = null;
            int count = 0;
            panel.ActionRequested += _ => count++;
            panel.Continue();
            panel.Continue();
            Assert.That(action, Is.EqualTo("Map"));
            Assert.That(count, Is.EqualTo(1));
            Assert.That(retryButton.gameObject.activeSelf, Is.False, "a win never offers a retry");
        }

        [Test]
        public void PanelsWithoutRetryKeepTheirSingleContinueAction()
        {
            var routes = new System.Collections.Generic.List<string>();
            panel.ActionRequested += routes.Add;
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Punishment");
            Assert.That(retryButton.gameObject.activeSelf, Is.False);
            Assert.That(lives.gameObject.activeSelf, Is.False, "lives only show for panels that offer a retry");
            panel.Continue();
            panel.Continue();
            Assert.That(routes, Is.EqualTo(new[] { "Punishment" }));
        }

        [Test]
        public void CustomTitlesUseTheSuccessAndEnergyTokens()
        {
            panel.SetTitles("HOÀN THÀNH!", "THẤT BẠI");
            panel.Show(new MinigameResult(true, 8.4f, Rank.A), "Map");
            Assert.That(status.text, Is.EqualTo("HOÀN THÀNH!"));
            Assert.That(status.color, Is.EqualTo(MinigameUiTheme.Success));
            Assert.That(score.text, Is.EqualTo("8"));
            Assert.That(rank.text, Is.EqualTo("XẾP HẠNG A"));
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            Assert.That(status.text, Is.EqualTo("THẤT BẠI"));
            Assert.That(status.color, Is.EqualTo(MinigameUiTheme.Energy));
        }

        [Test]
        public void LoneContinueIsCentredAndPrimaryWhilePairedContinueIsSecondary()
        {
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            var action = (RectTransform)continueButton.transform;
            Assert.That(action.anchorMin.x, Is.EqualTo(.25f).Within(.001f));
            Assert.That(UiKit.ButtonParts(continueButton).Fill.gameObject.activeSelf, Is.False, "Primary");

            panel.ConfigureRetry(2);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            Assert.That(action.anchorMin.x, Is.EqualTo(.52f).Within(.001f));
            Assert.That(UiKit.ButtonParts(continueButton).Fill.gameObject.activeSelf, Is.True, "Secondary");
        }

        [Test]
        public void ShowOutsidePlayModeSnapsTheRevealToItsFinalState()
        {
            panel.Show(new MinigameResult(true, 8.4f, Rank.A), "Map");
            Transform content = root.transform.Find("Content");
            Assert.That(content.localScale, Is.EqualTo(Vector3.one));
            var group = content.GetComponent<CanvasGroup>();
            Assert.That(group == null || Mathf.Approximately(group.alpha, 1f), Is.True);
            Assert.That(score.text, Is.EqualTo("8"));
        }

        static TMP_Text Text(GameObject parent, string name)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent.transform, false);
            return text;
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run tests: `EditMode` `KMA.Tests.Presentation.ResultPanelRetryTests` `result-panel-red`
Expected: no XML; `error CS1061` for `Configure`, `SetDetail`, `ConfigureRetry`, `SetTitles` on `ResultPanel`.

- [ ] **Step 3: Let the UI assembly read the keyboard**

In `Assets/_Project/Scripts/UI/KMA.Gameplay.UI.asmdef`, add `"Unity.InputSystem"` to the `references` array.

- [ ] **Step 4: Rewrite `ResultPanel`**

Replace the whole content of `Assets/_Project/Scripts/UI/ResultPanel.cs`:

```csharp
using System;
using System.Collections;
using KMA.Gameplay;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    /// The shared result screen of every minigame. Tiếp tục raises the preview route; Chơi lại
    /// (only when a retry is configured) raises ResultPanelActions.Retry.
    public sealed class ResultPanel : MonoBehaviour, IRetryResultPreviewPanel
    {
        const float ScrimDuration = .12f;
        const float ModalDuration = .18f;
        const float TitleDuration = .14f;
        const float ScoreDuration = .35f;
        const float RankDuration = .16f;

        [SerializeField] GameObject contentRoot;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text detailLabel;
        [SerializeField] TMP_Text livesLabel;
        [SerializeField] TMP_Text errorLabel;
        [SerializeField] Button actionButton;
        [SerializeField] Button retryButton;

        string successTitle = "CHIẾN THẮNG";
        string failureTitle = "THẤT BẠI";
        int remainingLives;
        bool retryConfigured;
        bool retryAvailable;
        bool actionPending;
        bool listenersBound;
        string finalScoreText = string.Empty;

        public event Action<string> ActionRequested;

        public MinigameResult CurrentResult { get; private set; }
        public string PreviewRoute { get; private set; } = string.Empty;
        public bool HasContinued { get; private set; }
        public bool RetryAvailable => retryAvailable;
        public bool IsActionPending => actionPending;
        public bool IsVisible => contentRoot ? contentRoot.activeInHierarchy : gameObject.activeInHierarchy;
        public bool ContinueInteractable => actionButton && actionButton.interactable;
        public bool SupportsRetry => retryButton && detailLabel && livesLabel;

        /// Wires a panel built in code; prefab instances are wired by MinigamePrefabStyler.
        public void Configure(GameObject content, TMP_Text status, TMP_Text detail, TMP_Text score, TMP_Text rank,
            TMP_Text lives, Button continueAction, Button retryAction, TMP_Text error = null)
        {
            contentRoot = content;
            statusLabel = status;
            detailLabel = detail;
            scoreLabel = score;
            rankLabel = rank;
            livesLabel = lives;
            actionButton = continueAction;
            retryButton = retryAction;
            errorLabel = error;
            BindButtons();
            RefreshButtons();
        }

        public bool ValidateReferences() => contentRoot && statusLabel && scoreLabel && rankLabel && actionButton;

        public void SetTitles(string success, string failure)
        {
            successTitle = success ?? successTitle;
            failureTitle = failure ?? failureTitle;
        }

        public void SetDetail(string text)
        {
            if (detailLabel == null)
                return;
            detailLabel.text = text ?? string.Empty;
            detailLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void ConfigureRetry(int livesAfterFailure)
        {
            retryConfigured = true;
            remainingLives = Mathf.Max(0, livesAfterFailure);
            retryAvailable = remainingLives > 0;
            RefreshButtons();
        }

        public void Show(MinigameResult result, string previewRoute)
        {
            CurrentResult = result ?? throw new ArgumentNullException(nameof(result));
            PreviewRoute = previewRoute ?? string.Empty;
            HasContinued = false;
            actionPending = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null)
                contentRoot.SetActive(true);
            if (errorLabel != null)
                errorLabel.text = string.Empty;
            if (statusLabel != null)
            {
                statusLabel.text = result.Pass ? successTitle : failureTitle;
                statusLabel.color = result.Pass ? MinigameUiTheme.Success : MinigameUiTheme.Energy;
            }
            finalScoreText = Mathf.RoundToInt(result.Score).ToString();
            if (scoreLabel != null)
                scoreLabel.text = finalScoreText;
            if (rankLabel != null)
                rankLabel.text = $"XẾP HẠNG {result.Rank}";
            if (livesLabel != null)
            {
                livesLabel.text = $"CÒN {remainingLives} MẠNG";
                livesLabel.gameObject.SetActive(retryConfigured);
            }
            SetButtonLabel(actionButton, "TIẾP TỤC");
            SetButtonLabel(retryButton, "CHƠI LẠI");

            retryAvailable = !result.Pass && remainingLives > 0;
            RefreshButtons();
            Reveal(result.Score);
        }

        public void Continue()
        {
            if (CurrentResult == null || HasContinued || actionPending)
                return;
            HasContinued = true;
            ActionRequested?.Invoke(PreviewRoute);
        }

        public void Retry()
        {
            if (CurrentResult == null || HasContinued || actionPending || !retryAvailable)
                return;
            HasContinued = true;
            ActionRequested?.Invoke(ResultPanelActions.Retry);
        }

        public void SetActionPending(bool pending, string error)
        {
            actionPending = pending;
            if (errorLabel != null)
                errorLabel.text = error ?? string.Empty;
            if (!pending)
                HasContinued = false;
            RefreshButtons();
        }

        void Awake() => BindButtons();
        void OnEnable() => BindButtons();

        void OnDisable()
        {
            if (!listenersBound)
                return;
            if (actionButton != null) actionButton.onClick.RemoveListener(Continue);
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            listenersBound = false;
        }

        void Update()
        {
            if (IsVisible && CurrentResult != null && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
                Continue();
        }

        void BindButtons()
        {
            if (listenersBound)
                return;
            if (actionButton != null) actionButton.onClick.AddListener(Continue);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            listenersBound = actionButton != null || retryButton != null;
        }

        void RefreshButtons()
        {
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(retryAvailable);
                retryButton.interactable = !actionPending;
            }
            if (actionButton == null)
                return;
            actionButton.interactable = !actionPending;

            bool paired = retryAvailable && retryButton != null;
            var action = (RectTransform)actionButton.transform;
            action.anchorMin = new Vector2(paired ? .52f : .25f, action.anchorMin.y);
            action.anchorMax = new Vector2(paired ? .94f : .75f, action.anchorMax.y);
            if (actionButton.GetComponent<KitPressFeedback>() != null)
                UiKit.ApplyVariant(UiKit.ButtonParts(actionButton), paired ? ButtonVariant.Secondary : ButtonVariant.Primary);
        }

        static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
                return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value;
        }

        void Reveal(float finalScore)
        {
            StopAllCoroutines();
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                SnapRevealed();
                return;
            }
            StartCoroutine(AnimateReveal(finalScore));
        }

        CanvasGroup ScrimGroup()
        {
            Transform backdrop = transform.Find("Backdrop");
            return backdrop == null ? null
                : backdrop.GetComponent<CanvasGroup>() ?? backdrop.gameObject.AddComponent<CanvasGroup>();
        }

        CanvasGroup ModalGroup() => contentRoot == null ? null
            : contentRoot.GetComponent<CanvasGroup>() ?? contentRoot.AddComponent<CanvasGroup>();

        void SnapRevealed()
        {
            Transform backdrop = transform.Find("Backdrop");
            CanvasGroup scrim = backdrop == null ? null : backdrop.GetComponent<CanvasGroup>();
            if (scrim != null) scrim.alpha = 1f;
            if (contentRoot != null)
            {
                CanvasGroup modal = contentRoot.GetComponent<CanvasGroup>();
                if (modal != null) modal.alpha = 1f;
                contentRoot.transform.localScale = Vector3.one;
            }
            if (rankLabel != null) rankLabel.rectTransform.localScale = Vector3.one;
            if (scoreLabel != null) scoreLabel.text = finalScoreText;
        }

        IEnumerator AnimateReveal(float finalScore)
        {
            yield return FadeGroup(ScrimGroup(), ScrimDuration);
            yield return ScaleAndFadeModal();
            yield return FadeText(statusLabel, TitleDuration);
            yield return CountUpScore(finalScore);
            yield return PopRank();
        }

        static IEnumerator FadeGroup(CanvasGroup group, float duration)
        {
            if (group == null)
                yield break;
            float elapsed = 0f;
            group.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            group.alpha = 1f;
        }

        IEnumerator ScaleAndFadeModal()
        {
            CanvasGroup group = ModalGroup();
            if (group == null)
                yield break;
            Transform modal = contentRoot.transform;
            float elapsed = 0f;
            group.alpha = 0f;
            while (elapsed < ModalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ModalDuration);
                group.alpha = t;
                modal.localScale = Vector3.one * Mathf.Lerp(.9f, 1f, t);
                yield return null;
            }
            group.alpha = 1f;
            modal.localScale = Vector3.one;
        }

        static IEnumerator FadeText(TMP_Text label, float duration)
        {
            if (label == null)
                yield break;
            Color target = label.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                label.color = new Color(target.r, target.g, target.b, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            label.color = target;
        }

        IEnumerator CountUpScore(float finalScore)
        {
            if (scoreLabel == null)
                yield break;
            float elapsed = 0f;
            while (elapsed < ScoreDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ScoreDuration);
                scoreLabel.text = Mathf.RoundToInt(Mathf.Lerp(0f, finalScore, t)).ToString();
                yield return null;
            }
            scoreLabel.text = finalScoreText;
        }

        IEnumerator PopRank()
        {
            if (rankLabel == null)
                yield break;
            RectTransform rect = rankLabel.rectTransform;
            float elapsed = 0f;
            while (elapsed < RankDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / RankDuration);
                float scale = t < .5f ? Mathf.Lerp(.6f, 1.2f, t / .5f) : Mathf.Lerp(1.2f, 1f, (t - .5f) / .5f);
                rect.localScale = Vector3.one * scale;
                yield return null;
            }
            rect.localScale = Vector3.one;
        }
    }
}
```

- [ ] **Step 5: Run the new tests and the existing result-panel users**

Run tests: `EditMode` `KMA.Tests.Presentation.ResultPanelRetryTests;KMA.Tests.Presentation.TutorialOverlayTests` `result-panel`
Expected: `result="Passed"`, `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Progression.CoreLoopTests;KMA.Tests.Gameplay.Progression.FullGameplayFlowTests;KMA.Tests.Presentation.PhaseFlowTests` `result-panel-flow`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/UI/ResultPanel.cs Assets/_Project/Scripts/UI/KMA.Gameplay.UI.asmdef Assets/Tests/EditMode/Presentation/ResultPanelRetryTests.cs*
git commit -m "feat(ui): shared result panel with retry, detail, pending state and reveal"
```

---

### Task 5: Presentation flags replace class-name checks in `PhaseOverlay`

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`, `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`, `Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`
- Test: `Assets/Tests/EditMode/Presentation/PhaseOverlayPresentationFlagsTests.cs`

**Interfaces:**
- Produces: `MinigameBase.UsesSharedTutorial` (virtual, default `true`), `MinigameBase.UsesSharedCountdown` (virtual, default `true`), `MinigameBase.OwnsStartGate` (virtual, default `false`).

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Presentation/PhaseOverlayPresentationFlagsTests.cs`:

```csharp
using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class PhaseOverlayPresentationFlagsTests
    {
        GameObject root;
        PhaseOverlay overlay;
        GameObject tutorialRoot, countdownRoot, playRoot, resolveRoot;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("PhaseOverlay");
            root.SetActive(false);
            overlay = root.AddComponent<PhaseOverlay>();
            tutorialRoot = Child("TutorialRoot");
            countdownRoot = Child("CountdownRoot");
            playRoot = Child("PlayRoot");
            resolveRoot = Child("ResolveRoot");
            Set("tutorialRoot", tutorialRoot);
            Set("countdownRoot", countdownRoot);
            Set("playRoot", playRoot);
            Set("resolveRoot", resolveRoot);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void SharedMinigamesShowTheTutorialThenTheCountdown()
        {
            FlagMinigame minigame = Minigame(sharedTutorial: true, sharedCountdown: true, ownsGate: false);
            overlay.Bind(minigame);
            Assert.That(tutorialRoot.activeSelf, Is.True);
            minigame.Advance(2f);
            Assert.That(countdownRoot.activeSelf, Is.True);
            Assert.That(tutorialRoot.activeSelf, Is.False);
        }

        [Test]
        public void CustomTutorialWithSharedCountdownIsReleasedStraightToTheCountdown()
        {
            FlagMinigame minigame = Minigame(sharedTutorial: false, sharedCountdown: true, ownsGate: false);
            minigame.SetTutorialGate(true);
            overlay.Bind(minigame);
            Assert.That(minigame.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown), "the overlay opens the gate");
            Assert.That(countdownRoot.activeSelf, Is.True);
            Assert.That(tutorialRoot.activeSelf, Is.False);
        }

        [Test]
        public void OwnedGateStaysClosedUntilTheMinigameOpensIt()
        {
            FlagMinigame minigame = Minigame(sharedTutorial: false, sharedCountdown: true, ownsGate: true);
            minigame.SetTutorialGate(true);
            overlay.Bind(minigame);
            minigame.Advance(10f);
            Assert.That(minigame.PresentationPhase, Is.EqualTo(MinigamePhase.Tutorial), "the difficulty picker is still up");
            Assert.That(tutorialRoot.activeSelf, Is.False);
            Assert.That(countdownRoot.activeSelf, Is.False);

            minigame.SetTutorialGate(false);
            Assert.That(minigame.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
            Assert.That(countdownRoot.activeSelf, Is.True, "the shared 3-2-1 follows BẮT ĐẦU");
        }

        [Test]
        public void MinigamesWithTheirOwnCountdownHideTheSharedOne()
        {
            FlagMinigame minigame = Minigame(sharedTutorial: false, sharedCountdown: false, ownsGate: true);
            overlay.Bind(minigame);
            minigame.SetTutorialGate(false);
            Assert.That(minigame.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
            Assert.That(countdownRoot.activeSelf, Is.False);
        }

        [Test]
        public void BaseDefaultsKeepTheSharedPresentation()
        {
            FlagMinigame minigame = Minigame(sharedTutorial: true, sharedCountdown: true, ownsGate: false);
            Assert.That(typeof(MinigameBase).GetProperty(nameof(MinigameBase.UsesSharedTutorial)).GetGetMethod().IsVirtual, Is.True);
            Assert.That(minigame.BaseUsesSharedTutorial, Is.True);
            Assert.That(minigame.BaseUsesSharedCountdown, Is.True);
            Assert.That(minigame.BaseOwnsStartGate, Is.False);
        }

        GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child;
        }

        void Set(string field, object value) =>
            typeof(PhaseOverlay).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(overlay, value);

        FlagMinigame Minigame(bool sharedTutorial, bool sharedCountdown, bool ownsGate)
        {
            var minigame = new GameObject("Minigame").AddComponent<FlagMinigame>();
            minigame.transform.SetParent(root.transform, false);
            minigame.Initialize(sharedTutorial, sharedCountdown, ownsGate);
            return minigame;
        }

        sealed class FlagMinigame : MinigameBase
        {
            bool sharedTutorial = true, sharedCountdown = true, ownsGate;

            public override bool UsesSharedTutorial => sharedTutorial;
            public override bool UsesSharedCountdown => sharedCountdown;
            public override bool OwnsStartGate => ownsGate;
            public bool BaseUsesSharedTutorial => base.UsesSharedTutorial;
            public bool BaseUsesSharedCountdown => base.UsesSharedCountdown;
            public bool BaseOwnsStartGate => base.OwnsStartGate;

            public void Initialize(bool tutorial, bool countdown, bool gate)
            {
                sharedTutorial = tutorial;
                sharedCountdown = countdown;
                ownsGate = gate;
                if (Lifecycle == null)
                    Awake();
            }

            public void Advance(float seconds) => Lifecycle.Tick(seconds);
            protected override void TickPlay(float dt) { }
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run tests: `EditMode` `KMA.Tests.Presentation.PhaseOverlayPresentationFlagsTests` `phase-flags-red`
Expected: no XML; `error CS0115` (`no suitable method found to override`) for `UsesSharedTutorial`.

- [ ] **Step 3: Add the flags to `MinigameBase`**

In `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs`, add after the `PresentationPhase` property:

```csharp
        /// PhaseOverlay shows the shared tutorial card and holds the start gate while it is open.
        public virtual bool UsesSharedTutorial => true;

        /// PhaseOverlay shows the shared 3-2-1 countdown.
        public virtual bool UsesSharedCountdown => true;

        /// The minigame opens its own start gate, so PhaseOverlay must leave the gate alone.
        public virtual bool OwnsStartGate => false;
```

- [ ] **Step 4: Make `PhaseOverlay` read the flags**

In `Assets/_Project/Scripts/UI/PhaseOverlay.cs`:

Replace `Update()` with:

```csharp
        void Update()
        {
            if ((source != null && !source.UsesSharedCountdown) || DisplayedPhase != MinigamePhase.Countdown)
                return;

            countdownElapsed += Time.deltaTime;
            RefreshCountdown();
        }
```

Replace `ApplyPhase` with:

```csharp
        void ApplyPhase(MinigamePhase phase)
        {
            DisplayedPhase = phase;
            if (phase == MinigamePhase.Countdown)
                countdownElapsed = 0f;

            bool sharedTutorial = source == null || source.UsesSharedTutorial;
            bool sharedCountdown = source == null || source.UsesSharedCountdown;
            SetActive(tutorialRoot, sharedTutorial && phase == MinigamePhase.Tutorial &&
                (tutorialOverlay == null || tutorialOverlay.ShouldShow));
            SetActive(countdownRoot, sharedCountdown && phase == MinigamePhase.Countdown);
            SetActive(playRoot, sharedTutorial && phase == MinigamePhase.Play);
            SetActive(resolveRoot, phase == MinigamePhase.Resolve);

            if (phaseLabel != null)
                phaseLabel.text = !sharedTutorial && phase != MinigamePhase.Resolve ? string.Empty : PhaseName(phase);
            if (sharedCountdown)
                RefreshCountdown();
        }
```

Replace `ConfigureTutorial` with:

```csharp
        void ConfigureTutorial()
        {
            if (source == null)
                return;

            UnsubscribeTutorialCompletion();

            if (source.OwnsStartGate)
                FindSprintStartPresentation()?.Bind(source);

            if (!source.UsesSharedTutorial)
            {
                if (!source.OwnsStartGate)
                    source.SetTutorialGate(false);
                return;
            }

            if (tutorialOverlay == null)
                return;

            if (tutorialOverlay.ShouldShow)
            {
                tutorialOverlay.Completed += ReleaseTutorialGate;
                tutorialSubscribed = true;
                source.SetTutorialGate(true);
            }
            else
            {
                source.SetTutorialGate(false);
            }
        }
```

Delete the two properties `IsSprintSource` and `IsVolleyballSource`.

- [ ] **Step 5: Declare each minigame's flags**

Add directly after the opening brace of each class:

`SprintController` (`Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`):

```csharp
        public override bool UsesSharedTutorial => false;
        public override bool UsesSharedCountdown => false;
        public override bool OwnsStartGate => true;
```

`VolleyballController` (`Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`):

```csharp
        public override bool UsesSharedTutorial => false;
```

`FootballController` (`Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`):

```csharp
        public override bool UsesSharedTutorial => false;
        public override bool OwnsStartGate => true;
```

- [ ] **Step 6: Run the new tests and the scene flows that use the overlay**

Run tests: `EditMode` `KMA.Tests.Presentation.PhaseOverlayPresentationFlagsTests;KMA.Tests.Presentation.MinigameLifecyclePresentationTests` `phase-flags`
Expected: `result="Passed"`, `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Presentation.PhaseFlowTests;KMA.Tests.Presentation.SprintPresentationGateTests` `phase-flags-play`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs Assets/_Project/Scripts/UI/PhaseOverlay.cs \
  Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs \
  Assets/_Project/Scripts/Gameplay/Football/FootballController.cs Assets/Tests/EditMode/Presentation/PhaseOverlayPresentationFlagsTests.cs*
git commit -m "refactor(ui): phase overlay reads presentation flags instead of class names"
```

---

### Task 6: Pause, tutorial, HUD and the shared prefabs on the kit

**Files:**
- Modify: `Assets/_Project/Scripts/UI/PausePanel.cs`, `TutorialOverlay.cs`, `MinigameHUD.cs`, `HeartBar.cs`, `MinigameUIAssembler.cs`
- Create: `Assets/Editor/MinigamePrefabStyler.cs`
- Modify (by running the styler): `Assets/_Project/Prefabs/UI/{HUD_Minigame,PhaseOverlay,ResultPanel}.prefab`, `Assets/_Project/Scenes/Punishment.unity`
- Modify: `Assets/Tests/PlayMode/Presentation/UIComponentPlayModeTests.cs` (drop `"theme"` from the HUD contract)
- Test: `Assets/Tests/EditMode/EditorTools/MinigamePrefabStylerTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 3–4.
- Produces: `KMA.EditorTools.MinigamePrefabStyler.RestyleAll()` (static void) and its path constants `HudPrefab`, `PhasePrefab`, `ResultPrefab`, `PunishmentScene`. `PausePanel` builds a kit menu; `TutorialOverlay` styles itself with the kit.

- [ ] **Step 1: Write the failing styler tests**

`Assets/Tests/EditMode/EditorTools/MinigamePrefabStylerTests.cs`:

```csharp
#if UNITY_EDITOR
using KMA.EditorTools;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.EditorTools
{
    public sealed class MinigamePrefabStylerTests
    {
        [OneTimeSetUp]
        public void Restyle()
        {
            MinigamePrefabStyler.RestyleAll();
            MinigamePrefabStyler.RestyleAll();
        }

        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void ResultPanelGainsRetryDetailLivesAndErrorAndKeepsItsNodeNames()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MinigamePrefabStyler.ResultPrefab);
            try
            {
                var panel = root.GetComponent<ResultPanel>();
                Assert.That(panel.ValidateReferences(), Is.True);
                Assert.That(panel.SupportsRetry, Is.True);
                var serialized = new SerializedObject(panel);
                foreach (string field in new[] { "detailLabel", "livesLabel", "errorLabel", "retryButton" })
                    Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                foreach (string path in new[] { "Backdrop", "Content", "Content/StatusLabel", "Content/ScoreLabel",
                             "Content/RankLabel", "Content/ActionButton", "Content/RetryButton" })
                    Assert.That(root.transform.Find(path), Is.Not.Null, path);
                int retries = 0;
                foreach (Transform child in root.transform.Find("Content"))
                    if (child.name == "RetryButton") retries++;
                Assert.That(retries, Is.EqualTo(1), "restyling twice must not add a second retry button");
                Assert.That(root.transform.Find("Content/ActionButton/Shadow"), Is.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase(MinigamePrefabStyler.HudPrefab)]
        [TestCase(MinigamePrefabStyler.PhasePrefab)]
        [TestCase(MinigamePrefabStyler.ResultPrefab)]
        public void SharedPrefabsUseOnlyKitSpritesFontsAndTokens(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                UiKitAssets assets = UiKitAssets.Load();
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.color.a < .01f || image.name == "Icon")
                        continue;
                    if (image.sprite == null)
                        Assert.That(MinigameUiTheme.RgbEquals(image.color, MinigameUiTheme.Scrim), Is.True, image.name);
                    else
                        Assert.That(System.Array.IndexOf(assets.AllSprites(), image.sprite), Is.GreaterThanOrEqualTo(0),
                            $"{image.name} uses {image.sprite.name}");
                }
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.That(text.font, Is.SameAs(assets.Font), text.name);
                    float smallest = text.enableAutoSizing ? text.fontSizeMin : text.fontSize;
                    Assert.That(smallest, Is.GreaterThanOrEqualTo(MinigameUiTheme.MinimumFontSize), text.name);
                }
                Assert.That(root.GetComponentsInChildren<BrutalButton>(true), Is.Empty);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void PunishmentPauseButtonIsTheKitPauseButton()
        {
            EditorSceneManager.OpenScene(MinigamePrefabStyler.PunishmentScene, OpenSceneMode.Single);
            var pause = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            Assert.That(pause, Is.Not.Null);
            Assert.That(pause.GetComponent<Image>().sprite, Is.SameAs(UiKitAssets.Load().RoundRect20));
            Assert.That(pause.transform.Find("BarLeft"), Is.Not.Null);
            Assert.That(pause.GetComponentsInChildren<Text>(true), Is.Empty);
        }
    }
}
#endif
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run tests: `EditMode` `KMA.Tests.EditorTools.MinigamePrefabStylerTests` `styler-red`
Expected: no XML; `error CS0103` for `MinigamePrefabStyler`.

- [ ] **Step 3: Build the pause menu with the kit**

In `Assets/_Project/Scripts/UI/PausePanel.cs`:

Add `using KMA.UI.Kit;` and `using TMPro;` to the usings.

Replace `Awake()` with:

```csharp
        void Awake()
        {
            pauseButton ??= GetComponent<Button>();
            if (pauseButton != null && transform is RectTransform rect)
                UiKit.StylePauseButton(rect);
            EnsureMenu();
            WireButtons();
            SetMenuVisible(false);
        }
```

Replace `EnsureMenu`, `CreateHeading` and `CreateMenuButton` with:

```csharp
        void EnsureMenu()
        {
            if (menuRoot != null)
                return;

            var parent = transform.parent;
            if (parent == null)
                return;

            menuRoot = new GameObject("PauseMenu", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
            menuRoot.transform.SetParent(parent, false);
            UiKit.Stretch((RectTransform)menuRoot.transform);
            var menuCanvas = menuRoot.GetComponent<Canvas>();
            menuCanvas.overrideSorting = true;
            var parentCanvas = GetComponentInParent<Canvas>();
            menuCanvas.sortingOrder = parentCanvas
                ? Mathf.Max(MenuSortingOrder, parentCanvas.sortingOrder + 1)
                : MenuSortingOrder;
            menuRoot.GetComponent<Image>().color = MinigameUiTheme.Scrim;

            Image card = UiKit.Panel(menuRoot.transform, "PauseCard");
            card.raycastTarget = true;
            menuCard = card.transform;
            Vector2 centre = new Vector2(.5f, .5f);
            UiKit.Place(card.rectTransform, centre, centre, Vector2.zero, new Vector2(560f, 440f));

            TMP_Text heading = UiKit.Label(card.transform, "Heading", "TẠM DỪNG", MinigameUiTheme.Headline,
                MinigameUiTheme.TextPrimary);
            UiKit.Place(heading.rectTransform, centre, centre, new Vector2(0f, 155f), new Vector2(480f, 70f));

            resumeButton = CreateMenuButton("ResumeButton", "TIẾP TỤC", 55f, ButtonVariant.Primary);
            restartButton = CreateMenuButton("RestartButton", "CHƠI LẠI", -49f, ButtonVariant.Secondary);
            exitButton = CreateMenuButton("ExitButton", "VỀ CHỌN MÔN", -153f, ButtonVariant.Danger);
        }

        Button CreateMenuButton(string buttonName, string label, float y, ButtonVariant variant)
        {
            ButtonHandle handle = UiKit.Button(menuCard ?? menuRoot.transform, buttonName, label, variant);
            Vector2 centre = new Vector2(.5f, .5f);
            UiKit.Place((RectTransform)handle.Button.transform, centre, centre, new Vector2(0f, y),
                new Vector2(400f, MinigameUiTheme.ButtonHeight));
            return handle.Button;
        }
```

- [ ] **Step 4: Style the tutorial with the kit**

In `Assets/_Project/Scripts/UI/TutorialOverlay.cs`, add `using KMA.UI.Kit;`, change the call in `Awake()` from `ApplyFestivalStyle();` to `ApplyKitStyle();`, and replace `ApplyFestivalStyle`, `StyleLabel` and `StyleButton` with:

```csharp
        void ApplyKitStyle()
        {
            if (contentRoot != null)
            {
                Image card = contentRoot.GetComponent<Image>();
                if (card != null)
                    UiKit.StylePanel(card);
                if (contentRoot.transform is RectTransform rect)
                    rect.sizeDelta = new Vector2(900f, 600f);
            }

            StyleLabel(titleLabel, MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            StyleLabel(instructionLabel, MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            if (instructionLabel != null)
                UiKit.FitLabel(instructionLabel, MinigameUiTheme.Body);
            StyleLabel(stepLabel, MinigameUiTheme.Caption, MinigameUiTheme.Accent);
            StyleButton(backButton, "QUAY LẠI", ButtonVariant.Secondary);
            StyleButton(nextButton, "TIẾP", ButtonVariant.Primary);
            StyleButton(skipButton, "BỎ QUA", ButtonVariant.Secondary);
            StyleButton(closeButton, "BẮT ĐẦU", ButtonVariant.Primary);
        }

        static void StyleLabel(TMP_Text label, float size, Color color)
        {
            if (label != null)
                UiKit.StyleLabel(label, size, color);
        }

        static void StyleButton(Button button, string value, ButtonVariant variant)
        {
            if (button != null)
                UiKit.StyleButton(button, variant, value);
        }
```

- [ ] **Step 5: Point the HUD and hearts at the kit tokens**

In `Assets/_Project/Scripts/UI/MinigameHUD.cs`: add `using KMA.UI.Kit;`, delete the `[SerializeField] UITheme theme;` line, and replace the two colour blocks in `RefreshFrom`:

```csharp
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(state.progress01);
                progressFill.color = MinigameUiTheme.Accent;
            }
            if (staminaFill != null)
            {
                staminaFill.fillAmount = Mathf.Clamp01(state.stamina01);
                staminaFill.color = MinigameUiTheme.Success;
            }
```

In `Assets/Tests/PlayMode/Presentation/UIComponentPlayModeTests.cs`, remove `"theme", ` from the property list in `AssertRequiredHudReferences`.

In `Assets/_Project/Scripts/UI/HeartBar.cs`: add `using KMA.UI.Kit;` and change the two field initialisers to:

```csharp
        [SerializeField] Color filledColor = MinigameUiTheme.Energy;
        [SerializeField] Color emptyColor = MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .25f);
```

- [ ] **Step 6: Give the assembler the kit pause button and drop the theme wiring**

In `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs`:
- add `using KMA.UI.Kit;`
- delete `const string ThemePath …`, the `var theme = …` line in `AssembleScene`, and the `ConfigureResultPanel` method and its call;
- change `ConfigureHud(hudRoot, minigame, theme);` to `ConfigureHud(hudRoot, minigame);` and make `ConfigureHud` take `(GameObject root, MinigameBase minigame)` and set only `minigameSource`;
- replace `EnsurePausePanel` with:

```csharp
        static void EnsurePausePanel(Scene scene, Transform canvasTransform)
        {
            var pause = FindInScene<PausePanel>(scene);
            if (pause == null)
            {
                var pauseObject = new GameObject("PausePanel", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(pauseObject, scene);
                pauseObject.transform.SetParent(canvasTransform, false);
                pause = pauseObject.AddComponent<PausePanel>();
            }

            var rect = (RectTransform)pause.transform;
            UiKit.StylePauseButton(rect);
            UiKit.Place(rect, Vector2.one, Vector2.one,
                new Vector2(-MinigameUiTheme.SpaceMd, -MinigameUiTheme.SpaceMd), Vector2.one * MinigameUiTheme.ButtonHeight);
        }
```

(`PausePanel.Awake` wires the button to `Open`; the old non-persistent `AddListener` was lost on save anyway.)

- [ ] **Step 7: Create the prefab styler**

`Assets/Editor/MinigamePrefabStyler.cs`:

```csharp
#if UNITY_EDITOR
using System;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    /// Restyles the shared minigame prefabs with the UI kit in place. Node names and serialized
    /// references stay, so every scene instance (GameOver, Punishment, minigames) follows.
    public static class MinigamePrefabStyler
    {
        public const string HudPrefab = "Assets/_Project/Prefabs/UI/HUD_Minigame.prefab";
        public const string PhasePrefab = "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab";
        public const string ResultPrefab = "Assets/_Project/Prefabs/UI/ResultPanel.prefab";
        public const string PunishmentScene = "Assets/_Project/Scenes/Punishment.unity";

        [MenuItem("KMA/UI/Restyle Shared Minigame Prefabs")]
        public static void RestyleAll()
        {
            Restyle(HudPrefab, StyleHud);
            Restyle(PhasePrefab, StylePhaseOverlay);
            Restyle(ResultPrefab, StyleResultPanel);
            AssetDatabase.SaveAssets();
            // Punishment keeps an assembler-made pause button in its scene; rebuild it with the kit.
            MinigameUIAssembler.AssembleScenePath(PunishmentScene);
            Debug.Log("[KMA] Shared minigame prefabs restyled.");
        }

        static void Restyle(string path, Action<GameObject> style)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                style(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void StyleHud(GameObject root)
        {
            Transform safe = root.transform.Find("SafeAreaRoot");
            StyleText(safe, "Timer", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary, true);
            StyleText(safe, "Phase", MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            StyleText(safe, "Score", MinigameUiTheme.Headline, MinigameUiTheme.Accent, true);
            StyleText(safe, "Status", MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary);
            StyleFilledBar(safe.Find("Progress"), MinigameUiTheme.Accent);
            StyleFilledBar(safe.Find("Stamina"), MinigameUiTheme.Success);

            Transform hearts = safe.Find("HeartBar");
            foreach (Transform heart in hearts)
            {
                var image = heart.GetComponent<Image>();
                image.sprite = UiKitAssets.Load().Circle;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = MinigameUiTheme.Energy;
            }
            var heartBar = new SerializedObject(hearts.GetComponent<HeartBar>());
            heartBar.FindProperty("filledColor").colorValue = MinigameUiTheme.Energy;
            heartBar.FindProperty("emptyColor").colorValue = MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .25f);
            heartBar.ApplyModifiedPropertiesWithoutUndo();
        }

        static void StyleFilledBar(Transform bar, Color fillColor)
        {
            float radius = MinigameUiTheme.BarHeight * .5f;
            var track = bar.GetComponent<Image>();
            if (track != null)
            {
                UiKit.SetRadius(track, radius);
                track.color = MinigameUiTheme.Track;
                StripEffects(track.gameObject);
            }

            // MinigameHUD drives fillAmount, so the fill stays a Filled image.
            var fill = bar.Find("Fill").GetComponent<Image>();
            Image.FillMethod method = fill.fillMethod;
            int origin = fill.fillOrigin;
            UiKit.SetRadius(fill, radius);
            fill.type = Image.Type.Filled;
            fill.fillMethod = method;
            fill.fillOrigin = origin;
            fill.color = fillColor;
        }

        static void StylePhaseOverlay(GameObject root)
        {
            Transform t = root.transform;
            Image tutorialCard = t.Find("TutorialRoot").GetComponent<Image>();
            if (tutorialCard != null)
            {
                StripEffects(tutorialCard.gameObject);
                UiKit.StylePanel(tutorialCard);
            }
            StyleText(t, "TutorialRoot/TutorialTitle", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            TMP_Text instruction = StyleText(t, "TutorialRoot/InstructionLabel", MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            UiKit.FitLabel(instruction, MinigameUiTheme.Body);
            StyleText(t, "TutorialRoot/StepLabel", MinigameUiTheme.Caption, MinigameUiTheme.Accent);
            StyleButton(t, "TutorialRoot/BackButton", ButtonVariant.Secondary);
            StyleButton(t, "TutorialRoot/NextButton", ButtonVariant.Primary);
            StyleButton(t, "TutorialRoot/SkipButton", ButtonVariant.Secondary);
            StyleButton(t, "TutorialRoot/CloseButton", ButtonVariant.Primary);

            // The countdown is a bare number, like Sprint's.
            RemoveImage(t.Find("CountdownRoot").gameObject);
            UiKit.StyleCountdown(t.Find("CountdownRoot/CountdownLabel").GetComponent<TMP_Text>());

            StyleChipRoot(t.Find("PlayRoot"));
            StyleChipRoot(t.Find("ResolveRoot"));
            StyleText(t, "PlayRoot/PlayLabel", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            StyleText(t, "ResolveRoot/ResolveLabel", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            StyleText(t, "PhaseLabel", MinigameUiTheme.Headline, MinigameUiTheme.TextPrimary, true);
        }

        static void StyleChipRoot(Transform node)
        {
            var image = node.GetComponent<Image>();
            if (image == null)
                return;
            StripEffects(node.gameObject);
            UiKit.StylePanel(image, MinigameUiTheme.RadiusPanel, MinigameUiTheme.SurfaceSoft);
        }

        static void StyleResultPanel(GameObject root)
        {
            Transform t = root.transform;
            Transform backdropNode = t.Find("Backdrop");
            var backdrop = backdropNode.GetComponent<Image>() ?? backdropNode.gameObject.AddComponent<Image>();
            StripEffects(backdrop.gameObject);
            backdrop.sprite = null;
            backdrop.type = Image.Type.Simple;
            backdrop.color = MinigameUiTheme.Scrim;

            Transform content = t.Find("Content");
            var card = content.GetComponent<Image>() ?? content.gameObject.AddComponent<Image>();
            StripEffects(card.gameObject);
            UiKit.StylePanel(card);
            var contentRect = (RectTransform)content;
            UiKit.Place(contentRect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900f, 720f));

            TMP_Text status = StyleText(content, "StatusLabel", MinigameUiTheme.Title, MinigameUiTheme.Success);
            UiKit.Anchor(status.rectTransform, new Vector2(.06f, .80f), new Vector2(.94f, .95f));
            TMP_Text caption = StyleText(content, "ScoreCaption", MinigameUiTheme.Caption,
                MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .7f));
            UiKit.Anchor(caption.rectTransform, new Vector2(.06f, .70f), new Vector2(.94f, .78f));
            TMP_Text score = StyleText(content, "ScoreLabel", MinigameUiTheme.Display, MinigameUiTheme.Accent, true);
            UiKit.Anchor(score.rectTransform, new Vector2(.06f, .46f), new Vector2(.94f, .70f));
            TMP_Text rank = StyleText(content, "RankLabel", MinigameUiTheme.Headline, MinigameUiTheme.TextPrimary);
            UiKit.Anchor(rank.rectTransform, new Vector2(.06f, .36f), new Vector2(.94f, .46f));

            TMP_Text detail = EnsureLabel(content, "DetailLabel", MinigameUiTheme.Body, MinigameUiTheme.TextPrimary, .29f, .36f);
            TMP_Text lives = EnsureLabel(content, "LivesLabel", MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary, .23f, .29f);
            TMP_Text error = EnsureLabel(content, "ErrorLabel", MinigameUiTheme.Caption, MinigameUiTheme.Energy, .17f, .23f);
            detail.gameObject.SetActive(false);
            lives.gameObject.SetActive(false);
            error.text = string.Empty;

            var action = content.Find("ActionButton").GetComponent<Button>();
            UiKit.StyleButton(action, ButtonVariant.Primary, "TIẾP TỤC");
            UiKit.Anchor((RectTransform)action.transform, new Vector2(.25f, .035f), new Vector2(.75f, .16f));

            Transform existingRetry = content.Find("RetryButton");
            Button retry = existingRetry != null
                ? existingRetry.GetComponent<Button>()
                : UiKit.Button(content, "RetryButton", "CHƠI LẠI", ButtonVariant.Primary).Button;
            UiKit.StyleButton(retry, ButtonVariant.Primary, "CHƠI LẠI");
            UiKit.Anchor((RectTransform)retry.transform, new Vector2(.06f, .035f), new Vector2(.48f, .16f));
            retry.gameObject.SetActive(false);

            var panel = new SerializedObject(root.GetComponent<ResultPanel>());
            panel.FindProperty("detailLabel").objectReferenceValue = detail;
            panel.FindProperty("livesLabel").objectReferenceValue = lives;
            panel.FindProperty("errorLabel").objectReferenceValue = error;
            panel.FindProperty("retryButton").objectReferenceValue = retry;
            panel.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Text EnsureLabel(Transform parent, string name, float size, Color color, float minY, float maxY)
        {
            Transform existing = parent.Find(name);
            TMP_Text label = existing != null
                ? existing.GetComponent<TMP_Text>()
                : UiKit.Label(parent, name, string.Empty, size, color);
            UiKit.StyleLabel(label, size, color);
            UiKit.Anchor(label.rectTransform, new Vector2(.06f, minY), new Vector2(.94f, maxY));
            return label;
        }

        static TMP_Text StyleText(Transform root, string path, float size, Color color, bool outline = false)
        {
            var label = root.Find(path).GetComponent<TMP_Text>();
            UiKit.StyleLabel(label, size, color, outline);
            return label;
        }

        static void StyleButton(Transform root, string path, ButtonVariant variant) =>
            UiKit.StyleButton(root.Find(path).GetComponent<Button>(), variant);

        static void RemoveImage(GameObject node)
        {
            StripEffects(node);
            var image = node.GetComponent<Image>();
            if (image != null)
                Object.DestroyImmediate(image);
        }

        static void StripEffects(GameObject node)
        {
            foreach (Shadow effect in node.GetComponents<Shadow>())
                Object.DestroyImmediate(effect);
        }
    }
}
#endif
```

- [ ] **Step 8: Run the styler on the real assets**

Run an editor method: `KMA.EditorTools.MinigamePrefabStyler.RestyleAll`, log `restyle-prefabs`.
Expected: `exit=0` and `[KMA] Shared minigame prefabs restyled.` in the log. If the log shows a `NullReferenceException` from `root.Find(path)`, a node listed in this step is missing from the prefab. Print the prefab's node names with `grep "m_Name:" Assets/_Project/Prefabs/UI/<Prefab>.prefab`, and fix the path in the styler to match.

- [ ] **Step 9: Run the tests**

Run tests: `EditMode` `KMA.Tests.EditorTools.MinigamePrefabStylerTests;KMA.Tests.Presentation.UIComponentTests;KMA.Tests.Presentation.TutorialOverlayTests` `styler`
Expected: `result="Passed"`, `failed="0"`. If `SharedPrefabsUseOnlyKitSpritesFontsAndTokens` names an image or text the styler does not handle yet, add a rule for that node to the matching `Style…` method (kit sprite, token colour, kit font, size ≥ 24). Do not loosen the test. Then repeat Step 8 and this step.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Core.PauseFlowTests;KMA.Tests.Presentation.UIComponentPlayModeTests;KMA.Tests.Presentation.MinigameHUDTests;KMA.Tests.Presentation.PhaseFlowTests` `styler-play`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 10: Commit**

```bash
git add Assets/_Project/Scripts/UI Assets/Editor/MinigamePrefabStyler.cs* Assets/_Project/Prefabs/UI \
  Assets/_Project/Scenes/Punishment.unity Assets/Tests/EditMode/EditorTools/MinigamePrefabStylerTests.cs* \
  Assets/Tests/PlayMode/Presentation/UIComponentPlayModeTests.cs
git commit -m "feat(ui): pause, tutorial, HUD and shared prefabs on the navy kit"
```

---

### Task 7: Sprint on the kit

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs` (the `SprintFestivalPresentation` class only; `SprintPlayerMarkerPlacement` and `SprintPlayerIdentityOutline` stay as they are)
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs`, `SprintControlPresenter.cs`
- Delete: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs`, `SprintResultPresentation.cs` (+ `.meta`)
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes: `UiKit`, `KitBar`, `KitControlState`, `ControlState`, `UiKitAssets`, `MinigameStyleAudit` is **not** used yet (Task 10); `ResultPanel.SetTitles` (Task 4).
- Produces: Sprint HUD node names unchanged (`SprintBroadcastChrome/ProgressRail/RailFill`, `…/PlayerPip`, `Scoreboard/Distance`, `Scoreboard/RankBadge/RankLabel`, `Scoreboard/Combo`, `ModeLabel`, `PausePanel`, `StartPresentation/CountdownLabel`, `StartPresentation/InstructionPlate/InstructionLabel`, tap `Visual/{Border,Background,Arrow,Label}`); `ProgressRail` now carries a `KitBar`.

- [ ] **Step 1: Update the gate tests to the kit contract (they fail first)**

In `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs` (it already has `using KMA.UI.Kit;` from Task 1):

In `SprintPresentationRendersDedicatedHudMetrics`, replace

```csharp
            Image distanceFill = chrome.Find("ProgressRail/RailFill")?.GetComponent<Image>();
```

with

```csharp
            KitBar distanceBar = chrome.Find("ProgressRail")?.GetComponent<KitBar>();
```

replace `Assert.That(distanceFill, Is.Not.Null);` and `Assert.That(distanceFill.type, Is.EqualTo(Image.Type.Filled));` with

```csharp
            Assert.That(distanceBar, Is.Not.Null);
            Assert.That(distanceBar.Fill.name, Is.EqualTo("RailFill"));
```

and replace `Assert.That(distanceFill.fillAmount, Is.EqualTo(.42f).Within(.001f));` with

```csharp
            Assert.That(distanceBar.Value, Is.EqualTo(.42f).Within(.001f));
            Assert.That(distanceBar.Fill.rectTransform.anchorMax.x, Is.EqualTo(.42f).Within(.001f));
```

In `SprintScene_BuildsTheApprovedScoreboardAndRail`, replace the three `railFill` lines with

```csharp
            KitBar rail = chrome.Find("ProgressRail").GetComponent<KitBar>();
            Assert.That(rail.Value, Is.EqualTo(0f).Within(.001f));
            Assert.That(rail.Fill.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(rail.Fill.sprite, Is.SameAs(UiKitAssets.Load().RoundRect24));
```

and change the scoreboard sprite assertion to

```csharp
            Assert.That(chrome.Find("Scoreboard").GetComponent<Image>().sprite, Is.SameAs(UiKitAssets.Load().RoundRect24),
                "The scoreboard must use the kit's rounded sprite, not the default square.");
```

Replace the whole method `SprintResultPresentationStylesOutcomesAndKeepsSingleContinueDuringAnimation` with:

```csharp
        [UnityTest]
        public IEnumerator SprintResultUsesSharedPanelWithSprintTitlesAndASingleContinue()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var panel = SceneObjects<ResultPanel>(scene)[0];
            Transform content = panel.transform.Find("Content");
            TMP_Text title = content.Find("StatusLabel").GetComponent<TMP_Text>();
            TMP_Text score = content.Find("ScoreLabel").GetComponent<TMP_Text>();
            TMP_Text rank = content.Find("RankLabel").GetComponent<TMP_Text>();
            Button action = content.Find("ActionButton").GetComponent<Button>();
            Assert.That(panel.GetComponent("SprintResultPresentation"), Is.Null, "the reveal now lives in ResultPanel");

            var routes = new List<string>();
            panel.ActionRequested += routes.Add;

            panel.Show(new MinigameResult(false, 0f, Rank.F), "Punishment");
            Assert.That(title.text, Is.EqualTo("THẤT BẠI"));
            AssertColor32(title.color, (Color32)MinigameUiTheme.Energy);
            Assert.That(score.text, Is.EqualTo("0"));
            Assert.That(rank.text, Is.EqualTo("XẾP HẠNG F"));
            Assert.That(action.interactable, Is.True);
            Assert.That(content.Find("RetryButton").gameObject.activeSelf, Is.False);

            panel.Continue();
            panel.Continue();
            Assert.That(routes, Is.EqualTo(new[] { "Punishment" }));

            panel.Show(new MinigameResult(true, 8.4f, Rank.A), "Map");
            Assert.That(title.text, Is.EqualTo("HOÀN THÀNH!"));
            AssertColor32(title.color, (Color32)MinigameUiTheme.Success);
            Assert.That(score.text, Is.EqualTo("8"));
            Assert.That(rank.text, Is.EqualTo("XẾP HẠNG A"));

            yield return new WaitForSecondsRealtime(1f);
            Assert.That(score.text, Is.EqualTo("8"), "the count-up ends on the final score");
        }
```

(`AssertColor32` is the existing helper in this file; if its colour comparison is exact on alpha, compare `title.color` after the title fade, i.e. keep the `WaitForSecondsRealtime` above the second colour check if it fails.)

Run tests: `PlayMode` `KMA.Tests.Presentation.SprintPresentationGateTests` `sprint-kit-red`
Expected: failures in the rail/result tests (`KitBar` missing on `ProgressRail`, titles not "HOÀN THÀNH!").

- [ ] **Step 2: Rewrite the Sprint HUD builder**

In `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`, replace the usings and the whole `SprintFestivalPresentation` class (from the top of the file through the closing brace of the class, before the `SprintPlayerMarkerPlacement` summary comment) with:

```csharp
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public static class SprintFestivalPresentation
    {
        const string SuccessTitle = "HOÀN THÀNH!";
        const string FailureTitle = "THẤT BẠI";
        // The world-space PLAYER plate is a sliced kit sprite drawn at this scale.
        const float MarkerSpriteScale = .25f;

        public static void Build()
        {
            if (GameObject.Find("SprintBroadcastChrome") != null)
                return;

            Canvas canvas = GameObject.Find("S2_HUD_Minigame")?.GetComponent<Canvas>()
                ?? Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            Transform safeArea = canvas.transform.Find("SafeAreaRoot");
            if (safeArea == null)
                return;
            PrepareSafeArea(safeArea);

            GameObject oldChrome = GameObject.Find("SprintFestivalChrome");
            if (oldChrome != null)
                Object.Destroy(oldChrome);

            DisableSharedMetrics(safeArea);

            RectTransform root = UiKit.Rect(safeArea, "SprintBroadcastChrome");
            UiKit.Stretch(root);
            root.SetAsLastSibling();

            RectTransform safeRect = (RectTransform)safeArea;
            Rect safe = SafeRect(safeRect);

            var chromeLayout = root.gameObject.AddComponent<SprintChromeLayout>();
            chromeLayout.Bind(safeRect);

            // Progress rail: the kit bar re-derives its corner radius whenever its height changes.
            KitBar rail = UiKit.Bar(root, "ProgressRail", pip: true);
            rail.Fill.name = "RailFill";
            rail.Pip.name = "PlayerPip";
            var railRect = (RectTransform)rail.transform;
            ApplyRect(railRect, safe, SprintUiLayout.ProgressRailRect(safe));
            chromeLayout.Register(railRect, SprintUiLayout.ProgressRailRect);
            RectTransform pip = rail.Pip.rectTransform;
            pip.sizeDelta = new Vector2(safe.height * .014f, 0f);
            // The pip's width comes from safe.height; if the Canvas has not laid out yet
            // (safe.height == 0) it would stay invisible, so re-derive it when the safe area changes.
            chromeLayout.Register(liveSafe => pip.sizeDelta = new Vector2(liveSafe.height * .014f, pip.sizeDelta.y));

            // Scoreboard
            Image scoreboard = UiKit.Panel(root, "Scoreboard");
            ApplyRect(scoreboard.rectTransform, safe, SprintUiLayout.ScoreboardRect(safe));
            chromeLayout.Register(scoreboard.rectTransform, SprintUiLayout.ScoreboardRect);

            TMP_Text distance = Metric(scoreboard.transform, "Distance", MinigameUiTheme.Title,
                MinigameUiTheme.TextPrimary, new Vector2(.05f, .46f), new Vector2(.62f, .92f));
            distance.alignment = TextAlignmentOptions.Left;
            distance.text = "0 / 100 m";

            Image rankBadge = UiKit.Shape(scoreboard.transform, "RankBadge", MinigameUiTheme.RadiusPanel,
                MinigameUiTheme.WithAlpha(MinigameUiTheme.Accent, .22f));
            UiKit.Anchor(rankBadge.rectTransform, new Vector2(.66f, .46f), new Vector2(.95f, .92f));
            TMP_Text rank = UiKit.Label(rankBadge.transform, "RankLabel", "1st", MinigameUiTheme.Headline,
                MinigameUiTheme.Accent);
            UiKit.Stretch(rank.rectTransform);

            TMP_Text combo = Metric(scoreboard.transform, "Combo", MinigameUiTheme.Body,
                MinigameUiTheme.Energy, new Vector2(.05f, .10f), new Vector2(.62f, .42f));
            combo.alignment = TextAlignmentOptions.Left;
            combo.text = "COMBO ×0";

            // Mode chip
            TMP_Text mode = UiKit.Label(root, "ModeLabel", "CHẠY NƯỚC RÚT · 100M", MinigameUiTheme.Caption,
                MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .6f));
            ApplyRect(mode.rectTransform, safe, SprintUiLayout.ModeChipRect(safe));
            chromeLayout.Register(mode.rectTransform, SprintUiLayout.ModeChipRect);

            EnsurePause(root, safe, chromeLayout);
            EnsureStartPresentation(root, safe);
            EnsureControls(root, safe);
            EnsurePlayerIdentity(root);
            EnsureFinishLine(root);
            EnsureResultPresentation();
        }

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

        /// Reachable from SprintChromeLayout, which lives outside this static class.
        internal static void ApplyRectPublic(RectTransform rect, Rect safe, Rect target) =>
            ApplyRect(rect, safe, target);

        static Rect SafeRect(RectTransform safeArea) =>
            new Rect(0f, 0f, safeArea.rect.width, safeArea.rect.height);

        static void EnsureFinishLine(RectTransform root)
        {
            RectTransform finish = UiKit.Rect(root, "FinishLine");
            finish.anchorMin = new Vector2(
                SprintUiLayout.FinishAnchorMinX(SprintUiLayout.FinishDistance),
                SprintUiLayout.FinishAnchorMinY);
            finish.anchorMax = new Vector2(
                SprintUiLayout.FinishAnchorMaxX(SprintUiLayout.FinishDistance),
                SprintUiLayout.FinishAnchorMaxY);
            finish.offsetMin = Vector2.zero;
            finish.offsetMax = Vector2.zero;

            const int squareCount = 10;
            for (int i = 0; i < squareCount; i++)
            {
                RectTransform square = UiKit.Rect(finish, $"Square{i}");
                UiKit.Anchor(square, new Vector2(0f, (float)i / squareCount), new Vector2(1f, (float)(i + 1) / squareCount));
                Image image = square.gameObject.AddComponent<Image>();
                image.color = i % 2 == 0 ? Color.black : Color.white;
                image.raycastTarget = false;
            }

            var presenter = root.GetComponent<SprintFinishLinePresenter>()
                ?? root.gameObject.AddComponent<SprintFinishLinePresenter>();
            presenter.Configure(Object.FindFirstObjectByType<SprintController>(), finish.gameObject);
        }

        static void EnsureResultPresentation()
        {
            KMA.Gameplay.UI.ResultPanel panel =
                Object.FindFirstObjectByType<KMA.Gameplay.UI.ResultPanel>(FindObjectsInactive.Include);
            if (panel != null)
                panel.SetTitles(SuccessTitle, FailureTitle);
        }

        static void EnsureStartPresentation(RectTransform parent, Rect safe)
        {
            RectTransform root = UiKit.Rect(parent, "StartPresentation");
            UiKit.Stretch(root);
            root.SetAsLastSibling();

            TMP_Text countdown = UiKit.Countdown(root, "CountdownLabel");
            if (safe.width > 0f && safe.height > 0f)
                ApplyRect(countdown.rectTransform, safe, SprintUiLayout.CountdownRect(safe));

            Image plate = UiKit.Panel(root, "InstructionPlate", MinigameUiTheme.RadiusPanel, MinigameUiTheme.SurfaceSoft);
            RectTransform instructionRoot = plate.rectTransform;
            if (safe.width > 0f && safe.height > 0f)
                ApplyRect(instructionRoot, safe, SprintUiLayout.InstructionRect(safe));

            TMP_Text instruction = UiKit.Label(instructionRoot, "InstructionLabel",
                SprintStartPresentation.InstructionCopy, MinigameUiTheme.BodyLarge, MinigameUiTheme.TextPrimary);
            UiKit.Stretch(instruction.rectTransform, new Vector2(MinigameUiTheme.SpaceMd, MinigameUiTheme.SpaceXs),
                new Vector2(-MinigameUiTheme.SpaceMd, -MinigameUiTheme.SpaceXs));

            SprintStartPresentation presenter = Object.FindFirstObjectByType<SprintStartPresentation>();
            if (presenter == null)
                presenter = root.gameObject.AddComponent<SprintStartPresentation>();
            presenter.Configure(countdown.gameObject, countdown, instructionRoot.gameObject, instruction);
            presenter.Bind(Object.FindFirstObjectByType<SprintController>());

            var chromeLayout = parent.GetComponent<SprintChromeLayout>();
            if (chromeLayout != null)
            {
                chromeLayout.Register(countdown.rectTransform, SprintUiLayout.CountdownRect);
                chromeLayout.Register(instructionRoot, SprintUiLayout.InstructionRect);
            }
        }

        static void PrepareSafeArea(Transform safeArea)
        {
            safeArea.gameObject.SetActive(true);

            // SafeAreaRoot is the only rect in the HUD that stretches (0,0)-(1,1), so it is the only
            // one where a safe-area offset reads as an inset rather than a resize. It owns the inset;
            // the Canvas root must not carry a fitter at all.
            var fitter = safeArea.GetComponent<KMA.Gameplay.UI.SafeAreaFitter>()
                ?? safeArea.gameObject.AddComponent<KMA.Gameplay.UI.SafeAreaFitter>();
            fitter.enabled = true;
            fitter.Apply(Screen.safeArea, new Vector2Int(Screen.width, Screen.height));
        }

        static void EnsureControls(RectTransform root, Rect safe)
        {
            KMA.Input.ScreenTapArea leftTap = FindTapArea("LeftTap");
            KMA.Input.ScreenTapArea rightTap = FindTapArea("RightTap");
            if (leftTap == null || rightTap == null)
                return;

            ControlPlateHandle left = BuildControl(leftTap, safe, true);
            ControlPlateHandle right = BuildControl(rightTap, safe, false);

            var presenter = root.GetComponent<SprintControlPresenter>()
                ?? root.gameObject.AddComponent<SprintControlPresenter>();
            presenter.Configure(Object.FindFirstObjectByType<SprintController>(),
                left.Visual, right.Visual, left.Background, right.Background, left.Border, right.Border);
            presenter.BindPressFeedback(leftTap, rightTap);

            var chromeLayout = root.GetComponent<SprintChromeLayout>();
            if (chromeLayout != null)
            {
                chromeLayout.Register(leftTap.GetComponent<RectTransform>(), safeRect => SprintUiLayout.ControlRect(safeRect, true));
                chromeLayout.Register(rightTap.GetComponent<RectTransform>(), safeRect => SprintUiLayout.ControlRect(safeRect, false));
            }
        }

        static ControlPlateHandle BuildControl(KMA.Input.ScreenTapArea tapArea, Rect safe, bool left)
        {
            var tapRect = tapArea.GetComponent<RectTransform>();
            // A degenerate safe rect (batchmode's headless canvas, or the first frame before
            // Canvas layout runs) would collapse InverseLerp to a zero-size anchor pin. Skip and
            // keep the authored tap-area rect; SprintChromeLayout re-applies once safe is valid.
            if (safe.width > 0f && safe.height > 0f)
                ApplyRect(tapRect, safe, SprintUiLayout.ControlRect(safe, left));

            Image tapImage = tapArea.GetComponent<Image>() ?? tapArea.gameObject.AddComponent<Image>();
            tapImage.color = new Color(1f, 1f, 1f, 0f);
            tapImage.raycastTarget = true;

            Transform existing = tapRect.Find("Visual");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            // The tap area keeps its full size — the hit box is unchanged. Only this visual shrinks,
            // so the button rests on the running lanes without covering the runner in them.
            RectTransform visual = UiKit.Rect(tapRect, "Visual");
            Rect visualRect = SprintUiLayout.ControlVisualRect01;
            UiKit.Anchor(visual, new Vector2(visualRect.xMin, visualRect.yMin), new Vector2(visualRect.xMax, visualRect.yMax));
            return UiKit.ControlPlate(visual, left ? "←" : "→", left ? "TRÁI" : "PHẢI");
        }

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

        static TMP_Text Metric(Transform parent, string name, float fontSize, Color color, Vector2 min, Vector2 max)
        {
            TMP_Text text = UiKit.Label(parent, name, string.Empty, fontSize, color);
            UiKit.Anchor(text.rectTransform, min, max);
            return text;
        }

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
            marker.localPosition = new Vector3(SprintPlayerMarkerPlacement.SideOffset,
                SprintPlayerMarkerPlacement.MarkerHeight, 0f);

            Sprite plateSprite = UiKitAssets.Load().RoundRect20;
            var plate = new GameObject("Plate", typeof(SpriteRenderer)).transform;
            plate.SetParent(marker, false);
            plate.localScale = new Vector3(MarkerSpriteScale, MarkerSpriteScale, 1f);
            var plateRenderer = plate.GetComponent<SpriteRenderer>();
            plateRenderer.sprite = plateSprite;
            plateRenderer.drawMode = SpriteDrawMode.Sliced;
            // 1.3 x 0.34 world units: the plate has to cover the PLAYER label, not sit behind it.
            plateRenderer.size = new Vector2(1.296f, .342f) / MarkerSpriteScale;
            plateRenderer.color = MinigameUiTheme.WithAlpha(MinigameUiTheme.Surface, MinigameUiTheme.SurfaceOpaque);
            plateRenderer.sortingOrder = 19;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(marker, false);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "PLAYER";
            label.fontSize = 48;
            label.characterSize = .055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = MinigameUiTheme.Player;
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer != null)
                labelRenderer.sortingOrder = 20;

            var chevron = new GameObject("Chevron", typeof(SpriteRenderer)).transform;
            chevron.SetParent(marker, false);
            chevron.localPosition = new Vector3(-SprintPlayerMarkerPlacement.ChevronOffset, 0f, 0f);
            chevron.localScale = new Vector3(.22f, .22f, 1f);
            chevron.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var chevronRenderer = chevron.GetComponent<SpriteRenderer>();
            chevronRenderer.sprite = plateSprite;
            chevronRenderer.drawMode = SpriteDrawMode.Sliced;
            chevronRenderer.size = new Vector2(.06f, .06f);
            chevronRenderer.color = MinigameUiTheme.Player;
            chevronRenderer.sortingOrder = 20;

            var placement = presentation.GetComponent<SprintPlayerMarkerPlacement>()
                ?? presentation.gameObject.AddComponent<SprintPlayerMarkerPlacement>();
            placement.Bind(marker);

            SpriteRenderer playerVisual = presentation.GetComponentInChildren<SpriteRenderer>(true);
            if (playerVisual != null && playerVisual.transform != plate && playerVisual.transform != chevron)
            {
                var identity = presentation.GetComponent<SprintPlayerIdentityOutline>()
                    ?? presentation.gameObject.AddComponent<SprintPlayerIdentityOutline>();
                identity.Bind(playerVisual, MinigameUiTheme.Player);
            }
        }

        static void EnsurePause(RectTransform parent, Rect safe, SprintChromeLayout chromeLayout)
        {
            if (Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>() != null)
                return;

            RectTransform button = UiKit.Rect(parent, "PausePanel");
            UiKit.StylePauseButton(button);
            if (safe.width > 0f && safe.height > 0f)
            {
                ApplyRect(button, safe, SprintUiLayout.PauseRect(safe));
            }
            else
            {
                // A degenerate first-frame safe rect (batchmode's headless canvas) collapses the
                // usual spread-anchor math to (0,0); pin to the top-right corner instead.
                // SprintChromeLayout re-applies the real anchors once safe becomes valid.
                UiKit.Place(button, Vector2.one, Vector2.one, new Vector2(-38f, -65f), new Vector2(96f, 96f));
            }
            chromeLayout.Register(button, SprintUiLayout.PauseRect);
            button.gameObject.AddComponent<KMA.Gameplay.UI.PausePanel>();
        }

        static KMA.Input.ScreenTapArea FindTapArea(string name)
        {
            GameObject target = GameObject.Find(name);
            return target == null ? null : target.GetComponent<KMA.Input.ScreenTapArea>();
        }
    }
```

The file still ends with the unchanged `SprintPlayerMarkerPlacement` and `SprintPlayerIdentityOutline` classes and the namespace's closing brace.

- [ ] **Step 3: Drive the rail through `KitBar` in `SprintHud`**

Replace the whole content of `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs`:

```csharp
using KMA.UI.Kit;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintHud : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] Transform metricsRoot;
        [SerializeField] TMP_Text distanceLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text cadenceLabel;
        [SerializeField] KitBar distanceBar;

        public string DistanceText { get; private set; } = string.Empty;
        public string RankText { get; private set; } = string.Empty;
        public string CadenceText { get; private set; } = string.Empty;
        public float PipProgress { get; private set; }

        void Awake()
        {
            if (controller == null)
                controller = Object.FindFirstObjectByType<SprintController>();
            SprintFestivalPresentation.Build();
            CacheVisuals();
        }

        void OnEnable() => Refresh();
        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null)
                return;

            var snapshot = controller.Snapshot;
            float progress = Mathf.Clamp01(snapshot.Distance / 100f);

            DistanceText = $"{Mathf.RoundToInt(snapshot.Distance)} / 100 m";
            RankText = controller.RankText;
            CadenceText = $"COMBO ×{controller.CadenceCombo}";
            PipProgress = progress;

            if (distanceLabel != null) distanceLabel.text = DistanceText;
            if (rankLabel != null) rankLabel.text = RankText;
            if (cadenceLabel != null) cadenceLabel.text = CadenceText;
            if (distanceBar != null) distanceBar.SetValue(progress);
        }

        public bool HasBoundVisuals => metricsRoot != null && distanceLabel != null && rankLabel != null &&
            cadenceLabel != null && distanceBar != null;

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
            distanceBar = chrome.Find("ProgressRail")?.GetComponent<KitBar>();
        }
    }
}
```

- [ ] **Step 4: Colour the tap zones through `KitControlState`**

In `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs` (it has `using KMA.UI.Kit;` from Task 1), change `const float PressScale = .94f;` to `const float PressScale = MinigameUiTheme.PressScale;` and replace `ApplyState` with:

```csharp
        void ApplyState(Side side, Image background, Image border)
        {
            if (background == null || border == null)
                return;

            bool pressed = pressRemaining > 0f && pressedSide == side;
            bool expected = HighlightedSide == side;
            bool finished = controller != null && controller.PresentationPhase == MinigamePhase.Resolve;

            ControlState state = pressed ? ControlState.Pressed
                : finished ? ControlState.Disabled
                : expected ? ControlState.Hint
                : ControlState.Rest;
            KitControlState.Apply(background, border, state);
        }
```

- [ ] **Step 5: Delete the runtime shape generator and the Sprint-only result presenter**

```bash
git rm Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs Assets/_Project/Scripts/Gameplay/Sprint/SprintUiShapes.cs.meta \
       Assets/_Project/Scripts/Gameplay/Sprint/SprintResultPresentation.cs Assets/_Project/Scripts/Gameplay/Sprint/SprintResultPresentation.cs.meta
grep -rn "SprintUiShapes\|SprintResultPresentation" Assets --include=*.cs
```

Expected: the grep prints nothing, except the `GetComponent("SprintResultPresentation")` string in the gate test.

- [ ] **Step 6: Run the Sprint tests**

Run tests: `PlayMode` `KMA.Tests.Presentation.SprintPresentationGateTests;KMA.Tests.Presentation.FestivalUiExperienceTests;KMA.Tests.Presentation.RunnerVisualTests` `sprint-kit`
Expected: `result="Passed"`, `failed="0"`.

Run tests: `EditMode` `KMA.Tests.Presentation.SprintUiLayoutTests;KMA.Tests.Presentation.SprintTrackLayoutTests` `sprint-kit-edit`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add -A Assets/_Project/Scripts/Gameplay/Sprint Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
git commit -m "feat(sprint): build the HUD from the shared UI kit"
```

---

### Task 8: Volleyball on the kit

**Files:**
- Modify: `Assets/Editor/VolleyballSceneConfigurator.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VirtualJoystick.cs`, `ActionButton.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/KMA.Gameplay.Volleyball.asmdef` (add `"KMA.Gameplay.UI"`)
- Modify (by running the configurator): `Assets/_Project/Scenes/MG_Volleyball.unity`
- Test: `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs`

**Interfaces:**
- Consumes: `UiKit.Joystick`, `UiKit.RoundButton`, `UiKit.Panel`, `UiKit.Label`, `UiKit.Chip`, `UiKit.Place`, `KitControlState` (Task 3).
- Produces: `VirtualJoystick.Configure(area, base, knob, radius, rest)` unchanged; `ActionButton` loses `Configure` and keeps `Pressed` / `Press()`.

- [ ] **Step 1: Write the failing configurator test**

Add to `Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs` (add `using KMA.Gameplay.UI;`, `using KMA.UI.Kit;` and `using UnityEngine.UI;` to the usings):

```csharp
        [Test]
        public void ControlsScoreboardAndPauseAreDrawnWithTheUiKit()
        {
            VolleyballSceneConfigurator.BuildScene();
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            UiKitAssets assets = UiKitAssets.Load();

            foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (image.sprite != null)
                    Assert.That(image.sprite.name, Is.Not.EqualTo("Knob"), image.name + " still stretches the built-in knob");

            var scoreboard = GameObject.Find("VolleyballScoreboard").GetComponent<Image>();
            Assert.That(scoreboard.sprite, Is.SameAs(assets.RoundRect24));
            Assert.That(scoreboard.type, Is.EqualTo(Image.Type.Sliced), "a rounded panel, not a stretched ellipse");

            var action = Object.FindFirstObjectByType<ActionButton>();
            var feedback = action.GetComponent<KitPressFeedback>();
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.RestColor, Is.EqualTo(MinigameUiTheme.Accent));

            Assert.That(GameObject.Find("JoystickBase").GetComponent<Image>().sprite, Is.SameAs(assets.Circle));
            Assert.That(GameObject.Find("PlayerTitle").GetComponent<TMPro.TMP_Text>().color, Is.EqualTo(MinigameUiTheme.Player));
            Assert.That(GameObject.Find("EnemyTitle").GetComponent<TMPro.TMP_Text>().color, Is.EqualTo(MinigameUiTheme.Energy));

            var pause = Object.FindFirstObjectByType<PausePanel>();
            Assert.That(pause.GetComponent<Image>().sprite, Is.SameAs(assets.RoundRect20));
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
        }
```

Run tests: `EditMode` `KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests` `volleyball-kit-red`
Expected: `ControlsScoreboardAndPauseAreDrawnWithTheUiKit` fails (the scoreboard sprite is `Knob`).

- [ ] **Step 2: Let Volleyball use the kit**

In `Assets/_Project/Scripts/Gameplay/Volleyball/KMA.Gameplay.Volleyball.asmdef`, add `"KMA.Gameplay.UI"` to `references`.

Replace the whole content of `Assets/_Project/Scripts/Gameplay/Volleyball/ActionButton.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KMA.Gameplay.Volleyball
{
    /// The ĐÁNH hit area. Its look and press response come from the kit's KitPressFeedback.
    public sealed class ActionButton : MonoBehaviour, IPointerDownHandler
    {
        public event Action Pressed;

        // Fires on touch-down, not release: timing windows are only a few frames wide.
        public void OnPointerDown(PointerEventData eventData) => Press();

        public void Press() => Pressed?.Invoke();
    }
}
```

In `Assets/_Project/Scripts/Gameplay/Volleyball/VirtualJoystick.cs`: add `using KMA.UI.Kit;`; replace the field `[SerializeField] Image knobImage;` with `[SerializeField] Image rimImage;`; in `Configure` replace the `knobImage = …` line with

```csharp
            rimImage = stickBase ? stickBase.Find("JoystickRim")?.GetComponent<Image>() : null;
```

in `Press` replace the two colour `if` blocks with `KitControlState.Apply(baseImage, rimImage, ControlState.Hint);`, and in `Release` replace the two colour `if` blocks with `KitControlState.Apply(baseImage, rimImage, ControlState.Rest);`.

- [ ] **Step 3: Build the Volleyball controls with the kit**

In `Assets/Editor/VolleyballSceneConfigurator.cs`:
- add `using KMA.UI.Kit;`;
- delete `FontPath`, `KnobSpritePath`, `ButtonColor`, `Cyan`, `Coral` and the `Circle`, `Panel` and `Label` helper methods;
- rename `Ink` to `BallInk` (the procedural ball keeps its outline colour) and update its use in `BallColor`;
- in `BuildWorld`, change the two marker calls to

```csharp
            AddAthleteMarker(player.transform, pixel, "Player", MinigameUiTheme.Player);
            AddAthleteMarker(opponent.transform, pixel, "Enemy", MinigameUiTheme.Energy);
```

- in `AddAthleteMarker`, change `edge.color = Ink;` to `edge.color = MinigameUiTheme.Surface;`;
- replace `AddControlsAndHud` with:

```csharp
        static void AddControlsAndHud()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hudRoot = scene.GetRootGameObjects().Single(go => go.name == HudRootName);
            hudRoot.GetComponent<Canvas>().sortingOrder = HudSortingOrder;
            var parent = (RectTransform)(hudRoot.transform.Find("SafeAreaRoot") ?? hudRoot.transform);
            // The generic HUD belongs to other sports. Its timer, progress and status fields do
            // not represent volleyball, so hide only its prefab children in this scene.
            foreach (Transform child in parent)
                child.gameObject.SetActive(false);
            Camera.main.orthographicSize = 5.45f;

            RectTransform controls = UiRect("VolleyballControls", parent, Vector2.zero, Vector2.one);
            controls.SetAsFirstSibling();

            RectTransform area = UiRect("JoystickArea", controls, Vector2.zero, new Vector2(.38f, .52f));
            area.gameObject.AddComponent<Image>().color = Color.clear;
            JoystickHandle stick = UiKit.Joystick(area);
            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(area, stick.Base.rectTransform, stick.Knob.rectTransform, 88f, new Vector2(-135f, -140f));

            RectTransform buttonRect = UiRect("ActionButton", controls, Vector2.one, Vector2.one);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.sizeDelta = new Vector2(310f, 310f);
            buttonRect.anchoredPosition = new Vector2(-18f, 18f);
            buttonRect.gameObject.AddComponent<Image>().color = Color.clear;
            var button = buttonRect.gameObject.AddComponent<ActionButton>();
            UiKit.RoundButton(buttonRect, "ĐÁNH");

            Vector2 centre = new Vector2(.5f, .5f);
            Image scoreboard = UiKit.Panel(controls, "VolleyballScoreboard");
            UiKit.Place(scoreboard.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -12f),
                new Vector2(650f, 84f));
            TMP_Text playerTitle = UiKit.Label(scoreboard.transform, "PlayerTitle", "PLAYER", MinigameUiTheme.Body,
                MinigameUiTheme.Player);
            UiKit.Place(playerTitle.rectTransform, new Vector2(0f, .5f), centre, new Vector2(136f, 0f), new Vector2(225f, 65f));
            TMP_Text score = UiKit.Label(scoreboard.transform, "Score", VolleyballHud.ScoreText(0, 0),
                MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            UiKit.Place(score.rectTransform, centre, centre, Vector2.zero, new Vector2(215f, 74f));
            TMP_Text enemyTitle = UiKit.Label(scoreboard.transform, "EnemyTitle", "ENEMY", MinigameUiTheme.Body,
                MinigameUiTheme.Energy);
            UiKit.Place(enemyTitle.rectTransform, new Vector2(1f, .5f), centre, new Vector2(-136f, 0f), new Vector2(225f, 65f));

            TMP_Text feedback = UiKit.Label(controls, "Feedback", string.Empty, MinigameUiTheme.Headline,
                MinigameUiTheme.Accent, TextAlignmentOptions.Center, outline: true);
            UiKit.Place(feedback.rectTransform, new Vector2(.5f, .75f), centre, Vector2.zero, new Vector2(680f, 100f));
            ChipHandle hint = UiKit.Chip(controls, "HintBanner", VolleyballHud.HintText);
            hint.Label.name = "Hint";
            UiKit.Place(hint.Background.rectTransform, new Vector2(.5f, .045f), centre, Vector2.zero, new Vector2(860f, 66f));
            var hud = controls.gameObject.AddComponent<VolleyballHud>();
            hud.Configure(score, feedback, hint.Label);
            hud.ConfigureHintBackdrop(hint.Background.gameObject);

            var pause = Object.FindFirstObjectByType<PausePanel>();
            if (pause)
            {
                pause.transform.SetParent(parent, false);
                UiKit.Place((RectTransform)pause.transform, Vector2.one, Vector2.one, new Vector2(-22f, -18f),
                    Vector2.one * MinigameUiTheme.ButtonHeight);
            }

            var controller = Object.FindFirstObjectByType<VolleyballController>();
            controller.Input.Configure(joystick, button);
            controller.Configure(controller.PlayerView, controller.OpponentView, controller.BallView, controller.Input, hud);

            foreach (Object dirty in new Object[] { joystick, button, hud, controller, controller.Input, hudRoot })
                EditorUtility.SetDirty(dirty);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
```

- [ ] **Step 4: Rebuild the Volleyball scene**

Run an editor method: `KMA.EditorTools.VolleyballSceneConfigurator.BuildScene`, log `build-volleyball`.
Expected: `exit=0` and `[KMA] MG_Volleyball built.` in the log.

- [ ] **Step 5: Run the Volleyball tests**

Run tests: `EditMode` `KMA.Tests.EditorTools.VolleyballSceneConfiguratorTests;KMA.Tests.EditorTools.HeroConsistencyTests` `volleyball-kit`
Expected: `result="Passed"`, `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Volleyball` `volleyball-kit-play`
Expected: `result="Passed"`, `failed="0"`. Also run `EditMode` `KMA.Tests.Gameplay.Volleyball` `volleyball-kit-touch` (the `TouchControlTests`); expected the same.

- [ ] **Step 6: Commit**

```bash
git add Assets/Editor/VolleyballSceneConfigurator.cs Assets/_Project/Scripts/Gameplay/Volleyball \
  Assets/_Project/Scenes/MG_Volleyball.unity Assets/Tests/EditMode/EditorTools/VolleyballSceneConfiguratorTests.cs
git commit -m "feat(volleyball): draw controls, scoreboard and hint with the shared UI kit"
```

---

### Task 9: Football on the kit and the shared screens

**Files:**
- Modify: `Assets/Editor/FootballSceneConfigurator.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballHud.cs`, `FootballController.cs`, `KMA.Gameplay.Football.asmdef` (add `"KMA.Gameplay.UI"`)
- Modify: `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs` (remove the `MinigamePresentationOwner` branch)
- Delete: `Assets/_Project/Scripts/Gameplay/Football/FootballResultPanel.cs`, `Assets/_Project/Scripts/Gameplay/Common/MinigamePresentationOwner.cs`, `Assets/Tests/PlayMode/Gameplay/Football/FootballResultPanelTests.cs` (+ `.meta`)
- Delete: `Assets/_Project/Art/Football/GoalView/{panel,fill}.{png,svg}`, `Assets/_Project/Art/Football/UI/{button,panel,power-fill}.png` (+ `.meta`)
- Modify: `tools/export-football-preview-art.py` (stop exporting `panel` and `fill`)
- Modify (by running the configurator): `Assets/_Project/Scenes/MG_Football.unity`
- Test: `Assets/Tests/EditMode/EditorTools/FootballSceneConfiguratorTests.cs`, `Assets/Tests/PlayMode/Gameplay/Football/{FootballControllerTests,FootballPresentationTests,FootballSceneTests}.cs`, `KMA.Gameplay.Football.PlayMode.Tests.asmdef` (add `"KMA.Gameplay.UI"`), `Assets/Tests/PlayMode/Progression/FootballResultRoutingTests.cs`

**Interfaces:**
- Consumes: `ResultPanel` (Task 4), flags (Task 5), `UiKit`, `KitBar`, `ButtonVariant` (Task 3), `MinigameUIAssembler.AssembleScenePath`.
- Produces: `FootballHud.Configure(Slider aim, FootballHoldButton shoot, KitBar power, GameObject warning, TMP_Text score, TMP_Text remaining, Image[] markers, GameObject start, Button startAction = null, Button easy = null, Button normal = null, Button hard = null, TMP_Text directionText = null, TMP_Text feedback = null)`; `FootballController.Configure(FootballDifficultyConfig, FootballInputBridge, FootballPresentation, FootballHud, ResultPanel)`.

- [ ] **Step 1: Rewrite the configurator test for the shared screens (fails first)**

Replace the body of `RepeatedBuildAndSharedAssemblerKeepOneSourcedFootballScene` in `Assets/Tests/EditMode/EditorTools/FootballSceneConfiguratorTests.cs` with (add `using KMA.UI.Kit;` and `using UnityEngine.UI;`):

```csharp
            FootballSceneConfigurator.BuildScene();
            FootballSceneConfigurator.BuildScene();
            MinigameUIAssembler.AssembleScenePath(FootballSceneConfigurator.ScenePath);
            var scene = EditorSceneManager.OpenScene(FootballSceneConfigurator.ScenePath, OpenSceneMode.Single);

            int controllers = 0, resultPanels = 0, huds = 0, placeholders = 0, pauses = 0;
            FootballPresentation presentation = null;
            EventSystem eventSystem = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                controllers += root.GetComponentsInChildren<FootballController>(true).Length;
                huds += root.GetComponentsInChildren<FootballHud>(true).Length;
                pauses += root.GetComponentsInChildren<PausePanel>(true).Length;
                placeholders += root.GetComponentsInChildren<PlaceholderMinigameController>(true).Length;
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour is IResultPreviewPanel) resultPanels++;
                presentation ??= root.GetComponentInChildren<FootballPresentation>(true);
                eventSystem ??= root.GetComponentInChildren<EventSystem>(true);
            }

            Assert.That(controllers, Is.EqualTo(1));
            Assert.That(resultPanels, Is.EqualTo(1), "only the shared ResultPanel");
            Assert.That(huds, Is.EqualTo(1));
            Assert.That(pauses, Is.EqualTo(1));
            Assert.That(placeholders, Is.Zero);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.ValidateReferences(), Is.True);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);

            GameObject hud = GameObject.Find("S2_HUD_Minigame");
            Assert.That(hud, Is.Not.Null, "Football uses the shared HUD canvas");
            Assert.That(hud.GetComponent<Canvas>().sortingOrder, Is.EqualTo(500));
            Assert.That(GameObject.Find("FootballHUD"), Is.Null);
            Assert.That(GameObject.Find("BackButton"), Is.Null, "the pause menu replaces the back button");
            Assert.That(GameObject.Find("CountdownPanel"), Is.Null, "the shared 3-2-1 replaces SẴN SÀNG!");

            var controller = UnityEngine.Object.FindFirstObjectByType<FootballController>();
            Assert.That(controller.ValidateReferences(), Is.True);
            var overlay = UnityEngine.Object.FindFirstObjectByType<PhaseOverlay>(FindObjectsInactive.Include);
            Assert.That(new SerializedObject(overlay).FindProperty("minigameSource").objectReferenceValue, Is.SameAs(controller));

            var pause = UnityEngine.Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            Assert.That(pause.transform.parent.name, Is.EqualTo("SafeAreaRoot"));
            Assert.That(pause.GetComponent<Image>().sprite, Is.SameAs(UiKitAssets.Load().RoundRect20));

            var slider = GameObject.Find("DirectionSlider").GetComponent<Slider>();
            Assert.That(slider.minValue, Is.EqualTo(-1f));
            Assert.That(slider.maxValue, Is.EqualTo(1f));
            Assert.That(slider.handleRect, Is.Not.Null);
            Assert.That(GameObject.Find("PowerBar").GetComponent<KitBar>(), Is.Not.Null);
            Assert.That(GameObject.Find("SHOOT").GetComponent<KitPressFeedback>(), Is.Not.Null);
            Assert.That(GameObject.Find("AIM"), Is.Null);
            Assert.That(GameObject.Find("TrajectoryDot0").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<Camera>().backgroundColor,
                Is.EqualTo((Color)new Color32(120, 207, 235, 255)), "the sky survives the assembler's camera setup");

            var subject = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/ScriptableObjects/Subjects/Football.asset");
            Assert.That(subject, Is.Not.Null);
            var serializedSubject = new SerializedObject(subject);
            Assert.That(serializedSubject.FindProperty("timeLimit").floatValue, Is.Zero);
            Assert.That(serializedSubject.FindProperty("goalText").stringValue, Is.EqualTo("Ghi ít nhất 3 bàn sau 5 lượt sút."));
            Assert.That(EditorBuildSettings.scenes, Has.Some.Matches<EditorBuildSettingsScene>(s => s.path == FootballSceneConfigurator.ScenePath && s.enabled));
```

Run tests: `EditMode` `KMA.Tests.EditorTools.FootballSceneConfiguratorTests` `football-kit-red`
Expected: the test fails (`S2_HUD_Minigame` is null).

- [ ] **Step 2: Put Football on the shared result panel and the kit HUD**

In `Assets/_Project/Scripts/Gameplay/Football/KMA.Gameplay.Football.asmdef`, add `"KMA.Gameplay.UI"` to `references`.

Replace the whole content of `Assets/_Project/Scripts/Gameplay/Football/FootballHud.cs`:

```csharp
using System;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class FootballHud : MonoBehaviour
    {
        const float OverPowerThreshold = .85f;

        [SerializeField] Slider directionSlider;
        [SerializeField] TMP_Text directionLabel;
        [SerializeField] TMP_Text shotFeedback;
        [SerializeField] FootballHoldButton shootButton;
        [SerializeField] KitBar powerBar;
        [SerializeField] GameObject overPowerWarning;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text remainingLabel;
        [SerializeField] Image[] kickMarkers = new Image[5];
        [SerializeField] GameObject startPanel;
        [SerializeField] Button startButton;
        [SerializeField] Button easyButton;
        [SerializeField] Button normalButton;
        [SerializeField] Button hardButton;

        FootballDifficulty selectedDifficulty = FootballDifficulty.Normal;
        bool listenersBound;

        public event Action<FootballDifficulty> StartRequested;
        public Slider DirectionSlider => directionSlider;
        public FootballHoldButton ShootButton => shootButton;

        public void Configure(Slider aim, FootballHoldButton shoot, KitBar power, GameObject warning, TMP_Text score,
            TMP_Text remaining, Image[] markers, GameObject start, Button startAction = null, Button easy = null,
            Button normal = null, Button hard = null, TMP_Text directionText = null, TMP_Text feedback = null)
        {
            directionSlider = aim;
            shootButton = shoot;
            powerBar = power;
            overPowerWarning = warning;
            scoreLabel = score;
            remainingLabel = remaining;
            kickMarkers = markers;
            startPanel = start;
            startButton = startAction;
            easyButton = easy;
            normalButton = normal;
            hardButton = hard;
            directionLabel = directionText;
            shotFeedback = feedback;
            BindButtons();
            UpdateDifficultyButtons();
        }

        public bool ValidateReferences() => directionSlider && shootButton && powerBar && overPowerWarning &&
            scoreLabel && remainingLabel && kickMarkers != null && kickMarkers.Length == 5 &&
            Array.TrueForAll(kickMarkers, marker => marker) && startPanel;

        public void ShowStart(FootballDifficulty selected)
        {
            selectedDifficulty = selected;
            if (startPanel)
                startPanel.SetActive(true);
            if (directionSlider) directionSlider.interactable = false;
            if (shootButton) shootButton.SetInteractable(false);
            UpdateDifficultyButtons();
        }

        public void HideStart()
        {
            if (startPanel) startPanel.SetActive(false);
        }

        public void SetDifficulty(FootballDifficulty selected)
        {
            selectedDifficulty = selected;
            UpdateDifficultyButtons();
        }

        public void RequestStart() => StartRequested?.Invoke(selectedDifficulty);

        public void Render(FootballRules rules)
        {
            if (rules == null)
                return;
            bool overPower = rules.Power > OverPowerThreshold;
            if (scoreLabel) scoreLabel.text = "BÀN: " + rules.Goals;
            if (remainingLabel) remainingLabel.text = "CÒN " + Mathf.Max(0, 5 - rules.Kicks) + " LƯỢT";
            if (powerBar)
            {
                powerBar.SetValue(rules.Power);
                powerBar.SetFillColor(overPower ? MinigameUiTheme.Energy : MinigameUiTheme.Accent);
                if (powerBar.Label) powerBar.Label.text = Mathf.RoundToInt(rules.Power * 100f) + "%";
            }
            if (overPowerWarning) overPowerWarning.SetActive(overPower);
            for (int i = 0; kickMarkers != null && i < kickMarkers.Length; i++)
            {
                if (!kickMarkers[i]) continue;
                kickMarkers[i].color = i < rules.Outcomes.Count
                    ? (rules.Outcomes[i] == FootballOutcome.Goal ? MinigameUiTheme.Accent : MinigameUiTheme.Energy)
                    : MinigameUiTheme.Track;
            }
            if (startPanel && rules.State != FootballState.Start)
                startPanel.SetActive(false);
            if (directionSlider) directionSlider.SetValueWithoutNotify(rules.AimX);
            if (directionLabel) directionLabel.text = Mathf.Abs(rules.AimX) < .02f ? "GIỮA" :
                (rules.AimX < 0f ? "TRÁI " : "PHẢI ") + Mathf.RoundToInt(Mathf.Abs(rules.AimX) * 100f) + "%";
            if (shotFeedback) shotFeedback.text = OutcomeText(rules.Flight?.Outcome);
        }

        static string OutcomeText(FootballOutcome? outcome) => outcome switch
        {
            FootballOutcome.Goal => "VÀO!",
            FootballOutcome.Saved => "THỦ MÔN CẢN PHÁ",
            FootballOutcome.Post => "TRÚNG CỘT DỌC",
            FootballOutcome.Crossbar => "TRÚNG XÀ NGANG",
            FootballOutcome.Wide => "CHỆCH KHUNG THÀNH",
            FootballOutcome.High => "BÓNG VƯỢT XÀ",
            FootballOutcome.Short => "BÓNG DỪNG TRƯỚC GOAL",
            _ => string.Empty
        };

        void OnEnable() => BindButtons();
        void OnDisable()
        {
            if (!listenersBound) return;
            if (startButton) startButton.onClick.RemoveListener(RequestStart);
            if (easyButton) easyButton.onClick.RemoveListener(SelectEasy);
            if (normalButton) normalButton.onClick.RemoveListener(SelectNormal);
            if (hardButton) hardButton.onClick.RemoveListener(SelectHard);
            listenersBound = false;
        }

        void BindButtons()
        {
            if (listenersBound) return;
            if (startButton) startButton.onClick.AddListener(RequestStart);
            if (easyButton) easyButton.onClick.AddListener(SelectEasy);
            if (normalButton) normalButton.onClick.AddListener(SelectNormal);
            if (hardButton) hardButton.onClick.AddListener(SelectHard);
            listenersBound = startButton || easyButton || normalButton || hardButton;
        }

        void SelectEasy() => SetDifficulty(FootballDifficulty.Easy);
        void SelectNormal() => SetDifficulty(FootballDifficulty.Normal);
        void SelectHard() => SetDifficulty(FootballDifficulty.Hard);

        void UpdateDifficultyButtons()
        {
            SetDifficultyVariant(easyButton, selectedDifficulty == FootballDifficulty.Easy);
            SetDifficultyVariant(normalButton, selectedDifficulty == FootballDifficulty.Normal);
            SetDifficultyVariant(hardButton, selectedDifficulty == FootballDifficulty.Hard);
        }

        static void SetDifficultyVariant(Button button, bool selected)
        {
            if (button && button.GetComponent<KitPressFeedback>())
                UiKit.ApplyVariant(UiKit.ButtonParts(button), selected ? ButtonVariant.Primary : ButtonVariant.Secondary);
        }
    }
}
```

In `Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`:
- change `[SerializeField] FootballResultPanel resultPanel;` to `[SerializeField] ResultPanel resultPanel;` (the file already has `using KMA.Gameplay.UI;`);
- change the last `Configure` parameter from `FootballResultPanel result` to `ResultPanel result`;
- in `BeginMatch`, delete the line `hud.ShowCountdown("SẴN SÀNG!");`;
- in `TickPlay`, delete the line `hud.HideCountdown();` and change `resultPanel.SetGoals(rules.Goals);` to `resultPanel.SetDetail($"{rules.Goals}/5 BÀN");`;
- change `ValidateReferences` to require retry support:

```csharp
        public bool ValidateReferences() => difficultyConfig && inputBridge && presentation && hud && resultPanel &&
            hud.ValidateReferences() && presentation.ValidateReferences() && resultPanel.ValidateReferences() &&
            resultPanel.SupportsRetry && hud.DirectionSlider && hud.ShootButton;
```

Delete the replaced files:

```bash
git rm Assets/_Project/Scripts/Gameplay/Football/FootballResultPanel.cs Assets/_Project/Scripts/Gameplay/Football/FootballResultPanel.cs.meta \
       Assets/_Project/Scripts/Gameplay/Common/MinigamePresentationOwner.cs Assets/_Project/Scripts/Gameplay/Common/MinigamePresentationOwner.cs.meta \
       Assets/Tests/PlayMode/Gameplay/Football/FootballResultPanelTests.cs Assets/Tests/PlayMode/Gameplay/Football/FootballResultPanelTests.cs.meta
```

In `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs`, delete the `if (FindInScene<MinigamePresentationOwner>(scene) != null) { … return; }` block in `AssembleScene`.

- [ ] **Step 3: Rebuild the Football configurator on the shared screens**

In `Assets/Editor/FootballSceneConfigurator.cs`:
- add `using System.Linq;` and `using KMA.UI.Kit;`;
- delete `FontPath`, `Ink`, `Grass`, `Gold` and the helpers `Rect`, `Anchor`, `Panel`, `CreateImage`, `Button`, `Text`, `Style`, `FindText`, `CreateDirectionSlider`;
- add constants `const string HudRootName = "S2_HUD_Minigame";` and `const int HudSortingOrder = 500;`;
- in `ImportGoalViewArt`, replace the two `panel` special cases with plain values: `importer.spritePixelsPerUnit = 200f;` and `settings.spriteBorder = Vector4.zero;`;
- replace `BuildScene` and `BuildUi` with:

```csharp
        [MenuItem("KMA/Football/Build Scene")]
        public static void BuildScene()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("MG_Football scene is missing; refusing to create a new scene identity.", ScenePath);
            ImportGoalViewArt();
            ToonCharacterArt.ImportAll();
            EnsureConfiguration();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
            BuildWorld(scene);
            BuildController(scene);
            EnsureEventSystem(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // The shared HUD, phase overlay, result panel and pause menu come from the assembler.
            MinigameUIAssembler.AssembleScenePath(ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RestoreSky(scene);
            BuildUi(scene);
            EnsureEventSystem(scene);

            ConfigureSubjectAsset();
            EnsureInBuildSettings();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMA] MG_Football penalty scene built and saved.");
        }

        static void BuildController(Scene scene)
        {
            var input = new GameObject("FootballInputBridge").AddComponent<FootballInputBridge>();
            SceneManager.MoveGameObjectToScene(input.gameObject, scene);
            input.gameObject.AddComponent<FootballController>();
        }

        // The assembler paints every gameplay camera the brutalist blue; Football keeps its sky.
        static void RestoreSky(Scene scene) =>
            scene.GetRootGameObjects().Single(go => go.name == "GameCamera").GetComponent<Camera>().backgroundColor = Sky;

        static void BuildUi(Scene scene)
        {
            GameObject hudRoot = scene.GetRootGameObjects().Single(go => go.name == HudRootName);
            hudRoot.GetComponent<Canvas>().sortingOrder = HudSortingOrder;
            var safe = (RectTransform)hudRoot.transform.Find("SafeAreaRoot");
            // The generic HUD widgets belong to other sports; hide only the prefab's children.
            foreach (Transform child in safe)
                child.gameObject.SetActive(false);

            RectTransform root = UiKit.Rect(safe, "FootballControls");
            UiKit.Stretch(root);
            root.SetAsFirstSibling();
            var hud = root.gameObject.AddComponent<FootballHud>();
            Vector2 centre = new Vector2(.5f, .5f);
            float edge = MinigameUiTheme.SpaceMd;

            // Top: score, shots left and one dot per kick.
            ChipHandle score = UiKit.Chip(root, "ScoreChip", "BÀN: 0");
            score.Label.name = "ScoreLabel";
            UiKit.StyleLabel(score.Label, MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            UiKit.Place(score.Background.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -edge),
                new Vector2(340f, 84f));

            ChipHandle remaining = UiKit.Chip(root, "RemainingChip", "CÒN 5 LƯỢT");
            remaining.Label.name = "RemainingLabel";
            UiKit.Anchor(remaining.Label.rectTransform, new Vector2(0f, .45f), new Vector2(1f, 1f));
            UiKit.Place(remaining.Background.rectTransform, Vector2.one, Vector2.one,
                new Vector2(-(MinigameUiTheme.ButtonHeight + edge * 2f), -edge), new Vector2(360f, 120f));
            var markers = new Image[5];
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = UiKit.Disc(remaining.Background.transform, "KickMarker" + (i + 1), false, MinigameUiTheme.Track);
                UiKit.Place(markers[i].rectTransform, new Vector2(.14f + i * .18f, .25f), centre, Vector2.zero, Vector2.one * 28f);
            }

            // Bottom left: aim.
            Image aim = UiKit.Panel(root, "DirectionPanel");
            UiKit.Anchor(aim.rectTransform, new Vector2(.025f, .025f), new Vector2(.395f, .235f));
            TMP_Text aimTitle = UiKit.Label(aim.transform, "DirectionTitle", "HƯỚNG BÓNG", MinigameUiTheme.Caption,
                MinigameUiTheme.TextPrimary, TextAlignmentOptions.Left);
            UiKit.Anchor(aimTitle.rectTransform, new Vector2(.05f, .70f), new Vector2(.58f, .95f));
            TMP_Text directionValue = UiKit.Label(aim.transform, "DirectionValue", "PHẢI 55%", MinigameUiTheme.Body,
                MinigameUiTheme.Accent, TextAlignmentOptions.Right);
            UiKit.Anchor(directionValue.rectTransform, new Vector2(.58f, .70f), new Vector2(.95f, .95f));
            SliderHandle direction = UiKit.Slider(aim.transform, "DirectionSlider");
            UiKit.Anchor((RectTransform)direction.Slider.transform, new Vector2(.06f, .34f), new Vector2(.94f, .70f));
            direction.Slider.minValue = -1f;
            direction.Slider.maxValue = 1f;
            direction.Slider.value = .55f;
            TMP_Text hint = UiKit.Label(aim.transform, "DirectionHint", "Kéo chọn hướng · Giữ SÚT để xem đường bay",
                MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary);
            UiKit.Anchor(hint.rectTransform, new Vector2(.04f, .04f), new Vector2(.96f, .34f));
            UiKit.FitLabel(hint, MinigameUiTheme.Caption);

            // Bottom right: power and shoot.
            KitBar power = UiKit.Bar(root, "PowerBar", label: true);
            UiKit.Anchor((RectTransform)power.transform, new Vector2(.80f, .16f), new Vector2(.975f, .20f));
            power.Label.name = "PowerPercent";
            TMP_Text warning = UiKit.Label(root, "OverPowerWarning", "DỄ VƯỢT XÀ", MinigameUiTheme.Caption,
                MinigameUiTheme.Energy, TextAlignmentOptions.Center, outline: true);
            UiKit.Anchor(warning.rectTransform, new Vector2(.79f, .205f), new Vector2(.985f, .25f));
            warning.gameObject.SetActive(false);
            ButtonHandle shoot = UiKit.Button(root, "SHOOT", "GIỮ ĐỂ SÚT", ButtonVariant.Primary);
            UiKit.Anchor((RectTransform)shoot.Button.transform, new Vector2(.80f, .035f), new Vector2(.975f, .145f));
            var hold = shoot.Button.gameObject.AddComponent<FootballHoldButton>();
            TMP_Text feedback = UiKit.Label(root, "ShotFeedback", string.Empty, MinigameUiTheme.Headline,
                MinigameUiTheme.TextPrimary, TextAlignmentOptions.Center, outline: true);
            UiKit.Anchor(feedback.rectTransform, new Vector2(.20f, .69f), new Vector2(.80f, .79f));

            // Start screen with the difficulty picker.
            Image start = UiKit.Panel(root, "StartPanel");
            start.raycastTarget = true;
            UiKit.Place(start.rectTransform, centre, centre, Vector2.zero, new Vector2(850f, 585f));
            TMP_Text startTitle = UiKit.Label(start.transform, "StartTitle", "LOẠT SÚT LUÂN LƯU", MinigameUiTheme.Title,
                MinigameUiTheme.TextPrimary);
            UiKit.Anchor(startTitle.rectTransform, new Vector2(.06f, .76f), new Vector2(.94f, .94f));
            TMP_Text instructions = UiKit.Label(start.transform, "StartInstructions",
                "Kéo thanh chọn hướng. Giữ SÚT để xem đường bay, thả để đá.\nGhi ít nhất 3 bàn sau 5 lượt.",
                MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            UiKit.Anchor(instructions.rectTransform, new Vector2(.08f, .55f), new Vector2(.92f, .77f));
            UiKit.FitLabel(instructions, MinigameUiTheme.Body);
            TMP_Text difficultyTitle = UiKit.Label(start.transform, "DifficultyTitle", "ĐỘ KHÓ", MinigameUiTheme.Caption,
                MinigameUiTheme.Accent);
            UiKit.Anchor(difficultyTitle.rectTransform, new Vector2(.1f, .45f), new Vector2(.9f, .55f));
            Button easy = StartButton(start.transform, "Easy", "DỄ", ButtonVariant.Secondary, .08f, .34f, .29f, .44f);
            Button normal = StartButton(start.transform, "Normal", "THƯỜNG", ButtonVariant.Primary, .35f, .65f, .29f, .44f);
            Button hard = StartButton(start.transform, "Hard", "KHÓ", ButtonVariant.Secondary, .66f, .92f, .29f, .44f);
            Button startButton = StartButton(start.transform, "StartButton", "BẮT ĐẦU", ButtonVariant.Primary, .29f, .71f, .06f, .22f);

            // The shared pause button sits in the safe area's top-right corner, as in Volleyball.
            var pause = UnityEngine.Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            pause.transform.SetParent(safe, false);
            UiKit.Place((RectTransform)pause.transform, Vector2.one, Vector2.one, new Vector2(-edge, -edge),
                Vector2.one * MinigameUiTheme.ButtonHeight);

            hud.Configure(direction.Slider, hold, power, warning.gameObject, score.Label, remaining.Label, markers,
                start.gameObject, startButton, easy, normal, hard, directionValue, feedback);
            hud.ShowStart(FootballDifficulty.Normal);

            var result = UnityEngine.Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            var controller = UnityEngine.Object.FindFirstObjectByType<FootballController>();
            var presentation = GameObject.Find("FootballWorld").GetComponent<FootballPresentation>();
            var config = AssetDatabase.LoadAssetAtPath<FootballDifficultyConfig>(ConfigPath);
            controller.Configure(config, controller.GetComponent<FootballInputBridge>(), presentation, hud, result);

            foreach (UnityEngine.Object dirty in new UnityEngine.Object[] { hud, controller, hudRoot, pause })
                EditorUtility.SetDirty(dirty);
        }

        static Button StartButton(Transform parent, string name, string label, ButtonVariant variant,
            float minX, float maxX, float minY, float maxY)
        {
            ButtonHandle handle = UiKit.Button(parent, name, label, variant);
            UiKit.Anchor((RectTransform)handle.Button.transform, new Vector2(minX, minY), new Vector2(maxX, maxY));
            return handle.Button;
        }
```

Keep `BuildWorld`, `LoadPoses`, `EnsureConfiguration`, `ConfigureSubjectAsset`, `EnsureInBuildSettings`, `EnsureEventSystem`, `AddUrpCameraData`, `Renderer` and `LoadSprite` unchanged.

- [ ] **Step 4: Delete the unused Football UI sprites**

```bash
git rm Assets/_Project/Art/Football/GoalView/panel.png Assets/_Project/Art/Football/GoalView/panel.png.meta \
       Assets/_Project/Art/Football/GoalView/panel.svg Assets/_Project/Art/Football/GoalView/panel.svg.meta \
       Assets/_Project/Art/Football/GoalView/fill.png Assets/_Project/Art/Football/GoalView/fill.png.meta \
       Assets/_Project/Art/Football/GoalView/fill.svg Assets/_Project/Art/Football/GoalView/fill.svg.meta \
       Assets/_Project/Art/Football/UI/button.png Assets/_Project/Art/Football/UI/button.png.meta \
       Assets/_Project/Art/Football/UI/panel.png Assets/_Project/Art/Football/UI/panel.png.meta \
       Assets/_Project/Art/Football/UI/power-fill.png Assets/_Project/Art/Football/UI/power-fill.png.meta
sed -i '/^export("panel"/d; /^export("fill"/d' tools/export-football-preview-art.py
grep -n "panel\|fill\.png" Assets/_Project/Art/Football/GoalView/README.md
grep -rn "GoalView/panel\|GoalView/fill\|Football/UI/button\|Football/UI/panel\|power-fill" Assets --include=*.cs --include=*.unity --include=*.prefab
```

If the README grep prints lines that list `panel` or `fill` as exported files, delete those lines from the README. The last grep must print nothing.

- [ ] **Step 5: Update the Football PlayMode fixtures**

In `Assets/Tests/PlayMode/Gameplay/Football/KMA.Gameplay.Football.PlayMode.Tests.asmdef`, add `"KMA.Gameplay.UI"` to `references`.

In `Assets/Tests/PlayMode/Gameplay/Football/FootballControllerTests.cs` (add `using KMA.Gameplay.UI;` and `using KMA.UI.Kit;`):
- change `public readonly FootballResultPanel resultPanel;` to `public readonly ResultPanel resultPanel;`;
- replace the power fill and markers setup and the `hud.Configure(...)` call with:

```csharp
                var hud = root.AddComponent<FootballHud>();
                KitBar power = UiKit.Bar(root.transform, "Power", label: true);
                TMP_Text Text(string name)
                {
                    var text = new GameObject(name).AddComponent<TextMeshPro>();
                    text.transform.SetParent(root.transform);
                    return text;
                }
                var markers = new Image[5];
                for (int i = 0; i < markers.Length; i++)
                {
                    markers[i] = new GameObject("Kick" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    markers[i].transform.SetParent(root.transform);
                }
                var warning = new GameObject("Warning", typeof(RectTransform)); warning.transform.SetParent(root.transform);
                var start = new GameObject("Start", typeof(RectTransform)); start.transform.SetParent(root.transform);
                score = Text("Score");
                remaining = Text("Remaining");
                hud.Configure(aim, shoot, power, warning, score, remaining, markers, start);
```

- change `resultPanel = root.AddComponent<FootballResultPanel>();` to `resultPanel = root.AddComponent<ResultPanel>();` (the `Configure` call keeps its arguments; its third argument is now the detail label).

In `Assets/Tests/PlayMode/Gameplay/Football/FootballPresentationTests.cs` (add `using KMA.UI.Kit;`), in `HudShowsDirectionPowerAndOutcomeWithoutOverridingInputOwnership`, replace the `power`, `percent` and `markers` setup and the `hud.Configure` call with:

```csharp
            KitBar power=UiKit.Bar(root.transform,"Power",label:true);var percent=power.Label;
            var score=Make<TextMeshProUGUI>("Score");var remaining=Make<TextMeshProUGUI>("Remaining");
            var warning=Make<RectTransform>("Warning").gameObject;var start=Make<RectTransform>("Start").gameObject;
            var markers=new Image[5];for(int i=0;i<5;i++)markers[i]=Make<Image>("Marker"+i);
            var hud=root.AddComponent<FootballHud>();hud.Configure(slider,shoot,power,warning,score,remaining,markers,start);
```

and replace `Assert.That(markers[0].text,Is.EqualTo("×"));` with `Assert.That(markers[0].color,Is.EqualTo(MinigameUiTheme.Energy));`. Also assert the over-power colour right after the `percent` assertion: `Assert.That(power.Fill.color,Is.EqualTo(MinigameUiTheme.Energy));`.

In `Assets/Tests/PlayMode/Gameplay/Football/FootballSceneTests.cs`, in `SavedSceneContainsPlayableSourcedFootballPresentation`, delete the `owners` variable, its `+=` line and `Assert.That(owners, Is.EqualTo(1));`.

In `Assets/Tests/PlayMode/Progression/FootballResultRoutingTests.cs`: add `using KMA.Gameplay.UI;` and replace every `FootballResultPanel` with `ResultPanel`.

- [ ] **Step 6: Rebuild the Football scene**

Run an editor method: `KMA.EditorTools.FootballSceneConfigurator.BuildScene`, log `build-football`.
Expected: `exit=0` and `[KMA] MG_Football penalty scene built and saved.` in the log.

- [ ] **Step 7: Run the Football tests**

Run tests: `EditMode` `KMA.Tests.EditorTools.FootballSceneConfiguratorTests;KMA.Tests.EditorTools.KeeperSilhouetteTests;KMA.Tests.EditorTools.HeroConsistencyTests` `football-kit`
Expected: `result="Passed"`, `failed="0"`.

Run tests: `PlayMode` `KMA.Tests.Gameplay.Football;KMA.Tests.Gameplay.Progression.FootballResultRoutingTests;KMA.Tests.Gameplay.Core.PauseFlowTests` `football-kit-play`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 8: Check the pause flow in the Football scene (Review Focus 2 of the spec's risk)**

Add to `Assets/Tests/PlayMode/Gameplay/Football/FootballSceneTests.cs`:

```csharp
        [UnityTest]
        public IEnumerator SavedScenePausesTheMatchAndResumesWithTheSharedPauseMenu()
        {
            var operation = SceneManager.LoadSceneAsync("MG_Football", LoadSceneMode.Single);
            yield return operation;
            yield return null;

            var controller = Object.FindFirstObjectByType<FootballController>();
            var pause = Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>();
            Assert.That(pause, Is.Not.Null);
            Assert.That(controller.BeginMatch(FootballDifficulty.Normal), Is.True);

            pause.Open();
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
            yield return null;
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown), "paused: the countdown does not run");

            pause.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return new WaitForSeconds(3.5f);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
        }
```

(Add `using KMA.Gameplay;` if the file lacks it.)

Run tests: `PlayMode` `KMA.Tests.Gameplay.Football.FootballSceneTests` `football-pause`
Expected: `result="Passed"`, `failed="0"`.

- [ ] **Step 9: Commit**

```bash
git add -A Assets/Editor/FootballSceneConfigurator.cs Assets/_Project/Scripts/Gameplay/Football Assets/_Project/Scripts/Gameplay/Common \
  Assets/_Project/Scripts/UI/MinigameUIAssembler.cs Assets/_Project/Scenes/MG_Football.unity Assets/_Project/Art/Football \
  tools/export-football-preview-art.py Assets/Tests/EditMode/EditorTools/FootballSceneConfiguratorTests.cs \
  Assets/Tests/PlayMode/Gameplay/Football Assets/Tests/PlayMode/Progression/FootballResultRoutingTests.cs
git commit -m "feat(football): move onto the shared HUD, countdown, pause and result screens"
```

---

### Task 10: Style guard across all three games, then the full suites

**Files:**
- Create: `Assets/_Project/Scripts/UI/Kit/MinigameStyleAudit.cs`
- Test: `Assets/Tests/EditMode/EditorTools/MinigameStyleConsistencyTests.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs` (one audit test)

**Interfaces:**
- Consumes: `UiKitAssets`, `MinigameUiTheme`.
- Produces: `KMA.UI.Kit.MinigameStyleAudit.Audit(GameObject root)` → `List<string>` of problems (empty when compliant).

- [ ] **Step 1: Write the failing guard tests**

`Assets/Tests/EditMode/EditorTools/MinigameStyleConsistencyTests.cs`:

```csharp
#if UNITY_EDITOR
using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class MinigameStyleConsistencyTests
    {
        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TestCase(MinigamePrefabStyler.HudPrefab)]
        [TestCase(MinigamePrefabStyler.PhasePrefab)]
        [TestCase(MinigamePrefabStyler.ResultPrefab)]
        public void SharedPrefabsFollowTheKit(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var problems = MinigameStyleAudit.Audit(root);
                Assert.That(problems, Is.Empty, string.Join("\n", problems));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase(FootballSceneConfigurator.ScenePath)]
        [TestCase(VolleyballSceneConfigurator.ScenePath)]
        public void MinigameHudFollowsTheKit(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject hud = scene.GetRootGameObjects().Single(go => go.name == "S2_HUD_Minigame");
            var problems = MinigameStyleAudit.Audit(hud);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void PunishmentPauseButtonFollowsTheKit()
        {
            EditorSceneManager.OpenScene(MinigamePrefabStyler.PunishmentScene, OpenSceneMode.Single);
            var pause = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            var problems = MinigameStyleAudit.Audit(pause.gameObject);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void AuditCatchesOffKitSpritesColoursFontsAndLegacyText()
        {
            var root = new GameObject("Offender", typeof(RectTransform));
            try
            {
                var image = new GameObject("Knob", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                image.transform.SetParent(root.transform, false);
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                image.color = new Color32(255, 152, 0, 255);
                var small = new GameObject("Small", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                small.transform.SetParent(root.transform, false);
                small.fontSize = 18f;
                new GameObject("Legacy", typeof(RectTransform), typeof(UnityEngine.UI.Text)).transform.SetParent(root.transform, false);

                var problems = MinigameStyleAudit.Audit(root);
                Assert.That(problems.Any(p => p.Contains("Knob") && p.Contains("sprite")), Is.True, string.Join("\n", problems));
                Assert.That(problems.Any(p => p.Contains("colour")), Is.True);
                Assert.That(problems.Any(p => p.Contains("Small") && p.Contains("size")), Is.True);
                Assert.That(problems.Any(p => p.Contains("legacy")), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
```

Add to `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`:

```csharp
        [UnityTest]
        public IEnumerator SprintHudFollowsTheKit()
        {
            yield return LoadSprint();
            yield return null;
            GameObject hud = GameObject.Find("S2_HUD_Minigame");
            var problems = MinigameStyleAudit.Audit(hud);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
```

Run tests: `EditMode` `KMA.Tests.EditorTools.MinigameStyleConsistencyTests` `style-guard-red`
Expected: no XML; `error CS0103` for `MinigameStyleAudit`.

- [ ] **Step 2: Create the audit**

`Assets/_Project/Scripts/UI/Kit/MinigameStyleAudit.cs`:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    /// The rules every minigame UI tree must follow, shared by EditMode and PlayMode guard tests.
    public static class MinigameStyleAudit
    {
        public static List<string> Audit(GameObject root)
        {
            var problems = new List<string>();
            UiKitAssets assets = UiKitAssets.Load();
            var kitSprites = new HashSet<Sprite>(assets.AllSprites());

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (IsExempt(image))
                    continue;
                string path = PathOf(image.transform);
                bool spriteOk = image.sprite == null ? IsScrim(image.color) : kitSprites.Contains(image.sprite);
                if (!spriteOk)
                    problems.Add($"{path}: sprite {(image.sprite == null ? "none" : image.sprite.name)} is not a UI kit sprite");
                if (!IsToken(image.color))
                    problems.Add($"{path}: colour #{ColorUtility.ToHtmlStringRGBA(image.color)} is not a theme token");
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string path = PathOf(text.transform);
                if (text.font != assets.Font)
                    problems.Add($"{path}: font {(text.font == null ? "none" : text.font.name)} is not the kit font");
                float smallest = text.enableAutoSizing ? text.fontSizeMin : text.fontSize;
                if (smallest < MinigameUiTheme.MinimumFontSize)
                    problems.Add($"{path}: text size {smallest} is below {MinigameUiTheme.MinimumFontSize}");
                if (!IsToken(text.color))
                    problems.Add($"{path}: text colour #{ColorUtility.ToHtmlStringRGBA(text.color)} is not a theme token");
            }

            foreach (Text legacy in root.GetComponentsInChildren<Text>(true))
                problems.Add($"{PathOf(legacy.transform)}: legacy UnityEngine.UI.Text");

            return problems;
        }

        static bool IsExempt(Image image) =>
            image.color.a < .01f || image.name == "Icon" || HasAncestor(image.transform, "FinishLine");

        static bool IsScrim(Color color) => MinigameUiTheme.RgbEquals(color, MinigameUiTheme.Scrim);

        static bool IsToken(Color color) =>
            Array.Exists(MinigameUiTheme.Palette(), token => MinigameUiTheme.RgbEquals(token, color));

        static bool HasAncestor(Transform node, string name)
        {
            for (Transform current = node; current != null; current = current.parent)
                if (current.name == name)
                    return true;
            return false;
        }

        static string PathOf(Transform node)
        {
            string path = node.name;
            for (Transform current = node.parent; current != null; current = current.parent)
                path = current.name + "/" + path;
            return path;
        }
    }
}
```

- [ ] **Step 3: Run the guard tests and fix what they find**

Run tests: `EditMode` `KMA.Tests.EditorTools.MinigameStyleConsistencyTests` `style-guard`
Run tests: `PlayMode` `KMA.Tests.Presentation.SprintPresentationGateTests.SprintHudFollowsTheKit` `style-guard-sprint`
Expected: `result="Passed"`, `failed="0"`.

If a test lists problems, each line names the node path and the rule it breaks. Fix the builder that creates that node (the styler for prefab nodes, the configurator for scene nodes, `SprintFestivalPresentation` for Sprint) so the node uses a kit sprite, a token colour, the kit font and a size of at least 24. Do not add exemptions to the audit. Then re-run the builder (Task 6 Step 8, Task 8 Step 4 or Task 9 Step 6) and the tests.

- [ ] **Step 4: Run both full suites**

Run the test command from Global Constraints **without** the `-testFilter "<filter>"` argument, once with `-testPlatform EditMode` (name `full-editmode`) and once with `-testPlatform PlayMode` (name `full-playmode`).
Expected: both `result="Passed"`, `failed="0"`. If a test outside this plan's files fails, run the same filter once on the commit before Task 1 (`git stash` is not needed; use `git worktree add ../kma-base <sha-before-task-1>` and run there). If it also fails there, report it as pre-existing; otherwise fix it here.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/UI/Kit/MinigameStyleAudit.cs* Assets/Tests/EditMode/EditorTools/MinigameStyleConsistencyTests.cs* \
  Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
git commit -m "test(ui): guard that every minigame graphic uses the shared kit"
```

---

### Task 11: Visual check with before/after screenshots

**Files:**
- Modify: `Assets/Editor/PlayModeScreenshot.cs` (QA seam `openPause`)
- Modify: `tools/qa-screenshot.sh` (7th argument `openPause`)
- Create: `Builds/Screenshots/ui-kit/*.png` (not committed; `Builds/` is local output)

**Interfaces:**
- Consumes: the running Editor's screenshot service (`testing-unity-ui-with-screenshots` skill).
- Produces: a side-by-side review of the three games in play, countdown, pause and result, plus Football's difficulty screen.

- [ ] **Step 1: Add a pause seam to the screenshot service**

In `Assets/Editor/PlayModeScreenshot.cs`, add `public bool openPause;` to the request class after `forceSprintResult`, add `const string KeyOpenPause = "KMA_PMS_OpenPause";` beside the other keys, store it where `KeyForceSprintResult` is stored (`SessionState.SetBool(KeyOpenPause, req.openPause);`), and after the `forceResult` block add:

```csharp
            if (SessionState.GetBool(KeyOpenPause, false))
                UnityEngine.Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>()?.Open();
```

In `tools/qa-screenshot.sh`, add `OPEN_PAUSE="${7:-false}"`, validate it like `HOLD_SPRINT_TUTORIAL` (`true|false`), and append `,"openPause":$OPEN_PAUSE` to the JSON. Update the usage comment to list `[openPause]`.

- [ ] **Step 2: Capture the "before" set from the commit before Task 1**

Follow the `testing-unity-ui-with-screenshots` skill (one background Editor, reuse it). Check out the commit before Task 1 in a separate worktree (`git worktree add ../kma-before <sha-before-task-1>`), open that worktree in the Editor, and capture into `Builds/Screenshots/ui-kit/before/`:

```bash
S=Assets/_Project/Scenes
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/before/sprint-play.png $S/MG_Sprint.unity 6
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/before/sprint-result.png $S/MG_Sprint.unity 3 false -1 pass
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/before/football-start.png $S/MG_Football.unity 2
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/before/volleyball-countdown.png $S/MG_Volleyball.unity 1
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/before/volleyball-play.png $S/MG_Volleyball.unity 6
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/before/volleyball-result.png $S/MG_Volleyball.unity 3 false -1 fail
```

(The "before" tree has no `openPause` seam; pause screenshots exist only in the "after" set.)

- [ ] **Step 3: Capture the "after" set on `master`**

Reopen the main project in the Editor and capture into `Builds/Screenshots/ui-kit/after/`:

```bash
S=Assets/_Project/Scenes
for game in Sprint Volleyball; do
  g=$(echo $game | tr A-Z a-z)
  tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/$g-play.png $S/MG_$game.unity 6
  tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/$g-pause.png $S/MG_$game.unity 6 false -1 "" true
  tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/$g-result.png $S/MG_$game.unity 3 false -1 fail
done
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/volleyball-countdown.png $S/MG_Volleyball.unity 1
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/football-start.png $S/MG_Football.unity 2
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/football-pause.png $S/MG_Football.unity 2 false -1 "" true
tools/qa-screenshot.sh Builds/Screenshots/ui-kit/after/football-result.png $S/MG_Football.unity 2 false -1 fail
```

- [ ] **Step 4: Look at every screenshot**

Read each PNG with the Read tool. For each one, check:
- every panel, chip and button is navy or yellow with rounded corners, with no white brutalist cards, black hard shadows or stretched ellipses;
- no text is clipped or overlaps another element; Vietnamese diacritics render;
- the pause button sits in the same top-right spot in all three games;
- the result card, pause card and Football start card read as the same family.

Fix any layout problem at its builder (styler, configurator or `SprintFestivalPresentation`). Adjust only positions and sizes; keep tokens unchanged. Re-run the builder, the Task 10 guard tests and the affected screenshot.

- [ ] **Step 5: Commit the QA seam**

```bash
git add Assets/Editor/PlayModeScreenshot.cs tools/qa-screenshot.sh
git commit -m "chore(qa): screenshot seam that opens the pause menu"
```

Report the before/after screenshot paths to your human partner. Include them side by side, one game per row.
