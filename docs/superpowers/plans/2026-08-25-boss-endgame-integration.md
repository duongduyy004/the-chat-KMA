# Boss and Endgame Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the campaign with touch-driven Boss phases, persisted victory, a usable Victory screen, and reliable GameOver/New Game routes.

**Architecture:** Keep `BossPhaseController` and its three adapters authoritative, replace keyboard-only production input with shared semantic surfaces, and let `SceneRouter` persist boss completion before routing to Victory. GameOver and Victory are thin uGUI views over session/save state.

**Tech Stack:** Unity 6000.3.22f1, C#, Input System, uGUI, JsonUtility, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires all seven subject checkpoints.
- Boss remains locked until all seven records pass.
- Boss sequence is exactly TapMash -> RhythmHold -> AlternateTap and lasts 30..40 seconds in production configuration.
- Victory and GameOver must survive save reload and offer a confirmed New Game.

---

### Task 1: Persisted boss completion and route contract

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Modify: `Assets/_Project/Scripts/Persistence/SaveEnvelope.cs`
- Test: `Assets/Tests/EditMode/Progression/BossCompletionSnapshotTests.cs`
- Test: `Assets/Tests/PlayMode/Progression/BossVictoryRouteTests.cs`

**Interfaces:**
- `bool GameSession.CompletedBoss` and `void MarkBossCompleted()` from plan 1 become production route state.
- Changes `SceneRouter.CompleteBoss(MinigameResult result)` to save then route `Victory`.

- [ ] **Step 1: Write failing locked, completed, save-reload, and duplicate-completion tests**

```csharp
[UnityTest]
public IEnumerator CompletedBoss_SavesBeforeVictoryAndRoutesOnce()
{
    var root = TestAppRoot.CreateWithAllSubjectsPassed();
    root.Router.StartBoss();
    yield return WaitForScene("MG_Boss");
    root.Router.CompleteBoss(new MinigameResult(true, 8f, Rank.A));
    root.Router.CompleteBoss(new MinigameResult(true, 8f, Rank.A));
    yield return WaitForScene("Victory");
    Assert.That(root.Session.CompletedBoss, Is.True);
    Assert.That(root.Saves.TryLoad(out var save, out _), Is.True);
    Assert.That(save.completedBoss, Is.True);
}
```

- [ ] **Step 2: Run focused tests; verify old `CompleteBoss()` signature and Map route fail**

- [ ] **Step 3: Mark completion once, save settled state, then load Victory**

```csharp
public bool CompleteBoss(MinigameResult result)
{
    if (session.CompletedBoss || IsTransitioning) return false;
    session.MarkBossCompleted();
    LastOutcome = RouteOutcome.ForBoss(result, SessionRoute.Victory);
    SaveSettledSession();
    return Route(SessionRoute.Victory);
}
```

- [ ] **Step 4: Run boss snapshot/route and all persistence/progression tests**

- [ ] **Step 5: Commit boss completion contract**

```bash
rtk git add Assets/_Project/Scripts/Progression Assets/_Project/Scripts/Core Assets/_Project/Scripts/Persistence Assets/Tests
rtk git commit -m "feat: persist boss victory"
```

### Task 2: Boss production touch and HUD

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Boss/BossPresentationAdapter.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Boss/BossRuntimeInputSource.cs`
- Modify: `Assets/_Project/Scenes/MG_Boss.unity`
- Modify: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Test: `Assets/Tests/PlayMode/Progression/BossProductionInputTests.cs`
- Create: `Assets/Tests/PlayMode/Helpers/BossInput.cs`

**Interfaces:**
- Shared tap, hold, and left/right semantic surfaces call existing detector adapters.
- `BossPresentationAdapter.Bind(BossPhaseController, MinigameHud)` renders active phase, progress, cue, and remaining time.

- [ ] **Step 1: Write failing test that completes all three phases using pointer events only**

```csharp
[UnityTest]
public IEnumerator TouchOnly_CompletesThreeAuthoredBossPhases()
{
    yield return ProductFlow.StartUnlockedBoss();
    var input = Object.FindFirstObjectByType<BossRuntimeInputSource>();
    Assert.That(input.TouchWired, Is.True);
    yield return BossInput.CompleteTapMashWithTouch();
    yield return BossInput.CompleteRhythmHoldWithTouch();
    yield return BossInput.CompleteAlternateTapWithTouch();
    yield return WaitForScene("Victory");
}
```

- [ ] **Step 2: Run test; verify `TouchWired` and production surfaces are missing**

- [ ] **Step 3: Bind shared surfaces and phase-specific panel visibility**

```csharp
tap.Pressed += input.OnTapMashPressed;
hold.Released += input.OnRhythmHoldReleased;
left.Pressed += () => input.OnAlternateTapPressed(BossTapSide.Left);
right.Pressed += () => input.OnAlternateTapPressed(BossTapSide.Right);
```

Enable only the active mechanic controls. Keep keyboard polling as an Editor mirror, not a production dependency.

`BossInput` sends pointer events to the same semantic surfaces and never calls detector adapters directly.

- [ ] **Step 4: Run all Boss PlayMode tests plus production touch test**

- [ ] **Step 5: Commit Boss UI/input**

```bash
rtk git add Assets/_Project/Scripts/Gameplay/Boss Assets/_Project/Scripts/Input Assets/_Project/Scenes/MG_Boss.unity Assets/Tests
rtk git commit -m "feat: complete touch Boss flow"
```

### Task 3: Victory and GameOver screens

**Files:**
- Create: `Assets/_Project/Scenes/Victory.unity`
- Modify: `Assets/_Project/Scenes/GameOver.unity`
- Create: `Assets/_Project/Scripts/UI/VictoryScreen.cs`
- Create: `Assets/_Project/Scripts/UI/GameOverScreen.cs`
- Modify: `Assets/Editor/MvpShellSceneFactory.cs`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/UI/EndgameScreenTests.cs`

