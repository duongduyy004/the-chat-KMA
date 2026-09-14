# Remove the Punishment Leg from the Loss Route — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Losing a minigame costs one life and returns the player to the subject-select screen, with the Punishment scene removed from the route.

**Architecture:** The cut is made in one place — `GameSession`. `SubmitResult` loses its special first-failure branch so every loss decrements a life and clears the attempt, and `RouteForResult` never returns `SessionRoute.Punishment`. `RestoreActiveAttempt` neutralises the two legacy save flags so an old save cannot route into the now-unreachable scene. Punishment types, scene, and enum members stay in the repo, unreferenced by the route.

**Tech Stack:** Unity 6000.3.23f1, C#, NUnit via Unity Test Framework (EditMode + PlayMode), Unity Input System test fixtures.

**Spec:** `docs/superpowers/specs/2026-09-14-remove-punishment-loss-route-design.md`

## Global Constraints

- Unity editor version: `6000.3.23f1` (from `ProjectSettings/ProjectVersion.txt`).
- **The Unity Editor must not be open while running batch tests** — it holds the project lock and the run will fail. Check with `tasklist //FI "IMAGENAME eq Unity.exe"` before every test step.
- Punishment code is retired in place, never deleted: `Assets/_Project/Scenes/Punishment.unity`, `PunishmentController`, `PunishmentSceneController`, `ChallengeSequence`, and the `SessionRoute.Punishment` / `SessionRoute.RetrySubject` enum members all stay.
- The `SaveData` format does not change. `visitAttempt` and `awaitingPunishment` keep their fields and their serialized names.
- No UI work. `ResultPanel`, the subject-select screen, and all Sprint chrome are untouched.
- No copy changes — no Vietnamese string is added or edited.
- `GameSession.MaxLives` stays `5`; `GameSession.FirstVisit` stays `1`.

## Expected interim state

Tasks 1 and 2 change production behaviour that PlayMode tests still assert the old way. **PlayMode is expected to be red from the end of Task 1 until Task 3 completes.** Each task still runs and passes its own scoped test filter. Task 4 is the gate where both platforms must be green together.

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `Assets/_Project/Scripts/Progression/GameSession.cs` | The loss rule and the save-restore guard. The only production file that changes. | 1, 2 |
| `Assets/Tests/EditMode/Progression/GameSessionTests.cs` | Unit-level loss/life/route rules. | 1 |
| `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs` | `PreviewRoute` agreement and the result-panel route string. | 1 |
| `Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs` | Session-level save round-trip and resume behaviour. | 2 |
| `Assets/Tests/EditMode/Progression/SaveSystemTests.cs` | Save-file level round-trip; only the resume assertions change. | 2 |
| `Assets/Tests/PlayMode/Progression/PunishmentRouteTests.cs` | Inverted: proves a loss does *not* reach Punishment. | 3 |
| `Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs` | End-to-end route and relaunch flows. | 3 |
| `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs` | Continue/resume flows. | 3 |
| `Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs` | Per-subject first-failure route. | 3 |
| `Assets/Tests/PlayMode/Progression/VolleyballCampaignTests.cs` | Per-subject first-failure route. | 3 |
| `Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs` | One `CompletePunishment()` setup call to replace. | 3 |
| `docs/qa/loss-route-gate.md` | Records the verification run, matching the repo's existing gate docs. | 4 |

**Not modified:** `Assets/Tests/EditMode/Progression/SaveDataTests.cs` and `Assets/Tests/EditMode/Progression/ChallengeSequenceTests.cs`. Both assert data-level behaviour that this change preserves. If either fails, stop and report — it means an assumption in this plan is wrong.

---

