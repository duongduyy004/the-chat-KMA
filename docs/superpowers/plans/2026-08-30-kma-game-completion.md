# KMA Game Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoàn thiện "Thể Chất KMA" thành game Android chơi được trọn campaign: 7 môn thật, punishment, boss 3 phase, save/load, ending, presentation, audio/art và release verification.

**Architecture:** Giữ rules engine deterministic hiện có làm nguồn sự thật; mọi gameplay mới đi qua controller, detector và presentation adapter additive. `SceneRouter` tiếp tục load scene bằng `Single`, mỗi scene gameplay sở hữu HUD; `GameManager` inject/restore `GameSession` từ một `save.json` atomic. Thực hiện tuần tự S1–S16, mỗi section kết thúc bằng test và gate trên thiết bị thật.

**Tech Stack:** Unity `6000.3.23f1`, URP `17.3.0`/2D Renderer, uGUI/TMP, Input System + EnhancedTouch, C#/.NET Standard 2.1, Unity Test Framework, Android IL2CPP ARM64.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md`

## Global Constraints

- Không sửa rules engine đã có test; chỉ thêm method/event, adapter hoặc component presentation additive.
- Giữ nguyên chữ ký và hành vi đã test của `SceneRouter`, `GameSession`, `MinigameBase`, các rules và `BossSequenceAsset`; khi cần mở rộng, thêm API mới rồi kiểm regression.
- Chạy mọi shell command qua `rtk`; dùng editor `/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity`.
- Giữ Android landscape; Canvas reference `1920×1080`, `Match Width Or Height = 1.0`, safe-area cả trái/phải.
- Chỉ `PrimaryObjective` được đặt `Pass = true`; mọi sự kiện bất lợi phải có cue và cửa sổ counterplay deterministic.
- Không dùng Unity random trong gameplay; variation đến từ ScriptableObject authored patterns.
- Không dùng `PlayerPrefs` cho save/settings; save chính là `Application.persistentDataPath/save.json`, ghi atomic qua `save.tmp` và `File.Replace`.
- Không commit hoặc xoá các thay đổi không thuộc task; repo đang có thay đổi chưa commit nên phải chạy preflight trước mỗi task.
- Mỗi task có commit riêng sau khi test pass; sau mỗi task chạy lại full EditMode và PlayMode.

## File Structure

