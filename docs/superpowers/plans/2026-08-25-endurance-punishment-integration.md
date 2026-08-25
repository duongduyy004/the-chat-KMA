# Endurance and Punishment Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate Endurance and Punishment into the saved product flow with production touch input, shared HUD conventions, and real scene routing.

**Architecture:** Keep `EnduranceRules` and `PunishmentController` authoritative. Scene adapters translate shared semantic input into their existing APIs and render state through focused uGUI presenters.

**Tech Stack:** Unity 6000.3.22f1, C#, uGUI, Input System 1.20.0, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires completed `2026-08-25-product-shell-sprint.md`.
- Touch and keyboard must invoke the same semantic methods.
- Endurance input modes remain mutually exclusive; punishment remains between attempts.
- Preserve all accumulated tests.

---

### Task 1: Shared lifecycle HUD and gesture surfaces

**Files:**
- Create: `Assets/_Project/Scripts/UI/MinigameHud.cs`
- Create: `Assets/_Project/Scripts/UI/TutorialCountdownView.cs`
- Create: `Assets/_Project/Scripts/Input/HoldGestureSurface.cs`
- Create: `Assets/_Project/Scripts/Input/SwipeGestureSurface.cs`
- Test: `Assets/Tests/PlayMode/UI/SharedGameplayUiTests.cs`
- Create: `Assets/Tests/PlayMode/Helpers/TestUi.cs`

**Interfaces:**
- `MinigameHud.SetLives(int)`, `SetTimer(float)`, `SetProgress(float)`, and `SetCue(string, Color)`.
- `HoldGestureSurface.Released(float seconds)` and `SwipeGestureSurface.Swiped(Vector2 delta)`.

- [ ] **Step 1: Write failing gesture and safe-area tests**

```csharp
[UnityTest]
public IEnumerator HoldAndSwipe_EmitOneSemanticEventPerGesture()
{
    var hold = TestUi.CreateHoldSurface();
    var swipe = TestUi.CreateSwipeSurface(80f);
    var releases = 0; var swipes = 0;
    hold.Released += _ => releases++;
    swipe.Swiped += _ => swipes++;
    yield return PointerFixture.Hold(hold.gameObject, .5f);
    yield return PointerFixture.Swipe(swipe.gameObject, Vector2.up * 120f);
    Assert.That(releases, Is.EqualTo(1));
    Assert.That(swipes, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run `SharedGameplayUiTests`; verify missing types fail compilation**

- [ ] **Step 3: Implement pointer-down/up hold duration and thresholded swipe**

```csharp
public void OnPointerUp(PointerEventData eventData)
{
    if (!tracking) return;
    tracking = false;
    Released?.Invoke(Time.unscaledTime - pressedAt);
}
```

Swipe dispatches only once when `abs(delta.y) >= 80` and vertical movement exceeds horizontal movement.

- [ ] **Step 4: Run shared UI plus Sprint production tests; expect zero failures**

- [ ] **Step 5: Commit shared UI/input**

```bash
rtk git add Assets/_Project/Scripts/UI Assets/_Project/Scripts/Input Assets/Tests/PlayMode/UI
rtk git commit -m "feat: add shared gameplay gesture UI"
```

### Task 2: Endurance production adapter and scene

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EndurancePresentationAdapter.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceInputBridge.cs`
- Modify: `Assets/_Project/Scenes/MG_Endurance.unity`
- Test: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceProductionSceneTests.cs`

**Interfaces:**
- `EndurancePresentationAdapter.Bind(EnduranceController, MinigameHud)`.
- Existing `TapAtCurrentBeat()`, `EndHold(float beatsHeld)`, and `Swipe(SwipeDirection)` remain the controller input boundary.

- [ ] **Step 1: Write a failing touch-mode test**

```csharp
[UnityTest]
public IEnumerator Scene_ExposesTapHoldSwipeAndVisibleModeCue()
{
    yield return ProductFlow.StartSubject(SubjectId.Endurance);
    var controller = Object.FindFirstObjectByType<EnduranceController>();
    Assert.That(Object.FindFirstObjectByType<EndurancePresentationAdapter>(), Is.Not.Null);
    Assert.That(Object.FindObjectsByType<SwipeGestureSurface>(FindObjectsSortMode.None), Is.Not.Empty);
    controller.AdvanceToBeatForTest(3);
    Assert.That(Object.FindFirstObjectByType<MinigameHud>().CueText, Is.Not.Empty);
}
```

- [ ] **Step 2: Run the test; verify adapter/HUD assertions fail**

- [ ] **Step 3: Wire shared gesture surfaces and render each exclusive mode**

```csharp
void OnSwipe(Vector2 delta) => controller.Swipe(delta.y >= 0f ? SwipeDirection.Up : SwipeDirection.Down);
void OnHold(float seconds) => controller.EndHold((float)(seconds / controller.BeatIntervalSeconds));
```

Show beat pulse in RhythmTap, breath ring in BreathHold, and obstacle arrow in ObstacleSwipe. Keep the authored two-beat warning and DSP metronome.

- [ ] **Step 4: Run all Running EditMode/PlayMode tests and production route tests**

- [ ] **Step 5: Commit Endurance integration**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/Endurance Assets/_Project/Scenes/MG_Endurance.unity Assets/Tests
rtk git commit -m "feat: complete Endurance MVP route"
```

