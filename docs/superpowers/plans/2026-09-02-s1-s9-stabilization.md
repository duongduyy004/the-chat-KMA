# S1-S9 Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `master` to a reproducible, green, end-to-end S1-S9 checkpoint in which the configured Unity/Android foundation is present, Menu/Continue persistence is truthful, Sprint and Endurance regressions are fixed, S8 is integrated, and Volleyball is playable from Map through scoring, Result, routing, and save.

**Architecture:** Keep `GameSession` authoritative for campaign and in-progress attempt state, `SceneRouter` authoritative for transitions, `GameManager` authoritative for persistence/settings/tutorial flags, and each rules engine authoritative for score/result. Port the already-reviewed S1 and S8 work by file responsibility rather than merging their divergent branches wholesale; then complete S9 through the existing `BallRig`/`VolleyballRules` boundaries. Every repair begins with a focused failing test and ends with a reviewer gate before the next dependency is started.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, Unity Input System + EnhancedTouch, uGUI/TextMeshPro, URP 2D `17.3.0`, Unity Test Framework `1.6.0`, Android ARM64.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md`, sections S1-S9 and Definition of Done.

## Global Constraints

- Work directly on the current shared `master`; do not create or switch Git worktrees.
- Prefix every shell command with `rtk`.
- Preserve `LoadSceneMode.Single`, scene names/build indices, normalized `MinigameResult`, and the existing two-attempt/five-life campaign rules.
- `SceneRouter` remains the only production scene-transition owner; `SubjectConfig` must not gain scene names.
- Save data remains `Application.persistentDataPath/save.json`, staged through `save.tmp`; settings and tutorial flags remain in that JSON, never PlayerPrefs.
- Detectors remain plain C# classes fed by one `GameplayInputRouter`; UI controls must not leak into gameplay input.
- `BallRig`/`Ballistics` own flight physics. Presentation may read prediction and snapshots but must not mutate position/velocity.
- Android remains landscape, ARM64, IL2CPP, package name `com.kma.thechat`, product name `Thể Chất KMA`, minimum API 23, target API 35.
- Do not accept a focused green result while either full EditMode or full PlayMode has a failure.
- Unity test runs currently regenerate `Assets/_Project/Fonts/Baloo2-ExtraBold.asset`; each task must check `git status` and must not commit this incidental churn.

## Review Baseline (2026-09-02)

- `master` is clean at `c726b61` and is 111 commits ahead of `origin/master`.
- Full EditMode: `/tmp/kma-cleanup-editmode.xml`, 192 passed, 0 failed.
- Full PlayMode: `/tmp/kma-cleanup-playmode.xml`, 125 passed, 3 failed.
- The three PlayMode failures reproduce independently:
  - `EnduranceInputBridgeTests.RouterEvents_OnlyReachControllerForTheirMatchingInputMode`: expected 2 taps, observed 1.
  - `GameplayInputRouterTests.EnhancedTouchScreenTapArea_RoutesTouchRhythmAndEnforcesOwnership`: expected 1 judgment, observed 0.
  - `SprintControllerTests.SprintScene_AuthorsThreeCosmeticRivalsAndThreeLayerParallax`: `Runner_04` is not recognized as a prefab instance by Unity.
- The two input failures share one source: `GameplayInputRouter.FeedPointerDown` dispatches rhythm only when `gameplayActionMap` is non-null and named `Endurance`, although the detector contract and test seam do not require an action asset.
- S1 implementation/config tests exist only on `codex/s1-toolchain`; current `master` still has Multiplayer Center, no configured render pipeline, DSP buffer `0`, README `6000.3.22f1`, and no S1 config test assembly.
- S8 implementation/tests/assets exist only on `codex/s8-ball-presentation-kit`; `master` has none of `TrajectoryPreview`, `BallShadow`, `BallPresentation.prefab`, or the five authored `FlightProfile` assets.
- `MG_Volleyball.unity` still contains `PlaceholderMinigameController`. `VolleyballController` never awards either side a point, and its tests award points directly through `VolleyballRules`, so the production controller cannot reach a normal pass.
- `SaveData` does not contain active subject/attempt/punishment state; `GameSession.Restore` clears it; `Continue` always routes to Map. The documented kill/relaunch continuation gate is therefore not represented by the model.
- `PhaseOverlay` shows multi-step tutorials, but `MinigameLifecycle.Tick` advances out of Tutorial after two seconds without waiting for Skip/Close. The user-controlled tutorial contract is therefore only visual, not lifecycle-authoritative.
- `SceneRouter` mutates session/binding state before the transition guard can return `false`; a duplicate route request during loading can leave the first destination unbound or an attempt active without a matching scene.

---

### Task 1: Reconcile and lock the S1 toolchain/configuration contract

**Files:**
- Port and reconcile: `Assets/Editor/{BuildScript,ProjectConfigurator,UrpBootstrap}.cs` and their `.meta` files from `codex/s1-toolchain`
- Port: `Assets/Editor/KMA.EditorTools.asmdef` and `.meta`
- Port: `Assets/Tests/EditMode/Config/{KMA.Config.EditMode.Tests.asmdef,PackageManifestTests,ProjectLayoutTests,ProjectSettingsTests,RenderPipelineTests}.cs` and metadata
- Port: `Assets/_Project/Settings/URP/{URP-2D.asset,URP-2D_Renderer2D.asset,DefaultVolumeProfile.asset}` and metadata
- Modify: `Packages/manifest.json`, `Packages/packages-lock.json`
- Modify through editor APIs: `ProjectSettings/{ProjectSettings,GraphicsSettings,QualitySettings,AudioManager}.asset`
- Modify: `README.md`

**Interfaces:**
- Produces `KMA.EditorTools.ProjectConfigurator.Apply()` and `KMA.EditorTools.UrpBootstrap.CreateOrRepair()` as reproducible headless configuration entry points.
- Produces `KMA.EditorTools.BuildScript.BuildAndroid()` with `-buildOutput` support.
- Establishes tests that read Unity APIs and exact package JSON instead of inferring enum values from YAML.

- [ ] **Step 1: Port only the S1 tests and editor utilities, not the divergent branch history.** Use `git show codex/s1-toolchain:<path>` as the review source; apply the files to current `master` while preserving newer S2-S9 runtime code and plans. Do not merge or cherry-pick the entire branch.

- [ ] **Step 2: Run the S1 tests and record RED.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Config -testResults /tmp/kma-stabilize-s1-red.xml -logFile /tmp/kma-stabilize-s1-red.log
```