| Area | Files/responsibility |
|---|---|
| Toolchain | `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `ProjectSettings/{ProjectSettings,GraphicsSettings,QualitySettings,AudioManager}.asset`, `README.md` |
| Shared presentation/input | `Assets/_Project/Scripts/UI/`, `Assets/_Project/Settings/Input/KMA.inputactions`, detector/router tests |
| Core/session | `Assets/_Project/Scripts/Core/`, `Assets/_Project/Scripts/Progression/`, `Assets/_Project/Scripts/Gameplay/Common/` |
| Data authoring | `Assets/_Project/ScriptableObjects/`, `Assets/_Project/Settings/`, `Assets/_Project/Prefabs/` |
| Scenes | `Assets/_Project/Scenes/{Bootstrap,Menu,Map,MG_Sprint,MG_Endurance,MG_Volleyball,MG_Basketball,MG_PingPong,MG_Badminton,MG_Football,MG_Boss,Punishment,GameOver}.unity` |
| Tests | `Assets/Tests/EditMode/` for pure contracts/save/detectors; `Assets/Tests/PlayMode/` for scenes, input and full routes |
| Release | `Assets/_Project/Art/`, `Assets/_Project/Audio/`, `Assets/_Project/CREDITS.md`, `docs/qa/`, `README.md` |

## Execution Order

`S1 → (S2, S3, S4) → S5 → (S6, S7, S8, S14) → (S9–S13) → S15 → S16`.

S5 is a playable progression checkpoint only. Completion requires every Definition of Done item in spec §10.

### Task 1: S1 — Pin toolchain and normalize project configuration

**Files:**
- Modify: `ProjectSettings/ProjectVersion.txt`, `README.md`, `Packages/manifest.json`, `Packages/packages-lock.json`
- Create/modify through Editor: `Assets/Editor/ProjectConfigurator.cs`, `Assets/Editor/UrpBootstrap.cs`, `Assets/_Project/Settings/URP/URP-2D.asset`, `Assets/_Project/Settings/URP/URP-2D_Renderer2D.asset`
- Modify through Unity import: `Assets/_Project/Scenes/*.unity`, `ProjectSettings/*.asset`
- Test: `Assets/Tests/EditMode/Config/`

**Interfaces:** Produces a compileable URP 2D project pinned to `6000.3.23f1`, with Android/IL2CPP/landscape settings, required packages and the required directory tree. It must not alter gameplay behavior.

- [ ] **Step 1: Snapshot the dirty worktree and verify the installed editor.** Run `rtk git status --short` and `rtk ~/.local/bin/unity editors -i`; record existing modified/untracked paths and confirm Android modules before touching files.
- [ ] **Step 2: Repin the project and packages.** Set `ProjectVersion.txt` to `6000.3.23f1`; add URP `17.3.0`, `com.unity.2d.sprite` `1.0.0`, retain uGUI `2.0.0`, remove `com.unity.multiplayer.center`, and regenerate `packages-lock.json` with Unity.
- [ ] **Step 3: Apply settings through Editor APIs.** Run `ProjectConfigurator.ApplyAll()` and `UrpBootstrap.Apply()` headlessly; assert product `Thể Chất KMA`, id `com.kma.thechat`, landscape, minSdk 24, IL2CPP, .NET Standard 2.1, ARM64, Vulkan/OpenGLES3, medium stripping, 60fps/vSync off and best-latency DSP.
- [ ] **Step 4: Create the required folders/assets and normalize scenes.** Create the `Art`, `Audio`, `Fonts`, `Prefabs`, `Settings`, and `ScriptableObjects` tree; open/save all existing scenes in Unity so Unity writes missing scene metadata; commit scene normalization separately.
- [ ] **Step 5: Run the S1 gate.** Run full EditMode and PlayMode XML tests, then build/install the hello APK on one Android device. Expected: no compile errors, all existing tests pass, project opens in landscape and no screen is black due to pipeline misconfiguration.
- [ ] **Step 6: Commit only S1-owned files.** Use `rtk git diff --check`, stage configuration plus dedicated tests, and commit `chore: pin Unity toolchain and configure URP 2D`; preserve the pre-existing dirty paths unless they are explicitly part of S1.

### Task 2: S2 — Build shared presentation foundation

**Files:**
- Existing/extend: `Assets/_Project/Scripts/UI/{UITheme,BrutalButton,SafeAreaFitter,ScreenBase,HeartBar,FloatingTextPool,MinigameHUD,PhaseOverlay,ResultPanel,TutorialOverlay,TutorialSeenStore}.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Common/{MinigameBase,MinigameLifecycle}.cs`
- Create/update: `Assets/_Project/Prefabs/UI/{HUD_Minigame,ResultPanel,PhaseOverlay,Btn_Brutal}.prefab`, `Assets/_Project/Prefabs/Gameplay/GameCamera.prefab`, `Assets/_Project/Settings/UI/UITheme.asset`, Vietnamese TMP assets
- Test: `Assets/Tests/EditMode/Presentation/`, `Assets/Tests/PlayMode/Presentation/`

**Interfaces:** `MinigameBase.BuildHudState()` returns the six-field `MinigameHudState`; `MinigameLifecycle.PhaseChanged` fires once per transition; HUD pulls state from its serialized source and never becomes a required controller dependency.

- [ ] **Step 1: Lock the presentation contracts with failing tests.** Cover lifecycle event count, default HUD state, exact palette, safe-area insets, tutorial next/back/skip and result completion-once behavior.
- [ ] **Step 2: Implement the additive lifecycle/ViewModel seam.** Add serialized tutorial/countdown defaults preserving `2f/3f`; add `PhaseChanged`; keep existing phase timing and tested methods unchanged; add a public/internal bridge only where UI needs to read the protected state.
- [ ] **Step 3: Implement reusable UI and prefabs.** Bind `UITheme`, brutal button offsets `(4,-4)` and shadow reset over `0.1s`, 9-slice cards, HeartBar, pooling, timer/progress/status HUD, tutorial multi-step overlay and pause/result panels.
- [ ] **Step 4: Generate and test Vietnamese font coverage.** Include project strings, Latin basic, ranges `1EA0–1EF9`, `0110/0111`, `01A0–01B0`, dynamic fallback and glyph assertions for `Đ đ ă Ă ộ ơ Ư ứ`; record source/license in `CREDITS.md`.
- [ ] **Step 5: Attach camera and scene-local HUD.** Add `GameCamera` and the shared HUD/overlay prefabs to every existing scene; retain `LoadSceneMode.Single`, leaving persistent UI only for loading/toast.
- [ ] **Step 6: Run S2 gate and commit.** Verify `MG_Sprint` shows tutorial → 3-2-1 → timer/stamina and Left/Right response; run both suites and commit `feat: add shared presentation foundation`.

### Task 3: S3 — Add deterministic shared input layer

**Files:**
- Create: `Assets/_Project/Settings/Input/KMA.inputactions`
- Create: `Assets/_Project/Scripts/Input/` with `TapMashInputDetector`, `RhythmBeatInputDetector`, `HoldInputDetector`, `AlternateTapInputDetector`, `SwipeInputDetector`, `GameplayInputRouter`, `ScreenTapArea`
- Create: `Assets/Tests/EditMode/Input/` and the `KMA.Input` asmdef

**Interfaces:** Detectors are plain C# classes with injected timestamps: `FeedTap(double)`, `FeedTap(double,double)`, `FeedDown/FeedUp(double)`, `FeedTap(Side,double)`, `FeedSample(Vector2,double)/FeedEnd()`. Router is the only MonoBehaviour reading Input System/EnhancedTouch; `rhythmOffsetMs` is applied by router.

- [ ] **Step 1: Write failing EditMode tests** for rhythm boundaries `±80/±160ms`, alternate-side correctness, hold clamp `ChargeRatio 0..1`, tap rate and swipe direction/length/duration/curvature.
- [ ] **Step 2: Implement each detector as a time-injected plain class.** Expose only the specified metrics/events; do not read `Time`, `Input`, or Unity scene state from detector code.
- [ ] **Step 3: Implement router and tap ownership.** Define five maps in `KMA.inputactions`; route gameplay taps only through `ScreenTapArea`, route UI through EventSystem, and prevent Input System/UI double-fire. Keep the four existing `KMA.Gameplay` detector stubs untouched.
- [ ] **Step 4: Run the focused detector suite, then full suites.** Expected: all detector boundary tests pass and legacy `ChallengeSequenceTests` still see the old stub types.
- [ ] **Step 5: Commit** `feat: add deterministic gameplay input layer`.

### Task 4: S4 — Add session persistence, settings and core services

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/{GameSession,SubjectRecord}.cs`, `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Create: `Assets/_Project/Scripts/Core/{GameManager,SaveSystem,AudioManager,HapticsService,Pool}.cs`, `Assets/_Project/Scripts/Progression/SaveData.cs`, `Assets/_Project/Scripts/ScriptableObjects/{SubjectConfig,InstructorQuoteSet,RivalPaceProfileAsset}.cs`
- Create: `Assets/_Project/ScriptableObjects/Subjects/`, `Assets/_Project/Scenes/Bootstrap.unity`
- Test: `Assets/Tests/EditMode/Core/`, `Assets/Tests/EditMode/Progression/`

**Interfaces:** `GameSession.Restore(SaveData)`, `ToSaveData()`, `SubjectRecord.FromData(SubjectRecordData)`, `SceneRouter.LoadSession(GameSession)` and pure `ScoreUtil.ToStars(Rank)` are additive. `SaveData` contains version, lives, seven records, boss/game completion, tutorial flags and settings; stars are derived, never persisted.

- [ ] **Step 1: Write failing round-trip/migration tests.** Assert atomic data shape, version migration, seven subject records, `ToStars` rank boundaries, restore, settings/tutorial retention and `PreviewRoute` parity with `SubmitResult`.
- [ ] **Step 2: Implement DTOs and atomic SaveSystem.** Serialize `save.json` through `save.tmp` then `File.Replace`; write on subject completion, life loss, settings change and application pause; migrate by version and recover safely from a missing/invalid file.
- [ ] **Step 3: Inject sessions without changing ownership contracts.** Keep `SceneRouter.Awake()` default construction; add `LoadSession` and let `GameManager` load save before loading Menu. Do not add serialization fields to tested `SubjectRecord`.
- [ ] **Step 4: Add audio/haptics/pooling and authored data.** Provide Music/SFX mixer groups, volume controls, vibration no-op fallback, generic pool, 10 `SubjectConfig` assets (7 playable + 3 coming soon), quotes and rival profile wrapper.
- [ ] **Step 5: Configure Bootstrap and verify persistence.** Set Bootstrap as scene index 0, target 60fps with vSync off, kill/relaunch after one subject and assert lives/record survive; run full suites and commit `feat: add persistent game session services`.

### Task 5: S5 — Assemble shell and verified core loop

**Files:**
- Modify: `Assets/_Project/Scripts/Core/{SceneRouter,PunishmentSceneController}.cs`, `Assets/_Project/Scripts/Progression/{GameSession,PunishmentController}.cs`
- Create: `Assets/_Project/Scripts/UI/{MapScreen,MainMenuScreen,SettingsScreen,CalibrateScreen,PausePanel,PlaceholderMinigameController,GameOverScreen}.cs`
- Create/modify: `Assets/_Project/Scenes/{Menu,Map,Punishment,GameOver}.unity` and five placeholder gameplay scenes
- Create: `Assets/Tests/PlayMode/Progression/CoreLoopTests.cs`

**Interfaces:** `GameSession.PreviewRoute(subject,result)` is pure and shares the route helper with `SubmitResult`; placeholder controllers expose debug Pass/Fail only for S5 verification and are all replaced before S16.

- [ ] **Step 1: Write failing PlayMode tests** for result completion-once, full Menu→Map→subject→punishment→GameOver route, seven-subject boss unlock, pause routes, New Game/Continue and placeholder scene binding.
- [ ] **Step 2: Implement route preview and result sequencing.** Show score/rank/stars/quote/consequence first; invoke `Completed` only after Continue; preserve immediate completion when no panel exists and preserve existing router routes.
- [ ] **Step 3: Build MainMenu, Map, settings and calibration.** Map reads `SubjectConfig`, shows lock/best rank/derived stars/hearts, blocks three coming-soon nodes, and calibrate writes `rhythmOffsetMs`; New Game confirmation resets progress but keeps settings/tutorial flags.
- [ ] **Step 4: Build placeholder scenes and punishment.** Add camera/HUD/debug Pass-Fail to five stubs; wire punishment tap/rhythm/alternate adapters to S3 detectors and progress; add pause `Time.timeScale = 0` plus explicit DSP clock pause/resume for Endurance/Boss.
- [ ] **Step 5: Run the S5 device gate.** On Android complete both attempts, lose all five hearts, unlock Boss through seven actual placeholder subjects, kill/relaunch, pause/resume/restart/exit, and verify Continue/New Game.
- [ ] **Step 6: Commit** `feat: close the playable campaign shell` only after EditMode, PlayMode and device gates pass.

### Task 6: S6 — Replace Sprint placeholder with the real vertical slice

**Files:** `Assets/_Project/Scripts/Gameplay/Sprint/`, `Assets/_Project/Scenes/MG_Sprint.unity`, `Assets/_Project/Prefabs/Gameplay/`, `Assets/Tests/PlayMode/Gameplay/Running/`.

**Interfaces:** `AlternateTapInputDetector` feeds existing `SprintController`; `BuildHudState()` exposes timer/stamina/distance; rival AI consumes an authored `RivalPaceProfileAsset` and never changes rules outcomes.

- [ ] **Step 1: Write InputTestFixture tests** for valid alternating taps, wind cue at least `0.8s` early, same-side tap at `40%` impulse and pass/fail result mapping.
- [ ] **Step 2: Rewire `SprintController.inputActions`** to `KMA.inputactions` map `Sprint`; route two ≥140px lower-corner zones and keep the center bottom clear for Android gestures.
- [ ] **Step 3: Add HUD and authored scene content.** Implement rank/cadence/wind extras, player locked at x=35%, three rivals, 3-layer parallax and required animations; pause remains top-right.
- [ ] **Step 4: Run tests, profiler and device gate.** Verify 60fps on a mid device and first-attempt pass rate near 40–60%; commit `feat: make sprint playable`.

### Task 7: S7 — Replace Endurance placeholder with rhythm/hold/swipe gameplay

**Files:** `Assets/_Project/Scripts/Gameplay/Endurance/`, `Assets/_Project/Scenes/MG_Endurance.unity`, `Assets/Tests/PlayMode/Gameplay/Running/EnduranceControllerTests.cs`.

**Interfaces:** One and only one of `RhythmBeatInputDetector`, `HoldInputDetector`, `SwipeInputDetector` is active at a time; `EnduranceController` keeps its tested `dspTime` clock and reads calibrated offset.

- [ ] **Step 1: Add failing tests** for correct swipe not becoming Miss, exclusive modes, obstacle warning lead and pause/resume DSP continuity.
- [ ] **Step 2: Rewire `EnduranceInputBridge`** to `KMA.inputactions` and the router; map detector events to the existing `Tap`, `EndHold` and `Swipe` APIs without changing rules.
- [ ] **Step 3: Replace runtime metronome generation** with licensed audio clip, retain scheduled `dspTime`, build beat ring/mode color/lap/oval mini-map/stamina HUD and two-beat obstacle cue.
- [ ] **Step 4: Verify on Android** with calibration offset and pause/resume; commit `feat: make endurance playable`.

### Task 8: S8 — Add shared ball presentation kit

**Files:** `Assets/_Project/Scripts/Gameplay/Ball/{TrajectoryPreview,BallShadow}.cs`, `FlightProfile.cs`, `Assets/_Project/ScriptableObjects/Ball/FlightProfile_*.asset`, `Assets/Tests/EditMode/Gameplay/Ball/`.

**Interfaces:** Preview consumes `BallRig.PredictLandingPoint()`/`Ballistics.PredictGround`; shadow consumes `BallRig` height; no ball rules are modified.

- [ ] **Step 1: Write failing tests** asserting preview landing matches `Ballistics.PredictGround` and shuttle profile has high drag with zero bounce.
- [ ] **Step 2: Implement dashed trajectory and height-driven shadow** with pooled line/visual objects and no `GetComponent` in `Update`.
- [ ] **Step 3: Author five profiles**, including `FlightProfile_Shuttle`, then run ball EditMode/PlayMode suites and commit `feat: add ball presentation kit`.

### Task 9: S9 — Implement Volleyball scene/controller

**Files:** `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs`, `Assets/_Project/Scenes/MG_Volleyball.unity`, volleyball prefabs/assets, `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs`.

**Interfaces:** Swipe maps to `ResolveGesture` and `TryResolveAndLaunch`; `BallContext` comes from ball height/velocity/reach zone; HUD exposes Touch 1/2/3, scores and combo.

- [ ] **Step 1: Test** swipe-to-action, rally-3 spin/fake cue and stable predicted landing.
- [ ] **Step 2: Implement controller, auto-positioning and shared ball kit wiring** without changing `VolleyballRules`.
- [ ] **Step 3: Add tutorial steps for dig/set/spike and run InputTestFixture plus device gate; commit `feat: add volleyball vertical slice`.

### Task 10: S10 — Implement Basketball scene/controller

**Files:** `Assets/_Project/Scripts/Gameplay/Basketball/BasketballController.cs`, `Assets/_Project/Scenes/MG_Basketball.unity`, basketball assets, `Assets/Tests/PlayMode/Gameplay/Ball/BasketballControllerTests.cs`.

**Interfaces:** Hold→pass swipe→AI alley-oop→apex tap maps to existing rules; HUD reports apex progress, finish judge, baskets/attempts and combo.

- [ ] **Step 1: Test** Hold charge, pass, alley-oop, `Ignored/Early/Perfect/Late`, and one-axis-per-phase difficulty.
- [ ] **Step 2: Implement controller/HUD/apex ring** and authored scene; keep ball apex source deterministic.
- [ ] **Step 3: Add tutorial and device gate**, then commit `feat: add basketball vertical slice`.

### Task 11: S11 — Implement PingPong scene/controller

**Files:** `Assets/_Project/Scripts/Gameplay/PingPong/PingPongController.cs`, `Assets/_Project/Scenes/MG_PingPong.unity`, ping-pong assets, `Assets/Tests/PlayMode/Gameplay/Ball/PingPongControllerTests.cs`.

**Interfaces:** Tap maps to `TryReturn`; after ball-speed cap, only authored placement pattern changes difficulty.

- [ ] **Step 1: Test** timing/placement, speed cap and post-cap placement-only difficulty.
- [ ] **Step 2: Implement hit-zone/shadow/speed/rally HUD and return loop** through existing rules.
- [ ] **Step 3: Add tutorial and device gate**, then commit `feat: add ping pong vertical slice`.

### Task 12: S12 — Implement Badminton scene/controller

**Files:** `Assets/_Project/Scripts/Gameplay/Badminton/BadmintonController.cs`, `Assets/_Project/Scenes/MG_Badminton.unity`, badminton assets, `Assets/Tests/PlayMode/Gameplay/Ball/BadmintonControllerTests.cs`.

**Interfaces:** Hold detector `ChargeRatio` plus release height maps to `TryExchange`; charge above `1.0` is an out-of-bounds miss; preview and shadow update when wind changes.

- [ ] **Step 1: Test** lift/drive/smash/overcharge, release height and visible wind/landing update.
- [ ] **Step 2: Implement charge ring, wind cue and shared ball presentation** over `BadmintonRules`.
- [ ] **Step 3: Add multi-step tutorial for charge/release/height and device gate**, then commit `feat: add badminton vertical slice`.

### Task 13: S13 — Implement Football scene/controller

**Files:** `Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`, `Assets/_Project/Scenes/MG_Football.unity`, `GKPatternSet` assets, `Assets/Tests/PlayMode/Gameplay/Ball/FootballControllerTests.cs`.

**Interfaces:** Swipe creates `FootballShot(placement, force, spin, kind)` and calls `ResolveAuthoredShot`; goalkeeper uses `LastKeeperPattern` from authored `GKPatternSet`.

- [ ] **Step 1: Test** shot mapping, five-kick scoring, keeper cue and one-axis-per-phase difficulty.
- [ ] **Step 2: Implement dashed preview, goal counter, keeper animation and target/reaction phase progression** through existing rules.
- [ ] **Step 3: Add tutorial and device gate**, then commit `feat: add football vertical slice`.

### Task 14: S14 — Connect Boss and polish Punishment

**Files:** `Assets/_Project/Scripts/Gameplay/Boss/`, `Assets/_Project/Scenes/{MG_Boss,Punishment}.unity`, `Assets/Tests/PlayMode/Progression/BossPhaseControllerTests.cs`.

**Interfaces:** Runtime input source feeds the three existing boss adapters, which consume S3 detector events; `BossSequence.asset` remains unchanged: TapMash `10s/40`, RhythmHold `12s/16`, AlternateTap `10s/32`.

- [ ] **Step 1: Add failing PlayMode tests** for all three runtime mechanics, 30–40s continuous phase flow, transition cues, camera presence and one completion event.
- [ ] **Step 2: Add the missing Boss camera and wire adapters** to `GameplayInputRouter`; preserve the tested test-input APIs.
- [ ] **Step 3: Add instructor sprite/animations, phase HUD, BPM/target progression and punishment mechanic cues/progress** without touching `BossSequenceAsset`.
- [ ] **Step 4: Verify boss completion routes to Map and punishment routes correctly** on Android; commit `feat: connect runtime boss and punishment input`.

### Task 15: S15 — Add ending, post-game state and credits

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Boss/BossPhaseController.cs`, `Assets/_Project/Scripts/Progression/{GameSession,SaveData}.cs`, `Assets/_Project/Scripts/UI/`
- Modify: `Assets/_Project/Scenes/{MG_Boss,Map,Menu,GameOver}.unity`
- Test: `Assets/Tests/PlayMode/Progression/EndingPanelTests.cs`, `Assets/Tests/EditMode/Progression/ResetSaveTests.cs`

**Interfaces:** Boss ending is an overlay; `Completed` still routes to `SessionRoute.Map` after Continue. `gameCompleted` is persisted; stars come from `ScoreUtil.ToStars`; post-game Map permits replay/improvement.

- [ ] **Step 1: Decide and test replay-heart semantics before implementation.** Use the existing session rules as default: post-game replay does not create a new campaign life-loss route unless a result is explicitly submitted; encode the chosen behavior in tests and UI copy.
- [ ] **Step 2: Implement `EndingPanel` completion sequencing.** Show all seven ranks/stars, average score, remaining hearts, total time and quote; save `gameCompleted` immediately; no panel still completes immediately for harnesses.
- [ ] **Step 3: Implement post-game Menu/Map and Credits.** Change Continue to Map after completion, allow replay for better best scores, show completed Boss state, and render credits from `Assets/_Project/CREDITS.md` or a matching SO.
- [ ] **Step 4: Kill/relaunch after victory** and verify summary/post-game state; run full tests and commit `feat: add campaign ending and post-game state`.

### Task 16: S16 — Finish art/audio, performance, balance and release

**Files:** `Assets/_Project/{Art,Audio,Fonts,Prefabs}`, `ProjectSettings/`, `Assets/_Project/CREDITS.md`, `README.md`, `docs/qa/`, `Assets/Editor/BuildScript.cs`.

**Interfaces:** Final assets replace placeholders without changing rules/controller contracts; release output is APK for demo and AAB when Play delivery is required.

- [ ] **Step 1: Run the required art/audio brainstorm and approve the asset source strategy.** Choose commissioned/self-made or license-safe CC0 assets; do not import an asset before its license is recorded in `CREDITS.md`.
- [ ] **Step 2: Replace placeholder visuals and add audio.** Recolor Kenney/approved assets to `UITheme`; add adaptive icon, custom splash, Music/SFX mixer groups and minimum SFX: tap, perfect, good, miss, whistle, crowd, fail, pass, button.
- [ ] **Step 3: Enforce performance budgets.** Build one 2048 Sprite Atlas per scene, ASTC 6×6 art/4×4 UI, pooling everywhere, no realtime light/post-process, no `GetComponent` in `Update`; measure draw calls and FPS with Unity Profiler on low-end GLES3 and mid Vulkan devices.
- [ ] **Step 4: Run the eight-person balance pass.** Record first-attempt pass rates, rank distribution and recognition answers; tune authored `primaryObjective/timeLimit/targetScore/BPM/timingWindow/weights` until each subject is 40–60% first-pass, no subject <25% or >80%, no rank >50% and ≥6/8 recognize both confusing sport pairs.
- [ ] **Step 5: Build and cold-start test release artifacts.** Produce APK (<100MB), install on low-end/notch/mid devices, measure cold start (<4s), verify landscape layouts at 16:9/19.5:9/21:9, run full 158+new tests and the complete DoD checklist.
- [ ] **Step 6: Update delivery docs and commit.** Update README with pinned version, build/test commands, device matrix and known deviations; ensure §7 deviations plus PLAN §0 deviations are in defense appendix slides; run `rtk git diff --check` and commit `release: complete KMA game validation`.

## Verification Commands

Set the exact editor once per shell:

```bash
export KMA_UNITY_EDITOR=/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity
rtk "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-editmode.xml -logFile /tmp/kma-editmode.log -quit
rtk "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-playmode.xml -logFile /tmp/kma-playmode.log -quit
```

The final claim of completion requires device evidence for every §10 item; passing Unity tests alone is insufficient.

## Plan Self-Review

- Spec coverage: S1–S5 are decomposed from the full decisions; S6–S16 each map their input, rules adapter, HUD, scene, tests and gate; S16 covers every content, quality and delivery checkbox.
- Placeholder scan: no deferred implementation step remains; the only approval gate is the explicitly required S16 art brainstorm.
- Type consistency: detector signatures, `BuildHudState`, `PreviewRoute`, `LoadSession`, DTO names and boss sequence values match the design spec and existing source names.
- Dirty worktree safety: Task 1 explicitly snapshots and preserves current uncommitted S1/S2 changes before any implementation task.
