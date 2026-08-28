# S2 — Presentation Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây lớp presentation uGUI/TMP dùng chung, camera landscape an toàn, tutorial nhiều bước và HUD kéo dữ liệu từ minigame để `MG_Sprint` mở lên có tutorial → countdown → HUD timer/stamina và phản hồi Left/Right nhìn thấy được.

**Architecture:** Giữ rules engine và các controller gameplay hiện có làm nguồn sự thật; lớp UI chỉ đọc `MinigameHudState` qua `MinigameBase.BuildHudState()`, không gọi ngược vào rules. HUD được đặt trong từng scene vì `SceneRouter` hiện load scene bằng `LoadSceneMode.Single`; mỗi scene sở hữu `HUD_Minigame`, `TutorialOverlay`, `PhaseOverlay` và các component presentation riêng. UI dùng uGUI/TMP, một `UITheme` ScriptableObject làm nguồn màu/typography/spacing, và `GameCamera` prefab dùng URP 2D camera data.

**Tech Stack:** Unity `6000.3.23f1`, URP `17.3.0`, `com.unity.ugui` `2.0.0` (TextMeshPro), Input System `1.20.0`, NUnit/Unity Test Framework `1.6.0`, Android landscape 1920×1080 reference canvas.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §5 S2, các quyết định S2-1/S2-2, và `PLAN.md` §2–§3.

## Global Constraints

- Không sửa rules engine đã có test; chỉ thêm event, method virtual, adapter hoặc component presentation additive.
- Không đổi chữ ký hoặc hành vi đã được test của `SceneRouter`, `GameSession`, `SprintRules`, `EnduranceRules` và các controller hiện có.
- `MinigameBase.BuildHudState()` là pull contract; controller không giữ reference bắt buộc tới HUD.
- `MinigameHudState` là struct immutable về mặt sử dụng, gồm đúng: `timeRemaining`, `primary01`, `primaryLabel`, `secondary01`, `secondaryLabel`, `statusText`.
- HUD chung chỉ hiển thị timer, tiến độ mục tiêu, tim, pause, phase/tutorial/countdown/result; HUD đặc thù môn để cho section S6–S14.
- Canvas Scaler: `Scale With Screen Size`, reference `1920×1080`, `matchWidthOrHeight = 1.0`.
- `SafeAreaFitter` phải tính cả cạnh trái và phải cho notch/thanh gesture ở landscape.
- `GameCamera` là orthographic, camera data URP 2D, kích thước camera cố định theo chiều cao; không thêm realtime light hoặc post-process.
- Font tiếng Việt phải được xử lý ngay trong S2: Latin cơ bản, `1EA0–1EF9`, `0110/0111`, `01A0–01B0`, toàn bộ ký tự xuất hiện trong text game, dynamic fallback bật.
- Nút/brutalist visual contract: pointer-down dịch `(+4,-4)`, shadow về `0`, tween `0.1s`; card viền `4`, shadow `(+6,-6)`, bo góc `24` qua sprite 9-slice.
- Không dùng `OnGUI` làm presentation sản phẩm; `GameplayPresentation` cũ chỉ giữ làm compatibility shim cho test/scene bootstrap cho đến khi scene đã có uGUI presentation.
- Mỗi task phải chạy test liên quan và commit riêng; sau mỗi task chạy lại full EditMode và PlayMode.

## File Structure

