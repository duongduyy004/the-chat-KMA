# Task 2 Report: Shared Sprint input wiring

## Status

DONE_WITH_CONCERNS

## Implementation

- `GameplayInputRouter` owns the Sprint-specific `AlternateTapInputDetector`, binds `SprintLeft` and `SprintRight` from the shared `Sprint` action map, and raises `OnSprintValidTap` only after detector validation.
- `SprintController` subscribes to that valid-tap event from its own `KMA.Gameplay.Sprint` assembly. This preserves the one-way assembly dependency (`KMA.Gameplay.Sprint` -> `KMA.Input`) and avoids an invalid reverse dependency.
- `MG_Sprint` now references `KMA.inputactions`, has the controller's legacy direct input reader disabled, and configures one router plus two transparent lower-screen `ScreenTapArea` targets. This prevents the controller and router from reading the same tap.
- `KMA.inputactions` already exposed the required shared `Sprint` map with `SprintLeft` and `SprintRight`; it was validated and intentionally not rewritten.
- The Task 1 runtime test now uses the router/controller event boundary rather than manually wiring a detector callback in the test.

## Files changed

- `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/KMA.Gameplay.Sprint.asmdef`
- `Assets/_Project/Scenes/MG_Sprint.unity`
- `Assets/Tests/PlayMode/Gameplay/Running/SprintRuntimeInputTests.cs`

## Verification

1. RED test command:

   `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task2-play-red.xml -logFile /tmp/kma-s6-task2-play-red.log -testFilter "SprintRuntimeInputTests" -quit`

   Result: expected compiler failure `CS1061`: `GameplayInputRouter` did not contain `ConfigureSprintForTest`.

2. Requested focused commands were attempted after the first implementation pass:

   - EditMode (`AlternateTapInputDetectorTests`) compiled far enough to reveal the assembly-boundary error: `KMA.Input` cannot reference `KMA.Gameplay`.
   - PlayMode (`SprintRuntimeInputTests|SprintControllerTests`) did not start because Unity reported another instance held the project lock.

   The implementation was then corrected to the one-way event bridge described above. Per the requested stop condition, Unity was not retried after that correction.

3. Static checks after the final correction:

   - `rtk git diff --check` — empty output, exit code 0.
   - `rtk jq empty Assets/_Project/Settings/Input/KMA.inputactions` — exit code 0.
   - `rtk jq empty Assets/_Project/Scripts/Gameplay/Sprint/KMA.Gameplay.Sprint.asmdef` — exit code 0.
   - Static scene checks confirmed the shared asset GUID, `directInputEnabled: 0`, router reference, `sprintRoutingEnabled: 1`, both `ScreenTapArea` components, and the `SprintLeft`/`SprintRight` action names.

## Self-review

- The input assembly no longer references gameplay, so the Unity compile failure's root cause is removed.
- Only valid detector events can reach the controller event handler; wrong-side taps do not invoke it.
- The scene has no enabled second controller input path for `SprintLeft`/`SprintRight`.
- Existing generic router tests retain `SetDetectors`; Sprint-owned routing is opt-in and therefore does not replace their injected detector on enable/disable.

## Implementation commit

`f6a2cd658549ae5090e2170bcfe3cf8271141f52` (`feat: wire sprint through shared input layer`)

## Concerns

- No authoritative Unity Test Runner XML is available for the final correction. The final source/scene state has static validation only.
- Unity reported a missing licensing access-token update, and the prior S6 runs consistently produced no Test Runner XML; the post-correction PlayMode attempt also encountered Unity's single-project lock. These are environment limitations, not a recorded test pass or fail.

## Fix round 1

### Addressed findings

1. `MG_Sprint`'s `Input` Canvas root `RectTransform` now serializes `m_LocalScale: {x: 1, y: 1, z: 1}`. The existing landscape `CanvasScaler` (`1920x1080`) and lower-left/lower-right anchored tap rectangles are preserved.
2. `SprintRuntimeInputTests` now contains `SprintScene_InputCanvasRoutesKeyboardAndScreenTapWithoutDuplicateControllerInput`. It loads `MG_Sprint`, forces canvas layout, asserts a live unit-scale canvas and two usable `ScreenTapArea` transforms, confirms the controller direct action path is disabled, verifies one left-arrow action adds exactly one 18-point impulse, and invokes the right scene tap area through the loaded `EventSystem` for a second exact one-impulse assertion.

