# Task 1 report: Sprint deterministic simulation

## Implemented files

- `Assets/_Project/Scripts/Gameplay/Sprint/SprintRules.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/RivalPaceProfile.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/KMA.Gameplay.Sprint.asmdef`
- `Assets/Tests/EditMode/Gameplay/Running/SprintRulesTests.cs`
- `Assets/Tests/EditMode/Gameplay/Running/KMA.Gameplay.Running.EditMode.Tests.asmdef`
- Unity folder/source/assembly metadata for the files above.

`SprintRules` implements an authored default Left/Right sequence (and consumable custom sequence), the 18f full impulse, 40% repeated-side impulse, invalid-input preservation of the expected side, speed cap and decay, deterministic distance/stamina ticking, valid-tap ratio, Low/Mid/High stamina bands at 30/70 boundaries, rival distances and computed rank from fixed pace profiles, and `BuildResult()` through the Foundation `ScoreUtil` contract. Pass requires only the single Sprint objective (100m within 14 seconds); stamina contributes efficiency and rank does not create a pass path.

## Verification

Working directory: `/home/duongduy/data/project/the-chat-KMA`

The review verification used the absolute Unity executable `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity`; no `KMA_UNITY_EDITOR` export was relied on. Commands were run without `-quit`.

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

## Review-fix verification

1. Exact requested Sprint command:

   `export KMA_UNITY_EDITOR=/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity; rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter SprintRulesTests -testResults /tmp/TestResults-sprint-review.xml -logFile /tmp/Unity-sprint-review.log`

   Result: exit 0. XML summary: 14 total, 14 passed, 0 failed, 0 inconclusive. Log completion: `Test run completed. Exiting with code 0 (Ok). Run completed.`

2. Foundation ScoreUtil regression:

   `export KMA_UNITY_EDITOR=/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity; rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common.ScoreUtilTests -testResults /tmp/TestResults-foundation-review.xml -logFile /tmp/Unity-foundation-review.log`

   Result: exit 0. XML summary: 11 total, 11 passed, 0 failed, 0 inconclusive.

3. Foundation lifecycle regression:

   `export KMA_UNITY_EDITOR=/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity; rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common.MinigameLifecycleTests -testResults /tmp/TestResults-foundation-lifecycle-review.xml -logFile /tmp/Unity-foundation-lifecycle-review.log`

   Result: exit 0. XML summary: 1 total, 1 passed, 0 failed, 0 inconclusive.

The exact Sprint filter initially produced zero tests because the Running folder had no assembly definition and the prior Foundation asmdef was scoped only to `Gameplay/Common`. Adding the scoped Running test asmdef and Sprint runtime asmdef made the requested command execute the 14 Sprint cases.

## Deviations and risks

- The plan did not state numeric stamina band cutoffs; this fix makes the contract explicit as Low `<30`, Mid `30–<70`, High `≥70` over the existing 0–100 stamina range.
- `RivalPaceProfile` remains a fixed data class; SprintRules now consumes its opening/sustained speeds to advance deterministic rival distances and recompute placement.
- No unrelated files were changed.
