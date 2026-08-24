# Ball Task 1 report

## Scope

Implemented the shared BallRig presentation/physics boundary and deterministic Ballistics on current master. No worktree or subagent was used.

## Files

- Added BallRig, FlightProfile, BallFlightSnapshot, Ballistics, runtime assembly definition, and Unity-generated metadata under `Assets/_Project/Scripts/Gameplay/Ball`.
- Added focused PlayMode tests and test assembly definition under `Assets/Tests/PlayMode/Gameplay/Ball`.
- No existing gameplay, score, timing, lifecycle, scene, or unrelated source files were changed.

## Commands and results

RED verification, intentionally without `-quit`:

```bash
timeout 180 rtk proxy /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter BallRigTests -testResults /tmp/TestResults-ballrig-red.xml -logFile /tmp/ballrig-red.log
```

Result: exit 1. Unity aborted because `BallRig` was missing (`CS0246`); no RED XML was emitted. This confirms the tests failed for the intended missing-production-type reason.

GREEN verification, intentionally without `-quit`:

```bash
timeout 240 rtk proxy /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter BallRigTests -testResults /tmp/TestResults-ballrig-green.xml -logFile /tmp/ballrig-green.log
```

Result: exit 0. XML: `testcasecount=6`, `total=6`, `passed=6`, `failed=0`, `inconclusive=0`, `skipped=0`, root `result="Passed"`. The log states: `Test run completed. Exiting with code 0 (Ok). Run completed.`

## Coverage

The six tests cover attachment/kinematic tracking, absolute-velocity apex detection, deterministic analytic landing, no-root and zero-gravity fallback, launch detachment/snapshot flight state, and reflected bounce behavior.

## Deviations and risks

- Added dedicated Ball runtime and PlayMode test assembly definitions because the existing `KMA.Gameplay` assembly is scoped to `Gameplay/Common`; sibling Ball scripts would otherwise be invisible to the focused test assembly.
- Unity 6.0.3 reports deprecation warnings for the brief's `Rigidbody2D.velocity` and `drag` API usage. They are warnings only and the focused run passed. The implementation follows the brief's API contract.
- The synthetic `Bounce` helper verifies reflection without fabricating a collision event; real `OnCollisionEnter2D` invokes `Collided` and applies the profile damping.
- BallRig intentionally does not own sport scoring or lifecycle transitions. It exposes physical/presentation state for the Foundation consumers; sport-specific rules remain outside Task 1.
- Test artifacts are retained at `/tmp/TestResults-ballrig-red.xml` (not created because RED compilation aborted), `/tmp/ballrig-red.log`, `/tmp/TestResults-ballrig-green.xml`, and `/tmp/ballrig-green.log`.
