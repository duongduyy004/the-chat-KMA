# Ball Minigames Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a shared projectile presentation boundary and five distinct, deterministic ball-sport rule engines.

**Architecture:** `BallRig` renders position, shadow, and trajectory but does not decide sport rules. Each sport owns a small rule state machine and asks a motion profile to launch an authored trajectory; tests exercise rule state without Rigidbody simulation wherever possible.

**Tech Stack:** Unity 6.3 LTS, C#, Physics2D, Input System, NUnit EditMode tests, PlayMode physics smoke tests.

**Spec:** `PLAN.md` sections 2.3b and M3–M7.

## Global Constraints

- Volleyball: gesture selection plus context timing; first to five rally points.
- Basketball: pass, authored alley-oop, apex tap; five baskets in 30 seconds.
- PingPong: tap timing; first to five points; capped speed.
- Badminton: hold/release mapped by contact height; first to five points.
- Football: five kicks, pass at three goals.
- AI variation uses authored patterns and visible cues; no per-action random failure.

---

### Task 1: BallRig presentation boundary

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Ball/BallRig.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Ball/FlightProfile.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Ball/BallFlightSnapshot.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/BallRigTests.cs`

**Interfaces:**
- Consumes: `Launch(Vector2 direction, float force, float curvature)` and `AttachTo(Transform)`.
- Produces: `IsNearApex(float)`, `PredictLandingPoint()`, collision event, and `BallFlightSnapshot`.

- [ ] **Step 1: Write failing attach/apex tests**

```csharp
[UnityTest] public IEnumerator AttachTo_MakesBodyKinematicAndTracksHand() {
  var hand = new GameObject("Hand").transform; hand.position = new Vector3(2, 3);
  var rig = BallTestFactory.Create(); rig.AttachTo(hand); yield return new WaitForFixedUpdate();
  Assert.Multiple(() => { Assert.That(rig.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic)); Assert.That(rig.transform.position, Is.EqualTo(hand.position)); });
}
[Test] public void IsNearApex_UsesAbsoluteVerticalVelocity() {
  var rig = BallTestFactory.Create(); rig.Body.velocity = new Vector2(4, -.09f);
  Assert.That(rig.IsNearApex(.1f), Is.True);
}
```

- [ ] **Step 2: Run focused test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter BallRigTests -testResults TestResults-ballrig-red.xml -quit`

- [ ] **Step 3: Implement BallRig without sport rules**

```csharp
public sealed class BallRig : MonoBehaviour {
  [SerializeField] Rigidbody2D body; [SerializeField] FlightProfile profile; Transform attachment; float currentCurvature;
  public Rigidbody2D Body => body;
  void Awake() { if (!body) body = GetComponent<Rigidbody2D>(); }
  public void AttachTo(Transform target) { attachment = target; body.velocity = Vector2.zero; body.bodyType = RigidbodyType2D.Kinematic; }
  public void Launch(Vector2 direction, float force, float curvature) {
    attachment = null; body.bodyType = RigidbodyType2D.Dynamic; body.gravityScale = profile.GravityScale;
    body.drag = profile.LinearDrag; body.velocity = direction.normalized * force; currentCurvature = curvature;
  }
  void FixedUpdate() { if (attachment) { body.position = attachment.position; return; } body.AddForce(Vector2.Perpendicular(body.velocity.normalized) * currentCurvature); }
  public bool IsNearApex(float threshold) => Mathf.Abs(body.velocity.y) < threshold;
  public Vector2 PredictLandingPoint() => Ballistics.PredictGround(body.position, body.velocity, Physics2D.gravity.y * body.gravityScale, profile.GroundY);
}

static class BallTestFactory {
  public static BallRig Create() {
    var go = new GameObject("BallRigTest"); go.AddComponent<Rigidbody2D>(); return go.AddComponent<BallRig>();
  }
}
```

- [ ] **Step 4: Implement analytic landing helper and rerun tests**

```csharp
public static class Ballistics {
public static Vector2 PredictGround(Vector2 p, Vector2 v, float gravity, float groundY) {
  if (Mathf.Approximately(gravity, 0)) return p;
  float c = p.y - groundY, discriminant = v.y * v.y - 2f * gravity * c;
  if (discriminant < 0) return p;
  float root = Mathf.Sqrt(discriminant);
  float t1 = (-v.y + root) / gravity, t2 = (-v.y - root) / gravity;
  float t = Mathf.Max(t1, t2); if (t < 0) return p;
  return new Vector2(p.x + v.x * t, groundY);
}
}
```