## Task 1: One life per loss, always route to Map

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs:178-209`
- Test: `Assets/Tests/EditMode/Progression/GameSessionTests.cs`
- Test: `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs:11-62`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `GameSession.SubmitResult(SubjectId, MinigameResult) → SessionRoute` now returns `SessionRoute.Map` for any failure that leaves at least one life, and `SessionRoute.GameOver` for the failure that empties the last life. It never returns `SessionRoute.Punishment` or `SessionRoute.RetrySubject`. Every failure decrements `Lives` by exactly 1, calls `RecordFailedVisit()` on that subject's record, and clears the active subject. `GameSession.PreviewRoute` returns the same route without mutating anything.

- [ ] **Step 1: Confirm no Unity Editor holds the project lock**

```bash
tasklist //FI "IMAGENAME eq Unity.exe"
```

Expected: `INFO: No tasks are running which match the specified criteria.`
If an Editor is listed, stop it with `taskkill //FI "IMAGENAME eq Unity.exe" //F` before continuing.

- [ ] **Step 2: Rewrite the failing EditMode tests**

In `Assets/Tests/EditMode/Progression/GameSessionTests.cs`, replace the test named `StartSubject_CannotBypassPunishmentBeforeSecondFailure` (lines 18-31) with:

```csharp
        [Test]
        public void AnotherSubjectCanBeStartedAfterALoss()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(4));
            Assert.That(session.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));

            Assert.That(session.StartSubject(SubjectId.Endurance), Is.EqualTo(SessionRoute.Subject));
        }
```

Replace the test named `FirstFail_RoutesPunishment_ThenSecondFailLosesLife` (lines 33-43) with:

```csharp
        [Test]
        public void EveryFailureCostsExactlyOneLifeAndRoutesToMap()
        {
            var session = new GameSession();

            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(4));

            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(3));
            Assert.That(session.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(2));
        }

        [Test]
        public void AFailureNeverRoutesToPunishment()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            SessionRoute route = session.SubmitResult(SubjectId.Sprint, Failed());

            Assert.That(route, Is.Not.EqualTo(SessionRoute.Punishment));
            Assert.That(route, Is.Not.EqualTo(SessionRoute.RetrySubject));
            Assert.That(session.AwaitingPunishment, Is.False);
            Assert.That(session.PendingPunishmentSubject, Is.Null);
        }
```

In `LastLifeLost_ReturnsGameOver` (lines 45-62), delete the `session.CompletePunishment();` line and the now-redundant second `SubmitResult`, so the loop body becomes one attempt per life:

```csharp
        [Test]
        public void LastLifeLost_ReturnsGameOver()
        {
            var session = new GameSession();

            for (var attempt = 0; attempt < 5; attempt++)
            {
                session.StartSubject(SubjectId.Sprint);
                Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()),
                    Is.EqualTo(attempt == 4 ? SessionRoute.GameOver : SessionRoute.Map));
            }

            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.StartSubject(SubjectId.Sprint), Is.EqualTo(SessionRoute.GameOver));
        }
```

At line 96, inside `AcceptedResult_IsRetainedAsSnapshot_AndFailedResultCannotReplaceIt`, change the expected route from `SessionRoute.Punishment` to `SessionRoute.Map`.

At line 111, inside `BonusScoreCannotOverrideFailedResult`, change the expected route from `SessionRoute.Punishment` to `SessionRoute.Map`.

- [ ] **Step 3: Run the EditMode tests to verify they fail**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform EditMode \
  -testFilter "KMA.Tests.Gameplay.Progression.GameSessionTests" \
  -testResults Builds/TestResults/task1-red.xml -logFile Builds/TestResults/task1-red.log
```

Unity exits 2 when tests fail. Read the result, do not trust the exit code alone:

```bash
grep -o 'result="[A-Za-z]*"' Builds/TestResults/task1-red.xml | sort | uniq -c
```

Expected: FAIL. `AnotherSubjectCanBeStartedAfterALoss`, `EveryFailureCostsExactlyOneLifeAndRoutesToMap`, `AFailureNeverRoutesToPunishment`, `LastLifeLost_ReturnsGameOver`, `AcceptedResult_IsRetainedAsSnapshot_AndFailedResultCannotReplaceIt`, and `BonusScoreCannotOverrideFailedResult` all fail with routes reported as `Punishment` where `Map` was expected.

- [ ] **Step 4: Make the production change**

In `Assets/_Project/Scripts/Progression/GameSession.cs`, replace the body of `SubmitResult` (lines 178-203) with:

```csharp
        public SessionRoute SubmitResult(SubjectId id, MinigameResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            RequireActive(id);

            SessionRoute route = RouteForResult(result);

            if (result.Pass)
            {
                records[id].Accept(result);
                ClearActiveSubject();
                return route;
            }

            Lives--;
            records[id].RecordFailedVisit();
            ClearActiveSubject();
            return route;
        }
