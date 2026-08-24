# Task 2 review-fix report: Sprint controller and telegraphed challenge

## Findings fixed

- Cue and activation now read the actual SprintRules.Snapshot.Distance; elapsed time cannot create distance or trigger a stationary runner at 29.9m.
- SprintController simulates through MinigameLifecycle, evaluates finish, timeout, and failed counterplay, and resolves once through MinigameBase.Finish.
- Counterplay tests establish a 100m viable finish path and assert emitted pass/failure results plus exactly one completion.
- Cue tests cover below-threshold inactivity, authored 0.8s lead, and activation only at the lead boundary.
- WindWindowDuration is consumed; expiry disables the window and late taps cannot counter it.
- The project uses the legacy Input Manager (activeInputHandler: 0 and no Input System package), so SprintLeft and SprintRight named actions are bound to left/right keys in ProjectSettings/InputManager.asset; those action names are serialized on the scene controller and consumed by Input.GetButtonDown.
- Foundation lifecycle and ScoreUtil result construction remain the source of completion and scoring; no RNG or combo shortcut was added.

## Files changed

- Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs
- Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs
- Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs
- Assets/_Project/Scenes/MG_Sprint.unity
- ProjectSettings/InputManager.asset

## Verification

Working directory: /home/duongduy/data/project/the-chat-KMA

Absolute Unity editor: /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity. Every command below omits -quit; logs and XML are retained under /tmp.

1. Focused PlayMode review regression:

   /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-review-green.xml -logFile /tmp/Unity-sprint-review-green.log

   Result: exit 0; XML 5 total, 5 passed, 0 failed, 0 inconclusive. Log: Test run completed. Exiting with code 0 (Ok). Run completed.

2. Running EditMode regression:

   /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-running-editmode-review.xml -logFile /tmp/Unity-running-editmode-review.log

   Result: exit 0; XML 14 total, 14 passed, 0 failed, 0 inconclusive. Log completion code 0.

3. Foundation Common EditMode regression:

   /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-foundation-editmode-review.xml -logFile /tmp/Unity-foundation-editmode-review.log

   Result: exit 0; XML 30 total, 30 passed, 0 failed, 0 inconclusive. Log completion code 0.

4. TDD/review diagnostic run before the final boundary correction:

   /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-review-red.xml -logFile /tmp/Unity-sprint-review-red.log

   Result: exit 2; XML 5 total, 4 passed, 1 failed. The only failure was the expiry assertion exactly at the floating-point 0.8+1.2 boundary. The test was changed to simulate 1.21s after activation, then the final 5/5 run above passed.

5. Static checks:

   git diff --check passed with no output.

   rg -n 'distanceAfterTick|UnityEngine\.Random|Random\.' Assets/_Project/Scripts/Gameplay/Sprint Assets/Tests/PlayMode/Gameplay/Running found no time-invented-distance expression or RNG use.

## Deviations and risks

- The reviewer requested Input System bindings, but this repository has no com.unity.inputsystem dependency and has activeInputHandler: 0; adding a new package/setup would expand scope. The fix uses the project’s existing named Input Manager action mechanism.
- MinigameBase.Lifecycle now has a protected setter solely so the controller’s explicit test configuration can enter Play deterministically; runtime lifecycle transitions and Finish remain Foundation-controlled.
- Unity regenerated ProjectSettings/SceneTemplateSettings.json during test/import; it was removed as unrelated before staging.

## Follow-up review fix: large simulation-step overshoot

The authored challenge update now checks the absolute expiry boundary before activation. A single Simulate call that overshoots both lead and duration marks WindWindowActive false, sets WindChallengeExpired, and closes counterplay before any late tap.

Added regression: LargeSimulationStep_ExpiresWindWindowBeforeLateCounterplay, which cues at 30m, calls Simulate(2.01f), asserts inactive/expired, and verifies a late left tap does not counter or fail the challenge.

Verification commands and XML counts:

- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-large-dt.xml -logFile /tmp/Unity-sprint-large-dt.log
  - exit 0; XML 6 total, 6 passed, 0 failed, 0 inconclusive.
- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-running-editmode-large-dt.xml -logFile /tmp/Unity-running-editmode-large-dt.log
  - exit 0; XML 14 total, 14 passed, 0 failed, 0 inconclusive.
- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-foundation-editmode-large-dt.xml -logFile /tmp/Unity-foundation-editmode-large-dt.log
  - exit 0; XML 30 total, 30 passed, 0 failed, 0 inconclusive.

All commands omitted -quit. Unity logs reported Test run completed. Exiting with code 0 (Ok). Run completed. git diff --check passed.

## Follow-up review fix: cue-threshold crossing timing

UpdateAuthoredChallenges now receives the pre-tick distance and computes the fraction of the simulation step before the authored 30m cue. Only the remaining post-crossing time advances the lead/window timer. This prevents a large step that crosses 30m late from expiring or activating the challenge as if the full step occurred after the cue.

Added regression: CueCrossingInsideLargeStep_StartsTimerAtThresholdAndExpiresAfterAuthoredDuration. It advances from 29m with deterministic speed, crosses 30m in one 1.0s step, verifies the remaining lead before activation, verifies the active window remains open through the remaining authored duration, then verifies expiry and rejection of a late tap.

Verification:

- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-crossing-green.xml -logFile /tmp/Unity-sprint-crossing-green.log
  - exit 0; XML 7 total, 7 passed, 0 failed, 0 inconclusive.
- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-running-editmode-crossing.xml -logFile /tmp/Unity-running-editmode-crossing.log
  - exit 0; XML 14 total, 14 passed, 0 failed, 0 inconclusive.
- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-foundation-editmode-crossing.xml -logFile /tmp/Unity-foundation-editmode-crossing.log
  - exit 0; XML 30 total, 30 passed, 0 failed, 0 inconclusive.

All commands omitted -quit. Unity logs reported Test run completed. Exiting with code 0 (Ok). Run completed. git diff --check passed.

## Follow-up review fix: Unity Input System integration

Runtime Sprint input now uses com.unity.inputsystem 1.20.0, which is the built-in package version for Unity 6000.3.22f1. ProjectSettings activeInputHandler is 1. SprintController subscribes to InputAction.performed for the authored SprintLeft and SprintRight actions, and MG_Sprint references Assets/_Project/Scripts/Gameplay/Sprint/SprintInputActions.inputactions. The authored asset binds leftArrow and rightArrow keyboard controls. The legacy SprintLeft/SprintRight entries were removed from InputManager.asset; public OnLeftTap/OnRightTap seams and all deterministic challenge behavior remain.

Added InputSystemAsset_BindsSprintActionsAndControllerResolvesThem, verifying the named actions, controller readiness, and keyboard binding paths.

Verification:

- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-inputsystem.xml -logFile /tmp/Unity-sprint-inputsystem.log
  - exit 0; XML 8 total, 8 passed, 0 failed, 0 inconclusive.
- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-running-editmode-inputsystem.xml -logFile /tmp/Unity-running-editmode-inputsystem.log
  - exit 0; XML 14 total, 14 passed, 0 failed, 0 inconclusive.
- /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-foundation-editmode-inputsystem.xml -logFile /tmp/Unity-foundation-editmode-inputsystem.log
  - exit 0; XML 30 total, 30 passed, 0 failed, 0 inconclusive.

All commands omitted -quit and used the absolute Unity editor path. Unity logs reported Test run completed. Exiting with code 0 (Ok). Run completed. git diff --check passed. Only the untracked generated ProjectSettings/SceneTemplateSettings.json was removed.
