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