| File | Trách nhiệm |
|---|---|
| `Assets/_Project/Scripts/UI/KMA.Gameplay.UI.asmdef` | Assembly UI tham chiếu `KMA.Gameplay`, `KMA.Gameplay.Core`, `KMA.Gameplay.Progression`, Unity UI và TMP |
| `Assets/_Project/Scripts/UI/MinigameHudState.cs` | Struct ViewModel mà `MinigameBase` trả về |
| `Assets/_Project/Scripts/UI/UITheme.cs` | `UITheme : ScriptableObject`, palette/token và asset mặc định |
| `Assets/_Project/Scripts/UI/BrutalButton.cs` | Pointer animation, shadow offset và click/SFX hook |
| `Assets/_Project/Scripts/UI/SafeAreaFitter.cs` | Insets trái/phải/trên/dưới từ `Screen.safeArea` |
| `Assets/_Project/Scripts/UI/ScreenBase.cs` | Base class cho screen/panel mở-đóng và theme binding |
| `Assets/_Project/Scripts/UI/HeartBar.cs` | Render số tim hiện tại từ session ViewModel |
| `Assets/_Project/Scripts/UI/FloatingTextPool.cs` | Pool floating text, không instantiate trong `Update` |
| `Assets/_Project/Scripts/UI/MinigameHUD.cs` | Pull `MinigameHudState` và render timer/progress/status |
| `Assets/_Project/Scripts/UI/PhaseOverlay.cs` | Tutorial/countdown/phase/result transition presentation |
| `Assets/_Project/Scripts/UI/ResultPanel.cs` | Pass/fail, score/rank, preview route và action tiếp tục |
| `Assets/_Project/Scripts/UI/TutorialOverlay.cs` | Multi-step tutorial, next/back/skip, per-subject seen store |
| `Assets/_Project/Scripts/UI/TutorialSeenStore.cs` | Narrow persistence interface + PlayerPrefs adapter cho S2; S4 có thể thay backend |
| `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs` | Editor-only helper tạo và bind hierarchy prefab/scene, không chứa runtime rules |
| `Assets/_Project/Prefabs/UI/HUD_Minigame.prefab` | Canvas, scaler, safe-area root, timer, progress, labels, HeartBar, pause |
| `Assets/_Project/Prefabs/UI/PhaseOverlay.prefab` | Tutorial/countdown/phase overlay |
| `Assets/_Project/Prefabs/UI/ResultPanel.prefab` | Result card và action buttons |
| `Assets/_Project/Prefabs/UI/Btn_Brutal.prefab` | Button visual chuẩn |
| `Assets/_Project/Prefabs/Gameplay/GameCamera.prefab` | Orthographic camera + `UniversalAdditionalCameraData` |
| `Assets/_Project/Settings/UI/UITheme.asset` | Palette `UITheme` dùng trong toàn S2 |
| `Assets/_Project/Fonts/*.asset` | TMP font assets/fallback assets có glyph tiếng Việt |
| `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs` | Thêm lifecycle serialized defaults và virtual HUD state |
| `Assets/_Project/Scripts/Gameplay/Common/MinigameLifecycle.cs` | Thêm `PhaseChanged` event |
| `Assets/Tests/EditMode/Presentation/*.cs` | Unit/config tests cho theme, safe area, lifecycle và ViewModel |
| `Assets/Tests/PlayMode/Presentation/*.cs` | Scene/prefab/input smoke tests cho S2 gate |
| `Assets/_Project/Scenes/{MG_Sprint,MG_Endurance,MG_Boss,Punishment,Map,GameOver}.unity` | Gắn camera và presentation prefab theo S2-2 |

## Task 1: Establish UI assembly, theme contract, and Vietnamese font assets

**Files:**
- Create: `Assets/_Project/Scripts/UI/KMA.Gameplay.UI.asmdef`
- Create: `Assets/_Project/Scripts/UI/UITheme.cs`
- Create: `Assets/_Project/Scripts/UI/TutorialSeenStore.cs`
- Create: `Assets/_Project/Settings/UI/UITheme.asset`
- Create: `Assets/Tests/EditMode/Presentation/UIThemeTests.cs`
- Create: `Assets/Tests/EditMode/Presentation/KMA.Gameplay.UI.EditMode.Tests.asmdef`
- Create: `Assets/_Project/Fonts/Baloo2-ExtraBold.asset` and its `.meta`
- Create: `Assets/_Project/Fonts/Nunito-Bold.asset` and its `.meta`
- Modify: `Assets/_Project/Fonts/` with source font files only if a license-approved local font is available; otherwise use the installed project-compatible font source and record provenance in `Assets/_Project/CREDITS.md`.

**Interfaces:**
- Consumes: Unity `com.unity.ugui`/TMP from S1 and existing `KMA.Gameplay` assembly.
- Produces: `KMA.Gameplay.UI.UITheme`, `KMA.Gameplay.UI.ITutorialSeenStore`, `KMA.Gameplay.UI.PlayerPrefsTutorialSeenStore`, and assembly `KMA.Gameplay.UI` for Tasks 2–6.

- [ ] **Step 1: Write the failing theme tests**

