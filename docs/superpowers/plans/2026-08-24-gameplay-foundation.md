# Gameplay Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide deterministic result, scoring, timing, input-evaluation, and minigame lifecycle contracts used by all seven subjects.

**Architecture:** Keep rules in Unity-independent C# classes under `Gameplay/Common`; `MonoBehaviour` classes only forward frame time/input and render state. Tests construct rule objects directly without scenes or real touch devices.

**Tech Stack:** Unity 6.3 LTS, C#/.NET Standard 2.1, NUnit EditMode tests, Input System test fixtures.

**Spec:** `PLAN.md` sections 2.3, 2.4, 2.5, and 10.

## Global Constraints

- `MinigameResult.Score` is `0..10`, rounded to one decimal.
- Only `PrimaryObjective` determines `Pass`.
- Failing results are score `0`, rank `F`.
- No common rule class reads global time, global input, or random state.

---

### Task 1: Result and score contracts

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Common/MinigameResult.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Common/ScoreUtil.cs`
- Create: `Assets/Tests/EditMode/Gameplay/Common/ScoreUtilTests.cs`

**Interfaces:**
- Consumes: four score components from each subject.
- Produces: `ScoreUtil.Build(bool, float, float, float) -> MinigameResult` and `ScoreUtil.ToRank(float) -> Rank`.

- [ ] **Step 1: Write failing boundary and composition tests**

```csharp
[TestCase(9f, Rank.S)] [TestCase(8f, Rank.A)] [TestCase(7f, Rank.B)]
[TestCase(6f, Rank.C)] [TestCase(5f, Rank.D)] [TestCase(4.9f, Rank.F)]
public void ToRank_UsesTenPointBoundaries(float score, Rank expected) =>
    Assert.That(ScoreUtil.ToRank(score), Is.EqualTo(expected));

[Test]
public void Build_PassedResult_ComposesAndRounds() {
    var result = ScoreUtil.Build(true, 1.64f, .72f, .56f);
    Assert.Multiple(() => {
        Assert.That(result.Score, Is.EqualTo(8.9f));
        Assert.That(result.Rank, Is.EqualTo(Rank.A));
        Assert.That(result.Pass, Is.True);
    });
}

[Test]
public void Build_FailedResult_IgnoresBonuses() =>
    Assert.That(ScoreUtil.Build(false, 2, 1, 1).Score, Is.Zero);
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common.ScoreUtilTests -testResults TestResults-score-red.xml -quit`

Expected: FAIL because `ScoreUtil`, `Rank`, and `MinigameResult` do not exist.

- [ ] **Step 3: Implement the contracts**

```csharp
namespace KMA.Gameplay {
  public enum Rank { F, D, C, B, A, S }

  [System.Serializable]
  public sealed class MinigameResult {
    public bool Pass;
    public float Score;
    public Rank Rank;
    public MinigameResult(bool pass, float score, Rank rank) {
      Pass = pass; Score = score; Rank = rank;
    }
  }

  public static class ScoreUtil {
    public static Rank ToRank(float score) => score >= 9 ? Rank.S : score >= 8 ? Rank.A :
      score >= 7 ? Rank.B : score >= 6 ? Rank.C : score >= 5 ? Rank.D : Rank.F;

    public static MinigameResult Build(bool pass, float accuracy, float efficiency, float mastery) {
      if (!pass) return new MinigameResult(false, 0, Rank.F);
      float raw = 6f + UnityEngine.Mathf.Clamp(accuracy, 0, 2) +
        UnityEngine.Mathf.Clamp01(efficiency) + UnityEngine.Mathf.Clamp01(mastery);
      float rounded = UnityEngine.Mathf.Round(UnityEngine.Mathf.Clamp(raw, 0, 10) * 10f) / 10f;
      return new MinigameResult(true, rounded, ToRank(rounded));
    }
  }
}
```

- [ ] **Step 4: Run the focused test and verify GREEN**

Run the Step 2 command again. Expected: all `ScoreUtilTests` pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Common Assets/Tests/EditMode/Gameplay/Common && rtk git commit -m "feat: add normalized gameplay result contract"`

### Task 2: Timing evaluators

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Common/TimingJudge.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Common/TimingWindow.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Common/RhythmBeatEvaluator.cs`
- Create: `Assets/Tests/EditMode/Gameplay/Common/TimingEvaluatorTests.cs`

**Interfaces:**
- Consumes: measured timing delta in milliseconds.
- Produces: `TimingWindow.Evaluate(float) -> float` and `RhythmBeatEvaluator.Judge(double, double) -> TimingJudge`.

- [ ] **Step 1: Write failing exact-boundary tests**

```csharp
[TestCase(80, TimingJudge.Perfect)] [TestCase(-80, TimingJudge.Perfect)]
[TestCase(160, TimingJudge.Good)] [TestCase(-160, TimingJudge.Good)]
[TestCase(160.1, TimingJudge.Miss)]
public void RhythmJudge_UsesInclusiveWindows(double deltaMs, TimingJudge expected) {
    var judge = new RhythmBeatEvaluator(80, 160).Judge(10 + deltaMs / 1000d, 10);
    Assert.That(judge, Is.EqualTo(expected));
}

