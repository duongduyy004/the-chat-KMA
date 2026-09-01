# S5 Shell and Core Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Assemble the offline KMA campaign shell so a player can start or resume a run, play all seven placeholder subjects, recover from a failed attempt, lose lives into Game Over, unlock the boss, and pause safely.

**Architecture:** Keep `GameSession` authoritative for attempts, punishment, lives, records, and boss eligibility. Add a pure route-preview API that shares the route decision helper with `SubmitResult`; the result overlay displays the consequence first and submits the result only after Continue. `SceneRouter` remains the single scene-transition owner, while each placeholder scene owns one `PlaceholderMinigameController` and uses existing presentation/input services.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, uGUI/TextMeshPro, Input System + EnhancedTouch, Unity Test Framework, Android landscape, JSON save system from S4.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §5 S5-1, S5-2, S5-3 and S5 device gate.

## Global Constraints

- `Map.unity` is the LevelSelect scene; `Menu.unity` contains MainMenu, Settings, and Calibrate screens.
- `GameSession.PreviewRoute(subject, result)` is pure and must call the same private route helper as `SubmitResult`; `SubmitResult` behavior remains unchanged.
- Result flow is `Finish(result) → ResultPanel → Continue → Completed`; `Completed` is emitted exactly once.
- `SceneRouter` is the only owner of scene transitions and continues using `LoadSceneMode.Single`.
- All seven playable subjects use real scene routes; the three coming-soon map nodes remain presentation-only and have no `SubjectId` record.
- `PlaceholderMinigameController` is an S5 verification stub and must be replaced before S16; it exposes only debug Pass/Fail controls for this checkpoint.
- Save data remains `Application.persistentDataPath/save.json`, written atomically through `save.tmp` and `File.Replace`; New Game preserves `settings` and `tutorialSeen`.
- Android remains landscape; Canvas reference resolution is `1920×1080`, `Match Width Or Height = 1.0`, and safe-area insets apply on both sides.
- Pause sets `Time.timeScale = 0`; Endurance/Boss rhythm clocks must explicitly pause/resume their `dspTime` schedule.
- Run every shell command through `rtk`; do not stage or overwrite unrelated dirty S1–S4 files.

---

## File Map

| Area | Responsibility |
|---|---|
| Session/routing | `Assets/_Project/Scripts/Progression/GameSession.cs`, `Assets/_Project/Scripts/Core/SceneRouter.cs`, `Assets/_Project/Scripts/Core/PunishmentSceneController.cs` |
| Shell UI | `Assets/_Project/Scripts/UI/{MainMenuScreen,MapScreen,SettingsScreen,CalibrateScreen,PausePanel,GameOverScreen}.cs` |
| Result flow | `Assets/_Project/Scripts/UI/ResultPanel.cs`, `Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs` only if an additive completion seam is required |
| Placeholder gameplay | `Assets/_Project/Scripts/Gameplay/Common/PlaceholderMinigameController.cs` |
| Scenes/build | `Assets/_Project/Scenes/{Menu,Map,Punishment,GameOver,MG_Volleyball,MG_Basketball,MG_PingPong,MG_Badminton,MG_Football}.unity`, `ProjectSettings/EditorBuildSettings.asset` |
| Tests | `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs`, plus focused shell/presentation tests under `Assets/Tests/PlayMode/` |

---

### Task 1: Lock route preview and result sequencing

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/UI/ResultPanel.cs`
- Create/modify: `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs`

**Interfaces:**
- Produces `GameSession.PreviewRoute(SubjectId id, MinigameResult result) → SessionRoute`.
- `PreviewRoute` validates the same active subject state as `SubmitResult` and never mutates `Lives`, records, active attempt, or punishment state.
- `ResultPanel.Show(MinigameResult result, string previewRoute)` resets its one-shot guard; `Continue()` raises `ActionRequested` once.

- [ ] **Step 1: Write failing tests for route preview and one-shot result.** Add these cases:

```csharp
[Test]
public void PreviewRoute_FirstFailureReturnsPunishmentWithoutMutation()
{
    var session = new GameSession();
    session.StartSubject(SubjectId.Sprint);
    var result = new MinigameResult(false, 0f, Rank.F);

    Assert.That(session.PreviewRoute(SubjectId.Sprint, result), Is.EqualTo(SessionRoute.Punishment));
    Assert.That(session.Lives, Is.EqualTo(5));
    Assert.That(session.PendingPunishmentSubject, Is.Null);
}

