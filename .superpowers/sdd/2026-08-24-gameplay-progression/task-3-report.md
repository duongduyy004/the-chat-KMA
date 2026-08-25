# Task 3 review-fix report: Boss phase controller

## Corrective implementation

- `MG_Boss` now loads the serialized `BossSequence.asset`, registers in build settings, and serializes three real detector adapter components. `BossPhaseController.Awake` consumes the serialized `BossSceneSessionHandoff`, loads/validates the asset sequence, binds all adapters, and logs a visible exception plus disables itself if required scene configuration is missing.
- `BossSceneSessionHandoff` derives the standalone scene session by running all seven canonical subjects through `GameSession.StartSubject` and `SubmitResult`. The normal progression owner can replace it with its canonical session through `SetSession(GameSession)`. `Begin` still requires the foundation Play phase and `GameSession.BossUnlocked`; the locked-session test replaces the handoff with a fresh locked session.
- `BossRuntimeInputSource` is serialized into `MG_Boss` and consumes Unity Input System keyboard callbacks, routing space/H/left-arrow/right-arrow input to the phase-specific adapters. PlayMode coverage exercises the runtime input component through all three phases.
- `BossSequenceAsset` and controller Configure enforce exactly `TapMash -> RhythmHold -> AlternateTap`; Configure also rejects foreign duration/target data that does not match the serialized asset.
- Removed the public `CompleteCurrent` shortcut. TapMash, RhythmHold, and AlternateTap adapter events are the only phase progress inputs. Inputs are incremental, phase-specific, duration-bounded, and wrong/repeated inputs do not advance progress.
- `BossPhaseController` now derives from `MinigameBase`, resolves through `Finish` exactly once, and builds results with `ScoreUtil.Build(pass, accuracy, efficiency, mastery)`. No own result event, hand-built result, RNG, combo, or shortcut remains.
- Removed generated `ProjectSettings/SceneTemplateSettings.json` after Unity verification.

## Exact verification commands and results

All Unity commands used the absolute editor path, `-batchmode`, and intentionally omitted `-quit`.

Focused Boss PlayMode final:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter BossPhaseControllerTests -testResults /tmp/task-3-boss-current.xml -logFile /tmp/task-3-boss-current.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

Full Progression EditMode:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter Progression -testResults /tmp/task-3-progression-current.xml -logFile /tmp/task-3-progression-current.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

Foundation lifecycle EditMode:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter MinigameLifecycleContractTests -testResults /tmp/task-3-foundation-lifecycle-current.xml -logFile /tmp/task-3-foundation-lifecycle-current.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

Foundation ScoreUtil EditMode:

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter ScoreUtilTests -testResults /tmp/task-3-foundation-score-current.xml -logFile /tmp/task-3-foundation-score-current.log
```

Exit 0. Unity log: `Test run completed. Exiting with code 0 (Ok). Run completed.`

## XML counts

- `/tmp/task-3-boss-current.xml`: total 8, passed 8, failed 0, inconclusive 0, skipped 0.
- `/tmp/task-3-progression-current.xml`: total 15, passed 15, failed 0, inconclusive 0, skipped 0.
- `/tmp/task-3-foundation-lifecycle-current.xml`: total 2, passed 2, failed 0, inconclusive 0, skipped 0.
- `/tmp/task-3-foundation-score-current.xml`: total 11, passed 11, failed 0, inconclusive 0, skipped 0.

`git diff --check` produced no output. Final Boss logs contain no compiler errors, warnings, malformed scene serialization, unhandled logs, or test failures.

## Deviations and risks

- Added three separate detector adapter scripts because Unity cannot reliably bind multiple MonoBehaviour classes from one script asset in serialized scene YAML.
- Added `MG_Boss` to `EditorBuildSettings.asset` so the scene-load test and runtime scene route can load it by name.
- The standalone `MG_Boss` scene uses an explicit canonical prerequisite seed so it is playable in isolation. Game flow should hand off its live session through `SetSession`; the gate remains derived from all seven `GameSession` records.
- An isolated runtime-input attempt initially failed because queued keyboard state was consumed before the frame poll; the source now uses `InputSystem.onAfterUpdate`, and the fresh focused run passed 8/8.
- The first attempt to run the three requested suites concurrently was rejected by Unity’s single-project-instance lock; the Progression and lifecycle suites were rerun sequentially and passed.
