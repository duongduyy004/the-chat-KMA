# Sprint Mobile UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved Android-landscape Sprint HUD, controls, start flow, finish reveal, runner identification, and result presentation without changing gameplay or progression behavior.

**Architecture:** Keep `SprintController`, `SprintRules`, `GameplayInputRouter`, `MinigameLifecycle`, and `ResultPanel` as the authoritative behavior layer. Add small Sprint-only presenters that read existing state and animate serialized/runtime-created uGUI elements; keep input routing and result events on their existing paths. Build responsive visual geometry from the safe-area rect and verify both semantic state and rendered layout.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, uGUI, TextMeshPro, Input System/EventSystem, Unity Test Framework EditMode and PlayMode, Android landscape.

**Spec:** `docs/superpowers/specs/2026-09-13-sprint-mobile-ui-redesign-design.md`

## Global Constraints

- Run every shell command through `rtk`.
- Work in the current checkout; do not create a worktree unless the user explicitly changes that instruction.
- Preserve the user's existing changes in `UIComponentTests.cs`, `FestivalUiExperienceTests.cs`, `S5NewGameTests.cs`, `MapNodeView.cs`, and `MapPresentationBuilder.cs`.
- Do not change `SprintRules`, rival pacing, stamina, wind timing, the 14-second limit, pass/fail conditions, score/rank calculation, lifecycle semantics, or result routing.
- Use `CanvasScaler` reference resolution `1920 × 1080` and anchor interactive visuals inside `SafeAreaRoot`.
- Visible controls target 20% × 16% of the safe area at 50% navy opacity; invisible hit areas target 28% × 24%, remain disjoint, and do not cross the midpoint.
- The finish line is hidden below 70 m and visible from exactly 70 m.
- Sprint tutorial copy is `← TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI →`; it auto-releases after 1.5 seconds.
- Countdown copy is `3`, `2`, `1`, `GO!`; Sprint input remains gated until `MinigamePhase.Play`.
- The redesign applies only to Sprint; do not globally restyle other minigames.
- No new package, bitmap asset, or required haptics dependency.

---

## File Map

| File | Responsibility |
|---|---|
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs` | Pure responsive layout and finish-threshold calculations. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs` | Populate consolidated distance/rank/combo/progress telemetry. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs` | Build/style the Sprint-only scoreboard, control visuals, player marker, finish visual, and result chrome. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs` | Press feedback and expected-side visual state without owning input logic. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintFinishLinePresenter.cs` | Reveal the finish visual from existing distance state. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs` | Automatic tutorial, countdown labels, and transient Play instruction. |
| `Assets/_Project/Scripts/Gameplay/Sprint/SprintResultPresentation.cs` | Sprint-only result modal styling and short animation. |
| `Assets/_Project/Scenes/MG_Sprint.unity` | Bind presenters, align runner roots to lane centers, and retain existing input/result objects. |
| `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs` | Pure geometry and threshold coverage. |
| `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs` | Real-scene HUD, runner, controls, start, finish, and result contracts. |
| `Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs` | Replace obsolete Sprint chrome expectations with the approved visual hierarchy. |

### Task 1: Lock responsive geometry and finish threshold

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintUiLayout.cs.meta`
- Create: `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs`
- Create: `Assets/Tests/EditMode/Presentation/SprintUiLayoutTests.cs.meta`

**Interfaces:**
- Produces `SprintUiLayout.LaneCenter01(int laneIndex, int laneCount) -> float`.
- Produces `SprintUiLayout.VisibleControlRect(Rect safeArea, bool left) -> Rect`.
- Produces `SprintUiLayout.HitAreaRect(Rect safeArea, bool left) -> Rect`.
- Produces `SprintUiLayout.FinishVisible(float distance) -> bool` and constant `FinishRevealDistance = 70f`.

- [ ] **Step 1: Write the failing EditMode tests.**

```csharp
[TestCase(0, 0.125f)]
[TestCase(1, 0.375f)]
[TestCase(2, 0.625f)]
[TestCase(3, 0.875f)]
public void LaneCenter01_CentersFourEqualLanes(int lane, float expected) =>
    Assert.That(SprintUiLayout.LaneCenter01(lane, 4), Is.EqualTo(expected).Within(.0001f));

