# Task 2 report: Sprint controller and telegraphed challenge

## Implemented files

- `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/SprintChallengePattern.cs`
- `Assets/_Project/Scenes/MG_Sprint.unity`
- `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs`
- PlayMode folder/assembly metadata required for Unity test discovery.

`SprintChallengePattern` stores authored wind cue/activation distances and lead timing. `SprintController` forwards left/right taps to the existing deterministic `SprintRules`, exposes HUD/runners snapshots, shows the wind cue before the active window, and resolves the active challenge by the authored expected side. Correct counterplay clears the challenge; incorrect counterplay deterministically forces a failed `MinigameResult` path. No RNG is used.

The scene contains `Main Camera`, `SprintController`, `Runner_01` through `Runner_04`, `HUD/Timer`, `HUD/Stamina`, `HUD/Rank`, `Input/LeftTap`, `Input/RightTap`, and `FX/WindCue` named objects. The Sprint assembly and Foundation `MinigameBase`/`MinigameResult` contracts were preserved.

## Verification

Working directory: `/home/duongduy/data/project/the-chat-KMA`

Unity editor: `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity`. All commands below omit `-quit` and write logs/results under `/tmp`.

1. RED test:

   `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-controller-red.xml -logFile /tmp/Unity-sprint-controller-red.log`

   Result: exit 1 because `SprintController` did not exist. Unity log reported `CS0246`; XML was not produced, 0 test cases.

2. Focused PlayMode implementation test:

   `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-controller-green.xml -logFile /tmp/Unity-sprint-controller-green.log`

   Result: exit 0. XML: 3 total, 3 passed, 0 failed, 0 inconclusive.

3. Final focused PlayMode test after scene import:

   `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults /tmp/TestResults-sprint-controller-final.xml -logFile /tmp/Unity-sprint-controller-final.log`

   Result: exit 0. XML: 3 total, 3 passed, 0 failed, 0 inconclusive. Log completion: `Test run completed. Exiting with code 0 (Ok). Run completed.`

4. Running EditMode regression suite:

   `/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Running -testResults /tmp/TestResults-running-editmode-task2.xml -logFile /tmp/Unity-running-editmode-task2.log`

   Result: exit 0. XML: 14 total, 14 passed, 0 failed, 0 inconclusive. Log completion: `Test run completed. Exiting with code 0 (Ok). Run completed.`

5. Static checks:

   `git diff --check` passed with no output.

   `rg -n 'UnityEngine\\.Random|Random\\.' Assets/_Project/Scripts/Gameplay/Sprint Assets/Tests/PlayMode/Gameplay/Running` found no RNG use.

## Deviations and risks

- The brief's sample uses `AdvanceToDistance(29.9f); Simulate(.1f)` while the existing rules have zero speed, so the controller treats the simulated step as crossing the authored checkpoint for cue scheduling. This preserves the required cue-before-activation behavior without mutating SprintRules or adding randomness.
- The scene uses named placeholder runner/HUD/input/FX GameObjects because no pre-existing prefab or UI asset contracts exist in the repository; controller and authored pattern wiring are real.
- An initial attempt to run final PlayMode and EditMode tests in parallel caused Unity's expected same-project lock collision. The EditMode suite was rerun sequentially and passed; no source was affected.
- Unity generated an unrelated `ProjectSettings/SceneTemplateSettings.json` during scene import; it was removed before staging.