```csharp
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class UIThemeTests
    {
        [Test]
        public void ThemeAssetUsesApprovedPalette()
        {
            var theme = Resources.Load<KMA.Gameplay.UI.UITheme>("UITheme");
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.Primary, Is.EqualTo(new Color32(0xFF, 0x59, 0x5E, 0xFF)));
            Assert.That(theme.Accent, Is.EqualTo(new Color32(0xFF, 0xCA, 0x3A, 0xFF)));
            Assert.That(theme.Background, Is.EqualTo(new Color32(0x19, 0x82, 0xC4, 0xFF)));
            Assert.That(theme.Success, Is.EqualTo(new Color32(0x8A, 0xCB, 0x88, 0xFF)));
            Assert.That(theme.Card, Is.EqualTo(Color.white));
            Assert.That(theme.Muted, Is.EqualTo(new Color32(0xE2, 0xE8, 0xF0, 0xFF)));
            Assert.That(theme.MutedForeground, Is.EqualTo(new Color32(0x47, 0x55, 0x69, 0xFF)));
            Assert.That(theme.Border, Is.EqualTo(Color.black));
        }

        [Test]
        public void TutorialSeenStoreRoundTripsBySubject()
        {
            var store = new KMA.Gameplay.UI.MemoryTutorialSeenStore();
            Assert.That(store.HasSeen("Sprint"), Is.False);
            store.MarkSeen("Sprint");
            Assert.That(store.HasSeen("Sprint"), Is.True);
            Assert.That(store.HasSeen("Endurance"), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run the focused tests and confirm red**

Run:

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation.UIThemeTests' --output /tmp/s2-theme-red.xml --timeout 600 -- -nographics
```

Expected: FAIL because `UITheme`, the asset, and the store do not exist yet.

- [ ] **Step 3: Implement the theme and store contract**

`UITheme` must expose these read-only runtime properties: `Primary`, `Accent`, `Background`, `Success`, `Card`, `Muted`, `MutedForeground`, `Border`, `Spacing`, `CornerRadius`, `BorderWidth`, and `ShadowOffset`. Create the asset at `Assets/_Project/Settings/UI/UITheme.asset`, add a `Resources` reference or explicit prefab reference so the test does not scan arbitrary assets, and set the documented palette exactly.

Define the persistence seam as:

```csharp
public interface ITutorialSeenStore
{
    bool HasSeen(string subjectId);
    void MarkSeen(string subjectId);
}
```

`MemoryTutorialSeenStore` is test-only/in-memory. `PlayerPrefsTutorialSeenStore` stores keys `KMA.tutorialSeen.<subjectId>` and never writes during a read. Do not depend on S4 `SaveSystem` yet; S4 can replace this adapter behind the interface.

- [ ] **Step 4: Create TMP font assets and verify glyph coverage**

Generate static TMP assets for `Baloo2-ExtraBold` and `Nunito-Bold` with this exact character source: all characters found by scanning user-visible strings in `Assets/_Project`, Latin basic printable range, `1EA0–1EF9`, `0110/0111`, and `01A0–01B0`. Enable dynamic fallback and ensure the fallback chain is serialized in the font assets. Add one EditMode test that loads both assets and asserts `characterLookupTable` contains `Đ`, `đ`, `ă`, `Ă`, `ộ`, `ơ`, `Ư`, and `ứ`.

- [ ] **Step 5: Run focused tests and commit**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation.UIThemeTests' --output /tmp/s2-theme-green.xml --timeout 600 -- -nographics
rtk git add Assets/_Project/Scripts/UI Assets/_Project/Settings/UI Assets/_Project/Fonts Assets/Tests/EditMode/Presentation Assets/_Project/CREDITS.md
rtk git commit -m "feat: add S2 UI theme and Vietnamese font foundation"
```

Expected: focused theme/font tests pass; no rules test changes.

## Task 2: Add lifecycle events and the HUD ViewModel contract

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Common/MinigameLifecycle.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs`
- Create: `Assets/_Project/Scripts/UI/MinigameHudState.cs`
- Create: `Assets/_Project/Scripts/UI/MinigameHUD.cs`
- Create: `Assets/Tests/EditMode/Presentation/MinigameLifecyclePresentationTests.cs`
- Create: `Assets/Tests/PlayMode/Presentation/MinigameHUDTests.cs`
- Modify: controller classes only when needed to override `BuildHudState()`; start with `SprintController.cs` and `EnduranceController.cs`.

**Interfaces:**
- Consumes: `UITheme` and `KMA.Gameplay.UI` assembly from Task 1.
- Produces: `MinigameLifecycle.PhaseChanged`, `MinigameBase.BuildHudState()`, `MinigameHudState`, and `MinigameHUD.RefreshFrom(MinigameHudState)`.

- [ ] **Step 1: Write failing lifecycle and ViewModel tests**