```

and replace `RouteForResult` (lines 205-209) with:

```csharp
        // Lives is read before SubmitResult decrements it, so "Lives <= 1" means
        // "this loss empties the last life".
        SessionRoute RouteForResult(MinigameResult result) => result.Pass
            ? SessionRoute.Map
            : Lives <= 1 ? SessionRoute.GameOver : SessionRoute.Map;
```

Leave `CompletePunishment()`, `PendingPunishmentSubject`, `AwaitingPunishment`, `VisitAttempt`, and the `FinalVisit` constant exactly as they are.

- [ ] **Step 5: Run the EditMode tests to verify they pass**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform EditMode \
  -testFilter "KMA.Tests.Gameplay.Progression.GameSessionTests" \
  -testResults Builds/TestResults/task1-green.xml -logFile Builds/TestResults/task1-green.log
grep -o 'result="[A-Za-z]*"' Builds/TestResults/task1-green.xml | sort | uniq -c
```

Expected: PASS, zero `result="Failed"`.

- [ ] **Step 6: Update the two CoreLoopTests assertions**

In `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs`, rename the test at line 11 from `PreviewRoute_FirstFailureReturnsPunishmentWithoutMutation` to `PreviewRoute_FailureReturnsMapWithoutMutation`, and at line 17 change the expected route from `SessionRoute.Punishment` to `SessionRoute.Map`. Leave line 19's `Assert.That(session.PendingPunishmentSubject, Is.Null);` — it is still the right assertion and now holds for a stronger reason.

At line 54 change `panel.Show(new MinigameResult(false, 0f, Rank.F), "Punishment");` to `panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");` and at line 62 change the expected string from `"Punishment"` to `"Map"`.

- [ ] **Step 7: Run CoreLoopTests to verify they pass**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform PlayMode \
  -testFilter "KMA.Tests.Gameplay.Progression.CoreLoopTests" \
  -testResults Builds/TestResults/task1-coreloop.xml -logFile Builds/TestResults/task1-coreloop.log
grep -o 'result="[A-Za-z]*"' Builds/TestResults/task1-coreloop.xml | sort | uniq -c
```

Expected: PASS, zero `result="Failed"`.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Project/Scripts/Progression/GameSession.cs \
        Assets/Tests/EditMode/Progression/GameSessionTests.cs \
        Assets/Tests/PlayMode/Progression/CoreLoopTests.cs
git commit -m "feat: charge one life per loss and drop the punishment route

Every failed subject attempt now costs a life and returns the player to
subject select, or ends the run when it empties the last life. The
punishment leg is no longer reachable from a result.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Neutralise legacy punishment flags on restore

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs:117-133`
- Test: `Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs`
- Test: `Assets/Tests/EditMode/Progression/SaveSystemTests.cs:150-175`

**Interfaces:**
- Consumes: the Task 1 rule — `SubmitResult` no longer produces punishment state, so only *pre-existing* save files can carry it.
- Produces: `GameSession.Restore(SaveData)` never leaves `AwaitingPunishment` true and never leaves `VisitAttempt` at `2`. A well-formed save with `hasActiveSubject = true` restores that subject with `VisitAttempt == 1` and `AwaitingPunishment == false`, so `ResumeRoute()` returns `SessionRoute.Subject`. Malformed saves keep falling back to no active attempt exactly as before.

**Why this matters:** a save written by the shipped build can carry `awaitingPunishment = true`. Restored verbatim, `ResumeRoute()` returns `SessionRoute.Punishment` and drops the player into a scene that nothing can complete any more — a soft-lock with no exit.

- [ ] **Step 1: Write the failing tests**

In `Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs`:

Rename the test at line 11 from `RoundTrip_AfterFirstFailure_KeepsActiveAttemptAndPendingPunishment` to `RoundTrip_AfterAFailure_KeepsNoPendingPunishment`, and change its line 20 assertion from `Is.EqualTo(SubjectId.Sprint)` to `Is.Null`.

Rename the test at line 69 from `RoundTrip_AwaitingPunishment_ResumesPunishmentWithRecordsAndLives` to `RoundTrip_LegacyAwaitingPunishment_ResumesTheSubjectInstead`, and change its assertions at lines 81-85 to:

```csharp
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Subject));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Endurance));
            Assert.That(restored.PendingPunishmentSubject, Is.Null);
            Assert.That(restored.VisitAttempt, Is.EqualTo(1));
            Assert.That(restored.AwaitingPunishment, Is.False);
```

At lines 102-103, change `restored.VisitAttempt` expected from `2` to `1`, leaving `AwaitingPunishment` expected `False`.

At lines 217-222, in the test that builds `active.visitAttempt = 2; active.awaitingPunishment = true;`, change the expected resume route from `SessionRoute.Punishment` to `SessionRoute.Subject`.

Append this new test to the same class:

```csharp
        [Test]
        public void Restore_LegacyPunishmentSave_NeverResumesIntoPunishment()
        {
            SaveData data = SaveData.CreateDefault();
            data.lives = 3;
            data.hasActiveSubject = true;
            data.activeSubject = SubjectId.Sprint;
            data.visitAttempt = 2;
            data.awaitingPunishment = true;

            var session = new GameSession();
            session.Restore(data);

            Assert.That(session.AwaitingPunishment, Is.False);
            Assert.That(session.PendingPunishmentSubject, Is.Null);
            Assert.That(session.VisitAttempt, Is.EqualTo(1));
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Subject));
            Assert.That(session.ActiveSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(session.Lives, Is.EqualTo(3));
        }
```

In `Assets/Tests/EditMode/Progression/SaveSystemTests.cs`, at lines 173-174, change the two assertions to:

```csharp
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Subject));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Badminton));
```

Leave lines 158-169 alone: they assert the *file* round-trips `visitAttempt = 2` and `awaitingPunishment = true` unchanged, which is still true — the save format does not change, only how a session interprets it.

- [ ] **Step 2: Run the tests to verify they fail**

```bash
tasklist //FI "IMAGENAME eq Unity.exe"
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform EditMode \
  -testFilter "KMA.Tests.Gameplay.Progression" \
  -testResults Builds/TestResults/task2-red.xml -logFile Builds/TestResults/task2-red.log
grep -o 'result="[A-Za-z]*"' Builds/TestResults/task2-red.xml | sort | uniq -c
```

Expected: FAIL. `Restore_LegacyPunishmentSave_NeverResumesIntoPunishment` and the renamed round-trip tests fail, reporting `Punishment` where `Subject` was expected.

- [ ] **Step 3: Make the production change**

In `Assets/_Project/Scripts/Progression/GameSession.cs`, replace the tail of `RestoreActiveAttempt` (lines 117-133) with:

```csharp
        void RestoreActiveAttempt(SaveData data)
        {
            ClearActiveSubject();

            if (!data.hasActiveSubject || Lives <= 0)
                return;
            if (!Enum.IsDefined(typeof(SubjectId), data.activeSubject))
                return;
            if (data.visitAttempt != FirstVisit && data.visitAttempt != FinalVisit)
                return;
            if (data.awaitingPunishment && data.visitAttempt != FinalVisit)
                return;

            // The punishment leg is no longer routable. A save written before it was removed
            // can carry awaitingPunishment and FinalVisit; restoring those verbatim would send
            // the player to a scene nothing can complete. Resume the subject attempt instead.
            // The two guards above still stand so a malformed save keeps falling back to no
            // active attempt.
            active = data.activeSubject;
            visitAttempt = FirstVisit;
            awaitingPunishment = false;
        }
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform EditMode \
  -testFilter "KMA.Tests.Gameplay.Progression" \
  -testResults Builds/TestResults/task2-green.xml -logFile Builds/TestResults/task2-green.log
grep -o 'result="[A-Za-z]*"' Builds/TestResults/task2-green.xml | sort | uniq -c
```