Add tests for negative discriminant and zero gravity returning the current position. Expected: all `BallRigTests` pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Ball Assets/Tests/PlayMode/Gameplay/Ball && rtk git commit -m "feat: add shared ball presentation rig"`

### Task 2: Volleyball contextual actions

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyReturnPattern.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Ball/VolleyballRulesTests.cs`

**Interfaces:**
- Consumes: ball context plus gesture direction and timing accuracy.
- Produces: Dig/Set/Spike action, touch count, rally score, and result.

- [ ] **Step 1: Write failing contextual-action and objective tests**

```csharp
[TestCase(BallContext.Low, 0, 1, VolleyAction.Dig)]
[TestCase(BallContext.Rising, 0, 1, VolleyAction.Set)]
[TestCase(BallContext.ApexNearNet, 1, -1, VolleyAction.Spike)]
public void ResolveGesture_UsesContext(BallContext context, float x, float y, VolleyAction expected) =>
  Assert.That(VolleyballRules.ResolveGesture(context, new Vector2(x, y)), Is.EqualTo(expected));

[Test] public void ComboWithoutFivePoints_DoesNotPass() {
  var rules = new VolleyballRules(targetScore: 5); rules.SetForTest(playerScore: 4, opponentScore: 0, combo: 100);
  Assert.That(rules.BuildResult().Pass, Is.False);
}
```

- [ ] **Step 2: Run tests and verify RED**

Run the ball EditMode command with `-testFilter VolleyballRulesTests`; expected missing types.

- [ ] **Step 3: Implement contextual resolution and score**

```csharp
public static VolleyAction ResolveGesture(BallContext c, Vector2 swipe) => c switch {
  BallContext.Low => VolleyAction.Dig,
  BallContext.Rising => VolleyAction.Set,
  BallContext.ApexNearNet when swipe.x > 0 && swipe.y < 0 => VolleyAction.Spike,
  _ => VolleyAction.Invalid
};
public void SetForTest(int playerScore, int opponentScore, int combo) {
  this.playerScore = playerScore; this.opponentScore = opponentScore; longestCombo = combo;
}
public MinigameResult BuildResult() {
  bool pass = playerScore >= targetScore && playerScore > opponentScore;
  return ScoreUtil.Build(pass, 2f * accurateTouches / Mathf.Max(1, totalTouches),
    1f - opponentScore / (float)targetScore, Mathf.Clamp01(longestCombo / 10f));
}
```

- [ ] **Step 4: Add authored spin cue assertion and run GREEN**

Add a test asserting `VolleyReturnPattern.CueLeadSeconds >= .5f` and that trajectory selection occurs before launch. Run `VolleyballRulesTests`; expected zero failures.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Volleyball Assets/Tests/EditMode/Gameplay/Ball/VolleyballRulesTests.cs && rtk git commit -m "feat: add contextual volleyball rules"`

### Task 3: Basketball alley-oop state machine

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/BasketballRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/AlleyOopPattern.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Ball/BasketballRulesTests.cs`

**Interfaces:**
- Consumes: pass vector, authored toss launch, ball height/velocity, finish tap time.
- Produces: `Holding`, `Passing`, `AlleyOopFlight`, `Resolved`; EARLY/PERFECT/LATE feedback; basket count; result.

- [ ] **Step 1: Write failing state and apex tests**

```csharp
[Test] public void TapBeforeAlleyOop_IsIgnored() {
  var r = new BasketballRules(5, 30); r.TapFinish(ballY: 3, velocityY: 0);
  Assert.That(r.State, Is.EqualTo(BasketballState.Holding));
}
[TestCase(3f, .05f, FinishJudge.Perfect)] [TestCase(2f, 2f, FinishJudge.Early)] [TestCase(2f, -2f, FinishJudge.Late)]
public void AlleyOopTap_JudgesApex(float y, float vy, FinishJudge expected) {
  var r = BasketballRules.InFlight(apexMin: 2.8f, apexMax: 3.2f, velocityThreshold: .1f);
  Assert.That(r.TapFinish(y, vy), Is.EqualTo(expected));
}
[Test] public void EightComboWithoutFiveBaskets_DoesNotPass() =>
  Assert.That(BasketballRules.ForTest(4, 8, 20).BuildResult().Pass, Is.False);
```