Expected failures must name the current contract drift: `com.unity.multiplayer.center`, missing `com.unity.2d.sprite`, unassigned URP 2D pipeline, incorrect DSP buffer, or Android settings.

- [ ] **Step 3: Reconcile package and ProjectSettings values.** Make the test-observable result exactly:

```text
Unity editor       6000.3.23f1
URP                17.3.0
uGUI               2.0.0
2D Sprite          1.0.0
Multiplayer Center absent
DSP buffer         256
Android ABI        ARM64 only
Scripting backend  IL2CPP
Min/target API     23 / 35
Package ID         com.kma.thechat
Orientation        landscape
URP renderer       Renderer2DData; HDR and post-processing disabled
```

Run `ProjectConfigurator.Apply` and `UrpBootstrap.CreateOrRepair` headlessly; do not hand-edit Unity enum values in YAML.

- [ ] **Step 4: Make README a current contract, not historical evidence.** Update the version, playable routes (Sprint, Endurance, Volleyball only after Task 8), current test command, and separate historical S2 evidence from the latest verified counts. Do not claim an Android/device result until Task 9.

- [ ] **Step 5: Run GREEN and full regression.** Run the focused S1 filter, full EditMode, and a compile-only import. Confirm `Packages/manifest.json` has no Multiplayer Center and `GraphicsSettings` resolves the authored URP asset through Unity API tests.

- [ ] **Step 6: Commit and review.** Commit only S1/config files as `fix: reconcile S1 toolchain contract`; request code review before Task 2.

### Task 2: Make the TMP font foundation deterministic and worktree-clean

**Files:**
- Modify/rebuild: `Assets/_Project/Fonts/Baloo2-ExtraBold.asset`
- Modify if required: `Assets/_Project/Fonts/TMPFallback.asset`
- Modify: `Assets/Tests/EditMode/Presentation/VietnameseFontTests.cs`
- Create: `Assets/Tests/EditMode/Presentation/FontAssetStabilityTests.cs`

