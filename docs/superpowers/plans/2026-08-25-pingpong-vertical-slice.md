# PingPong Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable PingPong scene with timing returns, authored placement, capped speed, five-point scoring, product routing, and save.

**Architecture:** `PingPongController` owns runtime sequencing around existing `PingPongRules`; `PingPongPresentationAdapter` renders the ball/table/hit zone while shared tap input submits timing attempts.

**Tech Stack:** Unity 6000.3.22f1, Physics2D, Input System, uGUI, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires plans 1 and 2.
- Win only at five points with a lead; rally length cannot pass.
- Ball speed stays at or below the rule cap; no random AI misses.
- Preserve accumulated suites.

---

### Task 1: PingPong runtime controller

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PingPong/PingPongController.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/PingPongControllerTests.cs`
- Create: `Assets/Tests/PlayMode/Helpers/TestSports.cs`

**Interfaces:**
- Produces `PingPongRules Rules`, `float TimingAccuracy`, `bool HitZoneActive`, and `void SubmitReturn()`.
- Emits inherited `Completed(MinigameResult)` exactly once.
- Produces `TestSports.CreatePingPong()`; later sport plans extend this shared test factory.

- [ ] **Step 1: Write failing controller completion and miss tests**

```csharp
[UnityTest]
public IEnumerator FiveAuthoredPoints_CompleteExactlyOnce()
{
    var controller = TestSports.CreatePingPong();
    var count = 0; controller.Completed += _ => count++;
    for (var point = 0; point < 5; point++)
        controller.CompleteAuthoredPointForTest(.9f, Vector2.zero);
    Assert.That(controller.Rules.PlayerPoints, Is.EqualTo(5));
    Assert.That(count, Is.EqualTo(1));
    yield return null;
}
```

- [ ] **Step 2: Run `PingPongControllerTests`; verify missing controller fails compilation**

- [ ] **Step 3: Implement rally state around existing rule APIs**

```csharp
public void SubmitReturn()
{
    if (!HitZoneActive) { Rules.AwardOpponentPoint(); ResetRally(); return; }
    if (Rules.TryReturn(ball, TimingAccuracy, currentPlacement))
        StartCoroutine(ResolveAuthoredReturn());
}
```

After each authored successful return, award the player point only when the opponent cannot defend that placement; otherwise continue the deterministic rally. Finish when either score reaches the rule terminal condition or time expires.

- [ ] **Step 4: Run controller and existing PingPong rule tests**

- [ ] **Step 5: Commit controller**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/PingPong/PingPongController.cs Assets/Tests/PlayMode/Gameplay/Ball/PingPongControllerTests.cs
rtk git commit -m "feat: add PingPong runtime controller"
```

### Task 2: PingPong scene, input, and presentation

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PingPong/PingPongPresentationAdapter.cs`
- Create: `Assets/_Project/Scenes/MG_PingPong.unity`
- Create: `Assets/Editor/PingPongSceneFactory.cs`
- Modify: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/PingPongProductionSceneTests.cs`

- [ ] **Step 1: Write failing scene test for one controller, BallRig, camera, HUD, and tap surface**

```csharp
[UnityTest]
public IEnumerator ProductionScene_HasRequiredRuntimeBoundary()
{
    yield return SceneManager.LoadSceneAsync("MG_PingPong");
    Assert.That(Object.FindObjectsByType<PingPongController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
    Assert.That(Object.FindFirstObjectByType<BallRig>(), Is.Not.Null);
    Assert.That(Object.FindFirstObjectByType<SemanticTapButton>(), Is.Not.Null);
}
```
- [ ] **Step 2: Run the scene test; verify scene is not loadable**
- [ ] **Step 3: Generate the scene and bind production input**

```csharp
public void Render(PingPongController controller)
{
    score.text = $"{controller.Rules.PlayerPoints} - {controller.Rules.OpponentPoints}";
    hitZone.color = controller.HitZoneActive ? activeColor : inactiveColor;
}
```

Use a visible table, net, paddles, ball/shadow, hit zone, score, rally count, tutorial/countdown, pause, and one full-screen tap surface.

- [ ] **Step 4: Run production scene and all BallRig/PingPong tests**
- [ ] **Step 5: Commit scene**

```bash
rtk git add Assets/_Project/Scenes/MG_PingPong.unity Assets/_Project/Scripts/Gameplay/PingPong Assets/_Project/Scripts/Input ProjectSettings/EditorBuildSettings.asset Assets/Editor/PingPongSceneFactory.cs Assets/Tests
rtk git commit -m "feat: add playable PingPong scene"
```

### Task 3: Route, save, asset provenance, and gate

**Files:**
- Modify: `Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset`
- Create or modify: `THIRD_PARTY_ASSETS.md`
- Create: `Assets/Tests/PlayMode/Progression/PingPongCampaignTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/ProductFlow.cs`
- Modify: `README.md`

- [ ] **Step 1: Write failing Map -> PingPong -> Result -> reload test using real controller input**

```csharp
[UnityTest]
public IEnumerator PassedPingPong_IsRestoredFromDisk()
{
    yield return ProductFlow.PassThroughProductionController(SubjectId.PingPong);
    Assert.That(TestAppRoot.RecreateFromDisk().Session.GetRecord(SubjectId.PingPong).Passed, Is.True);
}
```
- [ ] **Step 2: Run it; verify catalog/route assertion fails before enabling the entry**
- [ ] **Step 3: Enable catalog route; search a licensed table-tennis placeholder pack, record exact provenance if imported, otherwise retain generated shapes and record no external asset**
- [ ] **Step 4: Run full EditMode/PlayMode suites and inspect XML roots**
- [ ] **Step 5: Commit the PingPong checkpoint**

```bash
rtk git add Assets/_Project/ScriptableObjects/Subjects THIRD_PARTY_ASSETS.md Assets/Tests README.md
rtk git commit -m "test: verify PingPong campaign route"
```
