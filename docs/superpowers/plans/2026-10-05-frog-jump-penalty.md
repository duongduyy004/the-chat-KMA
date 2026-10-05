# Frog-Jump Penalty and Timed Life Regen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Failing a journey practice or exam sends the player through a mandatory 60-second frog-jump minigame that can save their life on the first failure of that challenge; lives regenerate one per five minutes instead of through the supplementary-practice round.

**Architecture:** Pure rules first: `LifeRegen` (Progression) owns the regen clock maths, `JourneyProgress` owns per-challenge fail counts and a single `FrogJumpPending` record, `FrogJumpRules` (new `KMA.Gameplay.FrogJump` assembly) owns the power bar and the 60 s race. `GameSession` stitches regen into every start/commit/restore. `SceneRouter.FrogJump.cs` routes failure → `MG_FrogJump` → back to the failed challenge (or Map at 0 lives) through a new `IFrogJumpResultPanel` implemented by the shared `ResultPanel`. The Map header shows the regen countdown on `HeartBar`.

**Tech Stack:** Unity 6000.3.23f1, C# (asmdef-split), uGUI + TextMeshPro, NUnit EditMode/PlayMode, Unity batch mode via `tools/run-unity-tests.sh`.

**Spec:** `docs/superpowers/specs/2026-10-05-frog-jump-penalty-design.md` (supersedes section 5 of `2026-10-04-kma-student-journey-design.md`).

## Global Constraints

- Applies only to `ChallengeKind.Practice` and `ChallengeKind.Exam` in `ChallengeAttemptMode.Journey`. Learn, `Review`, `FreePlay` never cost lives and never create a frog jump.
- Failure #1 of a challenge: no life lost at commit; frog jump win keeps the life, loss costs 1. Failure #2+: −1 life at commit; frog jump result does not change lives. A pass resets that challenge's count to 0. Counts are per challenge id.
- Lives: max `GameSession.MaxLives = 5`, +1 every **5 minutes** of real UTC time, clamped `[0, 5]`. Practice/exam in Journey mode needs ≥ 1 life; Learn and Review do not.
- Clock moved backwards (`now < nextLifeAt − 5 min`) resets `nextLifeAt = now + 5 min`; no free lives.
- Frog jump tuning: track **40 m**, needle sweep **1.2 s** edge-to-edge, jump **3.0 m** at centre → **1.0 m** at `d = 0.8`, `d > 0.8` falls, jump **0.6 s**, recover **2.5 s**, limit **60 s**. All in `FrogJumpTuning`.
- `volleyball_practice`: `timeLimit = 120`. `soccer_practice`: `attemptLimit = 6`, ends as soon as 2 goals are scored.
- Supplementary mode is removed from the flow. `ChallengeAttemptMode.Supplementary` stays in the enum (serialized as int); `TryBegin` always rejects it. `supplementaryRounds` is still read/written but no longer displayed.
- New enum members go **at the end**: `SessionRoute.FrogJump`, `JourneyResultAction.FrogJump`.
- Copy: header label stays `Lượt thi: X/5`. New strings use "lượt thi": button `BẬT CÓC`, `Về đích trong 60 s để giữ lượt thi`, `−1 lượt thi. Bật cóc xong mới được thi lại`, lesson lock `Hết lượt thi`, tutorial `Chạm khi kim ở giữa để bật xa. Sát mép là ngã!`. All UI strings go through `VietText.Fix`.
- `SaveData.CurrentVersion` 7 → 8. Version 7 saves keep their journey (only `< 7` goes through `MigrateLegacy`).
- Punishment code (`PunishmentController`, `PunishmentSceneController`, `Punishment.unity`) stays untouched.
- Commit directly to `master`; commit messages carry **no** `Co-Authored-By` trailer (user rule). Always `git add` explicit paths; never stage `chay-tron-the-chat-assets.zip`.
- Unity must be closed for batch runs. Tests: `tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>`, read `Builds/TestResults/<name>.xml` (Unity can exit 0 on failures). Editor scripts: `"$UNITY" -batchmode -projectPath . -executeMethod <Method> -quit -logFile Builds/<name>.log` where `$UNITY` is the editor the test script resolves (`/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe`).

## Review Focus

1. A repeated `Completed` event or double-tap on the frog result button must not apply the frog result or spend a life twice (pinned in Task 2 and Task 7).
2. App killed during a first-failure frog jump: next launch counts it as a loss (−1 life) exactly once, and persists that immediately so a second kill does not re-roll it (Task 3).
3. Sitting on the Map at 0 lives: when the countdown hits 0 the life appears, the card unlocks and the save is written without reloading the scene (Task 8).
4. Device clock moved backwards by hours: no lives granted, countdown resets to 5:00 rather than showing a huge wait (Task 1).
5. Second failure at 1 life → 0 lives: after the frog jump the player lands on the Map, not inside the failed challenge, and the Map refuses to start it (Task 2 and Task 7).

## File Structure

| File | Responsibility |
| --- | --- |
| `Assets/_Project/Scripts/Progression/LifeRegen.cs` (create) | `IClock`, `SystemClock`, `LifeRegen.Advance/Remaining` |
| `Assets/_Project/Scripts/Progression/Journey/ChallengeAttempt.cs` (modify) | add `FrogJumpPending` |
| `Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs` (modify) | `JourneyCommitOutcome` frog fields, `JourneyResultAction.FrogJump`, `FrogJumpResultView`, `IFrogJumpResultPanel` |
| `Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs` (modify) | fail counts, pending frog jump, life gate, supplementary removal |
| `Assets/_Project/Scripts/Progression/Journey/JourneyStateData.cs` (modify) | serialized fail counts / pending frog jump |
| `Assets/_Project/Scripts/Progression/GameSession.cs` (modify) | clock, regen, frog APIs, resume route, `SessionRoute.FrogJump` |
| `Assets/_Project/Scripts/Progression/SaveData.cs`, `Journey/JourneySaveMigration.cs` (modify) | v8 fields and normalization |
| `Assets/_Project/Scripts/Progression/Journey/ChallengeDefinition.cs` (modify) | `attemptLimit` |
| `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs` (modify) | practice 120 s limit |
| `Assets/_Project/Scripts/Gameplay/Football/FootballMatchOptions.cs`, `FootballRules.cs`, `FootballController.cs` (modify) | practice 6-kick limit, stop at target |
| `Assets/Editor/StudentJourneyContentBuilder.cs` (modify) + regenerated `Assets/_Project/ScriptableObjects/Journey/*.asset` | new practice limits and objectives |
| `Assets/_Project/Scripts/Gameplay/FrogJump/` (create: asmdef, `FrogJumpTuning.cs`, `FrogJumpRules.cs`, `FrogJumpBalanceConfig.cs`, `FrogJumpController.cs`, `FrogJumpView.cs`, `FrogJumpPowerBar.cs`, `FrogJumpTapArea.cs`) | the minigame |
| `Assets/Editor/FrogJumpSceneConfigurator.cs` (create) + `Assets/_Project/Scenes/MG_FrogJump.unity` (generated) | scene authoring, build settings |
| `Assets/_Project/Scripts/UI/PausePanel.cs` (modify) | hide restart/exit in the frog scene |
| `Assets/_Project/Scripts/Core/SceneRouter.cs`, `SceneRouter.Journey.cs` (modify), `SceneRouter.FrogJump.cs` (create) | frog routing |
| `Assets/_Project/Scripts/Core/GameManager.cs` (modify) | persist after restore-time forfeit |
| `Assets/_Project/Scripts/UI/ResultPanel.cs` (modify) | `BẬT CÓC` action, frog result display |
| `Assets/_Project/Scripts/UI/HeartBar.cs`, `MapScreen.cs`, `JourneyLessonList.cs`, `JourneyCourseSummary.cs`, `JourneyDialoguePresenter.cs` (modify) | countdown, live regen, `Hết lượt thi`, supplementary UI removal |
| Tests under `Assets/Tests/EditMode/{Progression,Gameplay/FrogJump,Gameplay/Volleyball,Gameplay/Ball,Presentation}` and `Assets/Tests/PlayMode/{Progression,Presentation}` | see each task |

---

### Task 1: `LifeRegen` and `IClock`

**Files:**
- Create: `Assets/_Project/Scripts/Progression/LifeRegen.cs`
- Create: `Assets/Tests/EditMode/Progression/LifeRegenTests.cs`
- Create: `Assets/Tests/EditMode/Progression/FakeClock.cs`

