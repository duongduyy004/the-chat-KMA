# Progression and Boss Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the seven minigames into the five-life/two-attempt loop, punishment sequence, boss unlock, and final three-phase boss.

**Architecture:** `GameSession` is the sole progression state machine. Minigames submit immutable results; the session chooses punishment, retry, map, game-over, or boss-unlocked transitions. Save records store facts, while `BossUnlocked` is derived from seven passed subject records.

**Tech Stack:** Unity 6.3 LTS, C#, ScriptableObject challenge sequences, NUnit EditMode tests, Unity PlayMode flow tests.

**Spec:** `PLAN.md` sections 0, 2.4, 2.5, 5, and “Hình phạt + Boss”.

## Global Constraints

- A subject visit starts at attempt one; first failure routes through punishment to attempt two.
- Second failure loses one life, records score zero for that visit, returns to the map, and resets that subject's visit-attempt counter for a later retry.
- Zero lives enters `GameOver`; no subject or boss can start from that state.
- Passed subjects remain replayable for best score but do not consume progression attempts or lives.
- Boss unlock is computed from all seven `Passed` records and is never independently persisted.
- Boss phases use TapMash, RhythmHold, and AlternateTap in a fixed authored sequence.

---

### Task 1: GameSession transition contract

**Files:**
- Create: `Assets/_Project/Scripts/Progression/SubjectId.cs`
- Create: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Create: `Assets/_Project/Scripts/Progression/SubjectRecord.cs`
- Test: `Assets/Tests/EditMode/Progression/GameSessionTests.cs`

**Interfaces:**
- Consumes: `StartSubject(SubjectId)`, `SubmitResult(SubjectId, MinigameResult)`, `CompletePunishment()`.
- Produces: `SessionRoute` and seven subject records.

- [ ] **Step 1: Write failing two-attempt and unlock tests**

```csharp
[Test] public void FirstFail_RoutesPunishment_ThenSecondFailLosesLife() {
  var s = new GameSession(); s.StartSubject(SubjectId.Sprint);
  Assert.That(s.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Punishment));
  Assert.That(s.CompletePunishment(), Is.EqualTo(SessionRoute.RetrySubject));
  Assert.That(s.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
  Assert.That(s.Lives, Is.EqualTo(4));
}
[Test] public void SevenPassedSubjects_DeriveBossUnlock() {
  var s = new GameSession();
  foreach (SubjectId id in Enum.GetValues(typeof(SubjectId))) { s.StartSubject(id); s.SubmitResult(id, Passed(6)); }
  Assert.That(s.BossUnlocked, Is.True);
}
[Test] public void BonusScoreCannotOverrideFailedResult() {
  var s = new GameSession(); s.StartSubject(SubjectId.Sprint);
  Assert.That(s.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 10, Rank.S)), Is.EqualTo(SessionRoute.Punishment));
}
static MinigameResult Failed() => new(false, 0, Rank.F);
static MinigameResult Passed(float score) => new(true, score, ScoreUtil.ToRank(score));
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter GameSessionTests -testResults TestResults-session-red.xml -quit`

- [ ] **Step 3: Implement explicit transitions**

```csharp
public enum SessionRoute { Subject, Punishment, RetrySubject, Map, GameOver, Boss }
public enum SubjectId { Sprint, Endurance, Volleyball, Basketball, PingPong, Badminton, Football }

public SessionRoute StartSubject(SubjectId id) {
  if (lives <= 0) return SessionRoute.GameOver;
  active = id; visitAttempt = 1; awaitingPunishment = false; return SessionRoute.Subject;
}
public SessionRoute CompletePunishment() {
  if (!awaitingPunishment || active is null) throw new InvalidOperationException("No punishment is active.");
  awaitingPunishment = false; return SessionRoute.RetrySubject;
}

public SessionRoute SubmitResult(SubjectId id, MinigameResult result) {
  RequireActive(id);
  if (result.Pass) { records[id].Accept(result); active = null; return SessionRoute.Map; }
  if (visitAttempt == 1) { visitAttempt = 2; awaitingPunishment = true; return SessionRoute.Punishment; }
  lives--; records[id].RecordFailedVisit(); active = null; visitAttempt = 1;
  return lives == 0 ? SessionRoute.GameOver : SessionRoute.Map;
}
void RequireActive(SubjectId id) {
  if (active != id) throw new InvalidOperationException($"Subject {id} is not active.");
  if (awaitingPunishment) throw new InvalidOperationException("Complete punishment before submitting attempt two.");
}
public bool BossUnlocked => Enum.GetValues(typeof(SubjectId)).Cast<SubjectId>().All(id => records[id].Passed);
```