**Interfaces:**
- The primary font asset keeps a stable object name matching its file, has pre-authored Vietnamese glyphs, and does not serialize dynamic glyph/kerning-table changes during test runs.
- `TMPFallback.asset` remains the fallback for glyphs not owned by Baloo 2.

- [ ] **Step 1: Add a failing stability contract.** Assert the loaded primary asset name is `Baloo2-ExtraBold`, the required glyph set `Đ đ ă Ă ộ ơ Ư ứ` is available, and the shipping asset uses a deterministic population strategy. Keep the existing fallback assertions.

- [ ] **Step 2: Prove the current mismatch.** Run the font tests and capture that the serialized object is currently named `Baloo2-ExtraBold.task3tmp` and uses dynamic atlas population.

- [ ] **Step 3: Rebuild through TMP APIs.** Generate the required Baloo atlas/character table once, set the shipping asset to a non-mutating population mode, rename the serialized object and material consistently, and retain fallback coverage. Do not manually edit the 60k-line character/kerning tables.

- [ ] **Step 4: Prove worktree stability.** Record the hash, run the focused font tests twice in separate Unity invocations, then compare:

```bash
rtk git diff --exit-code -- Assets/_Project/Fonts/Baloo2-ExtraBold.asset
```

Expected: no diff after either run.

- [ ] **Step 5: Commit and review.** Commit as `fix: stabilize Vietnamese TMP font asset`; request code review.

### Task 3: Repair shared pointer-to-rhythm dispatch and restore a green S3/S7 baseline

**Files:**
- Modify: `Assets/_Project/Scripts/Input/GameplayInputRouter.cs:438-458`
- Test: `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs:438-454`
- Test: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceInputBridgeTests.cs:87-113`

**Interfaces:**
- A pointer down owned by `ScreenTapArea` feeds an installed `RhythmBeatInputDetector` exactly once, independent of whether an `InputActionAsset` was configured.
- Installing no rhythm detector produces no rhythm event, so Sprint/Boss/Punishment remain isolated by detector ownership rather than an action-map name check.

- [ ] **Step 1: Keep the two existing focused failures as RED and add a negative ownership assertion.** In `GameplayInputRouterTests`, install only tap/hold/swipe detectors and assert a pointer down does not synthesize an `OnRhythmJudge` event.

- [ ] **Step 2: Run the three focused tests.** Expected before implementation: the two current rhythm tests fail with `1 vs 2` and `0 vs 1`; the no-detector test passes.

- [ ] **Step 3: Implement the minimal root fix.** Replace the action-map gate at pointer dispatch with detector ownership:

```csharp
if (rhythmBeatDetector != null)
    FeedRhythmTap(AudioSettings.dspTime, RhythmBeatDsp);