```csharp
[Test]
public void PhaseChangedFiresOncePerTransition()
{
    var lifecycle = new MinigameLifecycle(1f, 2f);
    var phases = new List<MinigamePhase>();
    lifecycle.PhaseChanged += phases.Add;
    lifecycle.Tick(1f);
    lifecycle.Tick(2f);
    lifecycle.Tick(5f);
    Assert.That(phases, Is.EqualTo(new[] { MinigamePhase.Countdown, MinigamePhase.Play }));
}

[Test]
public void DefaultHudStateIsEmptyAndSafe()
{
    var gameObject = new GameObject("test-minigame");
    try
    {
        var controller = gameObject.AddComponent<TestMinigameBase>();
        Assert.That(controller.ReadHudState().statusText, Is.EqualTo(string.Empty));
    }
    finally { Object.DestroyImmediate(gameObject); }
}
```

`TestMinigameBase` must be a private test subclass whose `TickPlay` is empty and whose public test method returns the protected virtual result. The test must not call a configurator to make itself pass.

- [ ] **Step 2: Run the focused tests and confirm red**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'PhaseChangedFiresOncePerTransition|DefaultHudStateIsEmptyAndSafe' --output /tmp/s2-hud-red.xml --timeout 600 -- -nographics
```

Expected: compile/test failure because the event, struct, and virtual method are absent.

- [ ] **Step 3: Implement the additive lifecycle and ViewModel changes**

Add `public event Action<MinigamePhase> PhaseChanged;`. In `Tick` and `BeginResolve`, capture the old phase and invoke the event exactly once after assigning a new phase. Keep timing and phase transitions unchanged.

Add serialized defaults to `MinigameBase`:

```csharp
[SerializeField] float tutorialSeconds = 2f;
[SerializeField] float countdownSeconds = 3f;
protected virtual MinigameHudState BuildHudState() => MinigameHudState.Empty;
```

`Awake()` must pass those fields to `new MinigameLifecycle(tutorialSeconds, countdownSeconds)`. `MinigameHUD.Update()` finds its explicitly serialized `MinigameBase` source, calls `BuildHudState()` through a public/internal bridge, and updates only UI fields. It must tolerate no controller, no theme, and no optional label.

- [ ] **Step 4: Add Sprint and Endurance state adapters**

`SprintController.BuildHudState()` returns:

```csharp
new MinigameHudState(
    timeRemaining: Mathf.Max(0f, rules == null ? 0f : 14f - rules.Elapsed),
    primary01: Mathf.Clamp01((rules == null ? 0f : rules.Stamina) / 100f),
    primaryLabel: "STAMINA",
    secondary01: Mathf.Clamp01((rules == null ? 0f : rules.Snapshot.Distance) / 100f),
    secondaryLabel: "DISTANCE",
    statusText: WindWindowActive ? "WIND — COUNTER NOW" : "TAP LEFT / RIGHT");
```

Use the actual existing public snapshot properties; if a named property differs, expose an additive read-only property in the controller rather than changing the rules API. Endurance returns time/lap progress and the current beat/cue status using existing public controller state.

- [ ] **Step 5: Run focused plus full tests and commit**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-hud-green-edit.xml --timeout 600 -- -nographics
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-hud-green-play.xml --timeout 900 -- -nographics
rtk git add Assets/_Project/Scripts/Gameplay/Common Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs Assets/_Project/Scripts/Gameplay/Endurance/EnduranceController.cs Assets/_Project/Scripts/UI Assets/Tests/EditMode/Presentation Assets/Tests/PlayMode/Presentation
rtk git commit -m "feat: add lifecycle events and minigame HUD state"
```

## Task 3: Build shared uGUI components and prefabs

**Files:**
- Create: `Assets/_Project/Scripts/UI/BrutalButton.cs`
- Create: `Assets/_Project/Scripts/UI/SafeAreaFitter.cs`
- Create: `Assets/_Project/Scripts/UI/ScreenBase.cs`
- Create: `Assets/_Project/Scripts/UI/HeartBar.cs`
- Create: `Assets/_Project/Scripts/UI/FloatingTextPool.cs`
- Create: `Assets/_Project/Prefabs/UI/Btn_Brutal.prefab`
- Create: `Assets/_Project/Prefabs/UI/HUD_Minigame.prefab`
- Create: `Assets/Tests/EditMode/Presentation/UIComponentTests.cs`
- Create: `Assets/Tests/PlayMode/Presentation/UIComponentPlayModeTests.cs`

