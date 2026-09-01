# S7 Task 2 report

## Scope

- Added mode-agnostic detector-result events to `GameplayInputRouter`.
- Refactored `EnduranceInputBridge` to subscribe to router events and removed its runtime `Update` touchscreen polling and direct InputAction callbacks.
- Added the shared `KMA.inputactions` router component to `MG_Endurance`.
- Added focused router event relay coverage.

## Verification

- RED: Unity compilation initially exposed the pre-existing `TimingJudge` namespace ambiguity in `EnduranceInputBridge`; the router-event test could not execute until the bridge refactor removed that stale direct detector callback.
- GREEN: `/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s7-task2-play.xml -logFile /tmp/kma-s7-task2-play.log -testFilter "EnduranceControllerTests|EnduranceInputBridgeTests|EnduranceRuntimeInputTests|GameplayInputRouterTests" -quit` completed with exit code 0.
- `rtk git diff --check` completed with exit code 0.

## Concern

The scene has the shared router serialized. Existing gameplay pointer capture still needs a `ScreenTapArea` UI surface serialized in the Endurance scene for pointer events to reach the router at runtime; this report records that limitation rather than claiming it is complete.

## Review follow-up: runtime input wiring

### Fixes

- Keyboard `Tap` now continues to feed the generic tap detector and also feeds `RhythmBeatInputDetector` through the existing calibrated router path.
- `MG_Endurance` now serializes exactly one transparent, behind-UI `ScreenTapArea` on `FullScreenGameplayInput`; it has a `GraphicRaycaster`, references the serialized router, and uses its full-screen `RectTransform` as `gameplayArea`.
- `EnduranceInputBridge` gates rhythm, hold, and swipe router events by `EnduranceRules.Mode` before calling the controller, so wrong-mode events cannot increment input counters. `TapFromCalibratedDelta` remains the single calibrated rhythm seam.
- Focused coverage now exercises keyboard generic-plus-rhythm dispatch, router-to-bridge mode gates, the serialized `ScreenTapArea` pointer path, and private serialized router/area references.

### Verification

- RED: `RouterEvents_OnlyReachControllerForTheirMatchingInputMode` initially failed with `Expected: 1, But was: 2`; the extra tap was the valid pointer-down rhythm event, so the test expectation was corrected while retaining wrong-mode counter assertions.
- GREEN: `KMA.Tests.Input.GameplayInputRouterTests.KeyboardTap_FeedsBothGenericAndRhythmDetectors` passed (`1/1`) via Unity 6000.3.23f1.
- GREEN: `KMA.Tests.Gameplay.Running.EnduranceInputBridgeTests.RouterEvents_OnlyReachControllerForTheirMatchingInputMode` passed (`1/1`).
- GREEN: `KMA.Tests.Gameplay.Running.EnduranceControllerTests.SerializedSceneActions_DispatchTouchSwipeThroughRuntimeBridge` passed (`1/1`), exercising `ScreenTapArea -> GameplayInputRouter -> EnduranceInputBridge -> EnduranceController`.
- A final `EnduranceScene_IsBuildRegisteredAndStartsDspMetronome` wiring run was blocked before execution by newly untracked presentation scripts (`EnduranceBeatRing`, `EnduranceHud`, `EnduranceObstacleCue`) missing a `TMPro` assembly reference; this is unrelated to Task 2 and was left untouched.