Expected: PASS, zero `result="Failed"`. This filter also covers `SaveDataTests` and `ChallengeSequenceTests`; if either fails, stop and report rather than editing them — the plan predicts they are unaffected.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Progression/GameSession.cs \
        Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs \
        Assets/Tests/EditMode/Progression/SaveSystemTests.cs
git commit -m "fix: stop legacy saves resuming into the dead punishment scene

A save written before the punishment leg was removed can carry
awaitingPunishment, which ResumeRoute would honour by loading a scene
nothing can complete. Neutralise both flags on restore and resume the
subject attempt instead. Malformed saves still fall back to no attempt.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Bring the PlayMode route tests to the new rule

**Files:**
- Modify: `Assets/Tests/PlayMode/Progression/PunishmentRouteTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs:73-91`
- Modify: `Assets/Tests/PlayMode/Progression/VolleyballCampaignTests.cs:77-100`
- Modify: `Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs:193`

**Interfaces:**
- Consumes: the Task 1 and Task 2 behaviour in full.
- Produces: no production interface. This task only realigns tests.

**Read this before editing:** the breakage here is mostly *setup*, not assertions. Many tests reach punishment state by calling `SubmitResult(fail)` then `CompletePunishment()`. After Task 1, `SubmitResult(fail)` clears the active subject, so the following `CompletePunishment()` throws `InvalidOperationException("No punishment is active.")`. Every such setup pair must be replaced by a plain `StartSubject` for the next attempt.

- [ ] **Step 1: Run the PlayMode suite and enumerate the real failures**

Do not predict the list — measure it.

```bash
tasklist //FI "IMAGENAME eq Unity.exe"
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform PlayMode \
  -testResults Builds/TestResults/task3-red.xml -logFile Builds/TestResults/task3-red.log
grep -o 'name="[^"]*" *[^>]*result="Failed"' Builds/TestResults/task3-red.xml
```

Write the failing test names into the task notes before changing anything. Work through that list, not through memory.

- [ ] **Step 2: Invert PunishmentRouteTests**

Replace the single `[UnityTest]` in `Assets/Tests/PlayMode/Progression/PunishmentRouteTests.cs` (the method `KeyboardInput_CompletesLivePunishmentAndRoutesSprintRetry`) with:

```csharp
        [UnityTest]
        public IEnumerator SprintLoss_ReturnsToSubjectSelect_WithoutEnteringPunishment()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            int livesBefore = router.Session.Lives;
            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForScene("MG_Sprint");

            Assert.That(router.SubmitSubjectResult(SubjectId.Sprint,
                new MinigameResult(false, 0f, Rank.F)), Is.True);
            yield return WaitForScene("Map");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Map"));
            Assert.That(router.Session.PendingPunishmentSubject, Is.Null);
            Assert.That(router.Session.AwaitingPunishment, Is.False);
            Assert.That(router.Session.Lives, Is.EqualTo(livesBefore - 1));
        }
```

The keyboard helpers `PressKey` and `HoldKey`, the `testKeyboard` field, and the `InputTestFixture` base are now unused. Delete the `testKeyboard` field, the two helper methods, the `InputSystem.RemoveDevice` block in `TearDown`, and the `using UnityEngine.InputSystem;` import; change the class to `public sealed class PunishmentRouteTests` with plain `[SetUp]`/`[TearDown]` attributes instead of the `InputTestFixture` overrides. Keep the `SceneRouter` cleanup loop and the `BossSceneSessionHandoff.ClearPendingSession()` calls in both.

Keep the file at its current path and name — it still earns its name by proving the route is gone.

- [ ] **Step 3: Realign FullGameplayFlowTests**

In `FullFlow_UsesAttemptsLivesNormalizedResultsAndBossUnlock`, replace lines 50-62 with:

```csharp
            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail();
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Map));
            harness.CompleteTransition();

            harness.Start(SubjectId.Sprint);
            Assert.That(harness.Route, Is.EqualTo(SessionRoute.Subject));
            harness.CompleteTransition();

            harness.Fail();
            Assert.That(harness.Session.Lives, Is.EqualTo(3));
```

