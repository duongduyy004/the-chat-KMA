# Remove Punishment Loss Route — Verification Gate

**Branch:** `feature/sprint-ui-polish`
**Spec:** `docs/superpowers/specs/2026-09-14-remove-punishment-loss-route-design.md`
**Plan:** `docs/superpowers/plans/2026-09-14-remove-punishment-loss-route.md`
**Unity:** `6000.3.23f1`
**Date:** 2026-09-14

## Automated verification — PASS

Full suites, unfiltered, run from a clean tracked status (Unity Editor confirmed not running
before either run):

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform EditMode \
  -testResults Builds/TestResults/gate-editmode.xml -logFile Builds/TestResults/gate-editmode.log

"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform PlayMode \
  -testResults Builds/TestResults/gate-playmode.xml -logFile Builds/TestResults/gate-playmode.log
```

Both runs exited `0`; exit code was not used to judge pass/fail. Every count below was read from
the single root `<test-run>` element's `total`/`passed`/`failed`/`skipped` attributes:

```bash
grep -o '<test-run[^>]*' Builds/TestResults/gate-editmode.xml | head -1 | tr ' ' '\n' | grep -E 'total=|passed=|failed=|skipped='
grep -o '<test-run[^>]*' Builds/TestResults/gate-playmode.xml | head -1 | tr ' ' '\n' | grep -E 'total=|passed=|failed=|skipped='
```

| Suite | total | passed | failed | skipped |
|---|---|---|---|---|
| EditMode | 369 | 366 | 0 | 3 |
| PlayMode | 268 | 268 | 0 | 0 |

The 3 EditMode skips are the `[Ignore]`d fixture-building tests in
`Assets/Tests/EditMode/Progression/ChallengeSequenceTests.cs` (lines 52, 74, 104), parked in place
rather than deleted because `PunishmentController` can no longer be constructed from a live
`GameSession` now that no route sets `PendingPunishmentSubject`. Each carries the reason and points
back at the spec above. This matches the repository's EditMode baseline of 369 total tests
(measured before this work at commit `2cd9f86`): the total test count did not change; three tests
that used to pass now report skipped by design, and zero regressed to failing.

### Reconciling the "471 passed, 10 skipped" figure

An earlier report for this plan (Task 3) stated EditMode results as "471 passed, 10 skipped" —
over 100 tests above baseline, which no task in this plan could account for. Read directly against
`Builds/TestResults/task3-editmode.xml`:

- The root `<test-run>` element on that same file reads `total="369" passed="366" failed="0"
  skipped="3"` — identical to the counts in this gate.
- `grep -c '<test-suite'` on that file returns `112`: the NUnit XML nests a `<test-suite>` rollup
  node for every namespace/fixture, and each rollup carries its own `result="Passed"` (or
  `"Skipped"`) attribute summarising its children, in addition to the leaf `<test-case>` nodes.
- Counting bare `result="Passed"` occurrences anywhere in the file (leaf test cases plus every
  passing suite rollup) yields exactly `471`. Counting bare `result="Skipped"` occurrences the same
  way yields exactly `10`.
- Counting only `<test-case ... result="...">` leaf nodes yields `366 Passed` / `3 Skipped` —
  matching the root node exactly.

**Verdict: the "471 passed, 10 skipped" figure was a misread, not a real change.** It came from
grepping `result="..."` across the whole XML document, which double- (and triple-, and
quadruple-, for nested namespaces) counts every passing/skipped fixture and namespace rollup on
top of the actual test cases. The true, current EditMode counts — read from the root node, and
cross-checked against a leaf-only `<test-case>` tally — are `366 passed / 3 skipped / 0 failed`
out of `369` total, unchanged from the `2cd9f86` baseline. The plan's own guidance that "ten
skipped EditMode tests are expected" is superseded by this reading: the actual, verified figure is
**3 skipped**, matching the three `[Ignore]`d `ChallengeSequenceTests` cases exactly — there is no
tenth skip to account for.

## Behaviour verified

- **A loss spends one life and returns to subject select.** `GameSession.SubmitResult` decrements
  `Lives` on any failed attempt and `RouteForResult` returns `SessionRoute.Map` (subject select)
  whenever more than one life remains after the loss (`Assets/_Project/Scripts/Progression/GameSession.cs:183-207`).
- **The last life ends the run.** `RouteForResult` checks `Lives <= 1` *before* `SubmitResult`
  decrements it, so a loss that would empty the last life routes to `SessionRoute.GameOver` instead
  of back to subject select (`GameSession.cs:203-207`).
- **A legacy save carrying `awaitingPunishment` resumes at the subject, not the dead scene.**
  `GameSession.RestoreActiveAttempt` explicitly discards `awaitingPunishment` and resets
  `visitAttempt` to `FirstVisit` when restoring, with an inline comment explaining that the
  punishment leg is no longer routable and that resuming the flag verbatim would send the player to
  a scene nothing can complete (`GameSession.cs:122-143`). This is covered by the EditMode test
  `Restore_ReplacesAPreviouslyRestoredAttempt`
  (`Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs:172-191`), which restores a
  save with `visitAttempt = 2` (`FinalVisit`) and `awaitingPunishment = true` and asserts
  `ResumeRoute() == SessionRoute.Subject`. That test landed in commit `566ff6f`.

## Punishment code: retired in place, not removed

`Assets/_Project/Scripts/Core/PunishmentSceneController.cs`,
`Assets/_Project/Scripts/Progression/PunishmentController.cs`, and the Punishment scene all remain
in the repository. `SessionRoute.Punishment` still exists in the route enum and is still wired into
`SceneRouter` (`Assets/_Project/Scripts/Core/SceneRouter.cs:82,381,459`), but `GameSession`'s
`RouteForResult` never returns it from live gameplay — the only remaining path that could produce
it is closed off by the `Restore` guard above. The scene and both controllers are therefore
unreachable from any route in current play, and the three `ChallengeSequenceTests` cases listed
above are `[Ignore]`d for exactly this reason: their fixtures need a `PendingPunishmentSubject` to
construct `PunishmentController` from a live `GameSession`, and no route sets that anymore.

## Housekeeping

Unity's batch run regenerated `Assets/_Project/Fonts/Nunito-Bold.asset` (a TMP font atlas asset);
this was reverted with `git checkout` before committing this document, per prior experience on this
branch. `ProjectSettings/GraphicsSettings.asset` and `ProjectSettings/QualitySettings.asset` carry
unrelated local modifications that predate this work and were deliberately left uncommitted; only
this file is staged for the QA-gate commit.