```

Do not change keyboard map filtering in `OnTapPerformed` or `OnRhythmPerformed`.

- [ ] **Step 4: Run focused and full PlayMode.** The two input failures must pass; only the known Sprint prefab failure may remain at this point.

- [ ] **Step 5: Commit and review.** Commit as `fix: restore shared touch rhythm dispatch`; request code review.

### Task 4: Re-author the Sprint rivals as valid prefab instances

**Files:**
- Create: `Assets/Editor/SprintSceneConfigurator.cs` and `.meta`
- Modify through editor API: `Assets/_Project/Scenes/MG_Sprint.unity`
- Test: `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs:175-205`

**Interfaces:**
- `SprintSceneConfigurator.CreateOrRepairRivals()` opens `MG_Sprint`, loads `Assets/_Project/Prefabs/Gameplay/RivalRunner.prefab`, and ensures exactly three prefab instances named `Runner_01`, `Runner_03`, `Runner_04` on lanes `1`, `3`, `4` with their authored profile assets and controller reference.
- The configurator is idempotent and saves only when it repairs an invalid/missing instance.

- [ ] **Step 1: Strengthen RED diagnostics.** Sort rivals by lane before assertions and report name, lane, corresponding source, and nearest prefab path for every rival. Assert all three mappings rather than stopping at whichever Unity returns first.

- [ ] **Step 2: Run the focused test and retain the failing object evidence.** Expected: Unity reports `Runner_04` with no corresponding prefab source even though hand-authored YAML contains a `PrefabInstance` block.

- [ ] **Step 3: Repair with Unity prefab APIs.** Use `PrefabUtility.InstantiatePrefab`, assign serialized fields through `SerializedObject`, and `EditorSceneManager.SaveScene`; never fabricate prefab file IDs in YAML.

- [ ] **Step 4: Run the configurator twice.** The first invocation repairs the scene; the second must produce no scene diff.

- [ ] **Step 5: Run the focused Sprint test and full PlayMode.** Expected after Tasks 3-4: all PlayMode tests pass.

- [ ] **Step 6: Commit and review.** Commit as `fix: reauthor sprint rival prefab instances`; request code review.

### Task 5: Persist and resume the in-progress campaign route

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/SaveData.cs`
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Core/SaveSystem.cs`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Modify: `Assets/_Project/Scripts/Core/GameManager.cs`
- Modify: `Assets/_Project/Scripts/Shell/S5ShellSceneController.cs`
- Modify: `Assets/_Project/Scripts/UI/MainMenuScreen.cs`
- Test: `Assets/Tests/EditMode/Progression/{SaveSystemTests,GameSessionPersistenceTests}.cs`
- Test: `Assets/Tests/PlayMode/Progression/{S5NewGameTests,FullGameplayFlowTests}.cs`

**Interfaces:**
- Bump `SaveData.CurrentVersion` to `2` and add `bool hasActiveSubject`, `SubjectId activeSubject`, `int visitAttempt`, `bool awaitingPunishment`.
- Expose read-only `GameSession.VisitAttempt` and `GameSession.AwaitingPunishment`.
- Add `SessionRoute GameSession.ResumeRoute()` returning `Map` when no attempt is active, `Punishment` when awaiting punishment, `Subject` for attempt 1, and `RetrySubject` for attempt 2.
- Add `event Action SessionChanged` to `SceneRouter`; raise it after every successful session mutation and before/with its transition.
- Add `bool SceneRouter.ResumeCampaign()`; it routes the restored state without calling `StartSubject` a second time.
- `GameManager` saves on `SessionChanged`; `S5ShellSceneController` routes Continue through `ResumeCampaign()`.
- Public mutating route methods reject while `IsTransitioning` before changing `GameSession`, `activeSubject`, or awaiting-binding flags.

- [ ] **Step 1: Write failing round-trip cases.** Cover kill/recreate at: active attempt 1, awaiting Punishment, active attempt 2 after punishment, and no active attempt. Assert `ResumeRoute`, lives, records, and subject identity survive.

- [ ] **Step 2: Write migration and shell RED tests.** A version-1 save must migrate to no active attempt; a version-2 save must retain it. Continue from each state must request exactly one of Map/Punishment/Subject/RetrySubject without resetting attempt counters.

- [ ] **Step 3: Implement schema export/restore.** `ToSaveData` writes the four fields; `Restore` validates `visitAttempt` as `1..2`, requires `hasActiveSubject` for punishment/attempt state, and falls back to no active attempt for structurally invalid combinations.

- [ ] **Step 4: Save every route-changing mutation.** Raise `SessionChanged` after successful start, result submission (including first failure), punishment completion, restart, abandon/exit, and campaign reset. Do not rely only on `SubjectCompleted`/`LifeLost`, because they miss the first-failure and active-attempt boundaries.

- [ ] **Step 5: Make transition rejection transactional.** Add tests that hold a transition open, then call `StartSubject`, `SubmitSubjectResult`, Restart, Exit, and Continue again. Each rejected call must return `false` without changing active subject, attempt, lives, records, pending binding, or emitted persistence events. Perform the `IsTransitioning` check before every session mutation.

- [ ] **Step 6: Implement truthful Continue.** Configure MainMenu from `SaveSystem`/loaded state so Continue is enabled only when a save existed; keep New Game confirmation and settings/tutorial preservation. Continue calls `SceneRouter.ResumeCampaign()` and never always routes to Map.

- [ ] **Step 7: Run persistence, shell, full EditMode, and full PlayMode suites.** Add an end-to-end PlayMode test that serializes after first failure, recreates the manager/router/session, invokes Continue, and reaches Punishment with the same subject.

- [ ] **Step 8: Commit and review.** Commit as `fix: persist and resume campaign attempts`; request code review.

### Task 6: Remove the second tutorial persistence source

**Files:**
- Modify: `Assets/_Project/Scripts/UI/TutorialSeenStore.cs`
- Modify: `Assets/_Project/Scripts/UI/TutorialOverlay.cs`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Common/{MinigameBase,MinigameLifecycle}.cs`
- Modify: `Assets/Tests/EditMode/Presentation/TutorialOverlayTests.cs`
- Modify: `Assets/Tests/PlayMode/Presentation/PhaseFlowTests.cs`
- Modify: `Assets/Tests/PlayMode/Core/S4BootstrapPersistenceGateTests.cs`