**Interfaces:**
- Produces: `interface IClock { DateTime UtcNow { get; } }`, `SystemClock.Instance`, `LifeRegen.Interval` (`TimeSpan`, 5 min), `static int LifeRegen.Advance(ref int lives, ref long nextLifeAtUtcTicks, DateTime utcNow, int maxLives)` (returns lives gained), `static TimeSpan? LifeRegen.Remaining(int lives, long nextLifeAtUtcTicks, DateTime utcNow, int maxLives)`. Test helper `FakeClock : IClock` with settable `UtcNow` and `Advance(TimeSpan)`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Progression/FakeClock.cs`:

```csharp
using System;
using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        public void Advance(TimeSpan by) => UtcNow += by;
    }
}
```

`Assets/Tests/EditMode/Progression/LifeRegenTests.cs`:

```csharp
using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class LifeRegenTests
    {
        static readonly DateTime T0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        static readonly TimeSpan Five = TimeSpan.FromMinutes(5);

        [Test]
        public void FullLivesClearTheMark()
        {
            int lives = 5; long next = T0.Ticks;
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0, 5), Is.Zero);
            Assert.That(next, Is.Zero);
            Assert.That(LifeRegen.Remaining(lives, next, T0, 5), Is.Null);
        }

        [Test]
        public void DroppingBelowMaxStartsAFiveMinuteMark()
        {
            int lives = 4; long next = 0;
            LifeRegen.Advance(ref lives, ref next, T0, 5);
            Assert.That(lives, Is.EqualTo(4));
            Assert.That(next, Is.EqualTo((T0 + Five).Ticks));
            Assert.That(LifeRegen.Remaining(lives, next, T0 + TimeSpan.FromSeconds(28), 5),
                Is.EqualTo(TimeSpan.FromSeconds(272)));
        }

        [Test]
        public void SpendingAgainWhileRunningKeepsTheMark()
        {
            int lives = 4; long next = 0;
            LifeRegen.Advance(ref lives, ref next, T0, 5);
            long mark = next;
            lives = 3;
            LifeRegen.Advance(ref lives, ref next, T0 + TimeSpan.FromMinutes(2), 5);
            Assert.That(next, Is.EqualTo(mark));
        }

        [Test]
        public void ReachingTheMarkGrantsOneAndChainsTheNext()
        {
            int lives = 3; long next = (T0 + Five).Ticks;
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0 + Five, 5), Is.EqualTo(1));
            Assert.That(lives, Is.EqualTo(4));
            Assert.That(next, Is.EqualTo((T0 + Five + Five).Ticks));
        }

        [Test]
        public void LongAbsenceGrantsSeveralAndStopsAtMax()
        {
            int lives = 0; long next = (T0 + Five).Ticks;
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0 + TimeSpan.FromMinutes(12), 5), Is.EqualTo(2));
            Assert.That(lives, Is.EqualTo(2));
            Assert.That(next, Is.EqualTo((T0 + TimeSpan.FromMinutes(15)).Ticks));
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0 + TimeSpan.FromDays(3), 5), Is.EqualTo(3));
            Assert.That(lives, Is.EqualTo(5));
            Assert.That(next, Is.Zero);
        }

        [Test]
        public void ClockMovedBackwardsResetsTheMarkWithoutGrantingLives()
        {
            int lives = 2; long next = (T0 + Five).Ticks;
            DateTime rewound = T0 - TimeSpan.FromHours(6);
            Assert.That(LifeRegen.Advance(ref lives, ref next, rewound, 5), Is.Zero);
            Assert.That(lives, Is.EqualTo(2));
            Assert.That(next, Is.EqualTo((rewound + Five).Ticks));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression.LifeRegenTests" life-regen`
Expected: no XML / `error CS0246: The type or namespace name 'IClock'` in the log.

- [ ] **Step 3: Write the implementation**

`Assets/_Project/Scripts/Progression/LifeRegen.cs`:

```csharp
using System;

namespace KMA.Gameplay
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new SystemClock();
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// One life every Interval of real time while below max. The mark is the UTC tick at which
    /// the next life arrives; 0 means the clock is not running.
    public static class LifeRegen
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        public static int Advance(ref int lives, ref long nextLifeAtUtcTicks, DateTime utcNow, int maxLives)
        {
            lives = Math.Max(0, Math.Min(maxLives, lives));
            if (lives >= maxLives)
            {
                nextLifeAtUtcTicks = 0;
                return 0;
            }

            long now = utcNow.Ticks;
            // A mark further away than one interval means the device clock went backwards.
            if (nextLifeAtUtcTicks <= 0 || now < nextLifeAtUtcTicks - Interval.Ticks)
            {
                nextLifeAtUtcTicks = now + Interval.Ticks;
                return 0;
            }

            int gained = 0;
            while (lives < maxLives && now >= nextLifeAtUtcTicks)
            {
                lives++;
                gained++;
                nextLifeAtUtcTicks += Interval.Ticks;
            }
            if (lives >= maxLives)
                nextLifeAtUtcTicks = 0;
            return gained;
        }

        public static TimeSpan? Remaining(int lives, long nextLifeAtUtcTicks, DateTime utcNow, int maxLives) =>
            lives >= maxLives || nextLifeAtUtcTicks <= 0
                ? (TimeSpan?)null
                : TimeSpan.FromTicks(Math.Max(0L, nextLifeAtUtcTicks - utcNow.Ticks));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression.LifeRegenTests" life-regen`
Expected: `result="Passed"`, 6 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Progression/LifeRegen.cs Assets/_Project/Scripts/Progression/LifeRegen.cs.meta \
  Assets/Tests/EditMode/Progression/LifeRegenTests.cs Assets/Tests/EditMode/Progression/LifeRegenTests.cs.meta \
  Assets/Tests/EditMode/Progression/FakeClock.cs Assets/Tests/EditMode/Progression/FakeClock.cs.meta
git commit -m "feat(progression): add real-time life regeneration rules"
```

---

### Task 2: Fail counts and pending frog jump in `JourneyProgress`

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeAttempt.cs` (append `FrogJumpPending`)
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs` (`JourneyCommitOutcome`, `JourneyResultAction`)
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneyStateData.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs`
- Create: `Assets/Tests/EditMode/Progression/FrogJumpPenaltyTests.cs`
- Modify: `Assets/Tests/EditMode/Progression/JourneyProgressTests.cs` (replace two tests, see Step 5)

**Interfaces:**
- Consumes: nothing new.
- Produces:
  - `sealed class FrogJumpPending { string Id; string FailedAttemptId; string FailedChallengeId; bool SavesLife; }` (ctor in that order).
  - `JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining, bool frogJumpRequired, bool frogJumpSavesLife, bool courseComplete)` with properties `FrogJumpRequired`, `FrogJumpSavesLife` (replaces `AwaitingSupplementary`).
  - `JourneyResultAction.FrogJump` appended after `RetrySave`; `Practice` stays (unused) for value stability.
  - `JourneyProgress`: `FrogJumpPending PendingFrogJump`, `int FailCount(string id)`, `void SetAttemptsRemaining(int value)`, `bool TryApplyFrogJump(string frogJumpId, bool reachedFinish)`, `bool ForfeitPendingFrogJump()`.
  - Temporary compile shims (Unity refuses to run any test while one assembly fails to compile, and `GameSession`, `SceneRouter`, `ResultPanel`, `JourneyLessonList`, `JourneyDialoguePresenter` and PlayMode helpers still read the old members until Tasks 3, 7 and 8): `[Obsolete] JourneyProgress.AwaitingSupplementary => false`, `[Obsolete] JourneyCommitOutcome.AwaitingSupplementary => false`, and the old 5-argument `JourneyCommitOutcome` ctor forwarding to the new one. Task 8 Step 5 deletes all three.
  - `JourneyStateData`: `List<JourneyFailCountData> failCounts`, `JourneyFrogJumpData pendingFrogJump`, `string lastAppliedFrogJumpId`; `JourneyFrogJumpData.FromPending(FrogJumpPending)` / `ToPending()` (null when `id` empty).

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Progression/FrogJumpPenaltyTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FrogJumpPenaltyTests
    {
        [Test]
        public void LearnFailureNeverCreatesAFrogJump()
        {
            var session = new GameSession();
            JourneyCommitOutcome outcome = JourneyTestData.Play(session, "sprint_learn", false);
            Assert.That(outcome.FrogJumpRequired, Is.False);
            Assert.That(session.Journey.PendingFrogJump, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(5));
        }

        [Test]
        public void FirstFailureDefersTheLifeAndAFrogWinKeepsIt()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyCommitOutcome outcome = JourneyTestData.Play(session, "sprint_practice", false);

            Assert.That(outcome.FrogJumpRequired, Is.True);
            Assert.That(outcome.FrogJumpSavesLife, Is.True);
            Assert.That(outcome.AttemptsRemaining, Is.EqualTo(5));
            FrogJumpPending pending = session.Journey.PendingFrogJump;
            Assert.That(pending.FailedChallengeId, Is.EqualTo("sprint_practice"));
            Assert.That(session.Journey.TryApplyFrogJump(pending.Id, true), Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.PendingFrogJump, Is.Null);
        }

        [Test]
        public void FirstFailureThenFrogLossCostsOneLife()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            Assert.That(session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, false), Is.True);
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void SecondFailureCostsALifeAtCommitAndTheFrogResultChangesNothing()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);

            JourneyCommitOutcome second = JourneyTestData.Play(session, "sprint_exam", false);
            Assert.That(second.AttemptsRemaining, Is.EqualTo(4));
            Assert.That(second.FrogJumpSavesLife, Is.False);
            Assert.That(session.Journey.FailCount("sprint_exam"), Is.EqualTo(2));
            Assert.That(session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true), Is.True);
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void PassingResetsOnlyThatChallengesCount()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyTestData.Play(session, "sprint_practice", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            JourneyTestData.Play(session, "sprint_practice", true);
            Assert.That(session.Journey.FailCount("sprint_practice"), Is.Zero);

            JourneyCommitOutcome exam = JourneyTestData.Play(session, "sprint_exam", false);
            Assert.That(exam.FrogJumpSavesLife, Is.True);
        }

        [Test]
        public void PendingFrogJumpBlocksEveryStart()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyTestData.Play(session, "sprint_practice", false);
            Assert.That(session.TryStartChallenge("sprint_practice", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
            Assert.That(session.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void FrogResultAppliesOnlyOnceAndOnlyForTheCurrentId()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            string id = session.Journey.PendingFrogJump.Id;
            Assert.That(session.Journey.TryApplyFrogJump("other", false), Is.False);
            Assert.That(session.Journey.TryApplyFrogJump(id, false), Is.True);
            Assert.That(session.Journey.TryApplyFrogJump(id, false), Is.False);
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void ZeroLivesBlocksPracticeAndExamButNotLearnOrReview()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.Journey.SetAttemptsRemaining(0);
            Assert.That(session.Journey.TryBegin("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
            Assert.That(session.Journey.TryBegin("sprint_learn", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext review), Is.True);
            session.Journey.AbandonAttempt();

            var fresh = new GameSession();
            fresh.Journey.SetAttemptsRemaining(0);
            Assert.That(fresh.Journey.TryBegin("sprint_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
        }

        [Test]
        public void SecondFailureAtOneLifeLeavesZeroAndTheChallengeLocked()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            session.Journey.SetAttemptsRemaining(1);
            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.Journey.TryBegin("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void SupplementaryModeIsAlwaysRejected()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            Assert.That(session.Journey.TryBegin("sprint_practice", ChallengeAttemptMode.Supplementary,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void FailCountsAndPendingFrogJumpRoundTripThroughData()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            FrogJumpPending pending = session.Journey.PendingFrogJump;

            var restored = new JourneyProgress(ChallengeCatalog.LoadDefault());
            restored.Restore(session.Journey.ToData(), 5);
            Assert.That(restored.FailCount("sprint_exam"), Is.EqualTo(1));
            Assert.That(restored.PendingFrogJump.Id, Is.EqualTo(pending.Id));
            Assert.That(restored.PendingFrogJump.SavesLife, Is.True);
            Assert.That(restored.ForfeitPendingFrogJump(), Is.True);
            Assert.That(restored.AttemptsRemaining, Is.EqualTo(4));
            Assert.That(restored.ForfeitPendingFrogJump(), Is.False);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression.FrogJumpPenaltyTests" frog-penalty`
Expected: compile error `'JourneyCommitOutcome' does not contain a definition for 'FrogJumpRequired'`.

- [ ] **Step 3: Implement the contracts and data**

Append to `ChallengeAttempt.cs` (inside `namespace KMA.Gameplay`):

```csharp
    public sealed class FrogJumpPending
    {
        public string Id { get; }
        public string FailedAttemptId { get; }
        public string FailedChallengeId { get; }
        public bool SavesLife { get; }

        public FrogJumpPending(string id, string failedAttemptId, string failedChallengeId, bool savesLife)
        {
            Id = id;
            FailedAttemptId = failedAttemptId;
            FailedChallengeId = failedChallengeId;
            SavesLife = savesLife;
        }
    }
```

In `ChallengeContracts.cs` replace `JourneyCommitOutcome` and the enum:

```csharp
    public readonly struct JourneyCommitOutcome
    {
        public bool Accepted { get; }
        public string NextChallengeId { get; }
        public int AttemptsRemaining { get; }
        public bool FrogJumpRequired { get; }
        public bool FrogJumpSavesLife { get; }
        public bool CourseComplete { get; }

        public JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining,
            bool frogJumpRequired, bool frogJumpSavesLife, bool courseComplete)
        {
            Accepted = accepted;
            NextChallengeId = nextChallengeId;
            AttemptsRemaining = attemptsRemaining;
            FrogJumpRequired = frogJumpRequired;
            FrogJumpSavesLife = frogJumpSavesLife;
            CourseComplete = courseComplete;
        }

        [Obsolete("Supplementary rounds were removed; deleted in Task 8.")]
        public JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining,
            bool awaitingSupplementary, bool courseComplete)
            : this(accepted, nextChallengeId, attemptsRemaining, false, false, courseComplete) { }

        [Obsolete("Supplementary rounds were removed; deleted in Task 8.")]
        public bool AwaitingSupplementary => false;
    }
```

```csharp
    public enum JourneyResultAction
    {
        Continue,
        Retry,
        // Retained for serialized values; the supplementary practice route was removed.
        Practice,
        RetrySave,
        FrogJump
    }
```

Add to `JourneyStateData.cs`: fields on `JourneyStateData`

```csharp
        public List<JourneyFailCountData> failCounts = new List<JourneyFailCountData>();
        public JourneyFrogJumpData pendingFrogJump;
        public string lastAppliedFrogJumpId;
```

and new classes in the same namespace:

```csharp
    [Serializable]
    public sealed class JourneyFailCountData
    {
        public string challengeId;
        public int count;
    }

    [Serializable]
    public sealed class JourneyFrogJumpData
    {
        public string id;
        public string failedAttemptId;
        public string failedChallengeId;
        public bool savesLife;

        public static JourneyFrogJumpData FromPending(FrogJumpPending pending) => pending == null
            ? null
            : new JourneyFrogJumpData
            {
                id = pending.Id,
                failedAttemptId = pending.FailedAttemptId,
                failedChallengeId = pending.FailedChallengeId,
                savesLife = pending.SavesLife
            };

        // JsonUtility writes an empty object for null fields, so an empty id means "none".
        public FrogJumpPending ToPending() => string.IsNullOrWhiteSpace(id)
            ? null
            : new FrogJumpPending(id, failedAttemptId, failedChallengeId, savesLife);
    }
```

- [ ] **Step 4: Rewrite the `JourneyProgress` rules**

In `JourneyProgress.cs`:

1. Delete `NoSupplementaryChallenge`, the `awaitingSupplementaryChallengeId` field, `IsPracticeId` and `PracticeId`. Replace `AwaitingSupplementary` with the shim `[Obsolete("Supplementary rounds were removed; deleted in Task 8.")] public bool AwaitingSupplementary => false;`.
2. Add fields:

```csharp
        readonly Dictionary<string, int> failCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        FrogJumpPending pendingFrogJump;
        string lastAppliedFrogJumpId;
```

3. Replace `CheckpointChallengeId` with:

```csharp
        public string CheckpointChallengeId =>
            catalog.Ordered.FirstOrDefault(x => !completedChallengeIds.Contains(x.Id))?.Id;
```

4. Add members:

```csharp
        public FrogJumpPending PendingFrogJump => pendingFrogJump;

        public int FailCount(string id) =>
            !string.IsNullOrEmpty(id) && failCounts.TryGetValue(id, out int count) ? count : 0;

        public void SetAttemptsRemaining(int value) =>
            attemptsRemaining = Math.Max(0, Math.Min(GameSession.MaxLives, value));

        public bool TryApplyFrogJump(string frogJumpId, bool reachedFinish)
        {
            if (pendingFrogJump == null || string.IsNullOrEmpty(frogJumpId) ||
                !string.Equals(frogJumpId, pendingFrogJump.Id, StringComparison.Ordinal) ||
                string.Equals(frogJumpId, lastAppliedFrogJumpId, StringComparison.Ordinal))
                return false;
            if (pendingFrogJump.SavesLife && !reachedFinish)
                attemptsRemaining = Math.Max(0, attemptsRemaining - 1);
            lastAppliedFrogJumpId = frogJumpId;
            pendingFrogJump = null;
            return true;
        }

        /// An unfinished frog jump only ever means one thing: it was lost.
        public bool ForfeitPendingFrogJump() =>
            pendingFrogJump != null && TryApplyFrogJump(pendingFrogJump.Id, false);
```

5. In `TryBegin`, change the first guard to also reject `pendingFrogJump != null`, and replace the `allowed` switch with:

```csharp
            bool allowed = mode switch
            {
                ChallengeAttemptMode.Journey => id == CheckpointChallengeId &&
                    (definition.Kind == ChallengeKind.Learn || attemptsRemaining > 0),
                ChallengeAttemptMode.Review => IsChallengeComplete(id) && IsSubjectUnlocked(definition.Subject),
                ChallengeAttemptMode.FreePlay => CourseComplete,
                _ => false
            };
```

and the difficulty check to `if (mode == ChallengeAttemptMode.Journey && difficulty != definition.Difficulty) return false;`.

6. Replace the mode branches in `Apply` (from `ChallengeAttemptMode mode = activeAttempt.Mode;` up to `lastCommittedAttemptId = ...`) with:

```csharp
            if (activeAttempt.Mode == ChallengeAttemptMode.Journey)
            {
                if (result.Pass)
                {
                    completedChallengeIds.Add(definition.Id);
                    failCounts.Remove(definition.Id);
                }
                else if (definition.Kind != ChallengeKind.Learn)
                {
                    int count = FailCount(definition.Id) + 1;
                    failCounts[definition.Id] = count;
                    if (count >= 2)
                        attemptsRemaining = Math.Max(0, attemptsRemaining - 1);
                    pendingFrogJump = new FrogJumpPending(Guid.NewGuid().ToString("N"),
                        activeAttempt.AttemptId, definition.Id, count == 1);
                }
            }
```

7. `Outcome(bool accepted)` becomes:

```csharp
        JourneyCommitOutcome Outcome(bool accepted) => new JourneyCommitOutcome(accepted,
            CheckpointChallengeId, attemptsRemaining, pendingFrogJump != null,
            pendingFrogJump?.SavesLife ?? false, CourseComplete);
```

8. `ToData()` drops `awaitingSupplementaryChallengeId` and adds:

```csharp
            failCounts = catalog.Ordered.Where(x => FailCount(x.Id) > 0)
                .Select(x => new JourneyFailCountData { challengeId = x.Id, count = FailCount(x.Id) }).ToList(),
            pendingFrogJump = JourneyFrogJumpData.FromPending(pendingFrogJump),
            lastAppliedFrogJumpId = lastAppliedFrogJumpId,
```

9. In `Restore`, delete the `awaitingSupplementaryChallengeId = ...` statement and add before `activeAttempt = ...`:

```csharp
            failCounts.Clear();
            if (data?.failCounts != null)
            {
                foreach (JourneyFailCountData entry in data.failCounts)
                {
                    if (entry != null && entry.count > 0 && IsPenalized(entry.challengeId))
                        failCounts[entry.challengeId] = entry.count;
                }
            }
            lastAppliedFrogJumpId = data?.lastAppliedFrogJumpId;
            pendingFrogJump = data?.pendingFrogJump?.ToPending();
            if (pendingFrogJump != null && (!IsPenalized(pendingFrogJump.FailedChallengeId) ||
                pendingFrogJump.Id == lastAppliedFrogJumpId))
                pendingFrogJump = null;
```

with helper

```csharp
        bool IsPenalized(string id) => !string.IsNullOrEmpty(id) &&
            catalog.Ordered.Any(x => x.Id == id && x.Kind != ChallengeKind.Learn);
```

- [ ] **Step 5: Replace the two supplementary-era tests in `JourneyProgressTests.cs`**

Replace `LearnAndPracticeFailures_DoNotSpendExamAttempts` with:

```csharp
        [Test]
        public void LearnFailureIsFreeAndPracticeFailureRequiresAFrogJump()
        {
            var session = new GameSession();

            Assert.That(JourneyTestData.Play(session, "sprint_learn", false).FrogJumpRequired, Is.False);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_learn"));

            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyCommitOutcome practice = JourneyTestData.Play(session, "sprint_practice", false);
            Assert.That(practice.FrogJumpRequired, Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_practice"));
        }
```

Replace `FifthExamFailure_RequiresOneFreshPracticeBeforeRestoringBudget` with:

```csharp
        [Test]
        public void RepeatedExamFailuresDrainLivesAndNeverOpenSupplementaryPractice()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");

            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, false);
            for (int i = 0; i < 4; i++)
            {
                JourneyTestData.Play(session, "sprint_exam", false);
                session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            }

            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));
            Assert.That(session.Journey.TryBegin("sprint_practice", ChallengeAttemptMode.Supplementary,
                ChallengeDifficulty.Normal, out _), Is.False);
        }
```

`JourneyTestData.Play` stays as is. Callers that fail an exam twice in a row now need a `TryApplyFrogJump` between plays because the pending frog jump blocks the next start.

- [ ] **Step 6: Run the progression EditMode tests**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" progression`
Expected: the whole project compiles (obsolete warnings only); `FrogJumpPenaltyTests` and `JourneyProgressTests` pass. Failures remaining in `GameSessionTests`, `JourneySaveTests`, `GameSessionPersistenceTests` are expected and fixed in Task 3; list them in the task report.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Progression/Journey/ChallengeAttempt.cs \
  Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs \
  Assets/_Project/Scripts/Progression/Journey/JourneyStateData.cs \
  Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs \
  Assets/Tests/EditMode/Progression/FrogJumpPenaltyTests.cs Assets/Tests/EditMode/Progression/FrogJumpPenaltyTests.cs.meta \
  Assets/Tests/EditMode/Progression/JourneyProgressTests.cs
git commit -m "feat(progression): replace supplementary round with per-challenge frog-jump penalty"
```

---

### Task 3: `GameSession` regen wiring, save v8 and restore-time forfeit

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Progression/SaveData.cs`
- Modify: `Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs`
- Modify: `Assets/_Project/Scripts/Core/GameManager.cs` (`InitializeStartup`)
- Create: `Assets/Tests/EditMode/Progression/LifeRegenSessionTests.cs`
- Modify: `Assets/Tests/EditMode/Progression/GameSessionTests.cs`, `JourneySaveTests.cs`, `GameSessionPersistenceTests.cs`

**Interfaces:**
- Consumes: Task 1 `IClock`, `LifeRegen`; Task 2 `JourneyProgress` members.
- Produces:
  - `SessionRoute.FrogJump` (appended last).
  - `GameSession(ChallengeCatalog catalog = null, IClock clock = null)`.
  - `GameSession.NextLifeAtUtcTicks` (`long`), `TimeSpan? TimeUntilNextLife`, `bool RefreshLives()` (true when lives or the mark changed), `FrogJumpPending PendingFrogJump`, `bool TryApplyFrogJump(string frogJumpId, bool reachedFinish)`, `bool ForfeitedFrogJumpOnRestore`.
  - `ResumeRoute()` → `Subject` if active, `FrogJump` if a frog jump is pending, else `Map` (never `GameOver`).
  - `SaveData.CurrentVersion = 8`, `SaveData.nextLifeAtUtcTicks`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Progression/LifeRegenSessionTests.cs`:

```csharp
using System;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class LifeRegenSessionTests
    {
        static GameSession FailOnceAndLose(FakeClock clock)
        {
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            session.TryApplyFrogJump(session.PendingFrogJump.Id, false);
            return session;
        }

        [Test]
        public void SpendingFromFullStartsTheCountdown()
        {
            var clock = new FakeClock();
            GameSession session = FailOnceAndLose(clock);
            Assert.That(session.Lives, Is.EqualTo(4));
            Assert.That(session.TimeUntilNextLife, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void RefreshGrantsLivesAfterTimePasses()
        {
            var clock = new FakeClock();
            GameSession session = FailOnceAndLose(clock);
            clock.Advance(TimeSpan.FromMinutes(5));
            Assert.That(session.RefreshLives(), Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.TimeUntilNextLife, Is.Null);
        }

        [Test]
        public void StartingAChallengeAppliesPendingRegenFirst()
        {
            var clock = new FakeClock();
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.Journey.SetAttemptsRemaining(0);
            session.RefreshLives();
            clock.Advance(TimeSpan.FromMinutes(5));
            Assert.That(session.TryStartChallenge("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
            Assert.That(session.Lives, Is.EqualTo(1));
        }

        [Test]
        public void SaveRoundTripKeepsTheMarkAndRegensWhileClosed()
        {
            var clock = new FakeClock();
            GameSession session = FailOnceAndLose(clock);
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(session.ToSaveData()));
            Assert.That(saved.version, Is.EqualTo(8));
            Assert.That(saved.nextLifeAtUtcTicks, Is.EqualTo(session.NextLifeAtUtcTicks));

            clock.Advance(TimeSpan.FromMinutes(7));
            var restored = new GameSession(null, clock);
            restored.Restore(saved);
            Assert.That(restored.Lives, Is.EqualTo(5));
        }

        [Test]
        public void RestoreForfeitsAnUnfinishedFirstFailureFrogJumpOnce()
        {
            var clock = new FakeClock();
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(session.ToSaveData()));

            var restored = new GameSession(null, clock);
            restored.Restore(saved);
            Assert.That(restored.ForfeitedFrogJumpOnRestore, Is.True);
            Assert.That(restored.PendingFrogJump, Is.Null);
            Assert.That(restored.Lives, Is.EqualTo(4));
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));

            SaveData after = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(restored.ToSaveData()));
            var again = new GameSession(null, clock);
            again.Restore(after);
            Assert.That(again.ForfeitedFrogJumpOnRestore, Is.False);
            Assert.That(again.Lives, Is.EqualTo(4));
        }

        [Test]
        public void RestoreSnapshotDoesNotForfeit()
        {
            var clock = new FakeClock();
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            SaveData snapshot = session.ToSaveData();
            session.RestoreSnapshot(snapshot);
            Assert.That(session.PendingFrogJump, Is.Not.Null);
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.FrogJump));
            Assert.That(session.Lives, Is.EqualTo(5));
        }

        [Test]
        public void Version7SaveKeepsJourneyDropsSupplementaryAndStartsRegen()
        {
            var clock = new FakeClock();
            SaveData data = SaveData.CreateDefault();
            data.version = 7;
            data.lives = 0;
            data.journey.completedChallengeIds.AddRange(new[] { "sprint_learn", "sprint_practice" });
            data.journey.awaitingSupplementaryChallengeId = "sprint_practice";
            data.journey.supplementaryRounds = 2;

            var restored = new GameSession(null, clock);
            restored.Restore(data);
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));
            Assert.That(restored.Lives, Is.Zero);
            Assert.That(restored.Journey.SupplementaryRounds, Is.EqualTo(2));
            Assert.That(restored.TimeUntilNextLife, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression.LifeRegenSessionTests" regen-session`
Expected: compile error (`GameSession` has no ctor taking `IClock`).

- [ ] **Step 3: `SaveData` v8**

In `SaveData.cs`: `public const int CurrentVersion = 8;` and add after `public int lives;`:

```csharp
        // UTC ticks when the next life arrives; 0 when lives are full.
        public long nextLifeAtUtcTicks;
```

- [ ] **Step 4: Migration**

In `JourneySaveMigration.cs`:

1. `MigrateLegacy`: delete the `if (!result.settingsOnly && result.lives == 0 && passedPrefix < ...)` block.
2. `Normalize`: change `if (data.version < SaveData.CurrentVersion) return MigrateLegacy(data, catalog);` to `if (data.version < 7) return MigrateLegacy(data, catalog);` (v7 already carries a journey).
3. `Normalize`: delete the `if (normalized.lives == 0 && journey.completedChallengeIds.Count < ...)` block and add after `journey.lastCommittedResult = ...`:

```csharp
                journey.failCounts = NormalizeFailCounts(source.failCounts, catalog);
                journey.lastAppliedFrogJumpId = source.lastAppliedFrogJumpId;
                FrogJumpPending pending = source.pendingFrogJump?.ToPending();
                string checkpoint = journey.completedChallengeIds.Count < catalog.Ordered.Count
                    ? catalog.Ordered[journey.completedChallengeIds.Count].Id : null;
                journey.pendingFrogJump = pending != null && pending.FailedChallengeId == checkpoint &&
                    catalog.Get(checkpoint).Kind != ChallengeKind.Learn
                        ? JourneyFrogJumpData.FromPending(pending) : null;
```

4. Add helper:

```csharp
        static List<JourneyFailCountData> NormalizeFailCounts(List<JourneyFailCountData> source, ChallengeCatalog catalog)
        {
            var result = new List<JourneyFailCountData>();
            if (source == null)
                return result;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JourneyFailCountData entry in source)
            {
                if (entry == null || entry.count <= 0 || !seen.Add(entry.challengeId ?? string.Empty) ||
                    !catalog.Ordered.Any(x => x.Id == entry.challengeId && x.Kind != ChallengeKind.Learn))
                    continue;
                result.Add(new JourneyFailCountData { challengeId = entry.challengeId, count = entry.count });
            }
            return result;
        }
```

5. `CopySave`: add `nextLifeAtUtcTicks = Math.Max(0L, source.nextLifeAtUtcTicks),` to the returned `SaveData`.
6. Delete `PracticeId` (now unused).

- [ ] **Step 5: `GameSession`**

1. Enum: append `FrogJump` after `GameOver` and change the comment on `Punishment` to `// Retained for old serialized route identifiers.`
2. Fields/ctor:

```csharp
        readonly IClock clock;
        long nextLifeAtUtcTicks;

        public GameSession(ChallengeCatalog catalog = null, IClock clock = null)
        {
            this.catalog = catalog == null ? ChallengeCatalog.LoadDefault() : catalog;
            this.clock = clock ?? SystemClock.Instance;
            Journey = new JourneyProgress(this.catalog);
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
                records.Add(id, new SubjectRecord());
        }
```

3. Replace `PendingPunishmentSubject`/`AwaitingPunishment` with fixed values and add the new members:

```csharp
        public SubjectId? PendingPunishmentSubject => null;
        public bool AwaitingPunishment => false;
        public long NextLifeAtUtcTicks => nextLifeAtUtcTicks;
        public TimeSpan? TimeUntilNextLife => LifeRegen.Remaining(Lives, nextLifeAtUtcTicks, clock.UtcNow, MaxLives);
        public FrogJumpPending PendingFrogJump => Journey.PendingFrogJump;
        public bool ForfeitedFrogJumpOnRestore { get; private set; }

        public bool RefreshLives()
        {
            int lives = Lives;
            long next = nextLifeAtUtcTicks;
            LifeRegen.Advance(ref lives, ref next, clock.UtcNow, MaxLives);
            bool changed = lives != Lives || next != nextLifeAtUtcTicks;
            Journey.SetAttemptsRemaining(lives);
            nextLifeAtUtcTicks = next;
            return changed;
        }

        public bool TryApplyFrogJump(string frogJumpId, bool reachedFinish)
        {
            RefreshLives();
            if (!Journey.TryApplyFrogJump(frogJumpId, reachedFinish))
                return false;
            RefreshLives();
            NotifyJourneyChanged();
            return true;
        }
```

4. `TryStartChallenge`: call `RefreshLives();` as its first statement.
5. `SubmitChallengeResult`: call `RefreshLives();` before `Journey.Apply(result)`, and `RefreshLives();` right after the `if (!outcome.Accepted) return outcome;` line.
6. `ResumeRoute`:

```csharp
        public SessionRoute ResumeRoute()
        {
            if (active.HasValue)
                return SessionRoute.Subject;
            return PendingFrogJump != null ? SessionRoute.FrogJump : SessionRoute.Map;
        }
```

7. `ResetCampaign`: add `nextLifeAtUtcTicks = 0;`.
8. `ToSaveData`: add `data.nextLifeAtUtcTicks = nextLifeAtUtcTicks;`.
9. `RestoreCore`: after the `active = ...` line add:

```csharp
            nextLifeAtUtcTicks = Math.Max(0L, normalized.nextLifeAtUtcTicks);
            ForfeitedFrogJumpOnRestore = false;
            if (normalize)
            {
                RefreshLives();
                ForfeitedFrogJumpOnRestore = Journey.ForfeitPendingFrogJump();
                RefreshLives();
            }
```

10. `StartSubject`: delete the `if (Lives <= 0 && !Journey.AwaitingSupplementary) return SessionRoute.GameOver;` guard; replace `return SessionRoute.GameOver;` in the course-incomplete branch with `return SessionRoute.Map;`; set `mode = ChallengeAttemptMode.Journey;` in the else-branch; final return becomes `... ? SessionRoute.Subject : SessionRoute.Map;`.
11. `CompletePunishment` message: `"The punishment route was retired; failed challenges use the frog jump."`.
12. `RouteForResult(MinigameResult result) => SessionRoute.Map;`.

- [ ] **Step 6: Persist after a restore-time forfeit**

In `GameManager.InitializeStartup`, after `initialized = true;` and before `loadScene(MenuScene);`:

```csharp
            // The forfeited frog jump cost a life; write it now so a second kill cannot re-roll it.
            if (session.ForfeitedFrogJumpOnRestore && HasSavedCampaign)
                TryPersistSession(out _);
```

- [ ] **Step 7: Update the old EditMode expectations**

Run `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" progression` and fix exactly these tests:

- `GameSessionTests.LastExamAttemptOpensSupplementaryPracticeInsteadOfGameOver` → rename to `ExhaustedLivesReturnToMapAndBlockTheExam`; body: complete through `sprint_practice`; `session.Journey.SetAttemptsRemaining(1)`; fail `sprint_exam` via `JourneyTestData.Play`, apply the frog jump with `false`; assert `Lives == 0`, `session.StartSubject(SubjectId.Sprint) == SessionRoute.Map`, `session.ActiveSubject` is null.
- The `GameSessionTests` test ending at line ~43 that expects `Lives == 4` after one `SubmitResult` failure: expect `Lives == 5`, `session.PendingFrogJump != null`, and after `session.TryApplyFrogJump(session.PendingFrogJump.Id, false)` expect `Lives == 4`.
- `JourneySaveTests.Normalize_ZeroBudgetCreatesSupplementaryCheckpointForCurrentSubject` → rename `Normalize_ZeroLivesKeepsCheckpointAndStartsRegen`; keep the setup; assert `Lives == 0`, checkpoint `volleyball_learn`, `TimeUntilNextLife == 5 min` (construct the restored session with a `FakeClock`).
- `JourneySaveTests.Restore_RecordedExamFailureKeepsSpentAttemptAndDoesNotReplayReceipt`: the first `AttemptsRemaining` assertion becomes `Is.EqualTo(5)` (frog deferred); after restore the forfeit makes `restored.Lives == 4`; keep the "does not replay" assertions.
- `JourneySaveTests.Restore_SupplementaryReceiptCannotResetAttemptsTwice` → delete (behaviour removed; covered by `SupplementaryModeIsAlwaysRejected`).
- `GameSessionPersistenceTests.Restore_LegacyPunishmentSave_NeverResumesIntoPunishment`: replace the last assertion with `Assert.That(session.PendingFrogJump, Is.Null);`.

Any other failing assertion in this assembly must be traced to one of the rules in Global Constraints and updated to it; do not change production code to satisfy an old expectation.

- [ ] **Step 8: Run all EditMode progression tests**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" progression`
Expected: all pass. PlayMode journey tests may now fail (old expectations); they are updated in Task 7.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Project/Scripts/Progression/GameSession.cs Assets/_Project/Scripts/Progression/SaveData.cs \
  Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs Assets/_Project/Scripts/Core/GameManager.cs \
  Assets/Tests/EditMode/Progression/LifeRegenSessionTests.cs Assets/Tests/EditMode/Progression/LifeRegenSessionTests.cs.meta \
  Assets/Tests/EditMode/Progression/GameSessionTests.cs Assets/Tests/EditMode/Progression/JourneySaveTests.cs \
  Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs
git commit -m "feat(progression): regenerate lives over time and forfeit unfinished frog jumps on restore"
```

---

### Task 4: Failure conditions for volleyball and soccer practice

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeDefinition.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Football/FootballMatchOptions.cs`, `FootballRules.cs`, `FootballController.cs`
- Modify: `Assets/Editor/StudentJourneyContentBuilder.cs`
- Regenerate: `Assets/_Project/ScriptableObjects/Journey/volleyball_practice.asset`, `soccer_practice.asset`
- Modify: `Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballChallengeTests.cs`, `Assets/Tests/EditMode/Gameplay/Ball/FootballChallengeTests.cs`

**Interfaces:**
- Produces: `ChallengeDefinition.AttemptLimit` (`int`, 0 = unlimited); `FootballMatchOptions(int? maxKicks, int requiredGoals, bool keeperEnabled, bool stopAtRequiredGoals = false)` with `StopAtRequiredGoals`.

- [ ] **Step 1: Write the failing tests**

In `VolleyballChallengeTests.cs` replace `PracticeRequiresReceiveReceiveSmashSequenceForPoint` with:

```csharp
        [Test]
        public void PracticeRunsOnA120SecondClockAndFailsWhenItExpires()
        {
            var definition = ChallengeCatalog.LoadDefault().Get("volleyball_practice");
            var rules = new VolleyballChallengeRules(definition);
            Assert.That(rules.Match.WinningPoints, Is.EqualTo(0));
            Assert.That(rules.Match.ClockLimit, Is.EqualTo(120f));
            Assert.That(rules.IsComplete, Is.False);
            rules.Tick(120.5f);
            Assert.That(rules.IsComplete, Is.True);
            Assert.That(rules.BuildResult(new ChallengeAttemptContext("attempt", "volleyball_practice",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Easy)).Pass, Is.False);
        }
```

Append to `FootballChallengeTests.cs`:

```csharp
        [Test]
        public void PracticeStopsAsSoonAsTwoGoalsAreScored()
        {
            var rules = new FootballRules(Tuning, new FootballMatchOptions(6, 2, true, stopAtRequiredGoals: true));
            rules.Start();
            for (int i = 0; i < 2; i++)
            {
                rules.SetAim(.55f);
                rules.BeginCharge();
                rules.Tick(1f);
                rules.ReleaseShot();
                rules.Tick(20f);
            }
            Assert.That(rules.State, Is.EqualTo(FootballState.MatchResult));
            Assert.That(rules.Kicks, Is.EqualTo(2));
            var context = new ChallengeAttemptContext("attempt", "soccer_practice",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal);
            Assert.That(rules.BuildChallengeResult(context).Pass, Is.True);
            Assert.That(rules.BuildChallengeResult(context).ExamResult, Is.Null);
        }

        [Test]
        public void PracticeFailsAfterSixKicksWithoutTwoGoals()
        {
            var rules = new FootballRules(Tuning, new FootballMatchOptions(6, 2, true, stopAtRequiredGoals: true));
            rules.Start();
            for (int i = 0; i < 6; i++)
            {
                Assert.That(rules.State, Is.Not.EqualTo(FootballState.MatchResult));
                rules.SetAim(.55f);
                rules.BeginCharge();
                rules.ReleaseShot();
                rules.Tick(20f);
            }
            Assert.That(rules.State, Is.EqualTo(FootballState.MatchResult));
            Assert.That(rules.Kicks, Is.EqualTo(6));
            Assert.That(rules.Goals, Is.LessThan(2));
            Assert.That(rules.BuildChallengeResult(new ChallengeAttemptContext("attempt", "soccer_practice",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal)).Pass, Is.False);
        }

        [Test]
        public void CatalogGivesSoccerPracticeSixKicks()
        {
            Assert.That(ChallengeCatalog.LoadDefault().Get("soccer_practice").AttemptLimit, Is.EqualTo(6));
        }
```

The uncharged shots mirror the exam test, where shots 4 and 5 are released without charging and do not score. If `PracticeFailsAfterSixKicksWithoutTwoGoals` scores two goals in practice, change those shots to `rules.SetAim(1f)` (the wide aim used in `LearnAndPracticeDoNotStopAtFiveKicks`) instead of touching production code.

- [ ] **Step 2: Run tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball.VolleyballChallengeTests;KMA.Tests.Gameplay.Football.FootballChallengeTests" practice-limits`
Expected: compile error (`stopAtRequiredGoals`, `AttemptLimit`).

- [ ] **Step 3: Implement**

`ChallengeDefinition.cs`: add `[SerializeField, Min(0)] int attemptLimit;` after `targetCount` and `public int AttemptLimit => attemptLimit;` after `TargetCount`.

`FootballMatchOptions.cs`:

```csharp
        public bool StopAtRequiredGoals { get; }

        public FootballMatchOptions(int? maxKicks, int requiredGoals, bool keeperEnabled,
            bool stopAtRequiredGoals = false)
        {
            MaxKicks = maxKicks;
            RequiredGoals = requiredGoals;
            KeeperEnabled = keeperEnabled;
            StopAtRequiredGoals = stopAtRequiredGoals;
        }
```

`FootballRules.cs` (Flying branch): change `if (!options.MaxKicks.HasValue && Goals >= options.RequiredGoals)` to

```csharp
                            if ((!options.MaxKicks.HasValue || options.StopAtRequiredGoals) &&
                                Goals >= options.RequiredGoals)
```

`FootballController.OptionsFor`:

```csharp
            ChallengeKind.Practice => new FootballMatchOptions(
                definition.AttemptLimit > 0 ? definition.AttemptLimit : (int?)null, definition.TargetCount, true,
                stopAtRequiredGoals: true),
```

`VolleyballChallengeRules.cs`: practice options become `new VolleyballMatchOptions(0, definition.TimeLimit, true)`, and `IsComplete` becomes:

```csharp
        public bool IsComplete => definition.Kind == ChallengeKind.Learn
            ? completedTargets >= definition.TargetCount
            : definition.Kind == ChallengeKind.Practice
                ? completedTargets >= definition.TargetCount || Match.IsOver : Match.IsOver;
```

`StudentJourneyContentBuilder.cs`:
- `ChallengeSpec`: add `public readonly int AttemptLimit;` and a trailing ctor parameter `int attemptLimit = 0` assigned to it.
- `volleyball_practice`: `0f, 120f, 2, false, ChallengeDifficulty.Easy, true, "Ghi hai điểm bằng chuỗi đỡ, chuyền rồi đập trong tối đa 120 giây."`
- `soccer_practice`: `0f, 0f, 2, true, ChallengeDifficulty.Normal, true, "Ghi hai bàn trước thủ môn Normal trong tối đa 6 cú sút.", 6`
- `Apply`: add `serialized.FindProperty("attemptLimit").intValue = spec.AttemptLimit;`.

- [ ] **Step 4: Regenerate the challenge assets**

Run: `"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.StudentJourneyContentBuilder.BuildChallenges -quit -logFile Builds/build-challenges.log`
Expected: `git diff --stat Assets/_Project/ScriptableObjects/Journey` shows only `volleyball_practice.asset` (`timeLimit: 120`, objective) and `soccer_practice.asset` (`attemptLimit: 6`, objective), plus `attemptLimit: 0` added to the other seven.

- [ ] **Step 5: Run tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Volleyball;KMA.Tests.Gameplay.Football" practice-limits`
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Progression/Journey/ChallengeDefinition.cs \
  Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs \
  Assets/_Project/Scripts/Gameplay/Football/FootballMatchOptions.cs Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs \
  Assets/_Project/Scripts/Gameplay/Football/FootballController.cs Assets/Editor/StudentJourneyContentBuilder.cs \
  Assets/_Project/ScriptableObjects/Journey/*.asset \
  Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballChallengeTests.cs Assets/Tests/EditMode/Gameplay/Ball/FootballChallengeTests.cs
git commit -m "feat(journey): give volleyball and soccer practice a way to fail"
```

---

### Task 5: `FrogJumpRules`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/FrogJump/KMA.Gameplay.FrogJump.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpTuning.cs`, `FrogJumpRules.cs`
- Create: `Assets/Tests/EditMode/Gameplay/FrogJump/KMA.Gameplay.FrogJump.EditMode.Tests.asmdef`, `FrogJumpRulesTests.cs`

**Interfaces:**
- Produces (namespace `KMA.Gameplay.FrogJump`):
  - `[Serializable] sealed class FrogJumpTuning` with public fields `trackMetres=40`, `sweepSeconds=1.2`, `maxJumpMetres=3`, `minJumpMetres=1`, `safeZone=0.8`, `jumpSeconds=0.6`, `recoverSeconds=2.5`, `timeLimitSeconds=60`.
  - `enum FrogJumpState { Aiming, Jumping, Fallen, Finished, TimedOut }`.
  - `sealed class FrogJumpRules(FrogJumpTuning tuning)`: `State`, `Elapsed`, `TimeRemaining`, `Distance`, `Progress01`, `Needle01`, `IsOver`, `ReachedFinish`, `float? LastJumpMetres` (0 for a fall), `int Jumps`, `int Falls`, `float StateElapsed`, `FrogJumpTuning Tuning`, `bool Stop()`, `void Tick(float dt)`, `static float JumpMetres(float needle01, FrogJumpTuning tuning)` (0 when fallen), `static bool IsFall(float needle01, FrogJumpTuning tuning)`, `event Action<float> Landed` (metres, 0 = fall).

- [ ] **Step 1: Create the assemblies**

`Assets/_Project/Scripts/Gameplay/FrogJump/KMA.Gameplay.FrogJump.asmdef`:

```json
{
    "name": "KMA.Gameplay.FrogJump",
    "rootNamespace": "KMA.Gameplay.FrogJump",
    "references": [
        "KMA.Gameplay",
        "KMA.Gameplay.Core",
        "KMA.Gameplay.UI",
        "KMA.Gameplay.Progression",
        "Unity.InputSystem",
        "Unity.TextMeshPro",
        "UnityEngine.UI",
        "KMA.Text"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Tests/EditMode/Gameplay/FrogJump/KMA.Gameplay.FrogJump.EditMode.Tests.asmdef`:

```json
{
    "name": "KMA.Gameplay.FrogJump.EditMode.Tests",
    "rootNamespace": "KMA.Tests.Gameplay.FrogJump",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "KMA.Gameplay",
        "KMA.Gameplay.FrogJump"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing tests**

`Assets/Tests/EditMode/Gameplay/FrogJump/FrogJumpRulesTests.cs`:

```csharp
using KMA.Gameplay.FrogJump;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.FrogJump
{
    public sealed class FrogJumpRulesTests
    {
        static readonly FrogJumpTuning Tuning = new FrogJumpTuning();
        const float Eps = 1e-3f;

        [TestCase(.5f, 3f)]
        [TestCase(.1f, 1f)]
        [TestCase(.9f, 1f)]
        [TestCase(.3f, 2f)]
        public void JumpShrinksLinearlyFromCentreToTheSafeEdge(float needle, float metres)
        {
            Assert.That(FrogJumpRules.JumpMetres(needle, Tuning), Is.EqualTo(metres).Within(Eps));
        }

        [TestCase(.05f)]
        [TestCase(.97f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void OutsideTheSafeZoneIsAFall(float needle)
        {
            Assert.That(FrogJumpRules.IsFall(needle, Tuning), Is.True);
            Assert.That(FrogJumpRules.JumpMetres(needle, Tuning), Is.Zero);
        }

        [Test]
        public void NeedleSweepsEdgeToEdgeAndBack()
        {
            var rules = new FrogJumpRules(Tuning);
            Assert.That(rules.Needle01, Is.Zero);
            rules.Tick(.6f);
            Assert.That(rules.Needle01, Is.EqualTo(.5f).Within(Eps));
            rules.Tick(.6f);
            Assert.That(rules.Needle01, Is.EqualTo(1f).Within(Eps));
            rules.Tick(.6f);
            Assert.That(rules.Needle01, Is.EqualTo(.5f).Within(Eps));
        }

        [Test]
        public void CentreStopJumpsThreeMetresAfterTheJumpTime()
        {
            var rules = new FrogJumpRules(Tuning);
            rules.Tick(.6f);
            Assert.That(rules.Stop(), Is.True);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Jumping));
            Assert.That(rules.Stop(), Is.False);
            rules.Tick(.59f);
            Assert.That(rules.Distance, Is.Zero);
            rules.Tick(.02f);
            Assert.That(rules.Distance, Is.EqualTo(3f).Within(Eps));
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Aiming));
            Assert.That(rules.Needle01, Is.Zero);
            Assert.That(rules.Jumps, Is.EqualTo(1));
        }

        [Test]
        public void EdgeStopFallsAndBlocksInputWhileRecovering()
        {
            var rules = new FrogJumpRules(Tuning);
            Assert.That(rules.Stop(), Is.True);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Fallen));
            Assert.That(rules.LastJumpMetres, Is.Zero);
            rules.Tick(2.4f);
            Assert.That(rules.Stop(), Is.False);
            rules.Tick(.2f);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Aiming));
            Assert.That(rules.Distance, Is.Zero);
            Assert.That(rules.Falls, Is.EqualTo(1));
        }

        [Test]
        public void ReachingFortyMetresWinsAndClampsDistance()
        {
            var rules = new FrogJumpRules(Tuning);
            for (int i = 0; i < 14 && !rules.IsOver; i++)
            {
                rules.Tick(.6f);
                rules.Stop();
                rules.Tick(.6f);
            }
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Finished));
            Assert.That(rules.ReachedFinish, Is.True);
            Assert.That(rules.Distance, Is.EqualTo(40f));
            Assert.That(rules.Progress01, Is.EqualTo(1f));
        }

        [Test]
        public void RunningOutOfTimeLoses()
        {
            var rules = new FrogJumpRules(Tuning);
            rules.Tick(59.9f);
            Assert.That(rules.IsOver, Is.False);
            rules.Tick(.2f);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.TimedOut));
            Assert.That(rules.ReachedFinish, Is.False);
            Assert.That(rules.Elapsed, Is.EqualTo(60f).Within(Eps));
            Assert.That(rules.TimeRemaining, Is.Zero);
            Assert.That(rules.Stop(), Is.False);
        }

        [Test]
        public void LandingOnTheFinishExactlyAtTheLimitStillWins()
        {
            var tuning = new FrogJumpTuning { trackMetres = 3f, timeLimitSeconds = 1.2f };
            var rules = new FrogJumpRules(tuning);
            rules.Tick(.6f);
            rules.Stop();
            rules.Tick(.6f);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Finished));
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.FrogJump" frog-rules`
Expected: compile error (`FrogJumpRules` not found).

- [ ] **Step 4: Implement**

`FrogJumpTuning.cs`:

```csharp
using System;

namespace KMA.Gameplay.FrogJump
{
    [Serializable]
    public sealed class FrogJumpTuning
    {
        public float trackMetres = 40f;
        public float sweepSeconds = 1.2f;
        public float maxJumpMetres = 3f;
        public float minJumpMetres = 1f;
        // Normalised distance from the centre (0 centre, 1 edge) beyond which the jump is a fall.
        public float safeZone = .8f;
        public float jumpSeconds = .6f;
        public float recoverSeconds = 2.5f;
        public float timeLimitSeconds = 60f;
    }
}
```

`FrogJumpRules.cs`:

```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public enum FrogJumpState
    {
        Aiming,
        Jumping,
        Fallen,
        Finished,
        TimedOut
    }

    public sealed class FrogJumpRules
    {
        readonly FrogJumpTuning tuning;
        float sweepTime;
        float stateTime;
        float pendingMetres;

        public FrogJumpRules(FrogJumpTuning tuning)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        public event Action<float> Landed;

        public FrogJumpState State { get; private set; } = FrogJumpState.Aiming;
        public float Elapsed { get; private set; }
        public float Distance { get; private set; }
        public float? LastJumpMetres { get; private set; }
        public int Jumps { get; private set; }
        public int Falls { get; private set; }
        /// Seconds spent in the current Jumping/Fallen state; the view interpolates the hop with it.
        public float StateElapsed => stateTime;
        public float TimeRemaining => Mathf.Max(0f, tuning.timeLimitSeconds - Elapsed);
        public float Progress01 => tuning.trackMetres <= 0f ? 1f : Mathf.Clamp01(Distance / tuning.trackMetres);
        public bool IsOver => State == FrogJumpState.Finished || State == FrogJumpState.TimedOut;
        public bool ReachedFinish => State == FrogJumpState.Finished;
        public FrogJumpTuning Tuning => tuning;

        /// 0 at the left edge, 1 at the right edge; ping-pongs once per sweepSeconds.
        public float Needle01
        {
            get
            {
                if (tuning.sweepSeconds <= 0f) return .5f;
                float phase = Mathf.Repeat(sweepTime / tuning.sweepSeconds, 2f);
                return phase <= 1f ? phase : 2f - phase;
            }
        }

        public static bool IsFall(float needle01, FrogJumpTuning tuning) =>
            Mathf.Abs(Mathf.Clamp01(needle01) - .5f) * 2f > tuning.safeZone + 1e-5f;

        public static float JumpMetres(float needle01, FrogJumpTuning tuning)
        {
            if (IsFall(needle01, tuning)) return 0f;
            float d = Mathf.Abs(Mathf.Clamp01(needle01) - .5f) * 2f;
            float t = tuning.safeZone <= 0f ? 0f : Mathf.Clamp01(d / tuning.safeZone);
            return Mathf.Lerp(tuning.maxJumpMetres, tuning.minJumpMetres, t);
        }

        public bool Stop()
        {
            if (State != FrogJumpState.Aiming)
                return false;
            float needle = Needle01;
            stateTime = 0f;
            if (IsFall(needle, tuning))
            {
                LastJumpMetres = 0f;
                Falls++;
                State = FrogJumpState.Fallen;
                Landed?.Invoke(0f);
            }
            else
            {
                pendingMetres = JumpMetres(needle, tuning);
                LastJumpMetres = pendingMetres;
                State = FrogJumpState.Jumping;
            }
            return true;
        }

        public void Tick(float dt)
        {
            if (IsOver || dt <= 0f)
                return;
            float step = Mathf.Min(dt, TimeRemaining);
            Elapsed += step;
            switch (State)
            {
                case FrogJumpState.Aiming:
                    sweepTime += step;
                    break;
                case FrogJumpState.Jumping:
                    stateTime += step;
                    if (stateTime >= tuning.jumpSeconds - 1e-5f)
                    {
                        Distance = Mathf.Min(tuning.trackMetres, Distance + pendingMetres);
                        Jumps++;
                        Landed?.Invoke(pendingMetres);
                        if (Distance >= tuning.trackMetres - 1e-4f)
                        {
                            Distance = tuning.trackMetres;
                            State = FrogJumpState.Finished;
                            return;
                        }
                        BeginAim();
                    }
                    break;
                case FrogJumpState.Fallen:
                    stateTime += step;
                    if (stateTime >= tuning.recoverSeconds - 1e-5f)
                        BeginAim();
                    break;
            }
            if (!IsOver && Elapsed >= tuning.timeLimitSeconds - 1e-5f)
                State = FrogJumpState.TimedOut;
        }

        void BeginAim()
        {
            State = FrogJumpState.Aiming;
            sweepTime = 0f;
            stateTime = 0f;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.FrogJump" frog-rules`
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/FrogJump Assets/_Project/Scripts/Gameplay/FrogJump.meta \
  Assets/Tests/EditMode/Gameplay/FrogJump Assets/Tests/EditMode/Gameplay/FrogJump.meta
git commit -m "feat(frogjump): add power-bar frog jump rules"
```

---

### Task 6: Frog jump controller, presentation and `MG_FrogJump` scene

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/FrogJump/FrogJumpBalanceConfig.cs`, `FrogJumpController.cs`, `FrogJumpView.cs`, `FrogJumpPowerBar.cs`, `FrogJumpTapArea.cs`
- Modify: `Assets/_Project/Scripts/UI/PausePanel.cs`
- Create: `Assets/Editor/FrogJumpSceneConfigurator.cs`
- Generate: `Assets/_Project/Scenes/MG_FrogJump.unity`, `Assets/_Project/ScriptableObjects/FrogJump/FrogJumpBalance.asset`, `ProjectSettings/EditorBuildSettings.asset`
- Create: `Assets/Tests/EditMode/EditorTools/FrogJumpSceneTests.cs` (asmdef `KMA.EditorTools.EditMode.Tests`; add `KMA.Gameplay.FrogJump` to its references)

**Interfaces:**
- Consumes: Task 5 rules.
- Produces: `FrogJumpController : MinigameBase` with `Rules` (`FrogJumpRules`), `IsWired`, `Configure(FrogJumpBalanceConfig config, FrogJumpView view, FrogJumpPowerBar bar, FrogJumpTapArea tap, TMP_Text feedback)`, `Completed` fires once with `MinigameResult(pass: reachedFinish, 0f, pass ? Rank.C : Rank.F)`. `PausePanel.SetLeaveOptionsVisible(bool)`. Scene path `Assets/_Project/Scenes/MG_FrogJump.unity`, scene name `MG_FrogJump`. Editor entry `KMA.EditorTools.FrogJumpSceneConfigurator.BuildScene`.

- [ ] **Step 1: Write the failing scene contract test**

`Assets/Tests/EditMode/EditorTools/FrogJumpSceneTests.cs`:

```csharp
using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay.FrogJump;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class FrogJumpSceneTests
    {
        [Test]
        public void SceneIsInBuildSettingsAndWired()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == FrogJumpSceneConfigurator.ScenePath));
            EditorSceneManager.OpenScene(FrogJumpSceneConfigurator.ScenePath);
            var controller = Object.FindFirstObjectByType<FrogJumpController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsWired, Is.True);
            Assert.That(Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include), Is.Not.Null);
        }
    }
}
```

Add `"KMA.Gameplay.FrogJump"` to `references` in `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef`.

- [ ] **Step 2: Run to verify it fails**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.FrogJumpSceneTests" frog-scene`
Expected: compile error (`FrogJumpSceneConfigurator` / `FrogJumpController` not found).

- [ ] **Step 3: Runtime components**

`FrogJumpBalanceConfig.cs`:

```csharp
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    [CreateAssetMenu(menuName = "KMA/Frog Jump/Balance", fileName = "FrogJumpBalance")]
    public sealed class FrogJumpBalanceConfig : ScriptableObject
    {
        [SerializeField] FrogJumpTuning tuning = new FrogJumpTuning();
        public FrogJumpTuning Tuning => tuning ?? new FrogJumpTuning();
    }
}
```

`FrogJumpTapArea.cs` (full-screen transparent `Image` receives taps; Space works in the Editor):

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpTapArea : MonoBehaviour, IPointerDownHandler
    {
        public event Action Tapped;

        public void OnPointerDown(PointerEventData eventData) => Tapped?.Invoke();

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                Tapped?.Invoke();
        }
    }
}
```

`FrogJumpPowerBar.cs` (20 coloured segments built by the configurator; this component only moves the needle and toggles it):

```csharp
using KMA.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpPowerBar : MonoBehaviour
    {
        [SerializeField] RectTransform track;
        [SerializeField] RectTransform needle;

        public bool NeedleVisible => needle != null && needle.gameObject.activeSelf;

        public void Configure(RectTransform trackRect, RectTransform needleRect)
        {
            track = trackRect;
            needle = needleRect;
        }

        public void SetNeedle(float needle01, bool visible)
        {
            if (needle == null || track == null) return;
            needle.gameObject.SetActive(visible);
            needle.anchorMin = needle.anchorMax = new Vector2(Mathf.Clamp01(needle01), .5f);
            needle.anchoredPosition = Vector2.zero;
        }

        /// Colour of a segment centred at needle01: green that fades towards the safe edge, red past it.
        public static Color SegmentColor(float needle01, FrogJumpTuning tuning)
        {
            if (FrogJumpRules.IsFall(needle01, tuning)) return MinigameUiTheme.Energy;
            float jump01 = Mathf.InverseLerp(tuning.minJumpMetres, tuning.maxJumpMetres,
                FrogJumpRules.JumpMetres(needle01, tuning));
            return Color.Lerp(MinigameUiTheme.WithAlpha(MinigameUiTheme.Success, .45f), MinigameUiTheme.Success, jump01);
        }
    }
}
```

`FrogJumpView.cs` (moves the hero along the track and does squash & stretch):

```csharp
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer hero;
        [SerializeField] Sprite squatPose;
        [SerializeField] Sprite jumpPose;
        [SerializeField] Sprite fallPose;
        [SerializeField] float startX = -7f;
        [SerializeField] float finishX = 7f;
        [SerializeField] float groundY = -2f;
        [SerializeField] float hopHeight = 1.4f;

        float fromMetres;
        Vector3 baseScale = Vector3.one;

        public void Configure(SpriteRenderer heroRenderer, Sprite squat, Sprite jump, Sprite fall,
            float start, float finish, float ground)
        {
            hero = heroRenderer;
            squatPose = squat;
            jumpPose = jump;
            fallPose = fall;
            startX = start;
            finishX = finish;
            groundY = ground;
        }

        void Awake()
        {
            if (hero != null) baseScale = hero.transform.localScale;
        }

        public void Present(FrogJumpRules rules, float stateProgress01)
        {
            if (hero == null || rules == null) return;
            float track = Mathf.Max(.01f, rules.Tuning.trackMetres);
            float metres = rules.Distance;
            float y = groundY;
            Sprite pose = squatPose;
            Vector3 scale = baseScale;
            switch (rules.State)
            {
                case FrogJumpState.Jumping:
                    metres = Mathf.Lerp(rules.Distance, Mathf.Min(track, rules.Distance + (rules.LastJumpMetres ?? 0f)),
                        stateProgress01);
                    y += Mathf.Sin(stateProgress01 * Mathf.PI) * hopHeight;
                    pose = jumpPose;
                    scale = Vector3.Scale(baseScale, new Vector3(.9f, 1.12f, 1f));
                    break;
                case FrogJumpState.Fallen:
                    pose = fallPose;
                    break;
                case FrogJumpState.Aiming:
                    // Breathing squat so the waiting pose reads as "ready to spring".
                    float squash = 1f - Mathf.PingPong(Time.time * .6f, .08f);
                    scale = Vector3.Scale(baseScale, new Vector3(2f - squash, squash, 1f));
                    break;
            }
            hero.sprite = pose;
            hero.transform.localScale = scale;
            hero.transform.position = new Vector3(Mathf.Lerp(startX, finishX, metres / track), y, 0f);
        }
    }
}
```

`FrogJumpController.cs`:

```csharp
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpController : MinigameBase
    {
        [SerializeField] FrogJumpBalanceConfig balance;
        [SerializeField] FrogJumpView view;
        [SerializeField] FrogJumpPowerBar powerBar;
        [SerializeField] FrogJumpTapArea tapArea;
        [SerializeField] TMP_Text feedback;

        FrogJumpRules rules;
        float feedbackUntil;
        bool finished;

        public FrogJumpRules Rules => rules;
        public bool IsWired => view != null && powerBar != null && tapArea != null && feedback != null;

        public void Configure(FrogJumpBalanceConfig config, FrogJumpView frogView, FrogJumpPowerBar bar,
            FrogJumpTapArea tap, TMP_Text feedbackLabel)
        {
            balance = config;
            view = frogView;
            powerBar = bar;
            tapArea = tap;
            feedback = feedbackLabel;
        }

        protected override void Awake()
        {
            base.Awake();
            rules = new FrogJumpRules(balance != null ? balance.Tuning : new FrogJumpTuning());
            rules.Landed += OnLanded;
        }

        void Start()
        {
            if (tapArea != null) tapArea.Tapped += OnTapped;
            var pause = FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            if (pause != null) pause.SetLeaveOptionsVisible(false);
            if (feedback != null) feedback.text = string.Empty;
        }

        void OnDestroy()
        {
            if (tapArea != null) tapArea.Tapped -= OnTapped;
            if (rules != null) rules.Landed -= OnLanded;
        }

        protected override void Update()
        {
            base.Update();
            bool playing = PresentationPhase == MinigamePhase.Play && !rules.IsOver;
            if (powerBar != null) powerBar.SetNeedle(rules.Needle01, playing && rules.State == FrogJumpState.Aiming);
            if (view != null)
                view.Present(rules, Mathf.Clamp01(rules.StateElapsed / Mathf.Max(.01f, rules.Tuning.jumpSeconds)));
            if (feedback != null && Time.time > feedbackUntil) feedback.text = string.Empty;
        }

        protected override void TickPlay(float dt)
        {
            rules.Tick(dt);
            if (rules.IsOver && !finished)
            {
                finished = true;
                bool pass = rules.ReachedFinish;
                Finish(new MinigameResult(pass, 0f, pass ? Rank.C : Rank.F));
            }
        }

        protected override MinigameHudState BuildHudState() => rules == null
            ? MinigameHudState.Empty
            : new MinigameHudState("PLAY", rules.TimeRemaining, rules.Progress01, 0f, rules.Distance,
                VietText.Fix($"Còn {rules.Tuning.trackMetres - rules.Distance:0.0} m"));

        void OnTapped()
        {
            if (PresentationPhase != MinigamePhase.Play || rules.IsOver) return;
            if (rules.Stop()) GameAudio.Play(GameSound.Click);
        }

        void OnLanded(float metres)
        {
            GameAudio.Play(metres <= 0f ? GameSound.Miss : GameSound.SandStep);
            var haptics = FindFirstObjectByType<HapticsService>();
            if (haptics != null) { if (metres <= 0f) haptics.Fail(); else haptics.Light(); }
            if (feedback == null) return;
            feedback.text = VietText.Fix(metres <= 0f ? "NGÃ!"
                : metres >= rules.Tuning.maxJumpMetres - .3f ? $"ĐẸP! {metres:0.0} m" : $"{metres:0.0} m");
            feedbackUntil = Time.time + .8f;
        }
    }
}
```

If `VietText` is not visible from this assembly, add `using KMA.Text;` (namespace of `VietText` in `Assets/Scripts/Utils/VietText.cs`) — check the namespace with `grep -n namespace Assets/Scripts/Utils/VietText.cs`. The fixed tutorial copy is set by the configurator on the shared tutorial card (Step 5).

`PausePanel.cs`: add field `bool leaveOptionsVisible = true;`, method

```csharp
        /// The frog jump is mandatory: its pause menu only resumes.
        public void SetLeaveOptionsVisible(bool visible)
        {
            leaveOptionsVisible = visible;
            if (restartButton != null) restartButton.gameObject.SetActive(visible);
            if (exitButton != null) exitButton.gameObject.SetActive(visible);
        }
```

and guard `Restart()` / `ExitToMap()` with `if (!leaveOptionsVisible) return;` as the first line. In `SetMenuVisible(bool)`, after the existing body, re-apply `if (visible) SetLeaveOptionsVisible(leaveOptionsVisible);` so reopening the menu keeps them hidden.

- [ ] **Step 4: Scene configurator**

`Assets/Editor/FrogJumpSceneConfigurator.cs` — follow `VolleyballSceneConfigurator` (same `using`s, `Quad`, `UiRect`, `EnsureInBuildSettings` helpers copied verbatim), with:

```csharp
    public static class FrogJumpSceneConfigurator
    {
        public const string ScenePath = "Assets/_Project/Scenes/MG_FrogJump.unity";
        const string BalancePath = "Assets/_Project/ScriptableObjects/FrogJump/FrogJumpBalance.asset";
        const string PixelPath = "Assets/_Project/Art/Environments/Volleyball/Pixel.png";
        const string HudRootName = "S2_HUD_Minigame";
        const float StartX = -7f, FinishX = 7f, GroundY = -2f;
        const int Segments = 20;

        [MenuItem("KMA/Frog Jump/Build Scene")]
        public static void BuildScene()
        {
            CharacterArt.ImportAll();
            FrogJumpBalanceConfig balance = EnsureBalance();
            BuildWorld(balance);
            MinigameUIAssembler.AssembleScenePath(ScenePath);
            AddControls(balance);
            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
        }
```

- `EnsureBalance()`: create folder `Assets/_Project/ScriptableObjects/FrogJump` if missing; load or `CreateInstance<FrogJumpBalanceConfig>()` + `AssetDatabase.CreateAsset`.
- `BuildWorld`: `EditorSceneManager.NewScene(EmptyScene, Single)`; quads `Sky` (`new Color32(120, 205, 240, 255)`, centre `(0, 4)`, size `(60, 16)`), `Grass` (`new Color32(110, 190, 90, 255)`, centre `(0, GroundY - 6)`, size `(60, 12)`), `Track` (`new Color32(214, 120, 80, 255)`, centre `(0, GroundY - .6f)`, size `(FinishX - StartX + 2, 1.2)`), `StartLine` (white, `(StartX, GroundY - .6f)`, `(.12, 1.2)`), `FinishLine` (white, `(FinishX, GroundY - .6f)`, `(.2, 1.2)`), `FinishFlag` (`MinigameUiTheme.Accent`, `(FinishX + .3f, GroundY + .9f)`, `(.6, .4)`); hero `SpriteRenderer` "Hero" with `CharacterArt.Load(CharacterArt.Hero, "duck")`, scale `1.8`, sorting order 10; a `FrogJumpView` on "FrogJumpView" configured with poses `duck` / `jump` / `fallDown` and `StartX, FinishX, GroundY`; EventSystem with `InputSystemUIInputModule`; "FrogJumpController" GameObject with `FrogJumpController`; `SaveScene(scene, ScenePath)`.
- `AddControls`: open the scene, find `HudRootName`, set sorting order 500, parent = `SafeAreaRoot`. Keep the shared HUD children (timer/progress/status are driven by `BuildHudState`). Create:
  - `TapArea`: `UiRect(..., Vector2.zero, Vector2.one)`, transparent `Image` (`raycastTarget = true`), `FrogJumpTapArea`, `SetAsFirstSibling()`.
  - `PowerBar`: `UiKit.Panel(parent, "PowerBar")` placed with `UiKit.Place(rect, new Vector2(.5f, .1f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900f, 72f))`; child `Track` stretched with 12 px inset; `Segments` child `Image`s laid out by anchors `i/Segments .. (i+1)/Segments`, coloured `FrogJumpPowerBar.SegmentColor((i + .5f) / Segments, balance.Tuning)`; child `Needle` (`Image`, `Color.white`, size `(10, 92)`, outline `MinigameUiTheme.TextPrimary`); `FrogJumpPowerBar.Configure(track, needle)`. Every bar image sets `raycastTarget = false` so taps reach `TapArea`.
  - `Feedback`: `UiKit.Label(parent, "Feedback", string.Empty, MinigameUiTheme.Headline, MinigameUiTheme.Accent, TextAlignmentOptions.Center, outline: true)` placed at `(.5, .62)` size `(680, 100)`.
  - Tutorial copy: find the `TutorialOverlay` in the scene and set its body to `VietText.Fix("Chạm khi kim ở giữa để bật xa. Sát mép là ngã!")` using the same field/method the Sprint configurator uses (`grep -n "TutorialOverlay" Assets/Editor/SprintSceneConfigurator.cs` and mirror that call).
  - Move `PausePanel` to the top-right exactly like the volleyball configurator.
  - `controller.Configure(balance, view, bar, tap, feedback)`; `SetDirty` everything; save.

- [ ] **Step 5: Build the scene**

Run: `"$UNITY" -batchmode -projectPath . -executeMethod KMA.EditorTools.FrogJumpSceneConfigurator.BuildScene -quit -logFile Builds/frog-scene.log`
Expected: `Assets/_Project/Scenes/MG_FrogJump.unity` exists; `ProjectSettings/EditorBuildSettings.asset` lists it; no `Exception` in the log.

- [ ] **Step 6: Run the scene test and the existing minigame contract tests**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.EditorTools.FrogJumpSceneTests;KMA.Tests.Gameplay.Common;KMA.Tests.EditorTools.MinigameStyleConsistencyTests" frog-scene`
Expected: all pass. If `MinigameStyleConsistencyTests` enumerates minigame scenes and flags `MG_FrogJump` (font size below `MinigameUiTheme.MinimumFontSize`, colours off-theme), fix the configurator, not the test.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Gameplay/FrogJump Assets/_Project/Scripts/UI/PausePanel.cs \
  Assets/Editor/FrogJumpSceneConfigurator.cs Assets/Editor/FrogJumpSceneConfigurator.cs.meta \
  Assets/_Project/Scenes/MG_FrogJump.unity Assets/_Project/Scenes/MG_FrogJump.unity.meta \
  Assets/_Project/ScriptableObjects/FrogJump Assets/_Project/ScriptableObjects/FrogJump.meta \
  ProjectSettings/EditorBuildSettings.asset \
  Assets/Tests/EditMode/EditorTools/FrogJumpSceneTests.cs Assets/Tests/EditMode/EditorTools/FrogJumpSceneTests.cs.meta \
  Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef
git commit -m "feat(frogjump): add the frog jump scene and controller"
```

---

### Task 7: Routing — failure → frog jump → retry, and the result panel

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs` (frog panel contract)
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`, `SceneRouter.Journey.cs`
- Create: `Assets/_Project/Scripts/Core/SceneRouter.FrogJump.cs`
- Modify: `Assets/_Project/Scripts/UI/ResultPanel.cs`
- Create: `Assets/Tests/PlayMode/Progression/FrogJumpRoutingTests.cs`
- Modify: `Assets/Tests/PlayMode/Progression/JourneyGameplayDriver.cs`, `StudentJourneyFlowTests.cs`, `JourneyRoutingTests.cs`, `FullGameplayFlowTests.cs`

**Interfaces:**
- Consumes: Task 3 `GameSession` frog APIs; Task 6 scene name `MG_FrogJump`.
- Produces:
  - `readonly struct FrogJumpResultView(bool reachedFinish, bool savesLife, int livesRemaining, string error)`.
  - `interface IFrogJumpResultPanel { event Action FrogJumpContinueRequested; void ShowFrogJump(FrogJumpResultView view); }` — implemented by `ResultPanel`.
  - `SceneRouter.StartFrogJump()` (`bool`), `SceneRouter.FrogJumpScene` (`string`), test hook `internal void CompleteFrogJumpForTests(bool reachedFinish)`.

- [ ] **Step 1: Write the failing PlayMode tests**

Look at `Assets/Tests/PlayMode/Progression/JourneyRoutingTests.cs` for how a router is created with `ConfigureRouteAcceptanceForTests` and a `ResultPanel` in the scene; reuse its `SetUp` helpers (copy them into the new fixture if they are private). Then create `FrogJumpRoutingTests.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FrogJumpRoutingTests
    {
        readonly List<SceneRouteTransition> routes = new List<SceneRouteTransition>();
        SceneRouter router;
        ResultPanel panel;

        [SetUp]
        public void SetUp()
        {
            // Same construction as JourneyRoutingTests: a router with an in-memory session,
            // route acceptance recorded into `routes`, persistence that always succeeds,
            // and a code-built ResultPanel in the active scene.
            router = JourneyRoutingFixture.CreateRouter(routes);
            panel = JourneyRoutingFixture.CreateResultPanel();
        }

        [TearDown]
        public void TearDown() => JourneyRoutingFixture.Destroy(router, panel);

        [Test]
        public void FailedExamOffersOnlyTheFrogJumpAndRoutesToItsScene()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");

            Assert.That(panel.RetryAvailable, Is.False);
            Assert.That(panel.ContinueLabel, Is.EqualTo("BẬT CÓC"));
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.FrogJump));
            Assert.That(routes[routes.Count - 1].SceneName, Is.EqualTo("MG_FrogJump"));
        }

        [Test]
        public void WinningTheFirstFrogJumpKeepsTheLifeAndRestartsTheChallenge()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(true);

            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(router.Session.PendingFrogJump, Is.Null);
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.Subject));
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId, Is.EqualTo("sprint_exam"));
        }

        [Test]
        public void DuplicateFrogCompletionAndDoubleContinueApplyOnce()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(false);
            router.CompleteFrogJumpForTests(false);
            Assert.That(router.Session.Lives, Is.EqualTo(4));
            int before = routes.Count;
            panel.Continue();
            panel.Continue();
            Assert.That(routes.Count, Is.EqualTo(before + 1));
        }

        [Test]
        public void LastLifeLostLandsOnTheMap()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(true);
            panel.Continue();
            router.Session.Journey.SetAttemptsRemaining(1);
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(true);

            Assert.That(router.Session.Lives, Is.Zero);
            Assert.That(panel.ContinueLabel, Is.EqualTo("VỀ BẢN ĐỒ"));
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(router.Session.ActiveSubject, Is.Null);
        }
    }
}
```

Create `Assets/Tests/PlayMode/Progression/JourneyRoutingFixture.cs` as an `internal static class` by **moving** the router/panel construction already present in `JourneyRoutingTests` (`SetUp`/helpers) into `CreateRouter(List<SceneRouteTransition> routes)`, `CreateResultPanel()`, `Destroy(SceneRouter, ResultPanel)`, plus:

```csharp
        public static void CompleteThrough(GameSession session, string id) =>
            JourneyGameplayDriver.CompleteThrough(session, id);

        /// Starts `id` through the router and reports a failed result to the result panel.
        public static void FailActive(SceneRouter router, ResultPanel panel, string id)
        {
            ChallengeDefinition definition = router.Session.Journey.Catalog.Get(id);
            Assert.That(router.TryStartChallenge(id, ChallengeAttemptMode.Journey, definition.Difficulty), Is.True);
            router.ReportChallengeResultForTests(new ChallengeAttemptResult(router.Session.Journey.ActiveAttempt,
                false, new ChallengeMetrics(), definition.Kind == ChallengeKind.Exam
                    ? new MinigameResult(false, 0f, Rank.F) : null));
        }
```

and make `JourneyRoutingTests` call the fixture. If `JourneyGameplayDriver` has no `CompleteThrough`, add one mirroring `JourneyTestData.CompleteThrough` (EditMode) — PlayMode cannot see EditMode helpers. `router.ReportChallengeResultForTests(result)` is a new `internal` method in `SceneRouter.Journey.cs` that calls `PreviewChallengeResult(session.Journey.ActiveAttempt, result)`; check whether `JourneyRoutingTests` already has an equivalent hook and reuse it instead if so. The PlayMode test asmdef must have `InternalsVisibleTo` access — check for an existing `AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("KMA.Gameplay.Progression.PlayMode.Tests")]` in `Assets/_Project/Scripts/Core`; `ConfigureRouteAcceptanceForTests` is already `internal`, so it exists.

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression.FrogJumpRoutingTests" frog-routing`
Expected: compile errors (`CompleteFrogJumpForTests`, `ContinueLabel`, `SessionRoute.FrogJump` scene).

- [ ] **Step 3: Contracts and router**

1. `ChallengeContracts.cs` append:

```csharp
    public readonly struct FrogJumpResultView
    {
        public bool ReachedFinish { get; }
        public bool SavesLife { get; }
        public int LivesRemaining { get; }
        public string Error { get; }

        public FrogJumpResultView(bool reachedFinish, bool savesLife, int livesRemaining, string error)
        {
            ReachedFinish = reachedFinish;
            SavesLife = savesLife;
            LivesRemaining = livesRemaining;
            Error = error;
        }
    }

    public interface IFrogJumpResultPanel
    {
        event Action FrogJumpContinueRequested;
        void ShowFrogJump(FrogJumpResultView view);
    }
```

2. `SceneRouter.cs`:
   - `TryGetSceneName`: add `SessionRoute.FrogJump => frogJumpScene,`.
   - `PrepareSceneBinding`: add `case SessionRoute.FrogJump:` to the `Map`/`GameOver` group.
   - `OnSceneLoaded`: first line `UnbindFrogJump();`, last line `BindFrogJump(scene);`.
   - `ResumeCampaign` already routes `session.ResumeRoute()`; no change.
3. `SceneRouter.Journey.cs`:
   - Delete `PracticeCurrentSubject()`.
   - `HandleChallengeAction`: replace the `Practice` branch with

```csharp
            else if (action == JourneyResultAction.FrogJump)
            {
                if (!StartFrogJump()) RestoreChallengeActions();
            }
```

     and the final `else` route with `bool routed = Route(SessionRoute.Map);`.
   - Add `internal void ReportChallengeResultForTests(ChallengeAttemptResult result) => PreviewChallengeResult(session.Journey.ActiveAttempt, result);` (skip if an equivalent hook exists).
4. `SceneRouter.FrogJump.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.Gameplay.Core
{
    public sealed partial class SceneRouter
    {
        [SerializeField] string frogJumpScene = "MG_FrogJump";

        MinigameBase boundFrogJump;
        Action<MinigameResult> frogJumpHandler;
        IFrogJumpResultPanel frogPanel;
        FrogJumpResultView frogView;
        string frogRetryChallengeId;
        bool frogSavePending;
        bool frogContinueUsed;

        public string FrogJumpScene => frogJumpScene;

        public bool StartFrogJump()
        {
            lastRouteError = null;
            if (IsTransitioning || session.PendingFrogJump == null ||
                !TryGetSceneName(SessionRoute.FrogJump, null, out string sceneName))
                return false;
            PrepareSceneBinding(SessionRoute.FrogJump, null);
            if (!transitioner.TryRoute(SessionRoute.FrogJump, null, sceneName))
                return false;
            SessionChanged?.Invoke();
            return true;
        }

        internal void CompleteFrogJumpForTests(bool reachedFinish)
        {
            if (session.PendingFrogJump != null)
                OnFrogJumpCompleted(session.PendingFrogJump.Id, new MinigameResult(reachedFinish, 0f,
                    reachedFinish ? Rank.C : Rank.F));
        }

        void BindFrogJump(Scene scene)
        {
            FrogJumpPending pending = session.PendingFrogJump;
            if (pending == null || !string.Equals(scene.name, frogJumpScene, StringComparison.Ordinal))
                return;
            MinigameBase minigame = FindFirstObjectByType<MinigameBase>(FindObjectsInactive.Exclude);
            if (minigame == null)
                return;
            string id = pending.Id;
            boundFrogJump = minigame;
            frogJumpHandler = result => OnFrogJumpCompleted(id, result);
            minigame.Completed += frogJumpHandler;
        }

        void UnbindFrogJump()
        {
            if (boundFrogJump != null && frogJumpHandler != null)
                boundFrogJump.Completed -= frogJumpHandler;
            boundFrogJump = null;
            frogJumpHandler = null;
        }

        void OnFrogJumpCompleted(string frogJumpId, MinigameResult result)
        {
            FrogJumpPending pending = session.PendingFrogJump;
            if (pending == null || pending.Id != frogJumpId || result == null)
                return;
            string retryId = pending.FailedChallengeId;
            bool savesLife = pending.SavesLife;
            if (!session.TryApplyFrogJump(frogJumpId, result.Pass))
                return;

            frogRetryChallengeId = retryId;
            frogContinueUsed = false;
            frogSavePending = !TryPersistJourney(out string saveError);
            SessionChanged?.Invoke();

            UnbindFrogPanel();
            frogPanel = FindFrogJumpPanel() ??
                throw new InvalidOperationException("A frog jump result panel is required.");
            frogPanel.FrogJumpContinueRequested += OnFrogJumpContinue;
            frogView = new FrogJumpResultView(result.Pass, savesLife, session.Lives, saveError);
            frogPanel.ShowFrogJump(frogView);
        }

        void OnFrogJumpContinue()
        {
            if (frogPanel == null || frogContinueUsed)
                return;
            if (frogSavePending)
            {
                if (!TryPersistJourney(out string saveError))
                {
                    ShowFrogError(saveError);
                    return;
                }
                frogSavePending = false;
            }

            frogContinueUsed = true;
            bool routed;
            if (session.Lives > 0 && !string.IsNullOrEmpty(frogRetryChallengeId))
            {
                ChallengeDefinition definition = session.Journey.Catalog.Get(frogRetryChallengeId);
                routed = TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey, definition.Difficulty);
            }
            else
                routed = Route(SessionRoute.Map);

            if (routed)
                UnbindFrogPanel();
            else
            {
                frogContinueUsed = false;
                ShowFrogError(lastRouteError ?? "Không thể chuyển cảnh. Hãy thử lại.");
            }
        }

        void ShowFrogError(string error)
        {
            frogView = new FrogJumpResultView(frogView.ReachedFinish, frogView.SavesLife, session.Lives, error);
            frogPanel?.ShowFrogJump(frogView);
        }

        bool TryPersistJourney(out string error)
        {
            error = null;
            if (journeyPersist == null)
                return true;
            try { return journeyPersist(out error); }
            catch (Exception exception) { error = exception.Message; return false; }
        }

        void UnbindFrogPanel()
        {
            if (frogPanel != null)
                frogPanel.FrogJumpContinueRequested -= OnFrogJumpContinue;
            frogPanel = null;
        }

        static IFrogJumpResultPanel FindFrogJumpPanel()
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IFrogJumpResultPanel panel)
                    return panel;
            return null;
        }
    }
}
```

`TryStartChallenge` takes its own pre-start snapshot and handles persistence rollback, so no extra snapshot is needed here.

- [ ] **Step 4: `ResultPanel`**

1. Implement `IFrogJumpResultPanel`; add `public event Action FrogJumpContinueRequested;`, field `bool frogMode;`, property `public string ContinueLabel => actionButton != null ? actionButton.GetComponentInChildren<TMP_Text>()?.text : null;` (compare against `VietText.Fix(...)` output; if `VietText.Fix` changes the string, the tests compare `VietText.Fix("BẬT CÓC")`).
2. In `Show(...)` and `ShowChallenge(...)` set `frogMode = false;` first.
3. `Continue()`: after the `HasContinued = true;` line insert

```csharp
            if (frogMode)
            {
                FrogJumpContinueRequested?.Invoke();
                return;
            }
```

   and in the `challengeContext != null` branch replace the `AwaitingSupplementary ? JourneyResultAction.Practice` arm with `challengeOutcome.HasValue && challengeOutcome.Value.FrogJumpRequired ? JourneyResultAction.FrogJump : JourneyResultAction.Continue`.
4. `ShowChallengeCore`: replace every `AwaitingSupplementary` use:

```csharp
            bool frogJump = outcome.HasValue && outcome.Value.FrogJumpRequired;
            SetDetail(saveError ?? (context.Mode == ChallengeAttemptMode.Journey
                ? outcome.HasValue
                    ? frogJump
                        ? outcome.Value.FrogJumpSavesLife
                            ? "Về đích trong 60 s để giữ lượt thi"
                            : "−1 lượt thi. Bật cóc xong mới được thi lại"
                        : "Kết quả đã lưu"
                    : "Kết quả đang chờ lưu"
                : $"Thời gian {result.Metrics.Elapsed:0.0}s"));
            ...
            SetButtonLabel(actionButton, saveError == null ? frogJump ? "BẬT CÓC" : "TIẾP TỤC"
                : outcome.HasValue ? "THỬ LẠI" : "LƯU LẠI");
            retryAvailable = result.ExamResult != null && !result.Pass && outcome.HasValue &&
                outcome.Value.AttemptsRemaining > 0 && !frogJump;
```

   and show `livesLabel` when `result.ExamResult != null || frogJump`.
5. Add:

```csharp
        public void ShowFrogJump(FrogJumpResultView view)
        {
            frogMode = true;
            challengeContext = null;
            challengeOutcome = null;
            challengeResult = null;
            CurrentResult = new MinigameResult(view.ReachedFinish, 0f, view.ReachedFinish ? Rank.C : Rank.F);
            HasContinued = false;
            actionPending = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null) contentRoot.SetActive(true);
            ApplyTheme();
            if (statusLabel != null)
            {
                statusLabel.text = VietText.Fix(view.ReachedFinish ? "VỀ ĐÍCH!" : "HẾT GIỜ!");
                statusLabel.color = view.ReachedFinish ? MinigameUiTheme.Success : MinigameUiTheme.Energy;
            }
            finalScoreText = string.Empty;
            if (scoreLabel != null) scoreLabel.text = string.Empty;
            if (rankLabel != null) rankLabel.gameObject.SetActive(false);
            SetDetail(!view.SavesLife ? "Lượt thi đã bị trừ khi trượt bài"
                : view.ReachedFinish ? "Giữ được lượt thi" : "−1 lượt thi");
            if (livesLabel != null)
            {
                livesLabel.text = VietText.Fix($"LƯỢT THI: {view.LivesRemaining}/{GameSession.MaxLives}");
                livesLabel.gameObject.SetActive(true);
            }
            if (errorLabel != null) errorLabel.text = VietText.Fix(view.Error ?? string.Empty);
            SetButtonLabel(actionButton, view.LivesRemaining > 0 ? "THI LẠI" : "VỀ BẢN ĐỒ");
            retryAvailable = false;
            RefreshButtons();
            Reveal(0f);
        }
```

   (`Reveal(0f)` animates the card; if it also writes `scoreLabel`, pass the same value and accept a `0` score text, or skip `Reveal` and only fade in — keep whichever does not show a stray score; verify in Task 9 screenshots.)

- [ ] **Step 5: Update the existing PlayMode tests**

Run `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression;KMA.Tests.Gameplay.UI" playmode-journey` and update:

- `JourneyGameplayDriver.cs`: mode is always `ChallengeAttemptMode.Journey`; the outcome constructor becomes `new JourneyCommitOutcome(false, id, session.Lives, session.PendingFrogJump != null, session.PendingFrogJump?.SavesLife ?? false, session.Journey.CourseComplete)`.
- `StudentJourneyFlowTests.FailedExam_CanBeRecoveredThroughSupplementaryPracticeAndRetry` → rename `FailedExam_IsRecoveredThroughTheFrogJumpAndRetry`: fail `sprint_exam`; assert `failure.FrogJumpRequired`; `router.CompleteFrogJumpForTests(true)`; continue; assert the active attempt is `sprint_exam` and `Lives == 5`.
- `StudentJourneyFlowTests` lines ~52–58 (supplementary assertions) and ~98–99 (mode selection): use `Journey` mode and assert `PendingFrogJump`/lives per Global Constraints.
- `JourneyRoutingTests.FailedStartFromResultCanBeRetriedAfterPersistenceRecovers(bool supplementary)` → parameter becomes `bool frogJump`: `true` presses `panel.Continue()` (frog jump route) and expects `SessionRoute.FrogJump` with lives 5; `false` covers a learn failure retried via `TIẾP TỤC` → Map. Replace the `Lives` expectation `supplementary ? 0 : 4` accordingly.
- `FullGameplayFlowTests` line ~129 (`SessionRoute.GameOver` after the last life): expect `SessionRoute.Map`.
- `JourneyMapTests` is updated in Task 8.

- [ ] **Step 6: Run the PlayMode progression tests**

Run: `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.Progression" playmode-journey`
Expected: all pass, including `FrogJumpRoutingTests`.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs \
  Assets/_Project/Scripts/Core/SceneRouter.cs Assets/_Project/Scripts/Core/SceneRouter.Journey.cs \
  Assets/_Project/Scripts/Core/SceneRouter.FrogJump.cs Assets/_Project/Scripts/Core/SceneRouter.FrogJump.cs.meta \
  Assets/_Project/Scripts/UI/ResultPanel.cs \
  Assets/Tests/PlayMode/Progression/FrogJumpRoutingTests.cs Assets/Tests/PlayMode/Progression/FrogJumpRoutingTests.cs.meta \
  Assets/Tests/PlayMode/Progression/JourneyRoutingFixture.cs Assets/Tests/PlayMode/Progression/JourneyRoutingFixture.cs.meta \
  Assets/Tests/PlayMode/Progression/JourneyGameplayDriver.cs Assets/Tests/PlayMode/Progression/StudentJourneyFlowTests.cs \
  Assets/Tests/PlayMode/Progression/JourneyRoutingTests.cs Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs
git commit -m "feat(router): route failed challenges through the frog jump and back"
```

---

### Task 8: Map — regen countdown, live unlock, `Hết lượt thi`, supplementary UI removal

**Files:**
- Modify: `Assets/_Project/Scripts/UI/HeartBar.cs`, `MapScreen.cs`, `JourneyLessonList.cs`, `JourneyCourseSummary.cs`, `JourneyDialoguePresenter.cs`
- Create: `Assets/Tests/EditMode/Presentation/HeartBarCountdownTests.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/JourneyMapTests.cs`

**Interfaces:**
- Consumes: Task 3 `GameSession.RefreshLives`, `TimeUntilNextLife`; `GameManager.Instance.TryPersistSession(out string)`.
- Produces: `HeartBar.SetCountdown(TimeSpan? remaining, bool warning)`, `HeartBar.CountdownText` (`string`, empty when hidden), `HeartBar.CountdownVisible`, `static string HeartBar.FormatCountdown(TimeSpan)`; `MapScreen.Update` polls the bound session each frame.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/Presentation/HeartBarCountdownTests.cs`:

```csharp
using System;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.UI
{
    public sealed class HeartBarCountdownTests
    {
        [TestCase(272, "4:32")]
        [TestCase(300, "5:00")]
        [TestCase(59.2, "1:00")]
        [TestCase(0, "0:00")]
        public void FormatsMinutesAndSecondsRoundingUp(double seconds, string expected)
        {
            Assert.That(HeartBar.FormatCountdown(TimeSpan.FromSeconds(seconds)), Is.EqualTo(expected));
        }

        [Test]
        public void ShowsAboveTheHeartsOnlyWhileRegenerating()
        {
            var go = new GameObject("HeartBar", typeof(RectTransform));
            try
            {
                var bar = go.AddComponent<HeartBar>();
                bar.SetCountdown(TimeSpan.FromSeconds(272), false);
                Assert.That(bar.CountdownVisible, Is.True);
                Assert.That(bar.CountdownText, Is.EqualTo("4:32"));
                bar.SetCountdown(null, false);
                Assert.That(bar.CountdownVisible, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
```

Check the namespace/asmdef of `Assets/Tests/EditMode/Presentation` (`rootNamespace` in its asmdef) and use it.

In `JourneyMapTests.cs` replace `ReviewDoesNotMoveCheckpointAndExhaustedBudgetOffersCurrentSupplementaryPractice` with a test named `ReviewDoesNotMoveCheckpointAndZeroLivesLocksTheCurrentExam` that keeps the review half unchanged and, for the second half, sets `session.Journey.SetAttemptsRemaining(0)`, rebinds the lesson list, and asserts the current exam card's button `interactable == false`, its objective text `VietText.Fix("Hết lượt thi")`, and that the learn card of the same subject (when it is the checkpoint in a fresh session) stays interactable at 0 lives.

- [ ] **Step 2: Run to verify failure**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.UI.HeartBarCountdownTests" heart-countdown`
Expected: compile error (`SetCountdown`).

- [ ] **Step 3: Implement**

`HeartBar.cs` additions:

```csharp
        [SerializeField] TMPro.TMP_Text countdownLabel;

        public bool CountdownVisible => countdownLabel != null && countdownLabel.gameObject.activeSelf;
        public string CountdownText => CountdownVisible ? countdownLabel.text : string.Empty;

        public static string FormatCountdown(System.TimeSpan remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        public void SetCountdown(System.TimeSpan? remaining, bool warning)
        {
            EnsureCountdownLabel();
            countdownLabel.gameObject.SetActive(remaining.HasValue);
            if (!remaining.HasValue) return;
            countdownLabel.text = FormatCountdown(remaining.Value);
            countdownLabel.color = warning ? MinigameUiTheme.Energy : MinigameUiTheme.TextPrimary;
        }

        void EnsureCountdownLabel()
        {
            if (countdownLabel != null) return;
            countdownLabel = UiKit.Label(transform, "LifeTimer", string.Empty, 22f, MinigameUiTheme.TextPrimary);
            countdownLabel.enableWordWrapping = false;
            var layout = countdownLabel.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.ignoreLayout = true;
            RectTransform rect = countdownLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 2f);
            rect.sizeDelta = new Vector2(0f, 26f);
            countdownLabel.raycastTarget = false;
        }
```

If `UiKit.Label` clamps sizes to `MinimumFontSize` (28) and that pushes the label outside the 84 px header, set `countdownLabel.fontSize = 22f` after creation; the Map header is not under the minigame style audit (it already uses 24).

`MapScreen.cs`: store the session in `BindPresentation` (`boundSession = session;`) and add

```csharp
        GameSession boundSession;

        void Update()
        {
            if (boundSession == null) return;
            if (boundSession.RefreshLives())
            {
                KMA.Gameplay.Core.GameManager.Instance?.TryPersistSession(out _);
                RefreshJourney(boundSession);
            }
            Hearts?.SetCountdown(boundSession.TimeUntilNextLife, boundSession.Lives == 0);
        }
```

`JourneyLessonList.cs` (inside the card loop and below):

```csharp
                bool outOfLives = checkpoint && !complete && !session.Journey.CourseComplete &&
                    challenge.Kind != ChallengeKind.Learn && session.Lives == 0;
                card.Objective.text = VietText.Fix(outOfLives ? "Hết lượt thi" : objective);
                card.Button.interactable = unlocked && !outOfLives;
                ApplyState(card, index, complete, checkpoint, unlocked, chapterColor, outOfLives);
                ...
                ChallengeAttemptMode mode = session.Journey.CourseComplete
                    ? ChallengeAttemptMode.FreePlay
                    : complete ? ChallengeAttemptMode.Review
                        : checkpoint ? ChallengeAttemptMode.Journey : ChallengeAttemptMode.Review;
```

- `ApplyState`'s last parameter is renamed `outOfLives`; status text `checkpoint ? outOfLives ? "CHỜ HỒI LƯỢT" : "BẮT ĐẦU  ›"`.
- Hint: `session.Journey.CourseComplete ? "...chơi lại" : session.Lives == 0 ? "Hết lượt thi · Chờ hồi lượt để thi tiếp" : "Hoàn thành từng chặng để mở bài tiếp theo"`.
- `ContinueCheckpoint`: mode is always `ChallengeAttemptMode.Journey`; the continue button is not interactable when the current card is `outOfLives` (track it in a field set in the loop).

`JourneyCourseSummary.cs`: summary text becomes `VietText.Fix("HOÀN TẤT  |  " + string.Join("   |   ", scores))` (keep the `SupplementaryRounds` property assignment).

`JourneyDialoguePresenter.cs`: delete the `if (journey.AwaitingSupplementary) { Add("supplementary", ...); } else` prefix so the chain starts at `if (journey.CourseComplete)`. The `supplementary` node stays in the dialogue library (its data test requires it) but is never queued.

- [ ] **Step 4: Run tests**

Run: `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.UI" ui-editmode` then `tools/run-unity-tests.sh PlayMode "KMA.Tests.Gameplay.UI" ui-playmode`
Expected: all pass.

- [ ] **Step 5: Delete the Task 2 compile shims**

Remove `JourneyProgress.AwaitingSupplementary`, `JourneyCommitOutcome.AwaitingSupplementary` and the obsolete 5-argument `JourneyCommitOutcome` ctor. Run `grep -rn "AwaitingSupplementary" Assets/_Project Assets/Tests` — expected: no matches. Re-run both commands from Step 4 plus `tools/run-unity-tests.sh EditMode "KMA.Tests.Gameplay.Progression" progression`; all pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs \
  Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs \
  Assets/_Project/Scripts/UI/HeartBar.cs Assets/_Project/Scripts/UI/MapScreen.cs \
  Assets/_Project/Scripts/UI/JourneyLessonList.cs Assets/_Project/Scripts/UI/JourneyCourseSummary.cs \
  Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs \
  Assets/Tests/EditMode/Presentation/HeartBarCountdownTests.cs Assets/Tests/EditMode/Presentation/HeartBarCountdownTests.cs.meta \
  Assets/Tests/PlayMode/Presentation/JourneyMapTests.cs
git commit -m "feat(map): show the life regen countdown and lock practice and exams at zero lives"
```

---

### Task 9: Full verification and visual QA

**Files:**
- Create: `docs/qa/frog-jump-penalty.md` and screenshots under `docs/qa/images/frog-jump-*.png`

- [ ] **Step 1: Full test suites**

Run: `tools/run-unity-tests.sh EditMode "" all-editmode` and `tools/run-unity-tests.sh PlayMode "" all-playmode`
Expected: zero failures in both XML files. Any failure is fixed in the task that owns the code, then re-run.

- [ ] **Step 2: Grep for leftovers**

Run: `grep -rn "AwaitingSupplementary\|PracticeCurrentSubject\|ChallengeAttemptMode.Supplementary" Assets/_Project Assets/Tests`
Expected: only the enum member declaration in `ChallengeAttempt.cs` and `SupplementaryModeIsAlwaysRejected` / migration tests.

- [ ] **Step 3: Screenshots**

Use the `testing-unity-ui-with-screenshots` skill (`.claude/skills/testing-unity-ui-with-screenshots/SKILL.md`) to capture:
1. `MG_FrogJump` tutorial card (copy `Chạm khi kim ở giữa để bật xa. Sát mép là ngã!`).
2. `MG_FrogJump` mid-play: needle visible, coloured bar with red ends, timer and distance in the HUD, hero on the track.
3. `MG_FrogJump` right after a fall (`NGÃ!`, fall pose, needle hidden).
4. Frog result card for a win on first failure (`VỀ ĐÍCH!`, `Giữ được lượt thi`, `THI LẠI`) and for 0 lives (`VỀ BẢN ĐỒ`).
5. Exam failure result panel, first failure (`BẬT CÓC`, `Về đích trong 60 s để giữ lượt thi`) and second failure (`−1 lượt thi. Bật cóc xong mới được thi lại`).
6. Map header with `Lượt thi: 3/5` and the `m:ss` countdown above the hearts; Map at 0 lives with the exam card showing `Hết lượt thi`.

Check each image: no clipped Vietnamese diacritics, countdown does not overlap the hearts or header edge, power bar does not cover the hero, pause menu in the frog scene shows only `Tiếp tục`.

- [ ] **Step 4: Write QA notes and commit**

`docs/qa/frog-jump-penalty.md`: one line per screenshot with what was checked and the result, plus the test-run totals from Step 1.

```bash
git add docs/qa/frog-jump-penalty.md docs/qa/images/frog-jump-*.png
git commit -m "docs(qa): frog jump penalty and life regen evidence"
```