Note the life count on the last line: it was `4`, because the first failure used to be free. Two failures now cost two lives, so it becomes `3`. Read the rest of that test and adjust any later life or attempt expectation by the same reasoning — every failure in it now costs one more life than it used to.

In `FailedVisit_DoesNotStoreNonNormalizedFailedScore`, replace lines 102-108 with:

```csharp
            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail(new MinigameResult(false, 10f, Rank.S));
            harness.CompleteTransition();
            harness.Start(SubjectId.Sprint);
            harness.CompleteTransition();
            harness.Fail(new MinigameResult(false, 10f, Rank.S));
```

At line 138 change `AssertRoute(router, SessionRoute.Punishment, SubjectId.Sprint)` to `AssertRoute(router, SessionRoute.Map, SubjectId.Sprint)`. At lines 142, 144, and 156 change `SessionRoute.RetrySubject` to `SessionRoute.Subject`, since a retry is now an ordinary new attempt.

Rename the test at line 268 from `Continue_AfterFirstFailure_ResumesPunishmentForTheSameSubjectAcrossRelaunch` to `Continue_AfterFirstFailure_ResumesAtSubjectSelectAcrossRelaunch`. Change line 278 to `yield return WaitForRoutedScene(router, "Map");`, line 282 to expect `visitAttempt` of `1`, line 283 to expect `awaitingPunishment` `false`, line 292 to expect `SessionRoute.Map`, line 303 to expect `SessionRoute.Map`, line 305 to expect `PendingPunishmentSubject` `Is.Null`, and line 309 to wait for `"Map"`.

Delete the test at line 316, `CompletingProductionPunishment_PersistsTheRetryAttemptAcrossRelaunch`, in full. It exercises `PunishmentSceneController` through a route that no longer exists; there is nothing to rewrite it into. Remove the now-unused `PunishmentSceneController` import if no other test in the file references it.

Delete the harness method at line 466, `public void CompletePunishment() => RouteSession(Session.CompletePunishment(), active);`, once no test calls it.

- [ ] **Step 4: Realign S5NewGameTests**

Rename the test at line 422 from `Continue_AwaitingPunishment_RequestsPunishmentOnly` to `Continue_AfterALoss_RequestsSubjectSelectOnly`. At line 430 change the expected route/scene triple to `SessionRoute.Map, SubjectId.Sprint, "Map"`. Delete the `session.CompletePunishment();` at line 441 and replace it with `session.StartSubject(SubjectId.Endurance);`, leaving line 443's expectation as `SessionRoute.Subject, SubjectId.Endurance, "MG_Endurance"`.

At lines 554-555, the test seeds a legacy save with `visitAttempt = 2; awaitingPunishment = true;`. Keep that seed — it is now exactly the legacy-save case Task 2 guards — and confirm lines 576-577 still expect `visitAttempt` `1` and `awaitingPunishment` `false`. They should now pass for the new reason.

Lines 533-536 and 598-608 assert `AwaitingPunishment` is false or unchanged; they should pass untouched. Leave them.

- [ ] **Step 5: Realign the two campaign tests**

In `Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs`, rename the test at line 73 to `BasketballFailure_ReturnsToSubjectSelectAndSpendsALife`. Change line 81 to `yield return WaitForRoute(router, "Map");`, line 83 to expect `PendingPunishmentSubject` `Is.Null`, and line 86 to expect `saved.awaitingPunishment` `Is.False`. Delete lines 88-91 (the `CompletePunishment` call and its follow-up assertions) and replace them with an assertion that a life was spent:

```csharp
            Assert.That(router.Session.Lives, Is.EqualTo(livesBefore - 1));
```

capturing `int livesBefore = router.Session.Lives;` immediately before the `SubmitSubjectResult` call.

Apply the same shape to `Assets/Tests/PlayMode/Progression/VolleyballCampaignTests.cs`: rename the test at line 77 to `VolleyballFailure_ReturnsToSubjectSelectAndSpendsALife`, change line 85 to wait for `"Map"`, line 87 to expect `Is.Null`, line 93 to expect `Is.False`, and replace lines 95-100 with the same `livesBefore - 1` assertion. Drop the trailing message string at line 100 — it describes the removed rule.

