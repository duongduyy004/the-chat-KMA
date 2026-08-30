# Task 4: S3 integration verification and handoff

Date: 2026-08-30
Editor: Unity 6000.3.23f1 (09d2ecc7fb28)

## Verification evidence

Focused EditMode:

```text
rtk ~/.local/bin/unity test . --mode EditMode --filter 'KMA.Tests.Input' --output /tmp/kma-s3-task4-focused-edit.xml --timeout 1200 -- -nographics
```

Result: pass, 14 total / 14 passed / 0 failed / 0 inconclusive / 0 skipped.

Focused PlayMode:

```text
rtk ~/.local/bin/unity test . --mode PlayMode --filter 'KMA.Tests.Input' --output /tmp/kma-s3-task4-focused-play.xml --timeout 1200 -- -nographics
```

Result: pass, 11 total / 11 passed / 0 failed / 0 inconclusive / 0 skipped.

Full EditMode:

```text
rtk ~/.local/bin/unity test . --mode EditMode --output /tmp/kma-s3-task4-full-edit.xml --timeout 2400 -- -nographics
```

Result: pass, 149 total / 149 passed / 0 failed / 0 inconclusive / 0 skipped. The XML includes `ChallengeSequenceTests`: 6/6 passed.

Full PlayMode:

```text
rtk ~/.local/bin/unity test . --mode PlayMode --output /tmp/kma-s3-task4-full-play.xml --timeout 2400 -- -nographics
```

Result: fail, 56 total / 55 passed / 1 failed / 0 inconclusive / 0 skipped. The failed test is `KMA.Tests.Gameplay.Progression.BossPhaseControllerTests.AuthoredPhaseDurationFailsBeforeTargetIsReached`; its failure is the unhandled headless log `No graphic device is available to initialize the view.`

The first brief command using `--testFilter` was rejected by the installed wrapper (`unknown option '--testFilter'`) before Unity ran. The supported equivalent is `--filter`, used above.

## Contract and scope checks

- `rtk git diff --check -- Assets/_Project/Scripts/Input Assets/Tests/EditMode/Input Assets/Tests/PlayMode/Input Assets/_Project/Settings/Input`: clean.
- `Assets/_Project/Settings/Input/KMA.inputactions` declares exactly: `Sprint`, `Endurance`, `Boss`, `Punishment`, `UI`.
- Existing `SprintInputActions.inputactions` and `EnduranceInputActions.inputactions` remain present and are still referenced by their legacy consumers.
- `PunishmentController.cs`, `SprintController.cs`, and `EnduranceInputBridge.cs` have no diff from `HEAD`.
- No scene changes were made by Task 4. The scene paths already dirty at task start remain outside the Task 4 scope.
- Router evidence is covered by focused tests for single delivery, duplicate-pointer idempotence, UI/raycast exclusion, cleanup, and `RhythmOffsetMs` application.

## Handoff status

S3 focused contracts and full EditMode are verified. The final S3 gate is **not complete** because the full PlayMode suite has one environment-sensitive headless graphics failure. README was intentionally not updated because the brief’s documentation condition requires a passing full-suite handoff. Android device input remains deferred to S6/S7/S14; no gameplay scene rewiring was introduced.
