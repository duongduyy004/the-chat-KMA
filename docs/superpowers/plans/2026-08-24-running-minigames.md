# Running Minigames Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver playable Sprint and Endurance minigames with deterministic challenges, non-overlapping input modes, one primary objective each, and normalized scores.

**Architecture:** `SprintRules` and `EnduranceRules` own all simulation state. Controllers translate touch/audio events into rule calls and render snapshots; authored pace/lap patterns are ScriptableObjects selected before countdown.

**Tech Stack:** Unity 6.3 LTS, C#, Input System EnhancedTouch, AudioSettings.dspTime, NUnit EditMode/PlayMode tests.

**Spec:** `PLAN.md` M1 and M2 plus section 2.5.

## Global Constraints

- Sprint passes only by reaching 100m within its time limit.
- Endurance passes only by completing all laps before timeout and stamina depletion.
- `UnityEngine.Random` is forbidden in both rule engines.
- During Endurance exactly one of `RhythmTap`, `BreathHold`, or `ObstacleSwipe` accepts input.

---

### Task 1: Sprint deterministic simulation

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/RivalPaceProfile.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Running/SprintRulesTests.cs`

**Interfaces:**
- Consumes: `Tap(Side)`, `Tick(float)`, fixed `RivalPaceProfile[]`.
- Produces: distance, speed, stamina, valid-tap ratio, rank, and `BuildResult()`.

- [ ] **Step 1: Write failing objective, alternation, and determinism tests**

```csharp
[Test] public void SameSideTap_GivesFortyPercentImpulse() {
  var rules = SprintRules.Default(); rules.Tap(Side.Left); float first = rules.Speed;
  rules.Tap(Side.Left); Assert.That(rules.Speed - first, Is.EqualTo(7.2f).Within(.001));
}
[Test] public void TopTwoAfterTimeout_DoesNotPass() {
  var rules = SprintRules.ForTest(distance: 100, elapsed: 14.1f, rank: 1);
  Assert.That(rules.BuildResult().Pass, Is.False);
}
[Test] public void EqualInputs_ProduceEqualSnapshots() {
  var a = SprintRules.Default(); var b = SprintRules.Default();
  foreach (var side in new[]{Side.Left, Side.Right, Side.Left}) { a.Tap(side); b.Tap(side); a.Tick(.1f); b.Tick(.1f); }
  Assert.That(a.Snapshot, Is.EqualTo(b.Snapshot));
}
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter SprintRulesTests -testResults TestResults-sprint-red.xml -quit`

Expected: FAIL with missing Sprint types.

- [ ] **Step 3: Implement the minimal rule engine**

```csharp
public enum Side { Left, Right }
public readonly record struct SprintSnapshot(float Distance, float Speed, float Stamina, float Elapsed);

public sealed class SprintRules {
  const float FullImpulse = 18f, SpeedCap = 120f, FinishDistance = 100f;
  readonly float timeLimit; Side? expected; int valid, total, currentRank; float distance, speed, stamina = 100, elapsed;
  public SprintRules(float timeLimit = 14f) { this.timeLimit = timeLimit; }
  public static SprintRules Default() => new(14f);
  public static SprintRules ForTest(float distance, float elapsed, int rank) {
    var value = new SprintRules(14f); value.distance = distance; value.elapsed = elapsed; value.currentRank = rank; return value;
  }
  public float Speed => speed; public SprintSnapshot Snapshot => new(distance, speed, stamina, elapsed);
  public void Tap(Side side) {
    bool correct = expected is null || side == expected; total++; if (correct) valid++;
    speed = UnityEngine.Mathf.Min(SpeedCap, speed + FullImpulse * (correct ? 1f : .4f));
    expected = side == Side.Left ? Side.Right : Side.Left;
  }
  public void Tick(float dt) {
    elapsed += dt; speed = UnityEngine.Mathf.Max(0, speed - 15f * dt);
    distance += speed * dt * .08f; stamina = UnityEngine.Mathf.Clamp(stamina + (speed > 20 ? -speed * .25f : 6f) * dt, 0, 100);
  }
  public MinigameResult BuildResult() {
    bool pass = distance >= FinishDistance && elapsed <= timeLimit;
    float accuracy = total == 0 ? 0 : 2f * valid / total;
    float efficiency = UnityEngine.Mathf.Clamp01(stamina / 100f);
    float mastery = UnityEngine.Mathf.Clamp01((timeLimit - elapsed) / 3f);
    return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
  }
}
```

- [ ] **Step 4: Run Sprint tests and verify GREEN**

Run the Step 2 command again. Expected: all Sprint rule tests pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Sprint Assets/Tests/EditMode/Gameplay/Running/SprintRulesTests.cs && rtk git commit -m "feat: add deterministic sprint rules"`

