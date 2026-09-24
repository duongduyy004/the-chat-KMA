# Task 3 review-fix report: retired-route phase controller

## Corrective implementation

- `MG_retired-route` now loads the serialized `retired-routeSequence.asset`, registers in build settings, and serializes three real detector adapter components. `retired-routePhaseController.Awake` consumes the serialized `retired-routeSceneSessionHandoff`, loads/validates the asset sequence, binds all adapters, and logs a visible exception plus disables itself if required scene configuration is missing.
- `retired-routeSceneSessionHandoff` now exposes a pending runtime session bridge. It defaults to a locked `GameSession`, accepts the live progression session before or after scene load, and raises a session-change event consumed by `retired-routePhaseController`. `Begin` still requires the foundation Play phase and `GameSession.retired-routeUnlocked`; the locked-session test remains negative.
- `retired-routeRuntimeInputSource` is serialized into `MG_retired-route` and polls the Unity Input System keyboard on input updates and frame updates, routing space/H/left-arrow/right-arrow input to the phase-specific adapters. PlayMode coverage uses `Unity.InputSystem.TestFramework.InputTestFixture.Press/Release` to exercise the real runtime input path through all three phases.
- `retired-routeSequenceAsset` and controller Configure enforce exactly `TapMash -> RhythmHold -> AlternateTap`; Configure also rejects foreign duration/target data that does not match the serialized asset.
- Removed the public `CompleteCurrent` shortcut. TapMash, RhythmHold, and AlternateTap adapter events are the only phase progress inputs. Inputs are incremental, phase-specific, duration-bounded, and wrong/repeated inputs do not advance progress.
- `retired-routePhaseController` now derives from `MinigameBase`, resolves through `Finish` exactly once, and builds results with `ScoreUtil.Build(pass, accuracy, efficiency, mastery)`. No own result event, hand-built result, RNG, combo, or shortcut remains.
- Removed generated `ProjectSettings/SceneTemplateSettings.json` after Unity verification.

## Exact verification commands and results

All Unity commands used the absolute editor path, `-batchmode`, and intentionally omitted `-quit`.

Focused retired-route PlayMode final:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter retired-routePhaseControllerTests -testResults /tmp/task-3-retired-route-final-pass.xml -logFile /tmp/task-3-retired-route-final-pass.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

Full Progression EditMode:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Progression -testResults /tmp/task-3-progression-edit-final.xml -logFile /tmp/task-3-progression-edit-final.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

Full EditMode regression:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testResults /tmp/final-editmode.xml -logFile /tmp/final-editmode.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

Runtime keyboard path:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter SceneRuntimeInput_ProgressesAllPhasesThroughKeyboard -testResults /tmp/task-3-retired-route-input-6.xml -logFile /tmp/task-3-retired-route-input-6.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

## XML counts

- `/tmp/task-3-retired-route-final-pass.xml`: total 10, passed 10, failed 0, inconclusive 0, skipped 0.
- `/tmp/task-3-retired-route-input-6.xml`: total 1, passed 1, failed 0, inconclusive 0, skipped 0.
- `/tmp/task-3-progression-edit-final.xml`: total 15, passed 15, failed 0, inconclusive 0, skipped 0.
- `/tmp/final-editmode.xml`: total 121, passed 121, failed 0, inconclusive 0, skipped 0.

`git diff --check` produced no output. Final retired-route logs contain no compiler errors, warnings, malformed scene serialization, unhandled logs, or test failures.

## Deviations and risks

- Added three separate detector adapter scripts because Unity cannot reliably bind multiple MonoBehaviour classes from one script asset in serialized scene YAML.
- Added `MG_retired-route` to `EditorBuildSettings.asset` so the scene-load test and runtime scene route can load it by name.
- The standalone `MG_retired-route` scene now starts locked unless a live session is handed off through `retired-routeSceneSessionHandoff.SetPendingSession`/`SetSession`; the gate remains derived from all seven `GameSession` records.
- The runtime-input test initially used direct forwarding calls and was replaced with real `InputTestFixture.Press/Release` events. The focused runtime-input run passed 1/1 and the full retired-route suite passed 10/10.
- The first attempt to run the three requested suites concurrently was rejected by Unity’s single-project-instance lock; the Progression and lifecycle suites were rerun sequentially and passed.
