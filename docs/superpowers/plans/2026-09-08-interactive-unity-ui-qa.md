# Interactive Unity UI QA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the warm Unity screenshot bridge so repeatable scenarios can operate menus and gameplay through named UI targets, normalized pointer gestures, and keyboard input before capturing visual checkpoints.

**Architecture:** Keep `PlayModeScreenshot` as the request-file coordinator, but move the scenario contract, target resolution, input dispatch, and action sequencing into focused Editor-only classes. A new shell client submits complete JSON scenarios while the original static screenshot client remains backward compatible.

**Tech Stack:** Unity 6000.3.23f1, C# Editor assemblies, Unity Test Framework/NUnit, uGUI EventSystem, Unity Input System, Bash, `jq`.

**Spec:** `docs/superpowers/specs/2026-09-08-interactive-unity-ui-qa-design.md`

## Global Constraints

- Run every shell command through `rtk`; use `rtk proxy` when RTK has no adapter.
- Use the existing checkout directly. Do not create a worktree unless the user explicitly requests one.
- Preserve the current `tools/qa-screenshot.sh <output.png> [scene.unity] [waitSeconds]` interface and request behavior.
- Drive UI/gameplay through EventSystem/Input System paths; never call application button callbacks or gameplay methods directly.
- Do not add OCR, arbitrary method invocation, game-state mutation, APK automation, or Android rendering claims.
- Stop on the first failed action, release held input, leave Play Mode, and report the failing zero-based action index.
- A written PNG is not a visual pass; every requested checkpoint used as evidence must be inspected.
- Unity tests must omit trailing `-quit`, matching `README.md`.
- Unity test runs may mutate `Assets/_Project/Fonts/Nunito-Bold.asset`; preserve any pre-existing user change and do not commit test-generated atlas changes.

## File map

- Create `Assets/Editor/UnityQaScenario.cs`: serializable request/action/result models and deterministic request validation.
- Create `Assets/Editor/UnityQaTargetResolver.cs`: unique active target/path lookup, target-to-screen conversion, and EventSystem raycast selection.
- Create `Assets/Editor/UnityQaInputDriver.cs`: pointer/key lifecycle with guaranteed release.
- Create `Assets/Editor/UnityQaActionRunner.cs`: frame-driven ordered action state machine and checkpoint capture coordination.
- Modify `Assets/Editor/PlayModeScreenshot.cs`: request polling, legacy adapter, SessionState persistence, runner lifecycle, and result writing.
- Modify `Assets/Editor/KMA.EditorTools.asmdef`: add `Unity.InputSystem`.
- Create `Assets/Tests/EditMode/EditorTools/*`: contract, validation, and resolver tests plus an Editor-only test assembly.
- Create `Assets/Tests/PlayMode/EditorTools/*`: real EventSystem/Input System action tests plus a PlayMode test assembly.
- Create `tools/qa-unity-ui.sh`: scenario submission, timeout calculation, and result propagation.
- Create `tools/tests/qa-unity-ui-test.sh`: isolated shell contract tests using a fake completion writer.
- Modify `.claude/skills/testing-unity-ui-with-screenshots/SKILL.md`: route static versus interactive QA and document the scenario workflow.

---

### Task 1: Scenario contract and validation

**Files:**
- Create: `Assets/Editor/UnityQaScenario.cs`
- Modify: `Assets/Editor/KMA.EditorTools.asmdef`
- Create: `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef`
- Create: `Assets/Tests/EditMode/EditorTools/UnityQaScenarioTests.cs`
- Unity-generated metadata: matching `.meta` files

**Interfaces:**
- Produces: `UnityQaRequest`, `UnityQaAction`, `UnityQaResult`, `UnityQaScenarioValidator.Validate(UnityQaRequest request)`.
- `Validate` returns `null` for valid requests or an exact diagnostic string for invalid input.
- Later tasks consume `UnityQaAction.type`, `target`, `position`, `from`, `to`, `seconds`, `duration`, `key`, and `output`.

- [ ] **Step 1: Create the Editor test assembly and failing contract tests**