[Test]
public void Controls_AreSymmetricSmallerThanHitAreasAndDisjoint()
{
    var safe = new Rect(0, 0, 1920, 1080);
    Rect leftVisual = SprintUiLayout.VisibleControlRect(safe, true);
    Rect rightVisual = SprintUiLayout.VisibleControlRect(safe, false);
    Rect leftHit = SprintUiLayout.HitAreaRect(safe, true);
    Rect rightHit = SprintUiLayout.HitAreaRect(safe, false);
    Assert.That(leftVisual.width, Is.EqualTo(rightVisual.width));
    Assert.That(leftVisual.height, Is.EqualTo(rightVisual.height));
    Assert.That(leftVisual.width, Is.LessThan(leftHit.width));
    Assert.That(leftVisual.height, Is.LessThan(leftHit.height));
    Assert.That(leftHit.xMax, Is.LessThanOrEqualTo(safe.center.x));
    Assert.That(rightHit.xMin, Is.GreaterThanOrEqualTo(safe.center.x));
}

[TestCase(69.9f, false)]
[TestCase(70f, true)]
[TestCase(100f, true)]
public void FinishVisible_UsesApprovedThreshold(float distance, bool expected) =>
    Assert.That(SprintUiLayout.FinishVisible(distance), Is.EqualTo(expected));
```

- [ ] **Step 2: Run the focused test and verify RED.**

```bash
rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Presentation.SprintUiLayoutTests' --output /tmp/kma-sprint-layout-red.xml --timeout 600 -- -nographics
```

Expected: compile failure because `SprintUiLayout` does not exist.

- [ ] **Step 3: Implement the pure layout helper.**

```csharp
public static class SprintUiLayout
{
    public const float FinishRevealDistance = 70f;

    public static float LaneCenter01(int laneIndex, int laneCount)
    {
        if (laneCount <= 0) throw new System.ArgumentOutOfRangeException(nameof(laneCount));
        return (Mathf.Clamp(laneIndex, 0, laneCount - 1) + .5f) / laneCount;
    }

    public static Rect VisibleControlRect(Rect safe, bool left) =>
        BottomCornerRect(safe, left, .20f, .16f, .05f);

    public static Rect HitAreaRect(Rect safe, bool left) =>
        BottomCornerRect(safe, left, .28f, .24f, .01f);

    public static bool FinishVisible(float distance) => distance >= FinishRevealDistance;

    static Rect BottomCornerRect(Rect safe, bool left, float width01, float height01, float inset01)
    {
        float width = safe.width * width01;
        float height = safe.height * height01;
        float x = left ? safe.xMin + safe.width * inset01 : safe.xMax - safe.width * inset01 - width;
        return new Rect(x, safe.yMin + safe.height * .05f, width, height);
    }
}
```

- [ ] **Step 4: Run the focused test GREEN and commit.** Use the Task 1 command with `/tmp/kma-sprint-layout-green.xml`. Expected: all `SprintUiLayoutTests` pass. Commit only Task 1 files as `test: define responsive sprint UI geometry`.

### Task 2: Consolidate scoreboard and align race presentation

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs`

**Interfaces:**
- Consumes `SprintUiLayout.LaneCenter01` and current `SprintController.Snapshot`, `RankText`, and `CadenceCombo`.
- Produces hierarchy `SprintBroadcastChrome/Scoreboard/{Distance,Rank,Combo,ProgressTrack/ProgressFill}`.
- Produces hierarchy `SprintBroadcastChrome/PlayerMarker` and read-only `SprintHud.HasBoundVisuals`.

- [ ] **Step 1: Replace the obsolete real-scene assertions with a failing approved-hierarchy contract.** Assert exact text and unique ownership:

```csharp
Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
Assert.That(chrome.Find("Scoreboard/Distance").GetComponent<TMP_Text>().text, Is.EqualTo("0 / 100 m"));
Assert.That(chrome.Find("Scoreboard/Rank").GetComponent<TMP_Text>().text, Is.EqualTo("1st"));
Assert.That(chrome.Find("Scoreboard/Combo").GetComponent<TMP_Text>().text, Is.EqualTo("COMBO ×0"));
Assert.That(chrome.Find("Scoreboard/ProgressTrack/ProgressFill").GetComponent<Image>().type,
    Is.EqualTo(Image.Type.Filled));
Assert.That(chrome.GetComponentInParent<SafeAreaFitter>(), Is.Not.Null);
Assert.That(GameObject.Find("SprintFestivalChrome"), Is.Null);
```