[TestCase(0, 1)] [TestCase(50, .5f)] [TestCase(100, 0)] [TestCase(150, 0)]
public void TimingWindow_ClampsAccuracy(float error, float expected) =>
    Assert.That(new TimingWindow(100).Evaluate(error), Is.EqualTo(expected).Within(.001));
```

- [ ] **Step 2: Run focused tests and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common.TimingEvaluatorTests -testResults TestResults-timing-red.xml -quit`

Expected: FAIL with missing timing types.

- [ ] **Step 3: Implement deterministic evaluators**

```csharp
public enum TimingJudge { Perfect, Good, Miss }

public readonly struct TimingWindow {
  readonly float maxError;
  public TimingWindow(float maxErrorMs) { maxError = maxErrorMs; }
  public float Evaluate(float errorMs) => UnityEngine.Mathf.Clamp01(1f - UnityEngine.Mathf.Abs(errorMs) / maxError);
}

public readonly struct RhythmBeatEvaluator {
  readonly double perfectMs, goodMs;
  public RhythmBeatEvaluator(double perfectMs, double goodMs) { this.perfectMs = perfectMs; this.goodMs = goodMs; }
  public TimingJudge Judge(double inputDspTime, double beatDspTime) {
    double delta = System.Math.Abs(inputDspTime - beatDspTime) * 1000d;
    return delta <= perfectMs ? TimingJudge.Perfect : delta <= goodMs ? TimingJudge.Good : TimingJudge.Miss;
  }
}
```

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the Step 2 command again. Expected: all timing cases pass.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Common Assets/Tests/EditMode/Gameplay/Common/TimingEvaluatorTests.cs && rtk git commit -m "feat: add deterministic timing evaluators"`

### Task 3: Shared minigame lifecycle

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Common/MinigamePhase.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Common/MinigameLifecycle.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs`
- Test: `Assets/Tests/EditMode/Gameplay/Common/MinigameLifecycleTests.cs`

**Interfaces:**
- Consumes: explicit elapsed seconds and tutorial/countdown durations.
- Produces: `MinigameLifecycle.Tick(float)`, `BeginResolve()`, and `MinigameBase.Finish(MinigameResult)`.

- [ ] **Step 1: Write the failing phase-transition test**

```csharp
[Test]
public void Tick_AdvancesTutorialCountdownAndPlay() {
    var flow = new MinigameLifecycle(2f, 3f);
    flow.Tick(2f); Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Countdown));
    flow.Tick(3f); Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Play));
    flow.BeginResolve(); Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Resolve));
}
```

- [ ] **Step 2: Run focused test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter MinigameLifecycleTests -testResults TestResults-lifecycle-red.xml -quit`

Expected: FAIL because lifecycle types are missing.

- [ ] **Step 3: Implement lifecycle and adapter boundary**

```csharp
public enum MinigamePhase { Tutorial, Countdown, Play, Resolve }

public sealed class MinigameLifecycle {
  readonly float tutorialSeconds, countdownSeconds;
  float elapsed;
  public MinigamePhase Phase { get; private set; } = MinigamePhase.Tutorial;
  public MinigameLifecycle(float tutorialSeconds, float countdownSeconds) {
    this.tutorialSeconds = tutorialSeconds; this.countdownSeconds = countdownSeconds;
  }
  public void Tick(float dt) {
    elapsed += dt;
    if (Phase == MinigamePhase.Tutorial && elapsed >= tutorialSeconds) { Phase = MinigamePhase.Countdown; elapsed = 0; }
    else if (Phase == MinigamePhase.Countdown && elapsed >= countdownSeconds) { Phase = MinigamePhase.Play; elapsed = 0; }
  }
  public void BeginResolve() => Phase = MinigamePhase.Resolve;
}

public abstract class MinigameBase : UnityEngine.MonoBehaviour {
  public event System.Action<MinigameResult> Completed;
  protected MinigameLifecycle Lifecycle { get; private set; }
  protected virtual void Awake() => Lifecycle = new MinigameLifecycle(2f, 3f);
  protected virtual void Update() { Lifecycle.Tick(UnityEngine.Time.deltaTime); if (Lifecycle.Phase == MinigamePhase.Play) TickPlay(UnityEngine.Time.deltaTime); }
  protected abstract void TickPlay(float dt);
  protected void Finish(MinigameResult result) { Lifecycle.BeginResolve(); Completed?.Invoke(result); }
}
```

- [ ] **Step 4: Run all foundation EditMode tests**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Gameplay.Common -testResults TestResults-foundation.xml -quit`

Expected: zero failed tests.

- [ ] **Step 5: Commit**

Run: `rtk git add Assets/_Project/Scripts/Gameplay/Common Assets/Tests/EditMode/Gameplay/Common && rtk git commit -m "feat: add shared minigame lifecycle"`

