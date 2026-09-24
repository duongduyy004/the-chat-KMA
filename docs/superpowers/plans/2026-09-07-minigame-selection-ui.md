# Minigame Selection UI Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the gameplay overlay leak from `Map` and replace the broken fixed-position white rectangles with a responsive, Vietnamese minigame-selection screen.

**Architecture:** Treat `Map` explicitly as a shell scene in the editor assembler, while keeping camera/canvas/safe-area/event-system infrastructure. Move runtime Map construction into a focused `MapPresentationBuilder` that creates themed uGUI hierarchy with layout groups and configures `MapNodeView` through its existing presentation contract; `S5ShellSceneController` only wires routing and delegates construction.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, uGUI, Unity Test Framework PlayMode, Android landscape `1920x1080` CanvasScaler.

**Spec:** `docs/superpowers/specs/2026-09-07-minigame-selection-ui-design.md`

## Global Constraints

- Work directly in the current workspace and preserve the pre-existing deleted `.worktrees/*` entries.
- Run every shell command through `rtk`.
- Use TDD: observe every new regression test fail for the intended reason before production edits.
- `Map` keeps its camera, CanvasScaler `1920x1080` with height match, `SafeAreaFitter`, GraphicRaycaster and EventSystem.
- `Map` must not contain active `MinigameHUD`, `PhaseOverlay`, `ResultPanel` or `PausePanel` presentation.
- PushUps remains presentation-only future content and does not emit a `SubjectRequested` route.
- Use the current uGUI stack and `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`; add no dependency or bitmap asset.
- Do not change progression rules, `SubjectId`, or `SceneRouter` route mappings.

---

## File Map

| File | Responsibility |
|---|---|
| `Assets/_Project/Scripts/Shell/S5ShellSceneController.cs` | Discover shell screens, wire route events and delegate Map construction. |
| `Assets/_Project/Scripts/UI/MapNodeView.cs` | Apply progress/availability state and expose read-only UI state for regression tests. |
| `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs` | Distinguish shell Map assembly from gameplay-scene assembly and remove leaked prefab roots. |
| `Assets/_Project/Scenes/Map.unity` | Persist the repaired shell scene without gameplay overlays. |
| `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs` | Validate node semantics and the real Map scene presentation. |

### Task 1: Lock the Map regression contract

**Files:**
- Modify: `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`

**Interfaces:**
- Consumes current `MapNodeView.Configure`, `MapScreen.Nodes`, `MapScreen.Hearts`, and the build-enabled `Map` scene.
- Produces tests named `MapNode_ReportsReadyCompletedAndUnavailableStates` and `MapScene_ContainsOnlyResponsiveSelectionPresentation`.

- [x] **Step 1: Extend the node test with exact state assertions.** Bind a real `Button` plus title/detail `Text` components using `LegacyRuntime.ttf`, then configure these three cases:

- [x] **Step 2: Add a real-scene UnityTest.** Load `Map` in `LoadSceneMode.Single`, yield one frame for `Awake`, and assert:

```csharp
Assert.That(Object.FindObjectsByType<MinigameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
Assert.That(Object.FindObjectsByType<PhaseOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
Assert.That(Object.FindObjectsByType<ResultPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

var screen = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
Assert.That(screen.transform.Find("S5MapPresentation"), Is.Not.Null);
Assert.That(screen.Nodes.Count(node => node.IsInteractable), Is.EqualTo(3));
Assert.That(screen.Nodes.Count(node => node.DetailText == "ĐANG PHÁT TRIỂN"), Is.EqualTo(4));
Assert.That(GameObject.Find("SelectionGrid").GetComponent<GridLayoutGroup>(), Is.Not.Null);
```

- [x] **Step 3: Run the focused test and verify RED.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "S5NewGameTests" -testResults /tmp/kma-map-ui-red.xml -logFile /tmp/kma-map-ui-red.log
```

Expected: compilation first fails because `DetailText` and `IsInteractable` do not exist; after adding only those read-only seams if needed to reach runtime RED, the scene assertion fails because gameplay overlays are present and the current presentation has no `SelectionGrid`.

- [x] **Step 4: Commit the red contract only if repository policy permits red-test commits.** Otherwise leave it unstaged and proceed directly to Task 2. Commit message, when used: `test: lock minigame selection presentation contract`.

### Task 2: Build the responsive selection presentation

**Files:**
- Create: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs`
- Create: `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs.meta`
- Modify: `Assets/_Project/Scripts/UI/MapNodeView.cs`
- Modify: `Assets/_Project/Scripts/Shell/S5ShellSceneController.cs:35-49,179-249`
- Test: `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`

**Interfaces:**
- Produces `public static void MapPresentationBuilder.Build(MapScreen screen, GameSession session)`.
- Produces `MapNodeView.DetailText`, `MapNodeView.IsInteractable`, and `MapNodeView.SetAvailability(bool selectable, string unavailableLabel)` as additive APIs.

- [x] **Step 1: Add the smallest observable node seam.** Add read-only properties and keep the serialized fields private:

```csharp
public string DetailText => detailLabel == null ? string.Empty : detailLabel.text;
public bool IsInteractable => button != null && button.interactable;
```

Change normal, unfinished copy from `LOCKED` to `SẴN SÀNG`, passed copy to `HẠNG {BestRank}  ★ {Stars}`, and coming-soon copy to `ĐANG PHÁT TRIỂN`. Add:

```csharp
public void SetAvailability(bool selectable, string unavailableLabel)
{
    if (button != null) button.interactable = selectable;
    if (!selectable && detailLabel != null) detailLabel.text = unavailableLabel;
}
```