### Task 3: Punishment production UI and save boundary

**Files:**
- Create: `Assets/_Project/Scripts/UI/PunishmentPresentationAdapter.cs`
- Modify: `Assets/_Project/Scripts/Core/PunishmentSceneController.cs`
- Modify: `Assets/_Project/Scenes/Punishment.unity`
- Test: `Assets/Tests/PlayMode/Progression/PunishmentProductionTests.cs`
- Create: `Assets/Tests/PlayMode/Helpers/PunishmentInput.cs`

**Interfaces:**
- Adapter reads `PunishmentController.CurrentStep`, `StepProgress`, and `CompletedSteps` exposed as read-only properties.
- Shared tap/hold/alternate surfaces call existing `SubmitTap`, `SubmitRhythmHold`, and `SubmitAlternateTap`.

- [ ] **Step 1: Write a failing production punishment test**

```csharp
[UnityTest]
public IEnumerator FailedEndurance_CompletesTouchPunishmentAndRestoresRetry()
{
    yield return ProductFlow.FailFirstAttempt(SubjectId.Endurance);
    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Punishment"));
    yield return PunishmentInput.CompleteCurrentSequenceWithTouch();
    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MG_Endurance"));
    Assert.That(AppRoot.Instance.Session.PendingPunishmentSubject, Is.Null);
}
```

- [ ] **Step 2: Run test; verify missing presentation/input fixture fails**

- [ ] **Step 3: Expose read-only progress and bind the three mechanic panels**

```csharp
public ChallengeStep CurrentStep => punishment == null ? default : punishment.Current;
public float StepProgress => currentStepProgress;
```

Enable exactly one panel for TapMash, RhythmHold, or AlternateTap. Save after completion before loading retry.

`PunishmentInput.CompleteCurrentSequenceWithTouch()` reads the active mechanic and drives `SemanticTapButton`, `HoldGestureSurface`, or alternating semantic buttons with pointer events; it never calls `CompletePunishment()` directly.

- [ ] **Step 4: Run Punishment, saved-flow, and all progression tests**

- [ ] **Step 5: Commit Punishment integration**

```bash
rtk git add Assets/_Project/Scripts/Core Assets/_Project/Scripts/UI/PunishmentPresentationAdapter.cs Assets/_Project/Scenes/Punishment.unity Assets/Tests
rtk git commit -m "feat: integrate touch punishment flow"
```

### Task 4: Two-subject checkpoint gate

**Files:**
- Create: `Assets/Tests/PlayMode/Progression/TwoSubjectCampaignTests.cs`
- Modify: `README.md`

- [ ] **Step 1: Write campaign test covering Sprint pass, Endurance retry, save reload, and Map state**

```csharp
[UnityTest]
public IEnumerator TwoSubjectCampaign_RestoresBothRecords()
{
    yield return ProductFlow.PassSprintWithScore(7f);
    yield return ProductFlow.FailThenPassWithPunishment(SubjectId.Endurance);
    var restored = TestAppRoot.RecreateFromDisk();
    Assert.That(restored.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
    Assert.That(restored.Session.GetRecord(SubjectId.Endurance).Passed, Is.True);
}
```
- [ ] **Step 2: Run it before helper completion; verify RED on the first missing production behavior**
- [ ] **Step 3: Implement only missing fixture wiring; do not inject domain results**
- [ ] **Step 4: Run complete EditMode and PlayMode suites; inspect XML roots for zero failures/skips**
- [ ] **Step 5: Commit checkpoint documentation**

```bash
rtk git add Assets/Tests/PlayMode/Progression/TwoSubjectCampaignTests.cs README.md
rtk git commit -m "test: verify two-subject campaign"
```