Create an Editor-only test asmdef referencing `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `KMA.EditorTools`, `Unity.InputSystem`, and `UnityEngine.UI`, with `nunit.framework.dll`, `overrideReferences: true`, and `UNITY_INCLUDE_TESTS`.

Write these core tests, then add one test for each validation rule listed in Step 3:

```csharp
[Test]
public void LegacyRequestWithoutActions_IsValid()
{
    var request = JsonUtility.FromJson<UnityQaRequest>(
        "{\"id\":\"legacy\",\"output\":\"Builds/Screenshots/a.png\",\"waitSeconds\":3}");
    Assert.That(UnityQaScenarioValidator.Validate(request), Is.Null);
}

[TestCase("{\"id\":\"x\",\"actions\":[{\"type\":\"tap\",\"target\":\"Play\",\"position\":[.5,.5]}]}",
    "Action 0 must specify exactly one locator")]
[TestCase("{\"id\":\"x\",\"actions\":[{\"type\":\"swipe\",\"from\":[-.1,.5],\"to\":[.5,.5],\"duration\":.2}]}",
    "Action 0 from must contain coordinates within 0..1")]
[TestCase("{\"id\":\"x\",\"actions\":[{\"type\":\"dance\"}]}",
    "Action 0 has unsupported type: dance")]