**Interfaces:**
- Consumes: `UITheme`, `MinigameHUD`, `MinigameHudState` and `ITutorialSeenStore` from Tasks 1–2.
- Produces: reusable prefabs with serialized references, no runtime `FindObjectOfType` dependency, and a `SafeAreaFitter.Apply(Rect safeArea, Vector2Int screenSize)` test seam.

- [ ] **Step 1: Write failing component tests**

```csharp
[Test]
public void SafeAreaFitterMapsLandscapeInsetsToBothHorizontalEdges()
{
    var fitter = new SafeAreaFitter();
    var offsets = fitter.CalculateOffsets(new Rect(100f, 0f, 1720f, 1080f), new Vector2(1920f, 1080f));
    Assert.That(offsets.left, Is.EqualTo(100f).Within(.01f));
    Assert.That(offsets.right, Is.EqualTo(100f).Within(.01f));
}

[Test]
public void BrutalButtonReturnsToRestAfterPointerUp()
{
    var button = new BrutalButton();
    button.SetPressedForTest(true);
    Assert.That(button.CurrentVisualOffset, Is.EqualTo(new Vector2(4f, -4f)));
    button.SetPressedForTest(false);
    Assert.That(button.CurrentVisualOffset, Is.EqualTo(Vector2.zero));
}
```

- [ ] **Step 2: Run focused tests and confirm red**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'SafeAreaFitterMapsLandscapeInsetsToBothHorizontalEdges|BrutalButtonReturnsToRestAfterPointerUp' --output /tmp/s2-ui-red.xml --timeout 600 -- -nographics
```

- [ ] **Step 3: Implement the components**

`SafeAreaFitter` updates a `RectTransform.offsetMin/offsetMax` from `Screen.safeArea` in `OnEnable` and `OnRectTransformDimensionsChange`, with no per-frame allocation. `BrutalButton` implements `IPointerDownHandler`/`IPointerUpHandler`/`IPointerExitHandler`, stores its base anchored position, applies the `(+4,-4)` offset, restores it in `0.1s`, and exposes an optional `UnityEvent`/SFX hook without requiring an audio service.

`HeartBar` accepts an integer setter and renders exactly five slots with filled/empty state; `FloatingTextPool` prewarms a serialized count, returns inactive entries, and releases them via a coroutine/tween. `HUD_Minigame.prefab` must use the S2 Canvas Scaler values and anchor timer/progress to the safe-area root.

- [ ] **Step 4: Verify prefab contracts in PlayMode**

Load `HUD_Minigame.prefab` into a temporary scene and assert one Canvas, one `CanvasScaler`, one `SafeAreaFitter`, one `MinigameHUD`, one `HeartBar`, no `OnGUI`, and no missing serialized references. Simulate a safe-area inset and assert both horizontal offsets change.

- [ ] **Step 5: Run tests and commit**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-components-edit.xml --timeout 600 -- -nographics
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-components-play.xml --timeout 900 -- -nographics
rtk git add Assets/_Project/Scripts/UI Assets/_Project/Prefabs/UI Assets/Tests/EditMode/Presentation Assets/Tests/PlayMode/Presentation
rtk git commit -m "feat: add safe-area and brutal UI components"
```

## Task 4: Add phase/tutorial/result presentation

**Files:**
- Create: `Assets/_Project/Scripts/UI/PhaseOverlay.cs`
- Create: `Assets/_Project/Scripts/UI/TutorialOverlay.cs`
- Create: `Assets/_Project/Scripts/UI/ResultPanel.cs`
- Create: `Assets/_Project/Prefabs/UI/PhaseOverlay.prefab`
- Create: `Assets/_Project/Prefabs/UI/ResultPanel.prefab`
- Create: `Assets/Tests/EditMode/Presentation/TutorialOverlayTests.cs`
- Create: `Assets/Tests/PlayMode/Presentation/PhaseFlowTests.cs`

**Interfaces:**
- Consumes: `MinigameLifecycle.PhaseChanged`, `MinigameHudState`, `ITutorialSeenStore`, `GameSession`, and existing `MinigameResult`.
- Produces: `TutorialOverlay.Show(string subjectId, IReadOnlyList<TutorialStep> steps)`, `PhaseOverlay.Bind(MinigameBase)`, and `ResultPanel.Show(MinigameResult result, string previewRoute)`.

- [ ] **Step 1: Write failing tutorial and phase-flow tests**