### Commands and output

1. RED command before the scene correction:

   `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task2-fix1-red.xml -logFile /tmp/kma-s6-task2-fix1-red.log -testFilter "SprintRuntimeInputTests.SprintScene_InputCanvasRoutesKeyboardAndScreenTapWithoutDuplicateControllerInput" -quit`

   Process exit code: `0`; stdout: empty. `/tmp/kma-s6-task2-fix1-red.xml` was not created. The log contains `Batchmode quit successfully invoked - shutting down!` but no `test-suite`, `test-case`, pass, or fail record, so the intended zero-scale assertion was not authoritatively executed.

2. Focused rerun after the scene correction:

   `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task2-fix1-green.xml -logFile /tmp/kma-s6-task2-fix1-green.log -testFilter "SprintRuntimeInputTests.SprintScene_InputCanvasRoutesKeyboardAndScreenTapWithoutDuplicateControllerInput" -quit`

   Process exit code: `0`; stdout: empty. `/tmp/kma-s6-task2-fix1-green.xml` was not created. The log again contains successful batchmode shutdown but no Test Runner cases or XML. Both logs report `Licensing::Module Error: Access token is unavailable; failed to update` and `No .NET SDKs were found.`

3. Static checks:

   - `rtk git diff --check` — empty output, exit code 0.
   - Static scene inspection confirmed `Input` root scale `{x: 1, y: 1, z: 1}`, both child areas at unit scale, and the preserved `1920x1080` landscape scaler/left-right anchors.
   - Self-review confirmed the new test’s exact `+18f` keyboard assertion detects an additional controller action subscriber, while its `ScreenTapArea.OnPointerDown` path verifies the loaded scene EventSystem route.

### Implementation commit

`86c8a6e5d66dc3a118c2fd7526fe74f6feaac0c6` (`fix: restore sprint tap canvas input`)

### Remaining concern

The Unity CLI continues to compile and exit successfully without producing authoritative Test Runner XML or executing visible test cases. The fix-round test is therefore statically reviewed and compiler-observed only, not recorded as a passing Unity test.

## Fix round 2

### Addressed finding

`SprintScene_InputCanvasRoutesKeyboardAndScreenTapWithoutDuplicateControllerInput` no longer calls `ScreenTapArea.OnPointerDown` or `OnPointerUp` directly. It now creates a real `PointerEventData`, calls `EventSystem.RaycastAll` against the loaded `MG_Sprint` canvas, requires the raycast hit to be the right tap target, assigns that hit as `pointerCurrentRaycast`, and dispatches pointer down/up with `ExecuteEvents.pointerDownHandler` and `ExecuteEvents.pointerUpHandler`. The one-pointer-action assertion remains an exact `+18f` controller speed impulse.

### Command and output

`rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task2-fix2.xml -logFile /tmp/kma-s6-task2-fix2.log -testFilter "SprintRuntimeInputTests.SprintScene_InputCanvasRoutesKeyboardAndScreenTapWithoutDuplicateControllerInput" -quit`

Process exit code: `0`; stdout: empty. `/tmp/kma-s6-task2-fix2.xml` was not created. The log contains `Batchmode quit successfully invoked - shutting down!` but no Test Runner `test-suite`, `test-case`, pass, or fail records. It also contains `Licensing::Module Error: Access token is unavailable; failed to update` and `No .NET SDKs were found.` No compiler error appears in the log.

Static checks:

- `rtk git diff --check` — empty output, exit code 0.
- Static test inspection confirms `EventSystem.RaycastAll`, `ExecuteEvents.pointerDownHandler`, `ExecuteEvents.pointerUpHandler`, and the exact `speedBeforeScreenTap + 18f` assertion.

### Implementation commit

`71d1b2bc86d4eca41f1c6fdc37d45512d899cc1c` (`test: dispatch sprint taps through event system`)

### Remaining concern

The focused EventSystem integration contract could compile and start Unity batchmode, but the environment still produced no authoritative Unity Test Runner XML or case-level result. It is not reported as a passing runtime test.