**Interfaces:**
- `SaveDataTutorialSeenStore` reads/writes valid `SubjectId` values only through `GameManager` and `save.json`.
- When no initialized `GameManager` exists (direct-scene development/tests), fallback is explicitly in-memory and cannot write PlayerPrefs.
- `TutorialOverlay` emits one completion event on Skip/Close; an unseen interactive tutorial keeps `MinigameLifecycle` in Tutorial until that event, while an already-seen tutorial releases immediately into Countdown.

- [ ] **Step 1: Add a failing no-PlayerPrefs test.** Clear a sentinel `KMA.tutorialSeen.Sprint`, complete a direct-scene tutorial without `GameManager`, and assert PlayerPrefs remains absent while the injected memory store records the test state.

- [ ] **Step 2: Remove `PlayerPrefsTutorialSeenStore` from production fallback.** Default to `MemoryTutorialSeenStore`; retain constructor injection for tests. Valid production subjects still call `GameManager.MarkTutorialSeen`, which saves immediately.

- [ ] **Step 3: Add the interactive lifecycle RED test.** Bind an unseen three-step overlay, tick the minigame beyond two seconds, and assert it remains in Tutorial. Invoke Next twice and Close, then assert exactly one completion event, one transition to Countdown, and persisted seen state. Bind the same subject again and assert it skips directly to Countdown without displaying the overlay.

- [ ] **Step 4: Add an explicit tutorial gate to lifecycle.** `PhaseOverlay` sets the gate only when `TutorialOverlay.ShouldShow`; Skip/Close releases it through a public additive method on `MinigameBase`. `MinigameLifecycle.Tick` must not consume countdown time while the tutorial gate is closed, and repeated completion calls must be idempotent.

- [ ] **Step 5: Run tutorial, Bootstrap persistence, Sprint/Endurance phase gates, and full suites.** Verify New Game/reset preserves `tutorialSeen` in JSON, no PlayerPrefs key is created, and gameplay cannot start underneath an open tutorial.

- [ ] **Step 6: Commit and review.** Commit as `fix: make tutorials gate lifecycle and save data`; request code review.

### Task 7: Integrate the S8 ball presentation kit onto stabilized master

**Files:**
- Port/reconcile from `codex/s8-ball-presentation-kit`: `Assets/_Project/Scripts/Gameplay/Ball/{TrajectoryPreview,BallShadow}.cs` and metadata
- Reconcile: `Assets/_Project/Scripts/Gameplay/Ball/BallRig.cs`
- Port: `Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab` and metadata
- Port: `Assets/_Project/Materials/Gameplay/*` and folder metadata
- Port: `Assets/_Project/ScriptableObjects/Ball/FlightProfile_{Volleyball,Basketball,PingPong,Shuttle,Football}.asset` and metadata
- Port: `Assets/Tests/EditMode/Gameplay/Ball/{TrajectoryPreviewTests,FlightProfileTests}.cs` and metadata
- Port/rewrite evidence: `docs/qa/s8-ball-presentation-kit.md`

**Interfaces:**
- `TrajectoryPreview.Configure(BallRig, LineRenderer, int sampleCount, float sampleStep)`, `SetVisible(bool)`, and `Refresh(Vector2 direction, float force, float curvature)` render prediction without changing the ball.
- `BallShadow.Configure(Transform target, Transform shadow, SpriteRenderer renderer, ...)` derives position/scale/alpha from height only.
- `BallPresentation.prefab` provides one `BallRig`, one preview, and one shadow with valid references.