public void InvalidAction_ReturnsIndexedDiagnostic(string json, string expected)
{
    Assert.That(UnityQaScenarioValidator.Validate(JsonUtility.FromJson<UnityQaRequest>(json)),
        Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run:

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter UnityQaScenarioTests \
  -testResults /tmp/kma-unity-qa-contract-red.xml -logFile /tmp/kma-unity-qa-contract-red.log
```

Expected: compilation fails because `UnityQaRequest` and `UnityQaScenarioValidator` do not exist.

- [ ] **Step 3: Implement the minimal serializable contract and validator**

Use public serializable fields so `JsonUtility` supports arrays without a custom JSON package:

```csharp
[Serializable]
public sealed class UnityQaRequest
{
    public string id;
    public string scene;
    public string output;
    public float waitSeconds = 3f;
    public UnityQaAction[] actions;
}

[Serializable]
public sealed class UnityQaAction
{
    public string type;
    public string target;
    public float[] position;
    public float[] from;
    public float[] to;
    public float seconds;
    public float duration;
    public string key;
    public string output;
}

[Serializable]
public sealed class UnityQaResult
{
    public string id;
    public string status;
    public string message;
    public int failedActionIndex = -1;
    public float elapsedSeconds;
    public string[] screenshots;
}
```

Implement action-specific validation: finite/non-negative waits and durations; exactly one `target`/`position` for tap and hold; two valid normalized vectors for swipe; non-empty key for key; non-empty output for capture; and supported types only. Add `Unity.InputSystem` to `KMA.EditorTools.asmdef` now so later public signatures compile without another assembly-contract change.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Re-run the Step 2 command with `-testResults /tmp/kma-unity-qa-contract-green.xml`. Expected: all `UnityQaScenarioTests` pass.

- [ ] **Step 5: Commit the contract**

```bash
rtk git add Assets/Editor/UnityQaScenario.cs Assets/Editor/UnityQaScenario.cs.meta \
  Assets/Editor/KMA.EditorTools.asmdef Assets/Tests/EditMode/EditorTools
rtk git commit -m "feat: define Unity QA interaction scenarios"
```

---

### Task 2: Target resolution and pointer routing

**Files:**
- Create: `Assets/Editor/UnityQaTargetResolver.cs`
- Create: `Assets/Editor/UnityQaInputDriver.cs`
- Modify: `Assets/Tests/EditMode/EditorTools/UnityQaScenarioTests.cs`
- Create: `Assets/Tests/PlayMode/EditorTools/KMA.EditorTools.PlayMode.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/EditorTools/UnityQaPointerTests.cs`
- Unity-generated metadata: matching `.meta` files

**Interfaces:**
- Produces: `UnityQaTargetResolver.Resolve(string locator, EventSystem eventSystem, out ResolvedQaTarget target)`.
- Produces: `UnityQaTargetResolver.NormalizedToScreen(float[] position)`.
- Produces: `UnityQaInputDriver.PointerDown(Vector2)`, `PointerMove(Vector2)`, `PointerUp(Vector2)`, and `ReleaseAll()`.
- `ResolvedQaTarget` contains the resolved `GameObject`, screen position, and `RaycastResult`.

- [ ] **Step 1: Write failing resolver and pointer tests**

Cover unique active object names, hierarchy paths such as `Canvas/Menu/PLAYButton`, duplicate leaf names, inactive targets, normalized conversion, disabled `Selectable` targets, a real uGUI `Button`, and a real `ScreenTapArea`.

The central PlayMode assertions should be:

```csharp
driver.PointerDown(resolved.ScreenPosition);
driver.PointerUp(resolved.ScreenPosition);
Assert.That(clickCount, Is.EqualTo(1));

driver.PointerDown(new Vector2(Screen.width * .75f, Screen.height * .2f));
driver.PointerMove(new Vector2(Screen.width * .75f, Screen.height * .7f));
driver.PointerUp(new Vector2(Screen.width * .75f, Screen.height * .7f));
Assert.That(receivedDown, Is.True);
Assert.That(receivedMove, Is.True);
Assert.That(receivedUp, Is.True);
```

Create the PlayMode test asmdef with references to `UnityEngine.TestRunner`, `KMA.EditorTools`, `KMA.Input`, `Unity.InputSystem`, `Unity.InputSystem.TestFramework`, and `UnityEngine.UI`; include `nunit.framework.dll` and `UNITY_INCLUDE_TESTS`.

- [ ] **Step 2: Run resolver and pointer tests and confirm RED**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter UnityQaScenarioTests \
  -testResults /tmp/kma-unity-qa-resolver-red.xml -logFile /tmp/kma-unity-qa-resolver-red.log
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter UnityQaPointerTests \
  -testResults /tmp/kma-unity-qa-pointer-red.xml -logFile /tmp/kma-unity-qa-pointer-red.log
```

Expected: missing resolver/driver types cause compilation failures.

- [ ] **Step 3: Implement target resolution and a stateful EventSystem driver**

Resolve only loaded-scene, active-in-hierarchy GameObjects. For a leaf-name locator, require exactly one match. For a slash path, match the complete root-to-leaf transform path. Require a `RectTransform`, convert its world corners to screen coordinates using the canvas camera, and raycast at the center. Reject a raycast whose first eligible handler is not the resolved target or its child, and reject non-interactable `Selectable` controls.

The pointer driver owns one stable negative pointer ID and one `PointerEventData`. On down, populate `position`, `pressPosition`, `pointerCurrentRaycast`, and dispatch `pointerDownHandler` to the raycast result. Preserve the returned press handler for drag/up. On move, update delta/position, dispatch `beginDragHandler` once and then `dragHandler`. On up, dispatch `pointerUpHandler`, then `pointerClickHandler` only when press and release resolve to the same click handler. `ReleaseAll()` sends a pending up/cancel path and clears all state.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Re-run both Step 2 commands with `green` result filenames. Expected: resolver and pointer suites pass; each tap is delivered exactly once.

- [ ] **Step 5: Commit pointer interaction support**

```bash
rtk git add Assets/Editor/UnityQaTargetResolver.cs* Assets/Editor/UnityQaInputDriver.cs* \
  Assets/Tests/EditMode/EditorTools Assets/Tests/PlayMode/EditorTools
rtk git commit -m "feat: drive Unity QA pointer interactions"
```

---

### Task 3: Keyboard input and ordered action runner

**Files:**
- Create: `Assets/Editor/UnityQaActionRunner.cs`
- Modify: `Assets/Editor/UnityQaInputDriver.cs`
- Create: `Assets/Tests/EditMode/EditorTools/UnityQaActionRunnerTests.cs`
- Modify: `Assets/Tests/PlayMode/EditorTools/UnityQaPointerTests.cs`
- Unity-generated metadata: matching `.meta` files

**Interfaces:**
- Extends: `UnityQaInputDriver.KeyDown(string keyName)`, `KeyUp()`, and `ReleaseAll()`.
- Produces: `UnityQaActionRunner.Start(UnityQaRequest request, double now)` and `Tick(double now)`.
- Produces callbacks `Action<string> CaptureRequested`, `Action<UnityQaResult> Completed`, and `Action<UnityQaResult> Failed` so capture/file coordination stays outside the runner.

- [ ] **Step 1: Write failing runner tests with controllable time**

Inject clock, resolver, input-driver, and capture collaborators. Test: strict action order; wait does not use scaled game time; tap spans at least one tick; hold remains down until duration; swipe interpolates and ends exactly at `to`; key press/release; capture pauses until acknowledged; first error reports its index; `Cancel` releases input.

Include a PlayMode Input System assertion:

```csharp
var keyboard = InputSystem.AddDevice<Keyboard>();
driver.KeyDown("space");
InputSystem.Update();
Assert.That(keyboard.spaceKey.isPressed, Is.True);
driver.KeyUp();
InputSystem.Update();
Assert.That(keyboard.spaceKey.isPressed, Is.False);
```

- [ ] **Step 2: Run focused tests and confirm RED**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter UnityQaActionRunnerTests \
  -testResults /tmp/kma-unity-qa-runner-red.xml -logFile /tmp/kma-unity-qa-runner-red.log
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter UnityQaPointerTests \
  -testResults /tmp/kma-unity-qa-key-red.xml -logFile /tmp/kma-unity-qa-key-red.log
```

Expected: missing runner and key methods fail compilation.

- [ ] **Step 3: Implement keyboard lifecycle and the frame state machine**

Resolve keys with `KeyControl` lookup on `Keyboard.current`; return an error for absent devices or unknown controls. Queue `KeyboardState` through `InputSystem.QueueStateEvent`, call `InputSystem.Update`, and track the pressed key so every completion/error/cancel calls `KeyUp`.

Implement one action phase per tick. Use Editor time supplied by the coordinator. A capture action calls `CaptureRequested(output)` and remains pending until `AcknowledgeCapture(output)` is called. Enforce a per-action deadline of declared duration plus 10 seconds, or 10 seconds for instantaneous actions. Every terminal path calls `ReleaseAll()` once.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Re-run Step 2 with `green` result filenames. Expected: action-runner and keyboard tests pass.

- [ ] **Step 5: Commit the action runner**

```bash
rtk git add Assets/Editor/UnityQaActionRunner.cs* Assets/Editor/UnityQaInputDriver.cs* \
  Assets/Tests/EditMode/EditorTools/UnityQaActionRunnerTests.cs* \
  Assets/Tests/PlayMode/EditorTools/UnityQaPointerTests.cs
rtk git commit -m "feat: sequence Unity QA input actions"
```

---

### Task 4: Integrate scenarios with the warm Editor bridge

**Files:**
- Modify: `Assets/Editor/PlayModeScreenshot.cs`
- Create: `Assets/Tests/EditMode/EditorTools/PlayModeScreenshotTests.cs`
- Modify: `Assets/Tests/PlayMode/EditorTools/UnityQaPointerTests.cs`

**Interfaces:**
- Consumes: scenario validator, action runner, and `UnityQaResult` from Tasks 1–3.
- Preserves: `Run()` and legacy request JSON.
- Produces: atomic `done.json` including `failedActionIndex`, `elapsedSeconds`, and `screenshots`.

- [ ] **Step 1: Write failing bridge tests**

Add internal static `BuildFailure`, `BuildSuccess`, and `AppendScreenshot` helpers and test that legacy requests adapt to the old wait/final-capture sequence, interactive requests preserve action order, screenshot paths accumulate in completion order, malformed requests fail before entering Play Mode, and unexpected Play Mode exit returns an error and releases input.

Assert the result contract:

```csharp
UnityQaResult result = PlayModeScreenshot.BuildFailure(
    "run-123", 1, "Target is missing or ambiguous: PLAYButton", 2.1f,
    Array.Empty<string>());
Assert.That(result.status, Is.EqualTo("error"));
Assert.That(result.failedActionIndex, Is.EqualTo(1));
Assert.That(result.elapsedSeconds, Is.EqualTo(2.1f));
```

- [ ] **Step 2: Run focused tests and confirm RED**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter "PlayModeScreenshotTests|UnityQaScenarioTests|UnityQaActionRunnerTests" \
  -testResults /tmp/kma-unity-qa-bridge-red.xml -logFile /tmp/kma-unity-qa-bridge-red.log
```

Expected: the new bridge helpers and extended result fields are absent.

- [ ] **Step 3: Replace the single-capture phase flags with runner persistence**

Persist the active request JSON, start time, accumulated screenshot paths, final-capture phase, and action index/state needed across domain reloads in `SessionState`. After Play Mode starts, construct the runner and restore its state before ticking. For each capture, call `ScreenCapture.CaptureScreenshot`, wait until the file exists and has non-zero length for five rendered frames, append its absolute path once, then acknowledge it. After actions, honor request-level `waitSeconds` and `output`. If there is no final output, finish without adding an implicit capture.

Centralize `Complete` and `Fail`: release input, clear all active SessionState keys, request exit from Play Mode, and atomically write exactly one `done.json`. Keep `lastRequestId` so the same request is never replayed.

- [ ] **Step 4: Run bridge and interaction regression tests**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode -testFilter "UnityQaScenarioTests|UnityQaActionRunnerTests|PlayModeScreenshotTests" \
  -testResults /tmp/kma-unity-qa-bridge-green.xml -logFile /tmp/kma-unity-qa-bridge-green.log
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode -testFilter UnityQaPointerTests \
  -testResults /tmp/kma-unity-qa-interaction-green.xml -logFile /tmp/kma-unity-qa-interaction-green.log
```

Expected: both focused runs pass and no input remains pressed after teardown.

- [ ] **Step 5: Commit bridge integration**

```bash
rtk git add Assets/Editor/PlayModeScreenshot.cs Assets/Tests/EditMode/EditorTools/PlayModeScreenshotTests.cs*
rtk git commit -m "feat: execute scenarios in Unity screenshot bridge"
```

---

### Task 5: Interactive shell client

**Files:**
- Create: `tools/qa-unity-ui.sh`
- Create: `tools/tests/qa-unity-ui-test.sh`

**Interfaces:**
- Consumes: one scenario JSON path.
- Produces: atomic `Builds/Screenshots/request.json`, printed matching `done.json`, and exit `0` only for `status == "ok"`.

- [ ] **Step 1: Write the failing isolated shell contract test**

The test must run in a `mktemp -d` project-shaped directory, copy the client there, and use a background fake Editor that waits for `request.json` then writes matching `done.json`. Assert: source JSON checksum is unchanged; generated ID differs from a source ID; actions are preserved; the client's `Requested ... timeout=<n>s` line reports the sum of waits/durations plus 60 seconds; success returns zero; error status returns non-zero; missing files and malformed JSON fail before a request is written.

Use this observable check for action preservation:

```bash
jq -e '.actions == [
  {"type":"tap","target":"PLAYButton"},
  {"type":"wait","seconds":1.5}
]' "$sandbox/Builds/Screenshots/request.json"
```

- [ ] **Step 2: Run the shell test and confirm RED**

```bash
rtk proxy bash tools/tests/qa-unity-ui-test.sh
```

Expected: FAIL because `tools/qa-unity-ui.sh` does not exist.

- [ ] **Step 3: Implement the minimal Bash client**

Use `jq -e` to validate the root object and actions array, generate an ID with `date +%s%N`, and set it with `jq --arg id '.id = $id'`. Calculate the client deadline from the sum of positive `seconds`/`duration` values plus 60 seconds, clamped to at least 60 and at most 900 seconds, and print it in `Requested <file> (id=<id>, timeout=<n>s)`. Write to `request.json.tmp` and rename atomically. Poll once per second for a matching result ID, print it, and decide the exit status using `jq -e '.status == "ok"'`.

- [ ] **Step 4: Run shell tests and static checks**

```bash
rtk proxy bash tools/tests/qa-unity-ui-test.sh
rtk proxy bash -n tools/qa-unity-ui.sh
rtk proxy bash -n tools/tests/qa-unity-ui-test.sh
```

Expected: all commands exit zero.

- [ ] **Step 5: Commit the client**

```bash
rtk git add tools/qa-unity-ui.sh tools/tests/qa-unity-ui-test.sh
rtk git commit -m "feat: add interactive Unity QA client"
```

---

### Task 6: Rewrite and behavior-test the repository skill

**Files:**
- Modify: `.claude/skills/testing-unity-ui-with-screenshots/SKILL.md`

**Interfaces:**
- Documents: static capture with `qa-screenshot.sh`; interactive scenarios with `qa-unity-ui.sh`; checkpoint inspection; result/error interpretation.

- [ ] **Step 1: Run a RED skill-behavior scenario against the current skill**

Because the skill-authoring workflow explicitly requires independent behavioral testing, give a fresh subagent only the current skill and this request:

```text
Use the repository skill to verify that pressing PLAY opens the map, swiping the gameplay area works, and the post-swipe HUD looks correct. Describe the exact request artifact and commands you would use. Do not modify files or launch Unity.
```

Record its response in the task notes. Expected RED: it can only propose a static screenshot, manual interaction, or an undocumented request shape; it cannot produce the approved ordered scenario and checkpoint workflow.

- [ ] **Step 2: Rewrite the skill around two explicit modes**

Keep the frontmatter description trigger-only and under 500 characters. In the body:

- route a single static frame to `tools/qa-screenshot.sh`;
- route any navigation, UI state transition, tap/hold/swipe/key, or multiple checkpoint request to `tools/qa-unity-ui.sh`;
- provide one complete JSON scenario using a named `PLAYButton`, normalized swipe, and capture action;
- define named-target uniqueness and normalized-coordinate rules;
- require reading each PNG and correlating it with `done.json`;
- explain first-failure diagnostics and the action index;
- retain single-Editor locking, GUI/non-batchmode setup, warm-start polling, no arbitrary callbacks, and no Android-specific guarantees.

- [ ] **Step 3: Validate structure and rerun the behavior scenario (GREEN)**

```bash
rtk proxy python3 /home/duongduy/.codex/skills/.system/skill-creator/scripts/quick_validate.py \
  .claude/skills/testing-unity-ui-with-screenshots
rtk wc -w .claude/skills/testing-unity-ui-with-screenshots/SKILL.md
```

Then give a fresh subagent the same Step 1 request plus the revised skill. Expected GREEN: it chooses `qa-unity-ui.sh`, emits a valid ordered scenario using both target modes, calls for checkpoint inspection, and states the limitations without inventing callback execution.

- [ ] **Step 4: Commit the verified skill**

```bash
rtk git add .claude/skills/testing-unity-ui-with-screenshots/SKILL.md
rtk git commit -m "docs: teach interactive Unity visual QA"
```

---

### Task 7: End-to-end verification and visual smoke scenario

**Files:**
- Create after the real smoke flow passes: `tools/qa-scenarios/menu-to-map.json`
- No file change when GUI Unity is unavailable or the smoke flow fails; report the runtime gate instead.

**Interfaces:**
- Verifies all interfaces from Tasks 1–6 without expanding the action vocabulary.

- [ ] **Step 1: Run focused and full automated gates**

```bash
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults /tmp/kma-unity-qa-full-editmode.xml -logFile /tmp/kma-unity-qa-full-editmode.log
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/kma-unity-qa-full-playmode.xml -logFile /tmp/kma-unity-qa-full-playmode.log
rtk proxy bash tools/tests/qa-unity-ui-test.sh
```

Expected: all suites pass. Compare the font asset before/after and restore only test-generated atlas churn; never overwrite a pre-existing user edit.

- [ ] **Step 2: Run a real warm-Editor interaction scenario when GUI Unity is available**

Confirm there is no competing Editor, launch Unity 6000.3.23f1 as described by the skill, then submit a scenario that opens `Menu.unity`, waits for startup, taps the unique `PLAYButton`, waits for `Map.unity`, and captures `Builds/Screenshots/menu-to-map.png`.

```bash
rtk proxy tools/qa-unity-ui.sh /tmp/kma-menu-to-map.json
```

Expected: `done.json` reports `status: ok`, action index is absent or `-1`, and lists the PNG. If GUI Unity is unavailable, record this exact runtime gate as unverified rather than substituting a batchmode claim.

- [ ] **Step 3: Inspect every generated checkpoint**

Open each PNG with an image-viewing tool. Confirm the expected screen is visible, the image is non-empty, no loading overlay obscures the target state, and the tested UI has no clipping, overlap, missing text, or incorrect colors. A successful `done.json` without this inspection is not a pass.

- [ ] **Step 4: Run repository hygiene checks**

```bash
rtk git diff --check
rtk git status --short
rtk proxy python3 /home/duongduy/.codex/skills/.system/skill-creator/scripts/quick_validate.py \
  .claude/skills/testing-unity-ui-with-screenshots
```

Expected: no whitespace errors, only intended files remain changed, and the skill validates.

- [ ] **Step 5: Commit any stable smoke scenario or final documentation correction**

If and only if Step 2 returned `status: ok` and visual inspection passed, copy the exact proven request minus its generated `id` to `tools/qa-scenarios/menu-to-map.json`, then commit it:

```bash
rtk git add tools/qa-scenarios/menu-to-map.json
rtk git commit -m "test: add Unity UI interaction smoke scenario"
```

When Step 2 cannot run or Step 3 fails visual inspection, skip this commit and include the reason in the final verification report.