- [ ] **Step 2: Run tests and verify RED**

Run the ball EditMode command with `-testFilter BasketballRulesTests`.

- [ ] **Step 3: Implement explicit possession states**

```csharp
public enum BasketballState { Holding, Passing, AlleyOopFlight, Resolved }
public static BasketballRules InFlight(float apexMin, float apexMax, float velocityThreshold) {
  var value = new BasketballRules(5, 30); value.apexMin = apexMin; value.apexMax = apexMax;
  value.velocityThreshold = velocityThreshold; value.State = BasketballState.AlleyOopFlight; return value;
}
public static BasketballRules ForTest(int baskets, int combo, float elapsed) {
  var value = new BasketballRules(5, 30); value.baskets = baskets; value.combo = combo; value.elapsed = elapsed; return value;
}
public FinishJudge TapFinish(float y, float vy) {
  if (State != BasketballState.AlleyOopFlight) return FinishJudge.Ignored;
  FinishJudge judge = y < apexMin || vy > velocityThreshold ? FinishJudge.Early :
    y > apexMax || vy < -velocityThreshold ? FinishJudge.Late : FinishJudge.Perfect;
  attempts++; if (judge == FinishJudge.Perfect) { baskets++; combo++; perfects++; } else combo = 0;
  State = baskets >= targetBaskets || elapsed >= timeLimit ? BasketballState.Resolved : BasketballState.Holding;
  return judge;
}
public MinigameResult BuildResult() => ScoreUtil.Build(baskets >= targetBaskets,
  2f * perfects / Mathf.Max(1, attempts), Mathf.Clamp01(targetBaskets / (float)Mathf.Max(1, attempts)), Mathf.Clamp01(bestCombo / 5f));
```

- [ ] **Step 4: Run tests and verify GREEN**

Expected: state, apex, and no-combo-shortcut tests pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Basketball Assets/Tests/EditMode/Gameplay/Ball/BasketballRulesTests.cs && rtk git commit -m "feat: add basketball alley-oop rules"`

### Task 4: PingPong capped rally rules

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PingPong/PingPongRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/PingPong/ReturnPattern.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Ball/PingPongRulesTests.cs`

**Interfaces:**
- Consumes: timing accuracy and authored placement threshold.
- Produces: capped ball speed, points, rally length, and result.

- [ ] **Step 1: Write failing cap and pass tests**

```csharp
[Test] public void SpeedNeverExceedsConfiguredCap() {
  var r = new PingPongRules(10, 25); for (int i=0;i<100;i++) r.SuccessfulReturn(1);
  Assert.That(r.BallSpeed, Is.EqualTo(25));
}
[Test] public void LongRallyWithoutFivePoints_DoesNotPass() {
  var r = PingPongRules.ForTest(4, 0, 100); Assert.That(r.BuildResult().Pass, Is.False);
}
```

- [ ] **Step 2: Run RED; implement; run GREEN**

```csharp
public void SuccessfulReturn(float accuracy) { rally++; totalAccuracy += accuracy; returns++; ballSpeed = Mathf.Min(maxSpeed, ballSpeed * 1.08f); }
public static PingPongRules ForTest(int playerPoints, int opponentPoints, int rally) {
  var value = new PingPongRules(10, 25); value.playerPoints = playerPoints; value.opponentPoints = opponentPoints;
  value.longestRally = rally; return value;
}
public MinigameResult BuildResult() => ScoreUtil.Build(playerPoints >= 5 && playerPoints > opponentPoints,
  2f * totalAccuracy / Mathf.Max(1, returns), Mathf.Clamp01((playerPoints-opponentPoints)/5f), Mathf.Clamp01(longestRally/20f));
```

Run `PingPongRulesTests`; expected both cases pass.

- [ ] **Step 3: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/PingPong Assets/Tests/EditMode/Gameplay/Ball/PingPongRulesTests.cs && rtk git commit -m "feat: add capped ping pong rally rules"`

### Task 5: Badminton charge-height rules

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Badminton/BadmintonRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Badminton/RallyPattern.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Ball/BadmintonRulesTests.cs`

**Interfaces:**
- Consumes: hold duration, normalized contact height, authored wind cue.
- Produces: Lift/Drive/Smash/Overcharge, points, variety, and result.

- [ ] **Step 1: Write failing shot classification tests**