```csharp
[Test]
public void TutorialCanAdvanceBackAndSkipAndMarksSubjectSeen()
{
    var store = new MemoryTutorialSeenStore();
    var overlay = new TutorialOverlay();
    overlay.ConfigureForTest(store, "Sprint", new[]
    {
        new TutorialStep("LEFT / RIGHT", "Tap the matching side."),
        new TutorialStep("WIND", "Counter the cue before it expires.")
    });
    Assert.That(overlay.CurrentIndex, Is.EqualTo(0));
    overlay.Next();
    Assert.That(overlay.CurrentIndex, Is.EqualTo(1));
    overlay.Back();
    Assert.That(overlay.CurrentIndex, Is.EqualTo(0));
    overlay.Skip();
    Assert.That(store.HasSeen("Sprint"), Is.True);
}

[Test]
public void AlreadySeenSubjectStartsWithoutTutorial()
{
    var store = new MemoryTutorialSeenStore();
    store.MarkSeen("Sprint");
    var overlay = new TutorialOverlay();
    overlay.ConfigureForTest(store, "Sprint", new[] { new TutorialStep("x", "y") });
    Assert.That(overlay.ShouldShow, Is.False);
}
```

- [ ] **Step 2: Run focused tests and confirm red**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation.TutorialOverlayTests' --output /tmp/s2-tutorial-red.xml --timeout 600 -- -nographics
```

- [ ] **Step 3: Implement the multi-step flow**

`TutorialStep` contains `title`, `instruction`, optional `Sprite icon`, and optional `string animationKey`. `TutorialOverlay` must expose Next, Back, Skip, and Close buttons, disable Back on index 0, disable Next on the last step, and call `ITutorialSeenStore.MarkSeen(subjectId)` on Skip or completion. If a subject has no steps, it must close immediately and never block gameplay.

`PhaseOverlay` subscribes to `PhaseChanged`, presents the tutorial shell, a visible 3-2-1 countdown, Play, and Resolve states. It must unsubscribe in `OnDisable`. `ResultPanel` is passive: it receives a result and preview route, renders pass/fail/score/rank, and raises an action event; it must not mutate `GameSession` or route scenes.

- [ ] **Step 4: Build prefabs and verify phase transitions**

Bind TMP labels with the Vietnamese font assets, apply `.text-shadow` Underlay (`x=.04`, `y=-.04`, black, softness `0`) and `.text-stroke-dark` Outline `0.2` plus Underlay. PlayMode test a `SprintController` with tutorial/countdown defaults and assert the overlay displays Tutorial, then Countdown, then HUD Play without changing rules state.

- [ ] **Step 5: Run tests and commit**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-phase-edit.xml --timeout 600 -- -nographics
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-phase-play.xml --timeout 900 -- -nographics
rtk git add Assets/_Project/Scripts/UI Assets/_Project/Prefabs/UI Assets/Tests/EditMode/Presentation Assets/Tests/PlayMode/Presentation
rtk git commit -m "feat: add tutorial phase and result presentation"
```

## Task 5: Create GameCamera and attach S2 presentation to all existing scenes

**Files:**
- Create: `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs`
- Create: `Assets/_Project/Prefabs/Gameplay/GameCamera.prefab`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/_Project/Scenes/MG_Endurance.unity`
- Modify: `Assets/_Project/Scenes/MG_Boss.unity`
- Modify: `Assets/_Project/Scenes/Punishment.unity`
- Modify: `Assets/_Project/Scenes/Map.unity`
- Modify: `Assets/_Project/Scenes/GameOver.unity`
- Modify: `ProjectSettings/EditorBuildSettings.asset` only if a scene path is missing; do not reorder existing scenes.
- Create: `Assets/Tests/PlayMode/Presentation/ScenePresentationContractTests.cs`

**Interfaces:**
- Consumes: `GameCamera`, `HUD_Minigame`, `PhaseOverlay`, `ResultPanel`, and existing scene-owned `SceneRouter`/controllers.
- Produces: all six existing scenes with a tagged Main Camera, URP 2D camera data, landscape-safe Canvas, and visible uGUI presentation. `MinigameUIAssembler` is editor-only and must be idempotent.

- [ ] **Step 1: Write the failing scene contract test**

```csharp
[UnityTest]
public IEnumerator EveryExistingSceneHasS2CameraAndCanvas()
{
    foreach (var sceneName in new[] { "MG_Sprint", "MG_Endurance", "MG_Boss", "Punishment", "Map", "GameOver" })
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Assert.That(Camera.main, Is.Not.Null, sceneName);
        Assert.That(Camera.main.orthographic, Is.True, sceneName);
        Assert.That(Camera.main.GetComponent("UniversalAdditionalCameraData"), Is.Not.Null, sceneName);
        Assert.That(Object.FindFirstObjectByType<Canvas>(), Is.Not.Null, sceneName);
        Assert.That(Object.FindFirstObjectByType<CanvasScaler>().matchWidthOrHeight, Is.EqualTo(1f).Within(.001f));
        Assert.That(Object.FindFirstObjectByType<SafeAreaFitter>(), Is.Not.Null, sceneName);
    }
}
```

- [ ] **Step 2: Run the scene test and confirm missing presentation**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'EveryExistingSceneHasS2CameraAndCanvas' --output /tmp/s2-scenes-red.xml --timeout 900 -- -nographics
```

