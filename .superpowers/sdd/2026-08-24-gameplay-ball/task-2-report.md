# Ball plan Task 2 report

## Implemented

- Added deterministic `VolleyballRules` gesture resolution:
  - low context -> Dig;
  - rising context -> Set;
  - apex-near-net with down-right swipe -> Spike;
  - reach-zone and timing-accuracy gates reject invalid touches.
- Added `VolleyReturnPattern` with authored Dig/Set/Spike phases, a minimum 0.6s cue lead, deterministic trajectory selection before launch, and BallRig landing prediction integration.
- Added Foundation lifecycle access (`Phase`, `Tick`, `BeginResolve`) and Foundation `ScoreUtil.Build` result construction.
- `Pass` is true only when player score is at least five, strictly leads the opponent, and is within the 60s limit. Combo contributes only mastery and cannot shortcut the objective. No RNG or combo shortcut is used.
- Added focused tests for context, reach/timing gates, authored phases, objective gating, and deterministic scoring.

## Exact verification commands and results

Red phase:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter VolleyballRulesTests -testResults /tmp/task-2-volleyball-red.xml -logFile /tmp/task-2-volleyball-red.log
```

Result: Unity aborted on expected missing-type compiler errors (`BallContext`, `VolleyAction`); no XML was emitted because compilation stopped. Log retained at `/tmp/task-2-volleyball-red.log`.

Focused final run:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter VolleyballRulesTests -testResults /tmp/task-2-volleyball-focused-final.xml -logFile /tmp/task-2-volleyball-focused-final.log
```

Result: exit code 0; XML counts `testcasecount=8 total=8 passed=8 failed=0 inconclusive=0 skipped=0`.

Full EditMode regression:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testResults /tmp/task-2-editmode-all.xml -logFile /tmp/task-2-editmode-all.log
```

Result: exit code 0; XML counts `testcasecount=61 total=61 passed=61 failed=0 inconclusive=0 skipped=0`.

`git diff --check` passed.

## Deviations and risks

- Added `KMA.Gameplay.Volleyball.asmdef` and `KMA.Gameplay.Volleyball.EditMode.Tests.asmdef` plus Unity-generated metadata so the brief’s short test filter discovers the new tests. Before isolation, the same command completed with `0/0` executed tests.
- Unity logs contain the environment warning `Access token is unavailable; failed to update`; both runs still completed with exit code 0 and the XML counts above.
- This task adds rule/pattern contracts and tests only; no Volleyball scene/controller/prefab or visual QA is included.