```csharp
[TestCase(.8f, .2f, BadmintonShot.Lift)] [TestCase(.8f, .5f, BadmintonShot.Drive)]
[TestCase(.8f, .85f, BadmintonShot.Smash)] [TestCase(1.01f, .85f, BadmintonShot.Overcharge)]
public void Release_UsesChargeAndContactHeight(float charge, float height, BadmintonShot expected) =>
  Assert.That(new BadmintonRules().Release(charge, height), Is.EqualTo(expected));
[Test] public void RallyTargetWithoutFivePoints_DoesNotPass() =>
  Assert.That(BadmintonRules.ForTest(4, 0, 50).BuildResult().Pass, Is.False);
```

- [ ] **Step 2: Run RED; implement; run GREEN**

```csharp
public BadmintonShot Release(float charge, float height) {
  if (charge > 1f) return Record(BadmintonShot.Overcharge, false);
  var shot = height >= .7f ? BadmintonShot.Smash : height >= .35f ? BadmintonShot.Drive : BadmintonShot.Lift;
  return Record(shot, true);
}
BadmintonShot Record(BadmintonShot shot, bool accurate) {
  releases++; if (accurate) accurateReleases++; if (shot != BadmintonShot.Overcharge) usedShots.Add(shot); return shot;
}
public static BadmintonRules ForTest(int playerPoints, int opponentPoints, int rally) {
  var value = new BadmintonRules(); value.playerPoints = playerPoints; value.opponentPoints = opponentPoints;
  value.longestRally = rally; return value;
}
public MinigameResult BuildResult() => ScoreUtil.Build(playerPoints >= 5 && playerPoints > opponentPoints,
  2f * accurateReleases / Mathf.Max(1, releases), Mathf.Clamp01((playerPoints-opponentPoints)/5f), Mathf.Clamp01(distinctShots/3f));
```

Run `BadmintonRulesTests`; expected all classification and objective tests pass.

- [ ] **Step 3: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Badminton Assets/Tests/EditMode/Gameplay/Ball/BadmintonRulesTests.cs && rtk git commit -m "feat: add badminton charge height rules"`

### Task 6: Football five-kick rules

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Football/GKPatternSet.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Ball/FootballRulesTests.cs`

**Interfaces:**
- Consumes: shot placement/force/spin and one preselected goalkeeper pattern per kick.
- Produces: goal/miss, five-kick completion, modifier phase, and result.

- [ ] **Step 1: Write failing objective and single-modifier tests**

```csharp
[TestCase(2, false)] [TestCase(3, true)] [TestCase(5, true)]
public void FiveKicks_PassAtThreeGoals(int goals, bool pass) =>
  Assert.That(FootballRules.ForTest(kicks:5, goals:goals).BuildResult().Pass, Is.EqualTo(pass));
[Test] public void PhaseActivatesExactlyOneDifficultyModifier() {
  var phase = new FootballPhase(GKReaction.Fast, TargetWidth.Normal);
  Assert.That(phase.ActiveModifierCount, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run RED; implement; run GREEN**

```csharp
public void ResolveKick(bool goal, float placementAccuracy, ShotKind kind) {
  if (kicks >= 5) throw new InvalidOperationException("All five kicks are already resolved.");
  kicks++; if (goal) goals++; accuracyTotal += Mathf.Clamp01(placementAccuracy); shotKinds.Add(kind);
}
public static FootballRules ForTest(int kicks, int goals) {
  var value = new FootballRules(); value.kicks = kicks; value.goals = goals; return value;
}
public MinigameResult BuildResult() => ScoreUtil.Build(kicks == 5 && goals >= 3,
  2f * accuracyTotal / 5f, Mathf.Clamp01((goals-3)/2f), Mathf.Clamp01(shotKinds.Count/3f));
```

Run `FootballRulesTests`; expected all cases pass.

- [ ] **Step 3: Create five thin controllers/scenes and smoke-test completion events**

Create `MG_Volleyball`, `MG_Basketball`, `MG_PingPong`, `MG_Badminton`, `MG_Football`. Each scene contains exactly one sport controller, one `BallRig`, one gameplay camera, one audio listener, HUD, cue presenter, and input surface. Add one PlayMode test per scene asserting exactly one completion event and score within `0..10`.

- [ ] **Step 4: Run the complete ball suite**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Ball -testResults TestResults-ball.xml -quit`

Expected: zero failed tests for all five sports.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay Assets/_Project/Scenes Assets/Tests && rtk git commit -m "feat: complete five ball minigames"`