Expected: fail on the first scene without the new Canvas/prefab contract.

- [ ] **Step 3: Create and bind `GameCamera.prefab`**

Create the camera through the Unity Editor API/menu so `UniversalAdditionalCameraData` is serialized correctly. Set tag `MainCamera`, orthographic `true`, position `(0,0,-10)`, clear flags `SolidColor`, and a project theme background. Keep the orthographic size constant for the 1080 reference height; do not derive it from width.

- [ ] **Step 4: Add scene-owned presentation hierarchies**

Use `MinigameUIAssembler` to add one camera instance and one Canvas hierarchy to each scene, with serialized references to the scene’s minigame controller when present. `MG_Boss` must no longer render black because it has no camera. `Map` and `GameOver` receive the shared screen shell but no fake minigame HUD source. Keep `GameplayPresentation` only if the existing compatibility test needs it; disable its `OnGUI` drawing when a S2 Canvas is present so two presentations do not overlap.

- [ ] **Step 5: Run the scene contract and full suites**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation' --output /tmp/s2-scenes-green-play.xml --timeout 1200 -- -nographics
rtk ~/.local/bin/unity test . --mode EditMode --output /tmp/s2-full-edit.xml --timeout 2400 -- -nographics
rtk ~/.local/bin/unity test . --mode PlayMode --output /tmp/s2-full-play.xml --timeout 2400 -- -nographics
rtk git add Assets/_Project/Scripts/UI Assets/_Project/Prefabs/Gameplay Assets/_Project/Scenes Assets/Tests/PlayMode/Presentation
rtk git commit -m "feat: add S2 camera and scene presentation shell"
```

Commit scene YAML separately from runtime source if Unity rewrites unrelated scene metadata; inspect `git diff --stat` before staging.

## Task 6: Implement the Sprint HUD gate and final verification

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs` if Task 2 left only an adapter stub
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/_Project/Prefabs/UI/HUD_Minigame.prefab` only for Sprint bindings
- Create: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`
- Modify: `README.md` with verified S2 gate evidence
- Create: `.superpowers/sdd/2026-08-28-s2-presentation-foundation/task-6-report.md`

**Interfaces:**
- Consumes: all S2 components, `SprintController.BuildHudState()`, `PhaseOverlay`, and `GameCamera`.
- Produces: a repeatable S2 gate test and device evidence for tutorial/countdown/HUD/input response.

- [ ] **Step 1: Write the failing S2 gate test**

```csharp
[UnityTest]
public IEnumerator SprintSceneShowsTutorialCountdownHudAndInputResponse()
{
    yield return SceneManager.LoadSceneAsync("MG_Sprint", LoadSceneMode.Single);
    var controller = Object.FindFirstObjectByType<SprintController>();
    var hud = Object.FindFirstObjectByType<MinigameHUD>();
    var overlay = Object.FindFirstObjectByType<TutorialOverlay>();
    Assert.That(controller, Is.Not.Null);
    Assert.That(hud, Is.Not.Null);
    Assert.That(overlay, Is.Not.Null);
    Assert.That(overlay.ShouldShow, Is.True);
    yield return new WaitForSeconds(2.1f);
    Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
    yield return new WaitForSeconds(3.1f);
    Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
    var before = controller.Snapshot.Distance;
    controller.OnLeftTap();
    Assert.That(controller.Snapshot.Distance, Is.GreaterThan(before));
    Assert.That(hud.LastState.statusText, Does.Contain("TAP").Or.Contain("WIND"));
}
```

The test may use a deterministic `ConfigureForTest` setup only for time/input, not to instantiate or mutate the HUD contract. Use the real scene and serialized bindings.

