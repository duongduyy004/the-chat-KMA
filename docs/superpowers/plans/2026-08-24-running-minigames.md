# Running Minigames Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Tech Stack:** Unity 6.3 LTS, C#, Input System EnhancedTouch, AudioSettings.dspTime, NUnit EditMode/PlayMode tests.

**Spec:** `PLAN.md` M1 and M2 plus section 2.5.

## Global Constraints

- Sprint passes only by reaching 100m within its time limit.
- `UnityEngine.Random` is forbidden in both rule engines.

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
