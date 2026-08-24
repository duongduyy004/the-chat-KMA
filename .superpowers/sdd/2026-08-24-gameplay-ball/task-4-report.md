# Task 4 report: PingPong capped rally rules

## Implementation

- Added `PingPongRules` with a fixed first-to-five primary objective, explicit `PrimaryObjectiveComplete` gating, deterministic timing/return accounting, lifecycle forwarding, and a hard `BallSpeed` cap that is applied to physical `BallRig` launches.
- Added `ReturnPattern` with authored placement exchanges and deterministic capped `BallRig` launches. `AwardPlayerPoint` now requires and consumes a one-shot token armed only by a validated authored return; direct scoring and rally-only paths cannot pass. No RNG or combo shortcut is used.
- Added focused tests for cap, long-rally rejection, tied-score objective rejection, authored BallRig exchanges, scoring, and lifecycle.
- Added the PingPong gameplay assembly and referenced it from the existing ball EditMode test assembly.

## Verification

Unity Editor used:

`/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity`

Focused command, run without `-quit`:

```text
/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testFilter PingPongRulesTests -testResults /tmp/kma-task4-review-green.xml -logFile /tmp/kma-task4-review-green.log
```

Result: XML `testcasecount=6`, `total=6`, `passed=6`, `failed=0`, `inconclusive=0`, `skipped=0`, result `Passed`.

Full EditMode command, run without `-quit`:

```text
/home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform EditMode -testResults /tmp/kma-task4-review-editmode.xml -logFile /tmp/kma-task4-review-editmode.log
```

Result: XML `testcasecount=82`, `total=82`, `passed=82`, `failed=0`, `inconclusive=0`, `skipped=0`, result `Passed`. The PingPong fixture contributed `6/6` passed.

Also ran `git diff --check` successfully.

## Deviations and risks

- Added the minimal PingPong asmdef and one existing test-asmdef reference because Unity requires the new gameplay code and tests to compile across assembly boundaries. No unrelated gameplay scope was changed.
- Unity logs report `Licensing::Module: Access token is unavailable; failed to update`; tests still completed and XML results passed.
- The full EditMode log retains one pre-existing warning in `VolleyballRulesTests.cs` for deprecated `Rigidbody2D.velocity`; the new PingPong test uses `linearVelocity`.
- Unity test XML files and logs remain under `/tmp` as requested.
- Reviewer-fix verification specifically asserts `BallRig.linearVelocity.magnitude` never exceeds the configured cap and covers invalid timing/placement plus direct and repeated scoring attempts.