After `controller.AdvanceToDistance(42f); sprintHud.Refresh();`, expect `42 / 100 m`, the unchanged ordinal rank, and `.42f` fill.

- [ ] **Step 2: Add failing lane/player assertions.** Resolve `SprintLane` roots in authored order and assert their runner visual world Y positions equal the lane centers calculated from the track region within `0.02f`. Assert `PlayerMarker` targets the player root and a cyan outline/highlight component exists only on the player.

- [ ] **Step 3: Run focused PlayMode RED.**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests|KMA.Tests.Presentation.FestivalUiExperienceTests' --output /tmp/kma-sprint-scoreboard-red.xml --timeout 600 -- -nographics
```

Expected: missing `SprintBroadcastChrome`, old distance format `42 m`, old combo character `x`, and lane/player presentation assertions fail.

- [ ] **Step 4: Build the approved scoreboard and player marker.** Make `SprintFestivalPresentation.Build()` idempotently create `SprintBroadcastChrome` under `SafeAreaRoot`, not the Canvas root. Change `SprintHud.Refresh()` to:

```csharp
DistanceText = $"{Mathf.RoundToInt(snapshot.Distance)} / 100 m";
RankText = controller.RankText;
CadenceText = $"COMBO ×{controller.CadenceCombo}";
distanceFill.fillAmount = Mathf.Clamp01(snapshot.Distance / 100f);
```

Disable the shared `Time`, `Phase`, `Score`, `Status`, shared progress, and stamina graphics only in the Sprint scene. Place pause outside the scoreboard's occupied anchors.

- [ ] **Step 5: Align the four authored runner roots and bind the player marker.** Preserve every runner's horizontal presenter. Set only vertical lane positions from the four equal track bands; make the marker a child/follower of the player's presentation root so its offset remains local and short.

- [ ] **Step 6: Run focused GREEN and inspect scene diff.** Run the Task 2 test command with `/tmp/kma-sprint-scoreboard-green.xml`. Expected: all selected tests pass; `MG_Sprint.unity` changes contain only intended runner/UI bindings and no controller/rule values.

- [ ] **Step 7: Commit** Task 2 files as `feat: consolidate sprint race presentation`.

### Task 3: Add small translucent controls with large authoritative hit areas

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintControlPresenter.cs.meta`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Consumes `SprintController.ExpectedSide`, existing `ScreenTapArea` objects `LeftTap` and `RightTap`, and `SprintUiLayout` rects.
- Produces `SprintControlPresenter.Configure(SprintController, RectTransform leftVisual, RectTransform rightVisual, Graphic leftGraphic, Graphic rightGraphic)`.
- Produces read-only `HighlightedSide`, `LeftScale`, and `RightScale` test seams.

- [ ] **Step 1: Write failing scene tests for geometry and state.** Assert visual buttons are symmetric, use alpha `.50f ± .02f`, are contained by their corresponding hit rects, and hit rects remain disjoint. Configure controller for Play, call `RefreshForTest()`, tap the expected side, and assert the highlight changes to the controller's next `ExpectedSide` without double-incrementing `CadenceCombo`.

- [ ] **Step 2: Write a failing press-feedback test.** Call `PressForTest(Side.Left)`, assert `LeftScale == .94f`, advance `0.091f` through `TickForTest`, and assert it returns to `1f` while controller distance/combo remain unchanged by the presenter itself.

- [ ] **Step 3: Run focused RED.** Use the Task 2 PlayMode command filtered to `SprintPresentationGateTests`; expected compile failure for missing `SprintControlPresenter`.

- [ ] **Step 4: Implement the presentation-only state machine.**

```csharp
public void RefreshForTest()
{
    HighlightedSide = controller.ExpectedSide;
    ApplyHighlight(leftGraphic, HighlightedSide == Side.Left);
    ApplyHighlight(rightGraphic, HighlightedSide == Side.Right);
}

public void PressForTest(Side side)
{
    pressedSide = side;
    pressRemaining = .09f;
    SetScale(side, .94f);
}

public void TickForTest(float deltaTime)
{
    pressRemaining = Mathf.Max(0f, pressRemaining - deltaTime);
    if (pressRemaining <= 0f) SetScale(pressedSide, 1f);
}
```

Wire visual feedback to EventSystem pointer-down on the existing tap areas, while leaving `ScreenTapArea` as the only input forwarder. Do not invoke `OnLeftTap`, `OnRightTap`, or router events from the presenter.