Note both files already carry uncommitted modifications on this branch (`git status` lists them). Do not revert that work; layer these edits on top.

- [ ] **Step 6: Realign GameManagerStartupTests**

The test is `LifeLost_SavesExactlyOnce`. Its old setup failed once at session level (free, under the old rule), completed the punishment, then failed once at router level to spend the life and trigger the save. Under the new rule the first session-level failure already spends the life and clears the active subject, so the router-level call would throw from `RequireActive`. One failure is now all the test needs. Replace lines 191-197 with:

```csharp
            router.Session.StartSubject(SubjectId.Sprint);
            router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F));

            Assert.That(manager.Session.Lives, Is.EqualTo(4));
            Assert.That(saves, Is.EqualTo(1));
```

- [ ] **Step 7: Run the PlayMode suite to verify it passes**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform PlayMode \
  -testResults Builds/TestResults/task3-green.xml -logFile Builds/TestResults/task3-green.log
grep -o 'result="[A-Za-z]*"' Builds/TestResults/task3-green.xml | sort | uniq -c
```

Expected: PASS, zero `result="Failed"`.

- [ ] **Step 8: Commit**

```bash
git add Assets/Tests/PlayMode
git commit -m "test: align the playmode route tests with the new loss rule

PunishmentRouteTests is inverted to prove a loss never reaches the
punishment scene. Flows that reached a retry by completing a punishment
now start an ordinary next attempt, and the relaunch test that drove
PunishmentSceneController through the route is removed with it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Full-suite gate and QA record

**Files:**
- Create: `docs/qa/loss-route-gate.md`

**Interfaces:**
- Consumes: all three previous tasks.
- Produces: a committed record of the verification run, matching the existing gate docs in `docs/qa/`.

- [ ] **Step 1: Run both platforms clean**

```bash
tasklist //FI "IMAGENAME eq Unity.exe"
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform EditMode \
  -testResults Builds/TestResults/gate-editmode.xml -logFile Builds/TestResults/gate-editmode.log
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/project/the-chat-KMA" -runTests -testPlatform PlayMode \
  -testResults Builds/TestResults/gate-playmode.xml -logFile Builds/TestResults/gate-playmode.log
```

- [ ] **Step 2: Confirm both results are clean**

```bash
for f in Builds/TestResults/gate-editmode.xml Builds/TestResults/gate-playmode.xml; do
  echo "== $f =="
  grep -o 'result="[A-Za-z]*"' "$f" | sort | uniq -c
done
```

Expected: zero `result="Failed"` in both. If anything fails, fix it before writing the gate doc — a gate doc recording a red run is worse than no gate doc.

- [ ] **Step 3: Write the gate document**

Create `docs/qa/loss-route-gate.md` following the shape of `docs/qa/sprint-ui-polish-gate.md`. Record: the date, the spec and plan paths, the exact two commands run, the counted pass/fail totals taken from the XML, and the behaviour verified — a loss spends one life and returns to subject select; the last life ends the run; a legacy save carrying `awaitingPunishment` resumes at the subject instead of the dead scene. State plainly that the Punishment scene and its controllers remain in the repo but are unreachable from any route.

- [ ] **Step 4: Commit**

```bash
git add docs/qa/loss-route-gate.md
git commit -m "docs: record the loss-route verification gate

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Known follow-up, deliberately not in this plan

The Sprint screen has a separate layout defect: the scoreboard, mode chip, pause button, and countdown collapse toward screen centre and overlap the instruction line (`Builds/Screenshots/diag_sprint.png`). One confirmed contributor is `SprintFestivalPresentation.cs:443`, where `EnsurePause` returns early because `MinigameUIAssembler.EnsurePausePanel()` has already placed a `PausePanel` in `MG_Sprint`, so the Sprint pause button is never built. The root cause of the wider collapse is not established. It needs its own debugging pass and its own spec. Do not attempt a fix inside this plan.
