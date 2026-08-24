# Ball plan Task 2 review-fix report

## Fixes

- `VolleyballRules.Tick` now performs exactly one `MinigameLifecycle.Tick` per rules tick. The default lifecycle is Tutorial (2s) -> Countdown (3s) -> Play; the live 60s match timer accumulates only on frames that begin in Play.
- Timeout at the strict 60s boundary calls Foundation `BeginResolve`; resolving remains rejected before Play and after timeout.
- `TryResolveAndLaunch` validates the contextual gesture, selects the authored Dig/Set/Spike trajectory, records the touch, and launches the actual `BallRig` with deterministic authored direction/force/curvature. `TryLaunchSelected` rejects launch before a valid selection.
- `BallRig` now lazily resolves its required `Rigidbody2D` through its existing public contract, allowing the real launch path to be tested in EditMode without invoking `Awake` via `SendMessage`.
- Removed direct elapsed-time injection from `VolleyballRules.ForTest`; objective timing tests now advance the actual lifecycle and live timer.
- Added substantive lifecycle, timer, timeout, Play-only resolve, and real BallRig launch tests. Normalized trailing whitespace from the four new folder/assembly `.meta` files; the unrelated Unity-generated `ProjectSettings/SceneTemplateSettings.json` was removed.

## Exact commands and results

Red review test:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter VolleyballRulesTests -testResults /tmp/task-2-review-red.xml -logFile /tmp/task-2-review-red.log
```

Result: expected compiler failure for the new missing `BallRig`/launch APIs; no XML was emitted. Log retained at `/tmp/task-2-review-red.log`.

Focused Volleyball final:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter VolleyballRulesTests -testResults /tmp/task-2-review-focused-final-2.xml -logFile /tmp/task-2-review-focused-final-2.log
```

Result: exit code 0; XML `12 total, 12 passed, 0 failed, 0 inconclusive, 0 skipped`. Log: `/tmp/task-2-review-focused-final-2.log`; XML: `/tmp/task-2-review-focused-final-2.xml`.

Full EditMode Volleyball/Ball/Foundation regression:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testResults /tmp/task-2-review-editmode-final-2.xml -logFile /tmp/task-2-review-editmode-final-2.log
```

Result: exit code 0; XML `65 total, 65 passed, 0 failed, 0 inconclusive, 0 skipped`, including Foundation 30, Running 23, Volleyball 12. Log: `/tmp/task-2-review-editmode-final-2.log`; XML: `/tmp/task-2-review-editmode-final-2.xml`.

Focused BallRig PlayMode regression:

```text
rtk proxy timeout 120s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform PlayMode -testFilter BallRigTests -testResults /tmp/task-2-review-ball-final-2.xml -logFile /tmp/task-2-review-ball-final-2.log
```

Result: exit code 0; XML `7 total, 7 passed, 0 failed, 0 inconclusive, 0 skipped`. Log: `/tmp/task-2-review-ball-final-2.log`; XML: `/tmp/task-2-review-ball-final-2.xml`.

Diff checks:

```text
rtk git diff --check
rtk rg -n "[ \t]+$" Assets/Tests/EditMode/Gameplay/Ball/*.meta Assets/_Project/Scripts/Gameplay/Volleyball/*.meta Assets/Tests/EditMode/Gameplay/Ball.meta Assets/_Project/Scripts/Gameplay/Volleyball.meta || true
```

Result: no tracked diff-check errors and no trailing-whitespace matches in the new `.meta` files.

## Deviations and risks

- The report path is ignored by the repository’s `.gitignore`, so it must be force-staged for the requested commit.
- Unity logs retain the environment warning `Access token is unavailable; failed to update`; all final test commands completed with exit code 0 and the XML counts above.
- No Volleyball scene/controller/prefab or visual QA was added; this fix remains limited to the Task 2 rules/pattern/BallRig contract and tests.
