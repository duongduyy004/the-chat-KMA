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