### Task 2: Sprint controller and telegraphed challenge

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintChallengePattern.cs`
- Create: `Assets/_Project/Scenes/MG_Sprint.unity`
- Test: `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs`

**Interfaces:**
- Consumes: left/right Input Actions and authored checkpoint cues.
- Produces: one `MinigameResult` event and visual snapshots for HUD/runners.

- [ ] **Step 1: Write a PlayMode test proving cue precedes challenge**

```csharp
[UnityTest] public IEnumerator WindCue_PrecedesNarrowWindowByPointEightSeconds() {
  var controller = CreateSprintController(cueLeadSeconds: .8f);
  controller.AdvanceToDistance(29.9f); controller.Simulate(.1f);
  Assert.That(controller.WindCueVisible, Is.True);
  Assert.That(controller.WindWindowActive, Is.False);
  controller.Simulate(.8f); Assert.That(controller.WindWindowActive, Is.True);
  yield return null;
}
```

- [ ] **Step 2: Run focused PlayMode test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter SprintControllerTests -testResults TestResults-sprint-controller-red.xml -quit`

Expected: FAIL because controller and factory do not exist.

- [ ] **Step 3: Implement controller input forwarding**

```csharp
public sealed class SprintController : MinigameBase {
  SprintRules rules; float cueAt, activeAt, challengeElapsed;
  public bool WindCueVisible { get; private set; } public bool WindWindowActive { get; private set; }
  public void ConfigureForTest(float cueLeadSeconds) { rules = SprintRules.ForTest(0, 0, 1); cueAt = 30f; activeAt = 30f; challengeElapsed = -cueLeadSeconds; }
  public void AdvanceToDistance(float value) => rules = SprintRules.ForTest(value, 0, 1);
  public void Simulate(float dt) { rules.Tick(dt); UpdateAuthoredChallenges(dt); }
  public void OnLeftTap() => rules.Tap(Side.Left);
  public void OnRightTap() => rules.Tap(Side.Right);
  protected override void TickPlay(float dt) { rules.Tick(dt); UpdateAuthoredChallenges(dt); if (rules.Snapshot.Stamina <= 0) Finish(rules.BuildResult()); }
  void UpdateAuthoredChallenges(float dt) {
    if (!WindCueVisible && rules.Snapshot.Distance >= cueAt) { WindCueVisible = true; challengeElapsed = 0; }
    if (WindCueVisible && !WindWindowActive) { challengeElapsed += dt; WindWindowActive = challengeElapsed >= .8f && rules.Snapshot.Distance >= activeAt; }
  }
}
```

Test helper implementation:

```csharp
static SprintController CreateSprintController(float cueLeadSeconds) {
  var value = new GameObject("SprintController").AddComponent<SprintController>();
  value.ConfigureForTest(cueLeadSeconds); return value;
}
```

- [ ] **Step 4: Build `MG_Sprint` hierarchy and rerun test**

Create `Main Camera`, `SprintController`, four runner prefabs, `HUD/Timer`, `HUD/Stamina`, `HUD/Rank`, `Input/LeftTap`, `Input/RightTap`, and `FX/WindCue`. Bind both input buttons once. Expected: focused PlayMode suite passes.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Sprint Assets/_Project/Scenes/MG_Sprint.unity Assets/Tests/PlayMode/Gameplay/Running && rtk git commit -m "feat: add playable sprint controller"`

### Task 3: Endurance exclusive input modes

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/LapPattern.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Running/EnduranceRulesTests.cs`

**Interfaces:**
- Consumes: authored beat events and `Tap`, `BeginHold`, `EndHold`, `Swipe` commands.
- Produces: current `EnduranceInputMode`, stamina, lap progress, judge counts, and result.

- [ ] **Step 1: Write failing exclusivity and pass tests**

```csharp
[Test] public void ObstacleBeat_DisablesRhythmMissAndAcceptsOnlySwipe() {
  var rules = EnduranceRules.AtObstacleBeat();
  Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
  rules.Swipe(SwipeDirection.Up);
  Assert.Multiple(() => { Assert.That(rules.MissCount, Is.Zero); Assert.That(rules.ObstacleCleared, Is.True); });
}
[Test] public void ComboCannotPassWithoutCompletingLaps() {
  var rules = EnduranceRules.ForTest(laps: 2, requiredLaps: 3, combo: 999, stamina: 100);
  Assert.That(rules.BuildResult().Pass, Is.False);
}
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter EnduranceRulesTests -testResults TestResults-endurance-red.xml -quit`

Expected: FAIL with missing Endurance types.

- [ ] **Step 3: Implement one-active-mode dispatch**

