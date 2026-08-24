# Task 1 report: Sprint deterministic simulation

## Implemented files

- `Assets/_Project/Scripts/Gameplay/Sprint/SprintRules.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/RivalPaceProfile.cs`
- `Assets/Tests/EditMode/Gameplay/Running/SprintRulesTests.cs`
- Unity folder/source metadata for the files above.

`SprintRules` implements alternating left/right taps, the 18f full impulse, 40% repeated-side impulse, speed cap and decay, deterministic distance/stamina ticking, valid-tap ratio, snapshot access, fixed rival-profile consumption, rank access, and `BuildResult()` through the Foundation `ScoreUtil` contract. Pass requires the single Sprint objective (100m within 14 seconds) and non-depleted stamina; rank does not create a pass path.

## Verification

Working directory: `/home/duongduy/data/project/the-chat-KMA`

The required Unity executable was unavailable: `KMA_UNITY_EDITOR` was unset and no `unity-editor`/`Unity` executable was found in the checked locations. Commands were run without `-quit` as requested.

1. RED attempt:

   `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter SprintRulesTests -testResults /tmp/TestResults-sprint-red.xml`

   Result: exit 1 before Unity startup: `rtk: Failed to execute command: : No such file or directory (os error 2)`. Log: `/tmp/sprint-red.log`. XML: missing, 0 test cases.

2. Sprint focused verification:

   `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter SprintRulesTests -testResults /tmp/TestResults-sprint-green.xml`

   Result: exit 1 before Unity startup with the same missing-command error. Log: `/tmp/sprint-editmode.log`. XML: missing, 0 test cases.

3. Foundation EditMode verification:

   `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-foundation.xml`

   Result: exit 1 before Unity startup with the same missing-command error. Log: `/tmp/foundation-editmode.log`. The XML already existed and was not produced by this invocation; its recorded summary is 28 total, 28 passed, 0 failed, 0 inconclusive, so it is not treated as current evidence.

4. Running EditMode verification:

   `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-running.xml`

   Result: exit 1 before Unity startup with the same missing-command error. Log: `/tmp/running-editmode.log`. XML: missing, 0 test cases.

5. Static checks:

   `rtk git diff --check` passed with no output.

   `rtk rg -n 'UnityEngine\\.Random|BuildResult|Pass' Assets/_Project/Scripts/Gameplay/Sprint Assets/Tests/EditMode/Gameplay/Running/SprintRulesTests.cs` found no random-state use and confirmed the result path/tests.

## Deviations and risks

- Unity compilation and execution could not be verified in this environment because the pinned editor executable was unavailable; the Sprint XML therefore has no current test count.
- `RivalPaceProfile` is intentionally a fixed data class consumed as a cloned array by `SprintRules`; authored asset selection and controller integration remain Task 2 scope.
- No unrelated files were changed.