`SubjectRecord.Accept` sets `Passed = true` and replaces `BestScore/Rank` only when the new score is higher. `SubmitResult` ignores the score/rank fields whenever `Pass` is false.

- [ ] **Step 4: Run session tests and verify GREEN**

Run Step 2 again. Expected: all transition cases pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Progression Assets/Tests/EditMode/Progression && rtk git commit -m "feat: add five-life two-attempt progression"`

### Task 2: Punishment challenge sequence

**Files:**
- Create: `Assets/_Project/Scripts/Progression/ChallengeMechanic.cs`
- Create: `Assets/_Project/Scripts/Progression/ChallengeSequence.cs`
- Create: `Assets/_Project/Scripts/Progression/PunishmentController.cs`
- Test: `Assets/Tests/EditMode/Progression/ChallengeSequenceTests.cs`

**Interfaces:**
- Consumes: authored list of `(mechanic, duration, target)` and detector progress.
- Produces: current challenge and completion event; outcome never directly modifies pass/lives.

- [ ] **Step 1: Write failing ordered-sequence test**

```csharp
[Test] public void Advance_UsesAuthoredOrderAndCompletesOnce() {
  var sequence = new ChallengeSequence(new[] {
    new ChallengeStep(ChallengeMechanic.TapMash, 5, 20),
    new ChallengeStep(ChallengeMechanic.RhythmHold, 6, 8) });
  Assert.That(sequence.Current.Mechanic, Is.EqualTo(ChallengeMechanic.TapMash));
  sequence.ReportProgress(20); Assert.That(sequence.Current.Mechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));
  sequence.ReportProgress(8); Assert.That(sequence.IsComplete, Is.True);
}
```

- [ ] **Step 2: Run RED; implement deterministic sequence; run GREEN**

```csharp
public void ReportProgress(float value) {
  if (IsComplete || value < Current.Target) return;
  index++; IsComplete = index >= steps.Length;
  if (!IsComplete) Current = steps[index];
}
public ChallengeSequence(ChallengeStep[] steps) {
  if (steps == null || steps.Length == 0) throw new ArgumentException("At least one challenge step is required.");
  this.steps = steps; Current = steps[0];
}
public static ChallengeSequence BossDefault() => new(new[] {
  new ChallengeStep(ChallengeMechanic.TapMash, 10, 40),
  new ChallengeStep(ChallengeMechanic.RhythmHold, 12, 16),
  new ChallengeStep(ChallengeMechanic.AlternateTap, 10, 32)
});
public void Reset() { index = 0; IsComplete = false; Current = steps[0]; }
```

Run `ChallengeSequenceTests`; expected authored order and single completion pass.

- [ ] **Step 3: Bind detector adapters**

`PunishmentController` activates exactly one adapter based on `Current.Mechanic`: `TapMashDetector`, combined `RhythmBeatDetector/HoldDetector`, or `AlternateTapDetector`. On completion it calls `GameSession.CompletePunishment()` once and routes to the same subject.

- [ ] **Step 4: Commit**

Run: `rtk git add Assets/_Project/Scripts/Progression Assets/Tests/EditMode/Progression && rtk git commit -m "feat: add authored punishment sequences"`

### Task 3: Boss phase controller

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Boss/BossPhaseController.cs`
- Create: `Assets/_Project/ScriptableObjects/Boss/BossSequence.asset`
- Create: `Assets/_Project/Scenes/MG_Boss.unity`
- Test: `Assets/Tests/PlayMode/Progression/BossPhaseControllerTests.cs`

**Interfaces:**
- Consumes: unlocked session and three-step `ChallengeSequence`.
- Produces: boss pass/fail result after 30–40 seconds.

- [ ] **Step 1: Write failing locked and phase-order tests**

```csharp
[UnityTest] public IEnumerator LockedSession_CannotStartBoss() {
  var boss = CreateBoss(new GameSession());
  Assert.Throws<InvalidOperationException>(() => boss.Begin()); yield return null;
}
[UnityTest] public IEnumerator BossRunsTapRhythmAlternateInOrder() {
  var boss = CreateBoss(UnlockedSession()); boss.Begin();
  Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.TapMash));
  boss.CompleteCurrent(); Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.RhythmHold));
  boss.CompleteCurrent(); Assert.That(boss.CurrentMechanic, Is.EqualTo(ChallengeMechanic.AlternateTap));
  yield return null;
}
static GameSession UnlockedSession() {
  var session = new GameSession();
  foreach (SubjectId id in Enum.GetValues(typeof(SubjectId))) {
    session.StartSubject(id); session.SubmitResult(id, new MinigameResult(true, 6, Rank.C));
  }
  return session;
}
static BossPhaseController CreateBoss(GameSession session) {
  var value = new GameObject("Boss").AddComponent<BossPhaseController>();
  value.Configure(session, ChallengeSequence.BossDefault(), 35f); return value;
}
```

