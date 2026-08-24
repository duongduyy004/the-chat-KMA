# Task 3 report: Endurance exclusive input modes

## Implemented

- `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceRules.cs`
  - Implements exactly one active mode at a time: `RhythmTap`, `BreathHold`, or `ObstacleSwipe`.
  - `EnterBeat` explicitly switches mode; inactive commands are ignored without judging or applying the wrong-mode penalty/reward.
  - Consumes Foundation `RhythmBeatEvaluator` timing and `ScoreUtil.Build` result contracts.
  - Keeps stamina, lap progress, combo, judge counts, miss count, obstacle state, elapsed time, and deterministic result output observable.
  - Pass requires the authored primary objective (`laps >= requiredLaps`), positive stamina, and the 90-second limit; combo/mastery cannot shortcut laps.
- `Assets/_Project/Scripts/Gameplay/Endurance/LapPattern.cs`
  - Provides cloned authored beat events and explicit lap-end markers.
- `Assets/Tests/EditMode/Gameplay/Running/EnduranceRulesTests.cs`
  - Covers exclusive obstacle swipe behavior, explicit mode switching, authored-lap objective gating against combo, and deterministic equal inputs/results.

## Verification

Working directory: `/home/duongduy/data/project/the-chat-KMA`

Absolute Unity editor: `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity`. All Unity commands below omit `-quit`; XML and logs are retained under `/tmp`.

1. Required TDD red phase:

   `rtk proxy "/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter EnduranceRulesTests -testResults /tmp/TestResults-endurance-red.xml -logFile /tmp/Unity-endurance-red.log`

   Result: Unity exited before test execution with compiler errors for the missing Endurance types (`EnduranceRules`, `EnduranceInputMode`, `BeatEvent`, `SwipeDirection`, and `LapPattern`). XML was not produced; the retained log contains the expected diagnostics.

2. Focused Endurance tests:

   `rtk proxy "/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter EnduranceRulesTests -testResults /tmp/TestResults-endurance-green.xml -logFile /tmp/Unity-endurance-green.log`

   Result: exit 0; XML 4 total, 4 passed, 0 failed, 0 inconclusive. Log reports `Test run completed. Exiting with code 0 (Ok). Run completed.`

3. Running EditMode regression:

   `rtk proxy "/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-endurance-running.xml -logFile /tmp/Unity-endurance-running.log`

   Result: exit 0; XML 18 total, 18 passed, 0 failed, 0 inconclusive. Log reports completion code 0.

4. Foundation Common EditMode regression:

   `rtk proxy "/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-endurance-foundation.xml -logFile /tmp/Unity-endurance-foundation.log`

   Result: exit 0; XML 30 total, 30 passed, 0 failed, 0 inconclusive. Log reports completion code 0.

5. Static/scope checks:

   `rtk proxy git diff --check` passed with no output.

   Final status contains only the Endurance source/pattern files and metadata, the focused test and metadata, the Endurance assembly definition and metadata, the required test-assembly reference, and this report.

## Deviations and risks

- Unity’s existing assembly layout had `KMA.Gameplay.Sprint` scoped only to the Sprint folder; Endurance is a sibling folder. Added `KMA.Gameplay.Endurance.asmdef` and its focused-test reference so Unity compiles the requested source under a named assembly. This is build integration only.
- The first attempt to run Running and Foundation regressions concurrently caused Unity’s single-project lock. Running completed successfully, then Foundation was rerun sequentially and completed successfully; no source change resulted.
- XML for the intentional red phase is absent because Unity cannot emit test results when compilation fails; the retained log is the evidence for that phase.
