# Basketball Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable Basketball scene with swipe passing, authored alley-oop flight, apex timing feedback, five baskets, product routing, and save.

**Architecture:** A focused controller sequences `Holding -> Passing -> AlleyOopFlight -> Holding/Resolved` through `BasketballRules`; presentation renders the authored flight and semantic swipe/tap adapters call controller methods.

**Tech Stack:** Unity 6000.3.22f1, Physics2D, Input System, uGUI, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires the PingPong checkpoint.
- Pass only after five baskets within 30 seconds.
- Every finish reports Early, Perfect, or Late; no random toss variance.
- Preserve accumulated suites.

---

### Task 1: Basketball runtime controller

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/BasketballController.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/BasketballControllerTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/TestSports.cs`

**Interfaces:**
- Produces `BasketballRules Rules`, `FinishJudge LastJudge`, `void SubmitPass(Vector2 swipe)`, and `void SubmitFinish()`.
- Adds `TestSports.CreateBasketball()`.

- [ ] **Step 1: Write failing pass/apex/five-basket tests**

```csharp
[UnityTest]
public IEnumerator PerfectApex_FiveTimes_CompletesOnce()
{
    var controller = TestSports.CreateBasketball();
    var completions = 0; controller.Completed += _ => completions++;
    for (var i = 0; i < 5; i++)
    {
        controller.SubmitPass(Vector2.up + Vector2.right);
        controller.LaunchAuthoredAlleyOopForTest();
        controller.PlaceBallAtApexForTest();
        controller.SubmitFinish();
    }
    Assert.That(controller.Rules.Baskets, Is.EqualTo(5));
    Assert.That(completions, Is.EqualTo(1));
    yield return null;
}
```

- [ ] **Step 2: Run controller tests; verify missing type fails compilation**
- [ ] **Step 3: Implement controller using `TryPass`, `TryLaunchAlleyOop`, `TapFinish`, and `BuildResult`**

```csharp
public void SubmitFinish()
{
    LastJudge = Rules.TapFinish(ball.Body.position.y, ball.Body.velocity.y);
    if (Rules.PrimaryObjectiveComplete) Finish(Rules.BuildResult());
    else if (LastJudge != FinishJudge.Ignored) ResetPossession();
}
```

- [ ] **Step 4: Run controller and BasketballRules tests**
- [ ] **Step 5: Commit controller**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/Basketball/BasketballController.cs Assets/Tests/PlayMode/Gameplay/Ball/BasketballControllerTests.cs
rtk git commit -m "feat: add Basketball runtime controller"
```

### Task 2: Basketball scene and production gestures

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/BasketballPresentationAdapter.cs`
- Create: `Assets/_Project/Scenes/MG_Basketball.unity`
- Create: `Assets/Editor/BasketballSceneFactory.cs`
- Modify: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/BasketballProductionSceneTests.cs`

- [ ] **Step 1: Write failing scene/input test asserting swipe pass, tap finish, apex ring, one controller, and one BallRig**

```csharp
[UnityTest]
public IEnumerator ProductionScene_HasSwipeTapAndApexFeedback()
{
    yield return SceneManager.LoadSceneAsync("MG_Basketball");
    Assert.That(Object.FindFirstObjectByType<BasketballController>(), Is.Not.Null);
    Assert.That(Object.FindFirstObjectByType<SwipeGestureSurface>(), Is.Not.Null);
    Assert.That(GameObject.Find("ApexRing"), Is.Not.Null);
}
```
- [ ] **Step 2: Run it; verify scene load fails**
- [ ] **Step 3: Generate scene and bind gestures**

```csharp
void OnSwipe(Vector2 delta) => controller.SubmitPass(delta.normalized * Mathf.Clamp(delta.magnitude / 300f, .25f, 1f));
void OnTap() => controller.SubmitFinish();
```

Render player, teammate, basket, ball/shadow, pass arrow, shrinking apex ring, timer, baskets, combo, and Early/Perfect/Late label.

- [ ] **Step 4: Run production scene plus Basketball/PingPong/BallRig suites**
- [ ] **Step 5: Commit scene**

```bash
rtk git add Assets/_Project/Scenes/MG_Basketball.unity Assets/_Project/Scripts/Gameplay/Basketball Assets/_Project/Scripts/Input Assets/Editor/BasketballSceneFactory.cs ProjectSettings/EditorBuildSettings.asset Assets/Tests
rtk git commit -m "feat: add playable Basketball scene"
```

### Task 3: Basketball campaign gate and provenance

**Files:**
- Modify: `Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset`
- Modify: `THIRD_PARTY_ASSETS.md`
- Create: `Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/ProductFlow.cs`
- Modify: `README.md`

- [ ] **Step 1: Add failing Map -> Basketball -> Result -> reload test**

```csharp
[UnityTest]
public IEnumerator PassedBasketball_IsRestoredFromDisk()
{
    yield return ProductFlow.PassThroughProductionController(SubjectId.Basketball);
    Assert.That(TestAppRoot.RecreateFromDisk().Session.GetRecord(SubjectId.Basketball).Passed, Is.True);
}
```
- [ ] **Step 2: Run it before catalog enablement; verify RED**
- [ ] **Step 3: Enable catalog; research a redistributable basketball placeholder pack and record provenance, or use generated shapes**
- [ ] **Step 4: Run complete accumulated suites; verify zero failures/skips**
- [ ] **Step 5: Commit checkpoint**

```bash
rtk git add Assets/_Project/ScriptableObjects/Subjects THIRD_PARTY_ASSETS.md Assets/Tests README.md
rtk git commit -m "test: verify Basketball campaign route"
```
