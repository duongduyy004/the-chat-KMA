# Ball plan Task 3 report

## Implementation

Implemented Basketball authored pass -> alley-oop -> apex tap flow on current master.

- `BasketballRules` owns the explicit `Holding`, `Passing`, `AlleyOopFlight`, and `Resolved` states.
- `Hold` cannot select a toss, create a flight, or advance `ApexProgress`.
- `AlleyOopPattern` supplies a deterministic authored launch and inclusive apex window with velocity threshold.
- Only a `Perfect` final tap awards a basket; early/late taps provide feedback and reset combo.
- `PrimaryObjectiveComplete` requires the configured basket count, so combo alone cannot pass.
- Resolution uses `MinigameLifecycle`; scoring uses `ScoreUtil`; launch uses `BallRig`/`Ballistics` through the existing `BallRig` contract.
- Added the Basketball assembly definition and Unity metadata required for the new scoped assets, plus the test assembly reference.

## Exact verification command

```bash
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.22f1/Editor/Unity -batchmode -nographics -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testFilter BasketballRulesTests -testResults /tmp/ball-task3-basketball.xml -logFile /tmp/ball-task3-basketball.log
```

The command intentionally omitted `-quit` as required. Unity exited after the test run with code 0 (`Test run completed. Exiting with code 0 (Ok). Run completed.`).

## XML/log results

Final XML: `/tmp/ball-task3-basketball.xml`  
Final log: `/tmp/ball-task3-basketball.log`

From the XML `<test-run>` summary:

- total/testcasecount: 11
- passed: 11
- failed: 0
- inconclusive: 0
- skipped: 0
- result: `Passed`

The 11 cases cover hold/no-apex, authored BallRig launch, six apex boundary cases, final tap/objective gating, combo-shortcut rejection/scoring, and lifecycle rejection/resolution.

`git diff --check` passed. The log contains a non-fatal Unity licensing message (`Access token is unavailable; failed to update`) and an existing Volleyball test warning for deprecated `Rigidbody2D.velocity`; no Basketball test warning remains.

## Deviations and risks

- The brief listed the two Basketball source files and one test file; a scoped Basketball assembly definition and standard Unity `.meta` files were also added so Unity can compile/import the new folder, and the existing ball test assembly received only the required Basketball reference.
- The authored pattern is deterministic and has no RNG or combo-based launch shortcut. Runtime apex integration remains delegated to the existing BallRig/Ballistics implementation; this task judges the authored tap against the deterministic window.
- Unity licensing output is environmental but did not affect compilation or the passing test result.