- [ ] **Step 5: Run focused GREEN and regression input tests.** Run `SprintPresentationGateTests`, then:

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Gameplay.Running.SprintRuntimeInputTests|KMA.Tests.Gameplay.Running.SprintControllerTests' --output /tmp/kma-sprint-input-green.xml --timeout 600 -- -nographics
```

Expected: all pass and one physical/UI tap still reaches gameplay exactly once.

- [ ] **Step 6: Commit** Task 3 files as `feat: add sprint control feedback`.

### Task 4: Replace Sprint's blocking tutorial with the approved automatic start presentation

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintStartPresentation.cs.meta`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/Tests/PlayMode/Presentation/PhaseFlowTests.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:**
- Produces `SprintStartPresentation.Bind(SprintController controller)`.
- Produces `TickForTest(float deltaTime)`, `TutorialVisible`, `CountdownText`, and `InstructionVisible` test seams.
- Consumes only `SetTutorialGate(bool)` and `PresentationPhase`; other minigames continue using `TutorialOverlay` unchanged.

- [ ] **Step 1: Write a failing Sprint start-flow test.** Bind a test controller, assert tutorial gate/visual are active with exact approved copy, tick `1.49f` and confirm it remains Tutorial, tick `.01f` and confirm the gate releases. Advance lifecycle and assert countdown sequence `3`, `2`, `1`, `GO!`; call `OnLeftTap()` before Play and assert distance stays zero.

- [ ] **Step 3: Run focused RED.**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation.PhaseFlowTests|KMA.Tests.Presentation.SprintPresentationGateTests' --output /tmp/kma-sprint-start-red.xml --timeout 600 -- -nographics
```

Expected: missing `SprintStartPresentation` and the current Sprint tutorial remains blocking.

- [ ] **Step 4: Implement the Sprint-only gate and labels.** On bind, call `controller.SetTutorialGate(true)`, show the banner, accumulate unscaled presentation time, and at exactly 1.5 seconds hide/fade the banner then call `SetTutorialGate(false)` once. Mirror `PresentationPhase` to countdown/instruction visuals; show `GO!` at the countdown-to-Play boundary and fade `TĂNG TỐC!` over `.25f` after Play begins.

- [ ] **Step 6: Run focused GREEN and full presentation regression.** Run the Task 4 command with `/tmp/kma-sprint-start-green.xml`, then all `KMA.Tests.Presentation`. Expected: Sprint auto-start passes; other minigame tutorial tests remain green.

- [ ] **Step 7: Commit** Task 4 files as `feat: streamline sprint start flow`.

### Task 5: Reveal finish at 70 m and redesign the Sprint result modal

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFinishLinePresenter.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFinishLinePresenter.cs.meta`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintResultPresentation.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintResultPresentation.cs.meta`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs`

**Interfaces:**
- Produces `SprintFinishLinePresenter.Configure(SprintController, GameObject finishRoot)` and `RefreshForTest()`.
- Produces `SprintFinishLinePresenter.IsVisible`.
- Produces `SprintResultPresentation.Bind(ResultPanel panel, SprintController controller)` and `ShowForTest(MinigameResult result)`.
- Consumes existing `ResultPanel.Show`, `CurrentResult`, and `Continue()` behavior without changing its event contract.

- [ ] **Step 1: Write failing finish tests.** Configure controller distance to `69.9f`, `70f`, and `100f`; refresh after each and assert `IsVisible` is false, true, true. Assert the finish root spans the complete authored track height and does not become a raycast target.

- [ ] **Step 2: Write failing result hierarchy and behavior tests.** For failure expect `THẤT BẠI`, red title, score `0`, rank badge `F`, and `TIẾP TỤC`; for success expect `HOÀN THÀNH!`, green title, score/rank values. Subscribe to `ActionRequested`, call Continue twice during animation, and assert one event with the original route.