- [ ] **Step 2: Run the gate and confirm red**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'SprintSceneShowsTutorialCountdownHudAndInputResponse' --output /tmp/s2-gate-red.xml --timeout 1200 -- -nographics
```

- [ ] **Step 3: Bind Sprint-specific presentation**

Set the tutorial steps to:

1. `LEFT / RIGHT` — “Tap the side shown by the cue.”
2. `WIND CUE` — “Counter the wind on the opposite side before the window closes.”

Bind timer to Sprint remaining time, primary bar to stamina, secondary bar to distance, status to phase/cue, and preserve the existing input action asset. The HUD must refresh after `OnLeftTap`/`OnRightTap` via the next UI update without changing the `SprintRules` contract.

- [ ] **Step 4: Run gate and all verification**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests' --output /tmp/s2-gate-green.xml --timeout 1200 -- -nographics
rtk ~/.local/bin/unity test . --mode EditMode --output /tmp/s2-final-edit.xml --timeout 2400 -- -nographics
rtk ~/.local/bin/unity test . --mode PlayMode --output /tmp/s2-final-play.xml --timeout 2400 -- -nographics
```

Expected: all tests pass, including the pre-S2 baseline, with no changed rules assertions.

- [ ] **Step 5: Verify on the current Android emulator**

Build and install a fresh APK using the S1 toolchain. Record the exact APK SHA-256, `adb devices -l`, `ro.product.cpu.abilist`, package foreground activity, landscape orientation, and a screenshot showing the Sprint tutorial/countdown/HUD. Do not claim physical-device verification from emulator evidence.

- [ ] **Step 6: Commit evidence and hand off**

```bash
rtk git add Assets/_Project/Scenes/MG_Sprint.unity Assets/_Project/Prefabs/UI/HUD_Minigame.prefab Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs README.md .superpowers/sdd/2026-08-28-s2-presentation-foundation/task-6-report.md
rtk git commit -m "test: verify S2 sprint presentation gate"
```

## Gate S2 — điều kiện coi là xong

- [ ] `UITheme.asset` có đúng palette/token S2 và mọi shared prefab bind cùng asset.
- [ ] TMP font assets load được và có glyph `Đ đ ă Ă ộ ơ Ư ứ` cùng dynamic fallback.
- [ ] `PhaseChanged` phát đúng một lần cho mỗi transition; lifecycle timing cũ không đổi.
- [ ] `MinigameBase` có serialized tutorial/countdown defaults `2f`/`3f`, và default HUD state rỗng không gây null reference.
- [ ] Canvas Scaler là `1920×1080`, match `1.0`; Safe Area áp dụng cả trái/phải.
- [ ] Có `HUD_Minigame`, `PhaseOverlay`, `ResultPanel`, `Btn_Brutal`, `GameCamera` prefab với serialized references hợp lệ.
- [ ] Sáu scene hiện có có Main Camera orthographic + URP camera data và Canvas presentation.
- [ ] Tutorial hỗ trợ nhiều bước, next/back/skip, và seen state theo subject qua `ITutorialSeenStore`.
- [ ] `MG_Sprint` gate chứng minh tutorial → countdown 3-2-1 → Play HUD timer/stamina → Left/Right làm giá trị đổi.
- [ ] Full EditMode và PlayMode pass; test XML được lưu ở `/tmp` và số liệu ghi vào report/README.
- [ ] Emulator Android hiện tại cài/mở được APK S2 và hiển thị landscape; giới hạn ABI/physical device ghi rõ.
- [ ] Scene YAML changes được commit riêng nếu Unity ghi lại scene; không stage `.utmp`, `Library`, crash blobs hoặc generated metadata ngoài scope.

## Self-review checklist before implementation

- Spec coverage: UITheme, brutal controls, TMP Vietnamese font, Canvas Scaler, SafeAreaFitter, GameCamera, multi-step tutorial, lifecycle event, ViewModel HUD contract, HUD placement, and S2 gate each have an explicit task and test.
- Dependency coverage: S1 provides URP/TMP and folder tree; S2 does not require S3 input detectors or S4 SaveSystem, using a narrow tutorial persistence seam instead.
- Scope boundary: subject-specific HUDs, ball-rig visuals, production minigame scenes, shared input normalization, and save-system replacement stay in S3–S14.
- Test integrity: tests inspect serialized scene/prefab/runtime state and do not call configurators before asserting the result.
- Reproducibility: all generated assets have `.meta` files, prefab references are serialized, and scene edits are isolated from source commits.