- [x] **Step 2: Create the builder hierarchy.** `Build` must be idempotent (`screen.transform.Find("S5MapPresentation")` short-circuit), stretch the root to its parent, and create this hierarchy using `RectTransform`, `VerticalLayoutGroup`, `HorizontalLayoutGroup`, `GridLayoutGroup`, `LayoutElement`, `Image`, `Outline`, `Button`, and `Text`:

Every `Text` receives `LegacyRuntime.ttf`; every label has stretched anchors. Cards use white background, black outline, subject-colored header stripe, and muted tint when unavailable. Layout groups own all node positions; do not assign per-node `anchoredPosition`.

- [x] **Step 3: Define the exact catalog in the builder.** Use one immutable internal definition per visible campaign node:

Selectable entries call `screen.SelectSubject(entry.Subject)`; unavailable entries have no listener and call `SetAvailability(false, "ĐANG PHÁT TRIỂN")`. Bind only these seven campaign nodes into `MapScreen.Nodes`; future chips are presentation-only.

- [x] **Step 5: Delegate from the shell controller.** Replace the body of `BuildMapPresentation` and remove `CreateMapNode`:

```csharp
void BuildMapPresentation(GameSession session) => MapPresentationBuilder.Build(map, session);
```

- [x] **Step 6: Run the focused tests.** Use the Task 1 PlayMode command. Expected: node state assertions pass; the scene test still fails only on leaked gameplay-overlay components until Task 3.

- [x] **Step 7: Review the runtime hierarchy.** Confirm no generated `Text` has a null font, `SelectionGrid` has a `GridLayoutGroup`, all card labels fit within their card rect, and `S5MapPresentation` is created once after repeated enable/load paths.

- [x] **Step 8: Commit** `feat: build responsive minigame selection UI` after the Task 2 assertions pass.

### Task 3: Remove gameplay presentation from Map at the source

**Files:**
- Modify: `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs:18-101,220-300`
- Modify: `Assets/_Project/Scenes/Map.unity`
- Test: `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`

**Interfaces:**
- Produces `static bool IsGameplayScene(Scene scene)` returning true only for names beginning `MG_` or equal to `Punishment`.
- Produces `static void RemoveGameplayPresentation(Scene scene)` that deletes the exact roots `S2_HUD_Minigame`, `S2_PhaseOverlay`, `S2_ResultPanel`, and any `PausePanel` root from shell scenes.
- Keeps `EnsureSceneCamera` and `EnsureEventSystem` active for Map.

- [x] **Step 1: Add an assembler guard before prefab creation.** After camera creation, branch on scene type:

```csharp
if (!IsGameplayScene(scene))
{
    RemoveGameplayPresentation(scene);
    EnsureEventSystem(scene);
    EditorSceneManager.MarkSceneDirty(scene);
    return;
}
```

`RemoveGameplayPresentation` uses `FindInSceneByName` and `Object.DestroyImmediate`; it must tolerate missing roots and repeated assembly.

- [x] **Step 2: Run the editor assembler to repair serialized Map.** Invoke Unity with a small editor entry point or the existing menu method:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -executeMethod KMA.Gameplay.UI.MinigameUIAssembler.AssembleTask5Presentation -logFile /tmp/kma-map-assemble.log -quit
```

Expected: `Map.unity` no longer serializes the three `S2_*` gameplay roots; gameplay scenes retain them. Inspect the diff and revert any unrelated generated asset churn.

- [x] **Step 3: Run the focused Map tests and verify GREEN.** Run the Task 1 command. Expected: every `S5NewGameTests` case passes and the real scene contains zero gameplay presentation components.

- [x] **Step 4: Run presentation regression tests.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "ScenePresentationContractTests|GameplayPresentationTests|S5NewGameTests" -testResults /tmp/kma-map-presentation.xml -logFile /tmp/kma-map-presentation.log
```

Expected: all selected tests pass; minigame scenes still expose their gameplay HUD and Map still satisfies camera/canvas/safe-area contracts.

- [x] **Step 5: Commit** `fix: remove gameplay overlays from map scene` with only assembler, Map scene and regression-test changes owned by this task.

### Task 4: Verify the complete repair

**Files:**
- Verify: all modified production, test, scene and documentation files

**Interfaces:** The output is verification evidence only; no new runtime API.

- [x] **Step 1: Run the complete PlayMode suite.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-map-full-playmode.xml -logFile /tmp/kma-map-full-playmode.log
```

Expected: all tests pass, with zero unexpected errors in XML/log.

- [x] **Step 2: Build the Android artifact.** Run the repository's existing Android build method targeting `Builds/Android/kma-report.apk`; expected Unity exit code `0` and no compile/build error.

- [x] **Step 3: Inspect repository hygiene.** Run:

```bash
rtk git diff --check
rtk git status --short
```

Expected: `diff --check` is clean; only planned files plus the user's pre-existing `.worktrees/*` deletions appear. Restore generated font/import churn only when it is byte-for-byte unrelated to this feature.

- [x] **Step 4: Report visual verification limits precisely.** If no emulator/device screenshot is available, report that automated hierarchy, scene, PlayMode and APK checks passed but do not claim pixel-perfect device validation. If a device is available, capture Map at `1920x1080` landscape and confirm no tutorial/HUD overlap, no clipped Vietnamese text, and all seven cards fit in the safe area.

## Self-Review Checklist

- Placeholder scan: no `TBD`, `TODO`, “similar to”, or unspecified implementation/error-handling step remains.
- Type consistency: `MapPresentationBuilder.Build(MapScreen, GameSession)`, `DetailText`, `IsInteractable`, and `SetAvailability(bool, string)` are defined once and consumed under the same signatures.
- Scope: no rules, routing map, package, bitmap or unrelated shell behavior changes are included.