- [ ] **Step 2: Run focused PlayMode test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter BossPhaseControllerTests -testResults TestResults-boss-red.xml -quit`

- [ ] **Step 3: Implement boss gate and timeout**

```csharp
public void Begin() {
  if (!session.BossUnlocked) throw new InvalidOperationException("Pass all seven subjects before starting the boss.");
  remainingSeconds = configuredDuration; sequence.Reset(); running = true;
}
public void Configure(GameSession session, ChallengeSequence sequence, float duration) {
  this.session = session; this.sequence = sequence; configuredDuration = duration;
}
void Update() {
  if (!running) return; remainingSeconds -= Time.deltaTime;
  if (remainingSeconds <= 0) Resolve(false);
  else if (sequence.IsComplete) Resolve(true);
}
```

- [ ] **Step 4: Build Boss scene and run GREEN**

Create one controller, instructor animator, phase HUD, timer, cue presenter, and three detector adapters. Configure `BossSequence.asset` in TapMash → RhythmHold → AlternateTap order with total duration `35s`. Run Step 2; expected zero failures.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Boss Assets/_Project/ScriptableObjects/Boss Assets/_Project/Scenes/MG_Boss.unity Assets/Tests/PlayMode/Progression && rtk git commit -m "feat: add final boss phase sequence"`

### Task 4: End-to-end gameplay flow

**Files:**
- Create: `Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`

**Interfaces:**
- Consumes: completion events from all minigame controllers.
- Produces: map/punishment/retry/game-over/boss scene routes without duplicate transitions.

- [ ] **Step 1: Write the complete flow test**

```csharp
[UnityTest] public IEnumerator FullFlow_UsesAttemptsLivesAndBossUnlock() {
  var h = GameplayFlowHarness.Create();
  h.Start(SubjectId.Sprint); h.Fail(); Assert.That(h.Route, Is.EqualTo(SessionRoute.Punishment));
  h.CompletePunishment(); Assert.That(h.Route, Is.EqualTo(SessionRoute.RetrySubject));
  h.Fail(); Assert.That(h.Session.Lives, Is.EqualTo(4));
  foreach (SubjectId id in Enum.GetValues(typeof(SubjectId))) { h.Start(id); h.Pass(score: 6); }
  Assert.That(h.Session.BossUnlocked, Is.True); h.StartBoss(); h.CompleteBoss();
  Assert.That(h.Route, Is.EqualTo(SessionRoute.Map)); yield return null;
}

sealed class GameplayFlowHarness {
  public GameSession Session { get; } = new(); public SessionRoute Route { get; private set; }
  SubjectId active;
  public static GameplayFlowHarness Create() => new();
  public void Start(SubjectId id) { active = id; Route = Session.StartSubject(id); }
  public void Fail() => Route = Session.SubmitResult(active, new MinigameResult(false, 0, Rank.F));
  public void Pass(float score) => Route = Session.SubmitResult(active, new MinigameResult(true, score, ScoreUtil.ToRank(score)));
  public void CompletePunishment() => Route = Session.CompletePunishment();
  public void StartBoss() { if (!Session.BossUnlocked) throw new InvalidOperationException(); Route = SessionRoute.Boss; }
  public void CompleteBoss() => Route = SessionRoute.Map;
}
```

- [ ] **Step 2: Run full flow and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter FullGameplayFlowTests -testResults TestResults-full-flow-red.xml -quit`

- [ ] **Step 3: Implement route mapping with a single transition guard**

```csharp
public void Route(SessionRoute route, SubjectId? subject = null) {
  if (transitioning) return; transitioning = true;
  string scene = route switch { SessionRoute.Punishment => "Punishment", SessionRoute.RetrySubject => SceneFor(subject.Value),
    SessionRoute.GameOver => "Menu", SessionRoute.Boss => "MG_Boss", _ => "Menu" };
  StartCoroutine(LoadGameplayScene(scene, () => transitioning = false));
}
```

- [ ] **Step 4: Run all gameplay tests**

Run EditMode and PlayMode test suites. Expected: zero failures; every returned score is within `0..10`; no failed result has a nonzero score; exactly seven subject records unlock the boss.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets && rtk git commit -m "feat: integrate complete KMA gameplay progression"`