- [ ] **Step 3: Run focused RED.**

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests|KMA.Tests.Progression.CoreLoopTests' --output /tmp/kma-sprint-result-red.xml --timeout 600 -- -nographics
```

Expected: missing presenter types and approved result hierarchy.

- [ ] **Step 4: Implement finish visibility as a pure read.**

```csharp
public void RefreshForTest()
{
    IsVisible = SprintUiLayout.FinishVisible(controller.Snapshot.Distance);
    if (finishRoot != null) finishRoot.SetActive(IsVisible);
}
```

Create the checkered visual from uGUI images/colors under the Sprint chrome; do not add a texture asset or collision/trigger component.

- [ ] **Step 5: Implement Sprint-only result styling and non-blocking motion.** Keep `ResultPanel` authoritative. `SprintResultPresentation` reads `CurrentResult`, styles the scene instance, runs scrim `120 ms`, modal `180 ms`, title `140 ms`, score count-up up to `350 ms`, and rank pop `160 ms`. Do not disable the CTA while animations run.

- [ ] **Step 6: Run focused GREEN and result regressions.** Use the Task 5 command with `/tmp/kma-sprint-result-green.xml`. Expected: all selected tests pass, including the pre-existing single-action Continue contract.

- [ ] **Step 7: Commit** Task 5 files as `feat: polish sprint finish and results`.

### Task 6: Verify responsive visuals, Android runtime, and repository hygiene

**Files:**
- Verify: all Task 1–5 production, scene, and test files
- Produce local evidence under: `Builds/Screenshots/` and `Builds/Android/`

**Interfaces:** No new runtime API; this task produces verification evidence.

- [ ] **Step 1: Run focused Sprint EditMode and PlayMode tests.** Run the Task 1 EditMode filter and:

```bash
rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Presentation.SprintPresentationGateTests|KMA.Tests.Presentation.PhaseFlowTests|KMA.Tests.Gameplay.Running.SprintRuntimeInputTests|KMA.Tests.Gameplay.Running.SprintControllerTests|KMA.Tests.Progression.CoreLoopTests' --output /tmp/kma-sprint-final-play.xml --timeout 600 -- -nographics
```

Expected: zero failed/inconclusive tests; inspect XML totals and Unity log, not only exit status.

- [ ] **Step 2: Run the complete EditMode and PlayMode suites.** Use the repository README commands with Unity `6000.3.23f1`. Expected: zero regressions. If the environment cannot run tests, record the exact blocker and do not report them as passing.

- [ ] **Step 3: Capture and inspect static visual checkpoints.** Keep one GUI Editor warm and run `rtk bash tools/qa-screenshot.sh` for `MG_Sprint.unity` at tutorial, countdown, early gameplay, late gameplay with finish, failure result, and success result. Use the interactive QA path for state transitions when it is available; otherwise add a test-only screenshot scenario that uses public test seams without changing production state flow. Inspect every PNG for text overlap, lane alignment, safe area, button opacity/size, finish timing, and modal hierarchy.

- [ ] **Step 4: Verify a wider landscape layout.** Capture the normal 16:9 reference and at least one 20:9 Game View size. Expected: scoreboard and controls remain inside safe area; hit areas remain disjoint; no player, label, or modal is clipped.

- [ ] **Step 5: Build, install, and inspect Android separately.** Close the GUI Editor, run:

```bash
rtk bash tools/build-apk.sh --x86_64 --output Builds/Android/kma-x86_64.apk
```

Install on the available x86_64 emulator, launch `com.kma.thechat`, inspect filtered logcat, exercise rapid alternating touches, and capture gameplay/result screenshots. Record build, install/process, logcat, input behavior, and visual acceptance as separate results.

- [ ] **Step 6: Run repository hygiene checks.**

```bash
rtk git diff --check
rtk git status --short --untracked-files=all
rtk git log --oneline --decorate -8
```

Expected: no whitespace errors; each implementation task is a focused commit; user-owned map/progression changes remain preserved. Do not stage `.superpowers/brainstorm/` artifacts.

## Self-Review Checklist

- Spec coverage: Task 1 covers responsive geometry; Task 2 covers scoreboard, runner alignment, and player identity; Task 3 covers controls and feedback; Task 4 covers tutorial/countdown/instruction; Task 5 covers finish and results; Task 6 covers static, responsive, automated, and Android verification.
- Placeholder scan: no `TBD`, `TODO`, “similar to”, unnamed test, or unspecified code step remains.
- Type consistency: `SprintUiLayout`, `SprintControlPresenter`, `SprintFinishLinePresenter`, `SprintStartPresentation`, and `SprintResultPresentation` signatures are defined once and consumed consistently.
- State ownership: all new components are presentation-only; existing controller, rules, lifecycle input gate, router, result action, and progression route remain authoritative.
- Scope: all runtime presentation changes are isolated to `MG_Sprint`; unrelated dirty files remain untouched except `FestivalUiExperienceTests.cs`, whose existing user changes must be merged deliberately rather than overwritten.
