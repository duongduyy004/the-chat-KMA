# Volleyball Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable Volleyball scene with contextual dig/set/spike gestures, three-touch feedback, deterministic returns, five-point scoring, routing, and save.

**Architecture:** `VolleyballController` owns ball context, reach, three-touch possession, and deterministic rally sequencing while delegating action validity and scoring to `VolleyballRules`. A presentation adapter renders cues and a full-screen swipe surface supplies semantic gestures.

**Tech Stack:** Unity 6000.3.22f1, Physics2D, Input System, uGUI, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires Basketball checkpoint.
- Low -> Dig, Rising -> Set, ApexNearNet plus forward/down swipe -> Spike.
- Maximum three player touches per possession; combo never bypasses five-point win.
- Opponent trajectories are authored and cued before launch.

---

### Task 1: Volleyball controller and possession state

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/TestSports.cs`

**Interfaces:**
- Produces `VolleyballRules Rules`, `int TouchNumber`, `BallContext CurrentContext`, and `VolleyAction SubmitGesture(Vector2 swipe)`.
- Adds `TestSports.CreateVolleyball()`.

- [ ] **Step 1: Write failing valid sequence, invalid fourth touch, and one-completion tests**

```csharp
[UnityTest]
public IEnumerator DigSetSpike_AwardsOneAuthoredRallyPoint()
{
    var controller = TestSports.CreateVolleyball();
    controller.SetBallContextForTest(BallContext.Low, true, .9f);
    Assert.That(controller.SubmitGesture(Vector2.down), Is.EqualTo(VolleyAction.Dig));
    controller.SetBallContextForTest(BallContext.Rising, true, .9f);
    Assert.That(controller.SubmitGesture(Vector2.up), Is.EqualTo(VolleyAction.Set));
    controller.SetBallContextForTest(BallContext.ApexNearNet, true, .9f);
    Assert.That(controller.SubmitGesture(new Vector2(1f, -1f)), Is.EqualTo(VolleyAction.Spike));
    controller.ResolveOpponentDefenseForTest(false);
    Assert.That(controller.Rules.PlayerScore, Is.EqualTo(1));
    yield return null;
}
```

- [ ] **Step 2: Run controller tests; verify missing controller fails compilation**

- [ ] **Step 3: Implement context resolution and three-touch rally sequencing**

```csharp
public VolleyAction SubmitGesture(Vector2 swipe)
{
    if (TouchNumber >= 3) return VolleyAction.Invalid;
    var action = Rules.ResolveTouch(CurrentContext, swipe, InReachZone, TimingAccuracy);
    if (action == VolleyAction.Invalid || !Rules.TryResolveAndLaunch(ball, CurrentContext, swipe, InReachZone, TimingAccuracy))
        return VolleyAction.Invalid;
    TouchNumber++;
    if (action == VolleyAction.Spike) BeginAuthoredOpponentResolution();
    return action;
}
```

Create the rule with a play-ready internal lifecycle so `MinigameBase` remains the only tutorial/countdown clock. Reset touches at each rally; finish only when rule score/time is terminal.

- [ ] **Step 4: Run controller and all Volleyball rule tests**

- [ ] **Step 5: Commit controller**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs
rtk git commit -m "feat: add Volleyball runtime controller"
```

### Task 2: Volleyball scene, cues, and swipe input

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballPresentationAdapter.cs`
- Create: `Assets/_Project/Scenes/MG_Volleyball.unity`
- Create: `Assets/Editor/VolleyballSceneFactory.cs`
- Modify: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballProductionSceneTests.cs`

- [ ] **Step 1: Write failing scene test for court/net, four actors, BallRig, full-screen swipe, touch count, and opponent cue**

```csharp
[UnityTest]
public IEnumerator ProductionScene_HasThreeTouchPresentationBoundary()
{
    yield return SceneManager.LoadSceneAsync("MG_Volleyball");
    Assert.That(Object.FindFirstObjectByType<VolleyballController>(), Is.Not.Null);
    Assert.That(GameObject.Find("TouchCount"), Is.Not.Null);
    Assert.That(GameObject.Find("OpponentCue"), Is.Not.Null);
}
```
- [ ] **Step 2: Run it; verify scene is not loadable**
- [ ] **Step 3: Generate scene and bind swipe to controller**

```csharp
void OnSwipe(Vector2 delta)
{
    var action = controller.SubmitGesture(delta.normalized);
    feedback.text = action == VolleyAction.Invalid ? "MISS" : action.ToString().ToUpperInvariant();
}
```

Render `TOUCH 1/2/3`, score, reach zone, timing feedback, selected action, opponent trail/cue, timer, tutorial/countdown, and pause.

- [ ] **Step 4: Run scene plus accumulated ball-sport tests**
- [ ] **Step 5: Commit scene**

```bash
rtk git add Assets/_Project/Scenes/MG_Volleyball.unity Assets/_Project/Scripts/Gameplay/Volleyball Assets/_Project/Scripts/Input Assets/Editor/VolleyballSceneFactory.cs ProjectSettings/EditorBuildSettings.asset Assets/Tests
rtk git commit -m "feat: add playable Volleyball scene"
```

### Task 3: Volleyball campaign gate and provenance

**Files:**
- Modify: `Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset`
- Modify: `THIRD_PARTY_ASSETS.md`
- Create: `Assets/Tests/PlayMode/Progression/VolleyballCampaignTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/ProductFlow.cs`
- Modify: `README.md`

- [ ] **Step 1: Add failing Map -> Volleyball -> Result/Punishment -> reload test**

```csharp
[UnityTest]
public IEnumerator PassedVolleyball_IsRestoredFromDisk()
{
    yield return ProductFlow.PassThroughProductionController(SubjectId.Volleyball);
    Assert.That(TestAppRoot.RecreateFromDisk().Session.GetRecord(SubjectId.Volleyball).Passed, Is.True);
}
```
- [ ] **Step 2: Run before catalog enablement; verify RED**
- [ ] **Step 3: Enable route; research a licensed volleyball placeholder pack, record provenance if imported, otherwise keep generated court/actors**
- [ ] **Step 4: Run complete EditMode and PlayMode suites; inspect XML totals**
- [ ] **Step 5: Commit checkpoint**

```bash
rtk git add Assets/_Project/ScriptableObjects/Subjects THIRD_PARTY_ASSETS.md Assets/Tests README.md
rtk git commit -m "test: verify Volleyball campaign route"
```