```csharp
public enum EnduranceInputMode { RhythmTap, BreathHold, ObstacleSwipe }
public enum BeatEvent { Tap, Breath, Jump, Slide }
public enum SwipeDirection { Up, Down }

public sealed class EnduranceRules {
  readonly int requiredLaps; int laps, combo, perfect, good, judged; float stamina = 100, elapsed, timeLimit = 90;
  BeatEvent currentBeat;
  public int MissCount { get; private set; } public bool ObstacleCleared { get; private set; }
  public EnduranceInputMode Mode { get; private set; }
  public EnduranceRules(int requiredLaps = 3) { this.requiredLaps = requiredLaps; }
  public static EnduranceRules AtObstacleBeat() { var value = new EnduranceRules(); value.EnterBeat(BeatEvent.Jump); return value; }
  public static EnduranceRules ForTest(int laps, int requiredLaps, int combo, float stamina) {
    var value = new EnduranceRules(requiredLaps); value.laps = laps; value.combo = combo; value.stamina = stamina; return value;
  }
  public void EnterBeat(BeatEvent beat) {
    currentBeat = beat;
    Mode = beat switch {
      BeatEvent.Breath => EnduranceInputMode.BreathHold,
      BeatEvent.Jump or BeatEvent.Slide => EnduranceInputMode.ObstacleSwipe,
      _ => EnduranceInputMode.RhythmTap
    };
  }
  public void Tap(double inputDsp, double beatDsp) {
    if (Mode != EnduranceInputMode.RhythmTap) return;
    judged++; var result = new RhythmBeatEvaluator(80, 160).Judge(inputDsp, beatDsp);
    if (result == TimingJudge.Perfect) { perfect++; combo++; }
    else if (result == TimingJudge.Good) { good++; combo++; stamina = Mathf.Max(0, stamina - 2); }
    else { MissCount++; combo = 0; stamina = Mathf.Max(0, stamina - 8); }
  }
  public void EndHold(float beatsHeld) { if (Mode != EnduranceInputMode.BreathHold) return; stamina = UnityEngine.Mathf.Min(100, stamina + 12 * UnityEngine.Mathf.Clamp01(beatsHeld)); }
  public void Swipe(SwipeDirection direction) {
    if (Mode != EnduranceInputMode.ObstacleSwipe) return;
    bool expected = (currentBeat == BeatEvent.Jump && direction == SwipeDirection.Up) || (currentBeat == BeatEvent.Slide && direction == SwipeDirection.Down);
    ObstacleCleared = expected; if (!expected) stamina = Mathf.Max(0, stamina - 15);
  }
  public void Tick(float dt) { elapsed += dt; }
  public MinigameResult BuildResult() {
    bool pass = laps >= requiredLaps && stamina > 0 && elapsed <= timeLimit;
    float accuracy = judged == 0 ? 0 : 2f * (perfect + .5f * good) / judged;
    return ScoreUtil.Build(pass, accuracy, stamina / 100f, Mathf.Clamp01(combo / 32f));
  }
}
```

Add `public void CompleteLap() => laps++;`; call it only when the authored lap-end event is dispatched.

- [ ] **Step 4: Run Endurance tests and verify GREEN**

Run the Step 2 command again. Expected: exclusive-mode and objective tests pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Endurance Assets/Tests/EditMode/Gameplay/Running/EnduranceRulesTests.cs && rtk git commit -m "feat: add phased endurance rules"`

### Task 4: Endurance DSP controller and scene smoke test

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceController.cs`
- Create: `Assets/_Project/Scenes/MG_Endurance.unity`
- Test: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceControllerTests.cs`

**Interfaces:**
- Consumes: `AudioSettings.dspTime`, calibrated offset, `LapPattern`, touch commands.
- Produces: cue at least two beats early and a single result.

- [ ] **Step 1: Write failing two-beat warning test**

```csharp
[UnityTest] public IEnumerator ObstacleIcon_AppearsTwoBeatsBeforeSwipeMode() {
  var c = new EnduranceCueSchedule(obstacleBeat: 8, warningBeats: 2);
  c.AdvanceToBeat(6); Assert.That(c.ObstacleCueVisible, Is.True);
  c.AdvanceToBeat(8); Assert.That(c.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
  yield return null;
}
```

- [ ] **Step 2: Run PlayMode test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter EnduranceControllerTests -testResults TestResults-endurance-controller-red.xml -quit`

- [ ] **Step 3: Implement DSP scheduling and calibrated input timestamp**

```csharp
double CalibratedInputTime(double rawDspTime) => rawDspTime + rhythmOffsetMs / 1000d;
void DispatchBeat(int index) {
  if (pattern.HasWarningAt(index)) obstacleCue.SetActive(true);
  rules.EnterBeat(pattern.EventAt(index));
}

public sealed class EnduranceCueSchedule {
  readonly int obstacleBeat, warningBeats; public bool ObstacleCueVisible { get; private set; }
  public EnduranceInputMode Mode { get; private set; } = EnduranceInputMode.RhythmTap;
  public EnduranceCueSchedule(int obstacleBeat, int warningBeats) { this.obstacleBeat = obstacleBeat; this.warningBeats = warningBeats; }
  public void AdvanceToBeat(int beat) { ObstacleCueVisible = beat >= obstacleBeat - warningBeats; if (beat >= obstacleBeat) Mode = EnduranceInputMode.ObstacleSwipe; }
}
```

- [ ] **Step 4: Build scene and run all Running tests**

Create `EnduranceController`, audio source/metronome, runner/parallax, `HUD/Beat`, `HUD/Stamina`, `HUD/Lap`, `HUD/ObstacleCue`, and one full-screen gameplay input area. Run both EditMode and PlayMode filters `KMA.Tests.Running`; expected: zero failures.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Endurance Assets/_Project/Scenes/MG_Endurance.unity Assets/Tests && rtk git commit -m "feat: add playable endurance rhythm flow"`