**Interfaces:**
- `VictoryScreen.Menu()` returns without deleting save; `NewGame(bool confirmed)` replaces it only on confirmation.
- `GameOverScreen.Menu()` and `NewGame(bool confirmed)` use the same router APIs.

- [ ] **Step 1: Write failing Victory summary and GameOver reset-confirmation tests**

```csharp
[UnityTest]
public IEnumerator NewGameRequiresConfirmationOnBothEndScreens()
{
    yield return SceneManager.LoadSceneAsync("Victory");
    Object.FindFirstObjectByType<VictoryScreen>().NewGame(false);
    Assert.That(AppRoot.Instance.Session.CompletedBoss, Is.True);
    yield return SceneManager.LoadSceneAsync("GameOver");
    Object.FindFirstObjectByType<GameOverScreen>().NewGame(false);
    Assert.That(AppRoot.Instance.Session.Lives, Is.Zero);
}
```
- [ ] **Step 2: Run test; verify screens/scenes absent or incomplete**
- [ ] **Step 3: Generate both uGUI screens with seven-score summary, lives state, Menu, and confirmed New Game**

```csharp
public void NewGame(bool confirmed)
{
    if (!confirmed) return;
    router.NewGame();
}
```

- [ ] **Step 4: Run endgame UI, save, and product shell tests**
- [ ] **Step 5: Commit endgame screens**

```bash
rtk git add Assets/_Project/Scenes Assets/_Project/Scripts/UI Assets/Editor/MvpShellSceneFactory.cs ProjectSettings/EditorBuildSettings.asset Assets/Tests
rtk git commit -m "feat: add Victory and GameOver screens"
```

### Task 4: Full campaign gate and final provenance pass

**Files:**
- Create: `Assets/Tests/PlayMode/Progression/ProductionCampaignTests.cs`
- Modify: `THIRD_PARTY_ASSETS.md`
- Modify: `README.md`

- [ ] **Step 1: Write a production-scene campaign test covering New Game, one punishment/retry, seven passes, Boss, Victory, save reload, and duplicate-route guards**

```csharp
[UnityTest]
public IEnumerator ProductionCampaign_ReachesPersistedVictory()
{
    yield return ProductFlow.NewGame();
    yield return ProductFlow.FailThenPassWithPunishment(SubjectId.Sprint);
    foreach (var id in ProductFlow.RemainingSubjectsAfterSprint)
        yield return ProductFlow.PassThroughProductionController(id);
    yield return ProductFlow.PassBossWithTouch();
    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Victory"));
    Assert.That(AppRoot.Instance.Session.CompletedBoss, Is.True);
}
```

- [ ] **Step 2: Run before fixture completion; verify RED at the first unsupported production action**
- [ ] **Step 3: Implement deterministic accelerated production fixtures only; do not submit results directly to `GameSession`**
- [ ] **Step 4: Run full EditMode/PlayMode suites, inspect XML roots, and validate all asset provenance entries**
- [ ] **Step 5: Commit campaign checkpoint**

```bash
rtk git add Assets/Tests/PlayMode/Progression/ProductionCampaignTests.cs THIRD_PARTY_ASSETS.md README.md
rtk git commit -m "test: verify complete production campaign"
```
