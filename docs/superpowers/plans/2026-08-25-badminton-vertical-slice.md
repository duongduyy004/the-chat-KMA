# Badminton Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable Badminton scene with hold-to-charge, height-based release, shuttle flight, authored rallies, five-point scoring, routing, and save.

**Architecture:** `BadmintonController` measures semantic hold duration and normalized shuttle height, calls `BadmintonRules.TryExchange`, and launches `BallRig` with a shuttle `FlightProfile`. Presentation renders charge, height bands, wind cue, and rally state.

**Tech Stack:** Unity 6000.3.22f1, Physics2D, Input System, uGUI, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires Volleyball checkpoint.
- Release below `.35` is Lift, `.35..<.7` is Drive, and `>=.7` is Smash; charge above `1` is Overcharge.
- Authored wind cue must match the active rally exchange.
- Pass only at five points with a lead.

---

### Task 1: Badminton controller and shuttle flight

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Badminton/BadmintonController.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Badminton/BadmintonRules.cs`
- Create: `Assets/_Project/ScriptableObjects/Gameplay/FlightProfile_Shuttle.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/BadmintonControllerTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/TestSports.cs`

**Interfaces:**
- Produces `BadmintonRules Rules`, `float ChargeRatio`, `float ShuttleHeight`, `BadmintonShot LastShot`, `void BeginCharge()`, and `void ReleaseCharge()`.
- Adds read-only `BadmintonShot BadmintonRules.LastShot` and `TestSports.CreateBadminton()`.

- [ ] **Step 1: Write failing lift/drive/smash/overcharge and completion tests**

```csharp
[UnityTest]
public IEnumerator HeightBands_SelectDistinctShotsAndLaunchShuttleProfile()
{
    var controller = TestSports.CreateBadminton();
    Assert.That(controller.ReleaseForTest(.6f, .2f), Is.EqualTo(BadmintonShot.Lift));
    Assert.That(controller.ReleaseForTest(.6f, .5f), Is.EqualTo(BadmintonShot.Drive));
    Assert.That(controller.ReleaseForTest(.6f, .8f), Is.EqualTo(BadmintonShot.Smash));
    Assert.That(controller.Ball.Profile.LinearDrag, Is.GreaterThan(0f));
    yield return null;
}
```

- [ ] **Step 2: Run controller tests; verify missing type/profile fails**
- [ ] **Step 3: Implement hold measurement, authored exchange, and shot launch table**

```csharp
public void ReleaseCharge()
{
    ChargeRatio = (Time.unscaledTime - chargeStartedAt) / maxChargeSeconds;
    var accepted = Rules.TryExchange(ChargeRatio, ShuttleHeight);
    LastShot = Rules.LastShot;
    if (!accepted || LastShot == BadmintonShot.Overcharge) { Rules.AwardOpponentPoint(); return; }
    LaunchShot(LastShot);
}
```

Use explicit direction/force values per shot and the shuttle profile; do not alter `BadmintonRules` height thresholds.

- [ ] **Step 4: Run controller, rule, and BallRig tests**
- [ ] **Step 5: Commit controller/profile**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/Badminton Assets/_Project/ScriptableObjects/Gameplay Assets/Tests/PlayMode/Gameplay/Ball/BadmintonControllerTests.cs
rtk git commit -m "feat: add Badminton runtime controller"
```

### Task 2: Badminton scene and hold/release presentation

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Badminton/BadmintonPresentationAdapter.cs`
- Create: `Assets/_Project/Scenes/MG_Badminton.unity`
- Create: `Assets/Editor/BadmintonSceneFactory.cs`
- Modify: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/BadmintonProductionSceneTests.cs`

- [ ] **Step 1: Write failing scene test for court/net, shuttle BallRig, charge surface, height bands, cue, HUD, and one controller**

```csharp
[UnityTest]
public IEnumerator ProductionScene_HasHoldReleaseAndHeightBands()
{
    yield return SceneManager.LoadSceneAsync("MG_Badminton");
    Assert.That(Object.FindFirstObjectByType<BadmintonController>(), Is.Not.Null);
    Assert.That(Object.FindFirstObjectByType<HoldGestureSurface>(), Is.Not.Null);
    Assert.That(GameObject.Find("HeightBands"), Is.Not.Null);
}
```
- [ ] **Step 2: Run it; verify missing scene fails**
- [ ] **Step 3: Generate scene and bind pointer hold/release**

```csharp
holdSurface.Pressed += controller.BeginCharge;
holdSurface.Released += _ => controller.ReleaseCharge();
```

Render charge ring, three labeled height bands, selected shot, wind cue, score/rally, timer, tutorial/countdown, and pause.

- [ ] **Step 4: Run production scene and accumulated ball suites**
- [ ] **Step 5: Commit scene**

```bash
rtk git add Assets/_Project/Scenes/MG_Badminton.unity Assets/_Project/Scripts/Gameplay/Badminton Assets/_Project/Scripts/Input Assets/Editor/BadmintonSceneFactory.cs ProjectSettings/EditorBuildSettings.asset Assets/Tests
rtk git commit -m "feat: add playable Badminton scene"
```

### Task 3: Badminton campaign gate and provenance

**Files:**
- Modify: `Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset`
- Modify: `THIRD_PARTY_ASSETS.md`
- Create: `Assets/Tests/PlayMode/Progression/BadmintonCampaignTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/ProductFlow.cs`
- Modify: `README.md`

- [ ] **Step 1: Add failing Map -> Badminton -> Result -> reload test using hold/release input**

```csharp
[UnityTest]
public IEnumerator PassedBadminton_IsRestoredFromDisk()
{
    yield return ProductFlow.PassThroughProductionController(SubjectId.Badminton);
    Assert.That(TestAppRoot.RecreateFromDisk().Session.GetRecord(SubjectId.Badminton).Passed, Is.True);
}
```
- [ ] **Step 2: Run before catalog enablement; verify RED**
- [ ] **Step 3: Enable route; research licensed badminton placeholders and record provenance, or retain shapes**
- [ ] **Step 4: Run full suites; verify zero failures/skips**
- [ ] **Step 5: Commit checkpoint**

```bash
rtk git add Assets/_Project/ScriptableObjects/Subjects THIRD_PARTY_ASSETS.md Assets/Tests README.md
rtk git commit -m "test: verify Badminton campaign route"
```