- [ ] **Step 1: Port tests first and run RED.** Expected failures are missing types/assets on `master`.

- [ ] **Step 2: Port by file responsibility.** Apply the 31-file S8 diff from the branch, but manually reconcile `BallRig.cs` against current S9-era code. Do not merge the branch or overwrite Tasks 1-6.

- [ ] **Step 3: Run S8 focused tests.** Verify preview endpoints match `Ballistics.PredictGround`, preview calls are immutable with respect to body state, invalid configuration logs once, shadow height is deterministic, and all five profiles satisfy their authored values.

- [ ] **Step 4: Run full EditMode/PlayMode and prefab inspection.** Load `BallPresentation.prefab` through Unity and assert no missing script/reference. Rewrite QA evidence with current paths/counts instead of copying stale branch claims.

- [ ] **Step 5: Commit and review.** Commit as `feat: integrate S8 ball presentation kit`; request code review.

### Task 8: Complete Volleyball rally scoring, presentation, and production scene

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/{VolleyballController,VolleyballRules,VolleyReturnPattern}.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs` and `.meta`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs`
- Modify: `Assets/_Project/Scenes/MG_Volleyball.unity`
- Modify: `Assets/Tests/PlayMode/Gameplay/Ball/Volleyball/VolleyballControllerTests.cs`
- Create: `Assets/Tests/PlayMode/Gameplay/Ball/Volleyball/VolleyballSceneTests.cs` and `.meta`
- Create: `Assets/Tests/PlayMode/Progression/VolleyballCampaignTests.cs` and `.meta`

**Interfaces:**
- Add a controller possession state with touch number `1..3`, expected dig/set/spike phase, completed-rally count, and one in-flight owner.
- A valid `Dig → Set → Spike` sequence awards exactly one player rally point after the authored landing/return resolution, resets touch number, and starts the next deterministic opponent return.
- Invalid/out-of-reach/timing input or own-court ground contact awards at most one opponent point and resets possession.
- Counterplay unlocks after three completed rallies, shows cue at least `VolleyReturnPattern.CueLeadSeconds` before launch, and never mutates a launched trajectory.
- `VolleyballHud` displays `TOUCH n/3`, player-opponent score, longest combo, context, timing feedback, and counter cue; generic lifecycle/timer remains in shared HUD.

- [ ] **Step 1: Replace test-only scoring with production-flow RED tests.** Remove direct `Rules.AwardRallyPoint()` calls from controller completion tests. Add cases proving a real three-touch sequence changes score, a failed possession changes opponent score, target score completes once, timeout fails once, fourth touch is impossible, and the ball resets/reattaches between possessions.

- [ ] **Step 2: Add S8 integration RED tests.** Configure preview/shadow once, show preview only during gesture preparation while attached, hide on launch, and assert auto-positioning/presentation never changes body position or velocity.

- [ ] **Step 3: Implement the possession state machine.** Resolve each swipe once through `VolleyballRules.TryResolveAndLaunch`; subscribe once to `BallRig.Collided` or explicit court trigger events to resolve the possession; centralize point award/reset in one method guarded by a possession token so duplicate collisions cannot double-score.

- [ ] **Step 4: Implement terminal result ownership.** Tick rules only during Play; after any point or deadline, if target/timeout is terminal, call `Finish(Rules.BuildResult())` once. The controller—not tests or HUD—must be the production caller of `AwardRallyPoint`/`AwardOpponentPoint`.

- [ ] **Step 5: Implement HUD and tutorial content.** Add three steps through the existing overlay:

```text
DIG   Swipe down when the ball is low.
SET   Swipe up while the ball is rising.
SPIKE Swipe toward the net near the apex.
```

Store completion under `SubjectId.Volleyball` through Task 6's JSON-backed store.

- [ ] **Step 6: Author the production scene.** Replace only the Volleyball placeholder with exactly one `VolleyballController`; instantiate `GameCamera.prefab` and `BallPresentation.prefab`; add visible court/net, player/teammate/opponents, reach/court scoring triggers, one shared input router/surface, generic HUD, Volleyball HUD, tutorial, result, and pause. Keep scene name/build index unchanged.