[Test]
public void ResultPanel_ContinueEmitsActionOnlyOnce()
{
    var panel = new GameObject().AddComponent<ResultPanel>();
    var calls = 0;
    panel.ActionRequested += _ => calls++;
    panel.Show(new MinigameResult(true, 8f, Rank.A), "Map");
    panel.Continue();
    panel.Continue();
    Assert.That(calls, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run the focused tests and verify RED.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s5-task1-red.xml -logFile /tmp/kma-s5-task1-red.log -testFilter "CoreLoopTests"
```

Expected: the new route-preview or one-shot assertions fail because the APIs/guards are missing; compiler errors must be fixed before interpreting test failures.

- [ ] **Step 3: Implement one shared route helper.** Refactor only the route decision in `GameSession`:

```csharp
SessionRoute RouteForResult(MinigameResult result) => result.Pass
    ? SessionRoute.Map
    : visitAttempt == 1
        ? SessionRoute.Punishment
        : Lives <= 1 ? SessionRoute.GameOver : SessionRoute.Map;
```

Call `RouteForResult` from both `PreviewRoute` and `SubmitResult`; keep mutation in `SubmitResult` only.

- [ ] **Step 4: Add the result one-shot guard and route handoff.** `ResultPanel.Show` clears `HasContinued`; `Continue` sets it before invoking `ActionRequested`. The scene-level result handler must call `PreviewRoute` for display and call `SubmitResult` only from the Continue callback.

- [ ] **Step 5: Run focused tests and the existing progression suite.** Expected: CoreLoop tests and all existing `FullGameplayFlowTests` pass; failed visits still decrement a life only on attempt two.

- [ ] **Step 6: Commit the isolated contract change.** Run `rtk git diff --check`, then commit only Task 1-owned files as `feat: add S5 route preview and result sequencing`.

### Task 2: Build MainMenu, Map, Settings, Calibrate, and GameOver shell

**Files:**
- Create/modify: `Assets/_Project/Scripts/UI/{MainMenuScreen,MapScreen,SettingsScreen,CalibrateScreen,GameOverScreen}.cs`
- Modify: `Assets/_Project/Scripts/Core/GameManager.cs` and `Assets/_Project/Scripts/Progression/GameSession.cs` with additive reset APIs
- Modify: `Assets/_Project/Scenes/Menu.unity`, `Assets/_Project/Scenes/Map.unity`, `Assets/_Project/Scenes/GameOver.unity`
- Create/modify: `Assets/Tests/PlayMode/Progression/S5NewGameTests.cs`

**Interfaces:**
- `GameManager.StartNewGame()` resets campaign records/lives, preserves `Settings` and tutorial flags, saves immediately, and routes to `Menu`.
- `MapScreen.SelectSubject(SubjectId)` calls `SceneRouter.StartSubject`; `SelectBoss()` calls `SceneRouter.StartBoss` and is disabled while `BossUnlocked == false`.
- `CalibrateScreen.SetOffset(float)` clamps to `[-500, 500]` milliseconds and calls `GameManager.UpdateSettings` with the updated `rhythmOffsetMs`.

- [ ] **Step 1: Write failing New Game and screen-event tests.** Verify a passed Sprint record and reduced lives are cleared while the same `Settings` reference/values and tutorial flags survive; verify Map emits the selected subject and Boss event without owning route state.

- [ ] **Step 2: Run the focused tests and verify RED.** Run the `S5NewGameTests` filter; expected failure is missing `ResetCampaign`, `StartNewGame`, or screen event APIs.

- [ ] **Step 3: Implement reset and screen contracts.** Use `GameSession.ResetCampaign()` to set lives to `5`, replace every `SubjectRecord`, and clear active attempt state. Do not reset `Settings` or tutorial flags in `GameSession`; `GameManager` owns those fields and persists them after reset.

- [ ] **Step 4: Assemble `Menu.unity`.** Add visible MainMenu buttons for Play, Continue, New Game, Settings, and Quit; require confirmation before New Game. Settings exposes music volume, SFX volume, vibration, and Calibrate; Calibrate displays the current offset and writes through `UpdateSettings`.

- [ ] **Step 5: Assemble `Map.unity`.** Create seven playable nodes plus three presentation-only locked nodes. Each playable node reads its `SubjectConfig`, shows lock/best rank/derived stars, and displays the shared five-heart bar. Use `ScoreUtil.ToStars(BestRank)`; never persist stars.

- [ ] **Step 6: Assemble `GameOver.unity` and run PlayMode tests.** Provide Retry/New Game and MainMenu actions. Verify the shell scenes have a tagged orthographic camera, Canvas Scaler `1920×1080`/match `1.0`, and `SafeAreaFitter`.

- [ ] **Step 7: Commit** `feat: add KMA campaign shell screens` after focused and full EditMode/PlayMode tests pass.

### Task 3: Create seven playable routes and five placeholder scenes

**Files:**
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Common/PlaceholderMinigameController.cs`
- Create/modify: `Assets/_Project/Scenes/MG_Volleyball.unity`, `MG_Basketball.unity`, `MG_PingPong.unity`, `MG_Badminton.unity`, `MG_Football.unity`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Create/modify: `Assets/Tests/PlayMode/Progression/PlaceholderSceneTests.cs`

**Interfaces:**
- `SceneRouter.DefaultSubjectScenes()` contains exactly seven mappings: Sprint, Endurance, Volleyball, Basketball, PingPong, Badminton, Football.
- `PlaceholderMinigameController : MinigameBase` exposes `DebugPass()` and `DebugFail()`, emits one normalized result, and ignores later debug calls after resolve.

- [ ] **Step 1: Write failing route and binding tests.** For every `SubjectId`, assert `TryGetSceneName(Subject)` and `TryGetSceneName(RetrySubject)` resolve a build-enabled scene. Load each of the five new scenes and assert exactly one placeholder controller, one camera, one HUD, and one completion event.

- [ ] **Step 2: Run the focused tests and verify RED.** Expected failure is missing mappings, missing build settings, or empty scenes that contain no `MinigameBase`.

- [ ] **Step 3: Implement the controller with the existing lifecycle.** `ConfigureForTest` advances the tutorial/countdown lifecycle for harnesses; `DebugPass` and `DebugFail` call the protected `Finish` method with score `6`/rank `C` or score `0`/rank `F`. Do not modify any subject rules.

- [ ] **Step 4: Generate each scene through an Editor script.** The generator must create a tagged orthographic `GameplayCamera`, shared HUD/phase/result prefabs, EventSystem/input surface, exactly one `PlaceholderMinigameController`, and `GameplayPresentation`; save the scene under its exact required name.

- [ ] **Step 5: Register scenes and verify automatic binding.** Add all five paths to `EditorBuildSettings.asset`; after `SceneRouter` loads a subject, `OnSceneLoaded` must bind the scene’s controller and clear the awaiting flag only after at least one controller is found.

- [ ] **Step 6: Run route, placeholder, presentation, and full progression tests.** Expected: seven routes resolve, the Boss unlock test passes after seven placeholder passes, and unsupported-route assertions are removed/replaced with the seven-subject contract.

- [ ] **Step 7: Commit** `feat: add seven-subject placeholder routes`.

### Task 4: Wire Punishment and pause/resume behavior

**Files:**
- Modify: `Assets/_Project/Scripts/Core/PunishmentSceneController.cs`
- Modify: `Assets/_Project/Scripts/Progression/PunishmentController.cs`
- Create/modify: `Assets/_Project/Scripts/UI/PausePanel.cs`
- Modify: `Assets/_Project/Scenes/Punishment.unity` and every gameplay scene
- Create/modify: `Assets/Tests/PlayMode/Progression/PunishmentRouteTests.cs` and `Assets/Tests/PlayMode/Core/PauseFlowTests.cs`

**Interfaces:**
- Punishment uses the existing `TapMashDetector`, `RhythmBeatDetector`/`HoldDetector`, and `AlternateTapDetector`; `PunishmentSceneController` remains the only scene-facing adapter.
- `PausePanel.Open()` stores the current time scale, sets `Time.timeScale = 0`, and `Resume()` restores it; Restart and ExitToMap raise distinct actions.
- Endurance/Boss pause integration stores the DSP elapsed offset and resumes from the same beat schedule without resetting the song clock.

- [ ] **Step 1: Write failing tests.** Cover first failure → Punishment → RetrySubject, detector progress across all authored steps, completion once, pause/resume time scale, and no DSP jump after pause.

- [ ] **Step 2: Run focused tests and verify RED.** Expected failure is missing pause actions or a route that completes punishment twice.

- [ ] **Step 3: Bind punishment UI to the existing controller methods.** The visible mechanic cue must match `sequence.Current.Mechanic`; progress reads `CurrentProgress`; tap, hold-release, and alternating touch zones call the already-defined public methods.

- [ ] **Step 4: Add pause UI to all gameplay scenes.** Anchor the pause button top-right in the safe area. Restart clears the active attempt through the router/session flow; ExitToMap rejects the unfinished attempt and routes to Map through the existing route owner.

- [ ] **Step 5: Run punishment, pause, route, and full suites.** Expected: no duplicate completion, no soft-lock after punishment, and existing Endurance/Boss timing tests remain green.

- [ ] **Step 6: Commit** `feat: wire S5 punishment and pause flow`.

### Task 5: Execute the S5 device gate and handoff

**Files:**
- Verify: all S5 scenes, `ProjectSettings/EditorBuildSettings.asset`, `README.md`
- Create: `docs/qa/s5-core-loop-device-gate.md`

**Interfaces:** The gate is evidence for the shell only; it does not claim the final S16 Definition of Done.

- [ ] **Step 1: Run complete EditMode and PlayMode XML suites.** Record exact totals and keep the XML files under `/tmp`; no compiler errors or unexpected log errors are allowed.

- [ ] **Step 2: Build and install Android.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -buildOutput Builds/Android/kma-s5.apk -logFile /tmp/kma-s5-build.log -quit
rtk adb install -r Builds/Android/kma-s5.apk
```

- [ ] **Step 3: Record the progression gate.** On-device verify Menu → Map → subject → first failure → Punishment → RetrySubject → second failure → life loss → Map, repeat to zero lives, and confirm GameOver.

- [ ] **Step 4: Record the unlock/persistence gate.** Pass all seven placeholders, confirm Boss unlocks and `MG_Boss` loads, kill/relaunch after a partial run, and verify lives/records restore while settings/tutorial flags remain.

- [ ] **Step 5: Record pause/input gate.** Pause during a subject, verify Resume, Restart, and ExitToMap; test Endurance/Boss beat continuity after pause and calibrate a non-zero rhythm offset.

- [ ] **Step 6: Update QA notes and commit.** Document device model/API, build result, any unavailable checks, and exact known S5 limitations. Run `rtk git diff --check`; commit `feat: close the playable campaign shell` only when the device gate is evidenced.

## Self-Review Checklist

- S5-1 is covered by Tasks 1–2: Map scene ownership, Menu screens, New Game/Continue, settings, calibration, GameOver.
- S5-2 is covered by Task 1: pure preview, shared route helper, consequence-before-Continue, one completion event.
- S5-3 is covered by Task 3: five named scene stubs, controller binding, debug Pass/Fail, seven routes/build settings.
- Punishment and DSP pause requirements are covered by Task 4.
- The exact device gate is covered by Task 5; S5 remains a checkpoint and does not remove placeholders from the final product.
- No step relies on `TBD`, unassigned validation, or a type/method not defined in this plan or the current source contracts.

Plan complete and saved to `docs/superpowers/plans/2026-08-31-s5-shell-core-loop.md`. Two execution options:

1. Subagent-Driven (recommended) — dispatch a fresh subagent per task with review checkpoints.
2. Inline Execution — execute the tasks in this session using `superpowers:executing-plans`.
