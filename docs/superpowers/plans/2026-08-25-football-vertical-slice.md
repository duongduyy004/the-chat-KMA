# Football Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable Football free-kick scene with swipe placement/force/spin, trajectory preview, five authored goalkeeper patterns, three-goal pass logic, routing, and save.

**Architecture:** `FootballController` converts a semantic swipe into one immutable `FootballShot`, previews the same inputs, and asks `FootballRules.ResolveAuthoredShot` to resolve the exact active goalkeeper pattern. Presentation renders the cue, preview, result, and five-kick sequence.

**Tech Stack:** Unity 6000.3.22f1, Physics2D, Input System, uGUI, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires Badminton checkpoint.
- Exactly five authored goalkeeper patterns; one difficulty modifier per phase.
- Pass at three goals in five kicks; no random keeper decisions.
- Preview and resolved shot must use the same normalized values.

---

### Task 1: Football controller and shot mapping

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/FootballControllerTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/TestSports.cs`

**Interfaces:**
- Produces `FootballRules Rules`, `FootballShot PreviewShot`, `bool SubmitSwipe(Vector2 start, Vector2 end, float curvature)`, and `bool LastGoal`.
- Adds `TestSports.CreateFootball()`.

- [ ] **Step 1: Write failing normalization, preview equality, five-kick, and one-completion tests**

```csharp
[UnityTest]
public IEnumerator SubmittedShot_EqualsPreviewAndResolvesCurrentPattern()
{
    var controller = TestSports.CreateFootball();
    controller.UpdatePreview(Vector2.zero, new Vector2(150f, 240f), .25f);
    var preview = controller.PreviewShot;
    controller.SubmitSwipe(Vector2.zero, new Vector2(150f, 240f), .25f);
    Assert.That(controller.Rules.LastShot, Is.EqualTo(preview));
    Assert.That(controller.Rules.LastKeeperPattern, Is.SameAs(controller.Rules.PatternSet.Patterns[0]));
    yield return null;
}
```

- [ ] **Step 2: Run controller tests; verify missing controller fails**
- [ ] **Step 3: Implement one canonical swipe-to-shot mapping**

```csharp
FootballShot MapShot(Vector2 start, Vector2 end, float curvature)
{
    var delta = end - start;
    var placement = Mathf.InverseLerp(-maxHorizontal, maxHorizontal, delta.x);
    var force = Mathf.Clamp01(delta.magnitude / maxSwipePixels);
    var spin = Mathf.Clamp(curvature, -1f, 1f);
    var kind = Mathf.Abs(spin) >= .35f ? ShotKind.Curve : force >= .75f ? ShotKind.Power : ShotKind.Placement;
    return new FootballShot(placement, force, spin, kind);
}
```

Call this method for preview and submission. After the fifth resolution, finish with `Rules.BuildResult()` exactly once.

- [ ] **Step 4: Run controller and all Football rule tests**
- [ ] **Step 5: Commit controller**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/Football/FootballController.cs Assets/Tests/PlayMode/Gameplay/Ball/FootballControllerTests.cs
rtk git commit -m "feat: add Football runtime controller"
```

### Task 2: Football scene, preview, and goalkeeper cues

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballPresentationAdapter.cs`
- Create: `Assets/_Project/Scenes/MG_Football.unity`
- Create: `Assets/Editor/FootballSceneFactory.cs`
- Modify: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/FootballProductionSceneTests.cs`

- [ ] **Step 1: Write failing scene test for ball/goal/keeper, swipe surface, trajectory, cue, five kick markers, HUD, and one controller**

```csharp
[UnityTest]
public IEnumerator ProductionScene_HasPreviewKeeperAndFiveKickMarkers()
{
    yield return SceneManager.LoadSceneAsync("MG_Football");
    Assert.That(Object.FindFirstObjectByType<FootballController>(), Is.Not.Null);
    Assert.That(GameObject.Find("TrajectoryPreview"), Is.Not.Null);
    Assert.That(GameObject.Find("KickMarkers").transform.childCount, Is.EqualTo(5));
}
```
- [ ] **Step 2: Run it; verify missing scene fails**
- [ ] **Step 3: Generate scene and bind drag preview plus release**

```csharp
swipe.Dragged += sample => controller.UpdatePreview(sample.Start, sample.Current, sample.Curvature);
swipe.Released += sample => controller.SubmitSwipe(sample.Start, sample.End, sample.Curvature);
```

Render goalkeeper modifier before the shot, dashed trajectory, force meter, spin indicator, GOAL/SAVED, kick markers, timer, tutorial/countdown, and pause.

- [ ] **Step 4: Run production scene and accumulated ball suites**
- [ ] **Step 5: Commit scene**

```bash
rtk git add Assets/_Project/Scenes/MG_Football.unity Assets/_Project/Scripts/Gameplay/Football Assets/_Project/Scripts/Input Assets/Editor/FootballSceneFactory.cs ProjectSettings/EditorBuildSettings.asset Assets/Tests
rtk git commit -m "feat: add playable Football scene"
```

### Task 3: Football campaign gate and provenance

**Files:**
- Modify: `Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset`
- Modify: `THIRD_PARTY_ASSETS.md`
- Create: `Assets/Tests/PlayMode/Progression/FootballCampaignTests.cs`
- Modify: `Assets/Tests/PlayMode/Helpers/ProductFlow.cs`
- Modify: `README.md`

- [ ] **Step 1: Add failing Map -> Football -> Result/Punishment -> reload test**

```csharp
[UnityTest]
public IEnumerator PassedFootball_IsRestoredFromDisk()
{
    yield return ProductFlow.PassThroughProductionController(SubjectId.Football);
    Assert.That(TestAppRoot.RecreateFromDisk().Session.GetRecord(SubjectId.Football).Passed, Is.True);
}
```
- [ ] **Step 2: Run before catalog enablement; verify RED**
- [ ] **Step 3: Enable route; research licensed football/goalkeeper placeholders and record provenance, or use shapes**
- [ ] **Step 4: Run complete accumulated suites and inspect XML roots**
- [ ] **Step 5: Commit checkpoint**

```bash
rtk git add Assets/_Project/ScriptableObjects/Subjects THIRD_PARTY_ASSETS.md Assets/Tests README.md
rtk git commit -m "test: verify Football campaign route"
```