- [ ] **Step 7: Add scene and campaign gates.** Assert no `PlaceholderMinigameController`, one active physics ball, valid S8 references, all HUD labels/tutorial steps, and a playable `Map → Volleyball → Result Continue → Map/Punishment` route that persists the resulting record/attempt.

- [ ] **Step 8: Run focused and full suites.** Run Volleyball rules/controller/scene/campaign filters, Ball tests, full EditMode, and full PlayMode. Expected: zero failures.

- [ ] **Step 9: Commit and review.** Split commits into `feat: complete volleyball rally flow` and `feat: author volleyball scene and HUD`; request code review after each.

### Task 9: Execute the S1-S9 release gate and reconcile documentation

**Files:**
- Create: `docs/qa/s1-s9-stabilization-gate.md`
- Modify: `README.md`
- Modify only if test-discovered: source/test files from the owning task; do not bundle unrelated cleanup

**Interfaces:**
- QA evidence records commands, XML/log paths, exact totals, commit SHA, APK path/hash/size, device model/API/ABI, and unavailable checks without upgrading them to pass claims.

- [ ] **Step 1: Run full clean verification.** From a clean status, run full EditMode and PlayMode twice. Confirm zero failures, no tracked font/scene/config churn, no duplicate asmdefs, and `rtk git diff --check` passes.

- [ ] **Step 2: Run end-to-end desktop/Editor smoke.** Verify Bootstrap → Menu → Continue/New Game → Map → Sprint/Endurance/Volleyball → Result → first failure Punishment → retry → pass/life loss → Map. Kill/recreate at active attempt 1, Punishment, and attempt 2; Continue must restore each exact route.

- [ ] **Step 3: Build Android.** Run:

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -buildOutput Builds/Android/kma-s1-s9.apk -logFile /tmp/kma-s1-s9-build.log -quit
```

Inspect the APK for ARM64-only native libraries, package/product/orientation settings, size, and build result.

- [ ] **Step 4: Run the physical-device gate when a device is available.** Install with `rtk adb install -r`, then verify touch ownership, Vietnamese glyphs, safe area, audio/haptics settings, pause/resume/restart/exit, Volleyball three-touch scoring/counter cue, save/Continue after process kill, FPS, and draw calls. If no device is connected, mark each physical/performance item unavailable and leave the gate incomplete.

- [ ] **Step 5: Reconcile README and QA.** Replace stale Unity/test/route counts with current evidence; state that S1-S9 is a checkpoint and S10-S16 remain outside this plan. Do not retain claims contradicted by XML/log/device evidence.

- [ ] **Step 6: Final review and commit.** Request a final S1-S9 code review against the spec, resolve all Critical/Important findings, rerun affected tests, then commit evidence/docs as `test: verify S1-S9 stabilization gate`.

## Verification Commands

```bash
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s1-s9-editmode.xml -logFile /tmp/kma-s1-s9-editmode.log
rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s1-s9-playmode.xml -logFile /tmp/kma-s1-s9-playmode.log
rtk git diff --check
rtk git status --short
```

## Plan Self-Review

- Spec coverage: S1 config/Android is Task 1; S2 font/worktree stability is Task 2; S3/S7 input is Task 3; S6 scene regression is Task 4; S4/S5 persistence, transactional routing, and tutorial lifecycle ownership are Tasks 5-6; S8 is Task 7; S9 is Task 8; accumulated gate/device evidence is Task 9.
- Dependency order: S1 supplies editor/config tooling; S3/S6 restore the green baseline; persistence must be trustworthy before the Volleyball campaign gate; S8 must exist before S9 scene authoring.
- Type consistency: `SaveData` v2 fields map directly to `GameSession` state; `ResumeRoute()` feeds `SceneRouter.ResumeCampaign()`; `VolleyballController` remains the only bridge from ball/court events to rules point APIs.
- Branch safety: neither divergent section branch is merged wholesale; only reviewed file responsibilities are ported and revalidated against current `master`.
- Completion condition: this plan is not complete until full EditMode and PlayMode are green, the worktree remains clean after repeated tests, and Android/device limitations are recorded without unsupported success claims.
