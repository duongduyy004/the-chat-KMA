# Task 5 report: Badminton charge-height rules

## Implemented

- Added `BadmintonRules` with Lift/Drive/Smash/Overcharge classification.
- Added normalized height bands: `< .35` Lift, `.35` to `< .7` Drive, `>= .7` Smash; charge `> 1` is Overcharge.
- Added explicit `PlayerPoints >= 5 && PlayerPoints > OpponentPoints` primary objective.
- Added Foundation lifecycle ticking/resolution, authored point gating, accuracy, efficiency, and distinct-shot variety scoring through `ScoreUtil`.
- Added deterministic authored rally exchanges carrying timing, wind cue, and trajectory data.
- Added focused EditMode coverage for charge bands, height boundaries, authored exchange determinism, five-point objective, scoring, and lifecycle.

## Commands and results

RED (before production implementation; intentionally no `-quit`):

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testFilter KMA.Tests.Gameplay.Ball.BadmintonRulesTests -testResults /tmp/ball-task5-red.xml -logFile /tmp/ball-task5-red.log
```

Result: Unity aborted on expected compiler errors because `BadmintonShot` and Badminton production APIs were absent. No RED XML was emitted; compiler evidence is retained in `/tmp/ball-task5-red.log`.

Focused GREEN (no `-quit`):

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testFilter KMA.Tests.Gameplay.Ball.BadmintonRulesTests -testResults /tmp/ball-task5-green-final.xml -logFile /tmp/ball-task5-green-final.log
```

XML: `testcasecount=9`, `total=9`, `passed=9`, `failed=0`, `inconclusive=0`, `skipped=0`, `result=Passed`.

Full EditMode regression (no `-quit`):

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testResults /tmp/ball-task5-all-editmode-final.xml -logFile /tmp/ball-task5-all-editmode-final.log
```

XML: `testcasecount=91`, `total=91`, `passed=91`, `failed=0`, `inconclusive=0`, `skipped=0`, `result=Passed`.

Additional checks:

```text
rtk git diff --check
```

Result: clean.

## Deviations and risks

- Added `KMA.Gameplay.Badminton.asmdef` and one reference in the existing Ball EditMode test asmdef because Unity’s assembly layout otherwise excludes the new sibling feature assembly from the test compilation. Unity also generated the matching `.meta` files.
- The authored pattern is deterministic rules data only; this task does not add a scene/controller or physical shuttlecock visual integration.
- The Unity log includes an existing unrelated `Rigidbody2D.velocity` deprecation warning from `VolleyballRulesTests`; it does not affect the 91/91 result.

## Reviewer follow-up: lifecycle scoring boundary

The reviewer issue was that score mutation was not lifecycle-gated and a pending player point survived `BeginResolve`. Fixed by requiring `MinigamePhase.Play` in both score methods, clearing pending player scoring on the single successful `BeginResolve` transition, and routing timeout resolution through that same boundary. Existing charge, height, authored exchange, and five-point objective behavior is preserved.

Focused RED after adding the boundary tests (no `-quit`):

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testFilter KMA.Tests.Gameplay.Ball.BadmintonRulesTests -testResults /tmp/ball-task5-review-red.xml -logFile /tmp/ball-task5-review-red.log
```

XML: `testcasecount=12`, `total=12`, `passed=10`, `failed=2`, `inconclusive=0`, `skipped=0`, `result=Failed(Child)`.

Focused GREEN (no `-quit`):

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testFilter KMA.Tests.Gameplay.Ball.BadmintonRulesTests -testResults /tmp/ball-task5-review-green.xml -logFile /tmp/ball-task5-review-green.log
```

XML: `testcasecount=12`, `total=12`, `passed=12`, `failed=0`, `inconclusive=0`, `skipped=0`, `result=Passed`.

Full EditMode regression (no `-quit`):

```text
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testResults /tmp/ball-task5-review-all-editmode.xml -logFile /tmp/ball-task5-review-all-editmode.log
```

XML: `testcasecount=94`, `total=94`, `passed=94`, `failed=0`, `inconclusive=0`, `skipped=0`, `result=Passed`.
