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

The original six tests covered attachment/kinematic tracking, absolute-velocity apex detection, deterministic analytic landing, no-root and zero-gravity fallback, launch detachment/snapshot flight state, and reflected bounce behavior.

## Deviations and risks

- Added dedicated Ball runtime and PlayMode test assembly definitions because the existing `KMA.Gameplay` assembly is scoped to `Gameplay/Common`; sibling Ball scripts would otherwise be invisible to the focused test assembly.
- Unity 6.0.3 reports deprecation warnings for the brief's `Rigidbody2D.velocity` and `drag` API usage. They are warnings only and the focused run passed. The implementation follows the brief's API contract.
- The pure `Bounce` helper remains supplemental; the reviewer follow-up adds real Physics2D collision coverage and verifies `Collided` plus damped reflection.
- BallRig intentionally does not own sport scoring or lifecycle transitions. It exposes physical/presentation state for the Foundation consumers; sport-specific rules remain outside Task 1.
- Test artifacts are retained at `/tmp/TestResults-ballrig-red.xml` (not created because RED compilation aborted), `/tmp/ballrig-red.log`, `/tmp/TestResults-ballrig-green.xml`, and `/tmp/ballrig-green.log`.


## Reviewer follow-up

The review gaps were addressed on current master:

- BallRig now disables Unity gravity/drag and advances velocity with deterministic Ballistics integration. PredictLandingPoint passes the same gravity, linear drag, curvature, and fixed-step parameters, so prediction follows the launched path.
- Bounce coverage now creates a dynamic Rigidbody2D with CircleCollider2D, a BoxCollider2D ground, waits for Physics2D collision, asserts Collided delivery, and verifies reflected damped velocity.
- Launch_UsesConfiguredGravityDragCurvatureAcrossFixedSteps asserts three fixed-step velocity/position updates and prediction agreement.

Focused Ball PlayMode command (no -quit):

```bash
timeout 240 rtk proxy /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter BallRigTests -testResults /tmp/TestResults-ballrig-review.xml -logFile /tmp/ballrig-review.log
```

Result: exit 0. XML counts: testcasecount=7, total=7, passed=7, failed=0, inconclusive=0, skipped=0, result=Passed. Log: Test run completed. Exiting with code 0 (Ok). Run completed.

Relevant Foundation command (no -quit):

```bash
timeout 240 rtk proxy /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults /tmp/TestResults-foundation-review.xml -logFile /tmp/foundation-review.log
```

Result: exit 0. XML counts: testcasecount=30, total=30, passed=30, failed=0, inconclusive=0, skipped=0, result=Passed. Log: Test run completed. Exiting with code 0 (Ok). Run completed.

Retained artifacts: /tmp/TestResults-ballrig-review.xml, /tmp/ballrig-review.log, /tmp/TestResults-foundation-review.xml, and /tmp/foundation-review.log. Unity 6 emits deprecation warnings for Rigidbody2D.velocity in the brief-compatible API; no compiler errors or test failures occurred.