# S7 Endurance Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay phần trình diễn/input còn tạm của `MG_Endurance` bằng một màn chạy sức bền chơi được trên Android, với đúng một mechanic active tại mỗi beat, nhịp DSP không lệch khi pause, HUD/tutorial đầy đủ và obstacle báo trước.

**Architecture:** `EnduranceRules` và các method gameplay hiện có của `EnduranceController` vẫn là nguồn sự thật cho điểm, stamina, lap và kết quả. `GameplayInputRouter` feed các detector plain-C#; `EnduranceInputBridge` chỉ làm adapter chọn đúng detector theo `EnduranceInputMode`, chống tap/hold/swipe chồng lên nhau và chuyển kết quả đã judge vào controller. Presentation đọc snapshot/HUD state, còn scene giữ một `dspTime` schedule duy nhất cho metronome, beat và cue.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, uGUI/TextMeshPro, Input System + EnhancedTouch, Unity Test Framework, Android landscape, scene-local HUD/pause từ S2–S5, AudioSource phát clip đã author.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §4 S7, §6 S7, §8, §9 và §10.

## Global Constraints

- Không sửa hành vi/chữ ký đã test của `EnduranceRules`, `EnduranceController`, `MinigameBase` hoặc core route; mọi seam mới là additive và phải giữ test cũ.
- Mỗi thời điểm chỉ một trong `RhythmTap`, `BreathHold`, `ObstacleSwipe` được phép làm thay đổi rules; input đến từ mode khác bị bỏ qua.
- Detector không tự đọc clock; timestamp rhythm dùng DSP time, còn `rhythmOffsetMs` được áp dụng đúng một lần trước khi judge.
- Obstacle icon phải hiện ít nhất 2 beat trước beat obstacle; ở beat obstacle mode chuyển sang swipe và tap rhythm không tạo Miss.
- Metronome production dùng AudioClip đã import, không gọi `AudioClip.Create` hoặc sinh sample runtime; vẫn giữ `PlayScheduled(songStartDspTime)`.
- Pause đặt ở góc trên bên phải trong safe area; `Time.timeScale = 0` không được tự động dừng `dspTime`, nên Endurance phải pause/resume metronome và elapsed DSP riêng.
- Không dùng `GetComponent` trong `Update`; không `Instantiate/Destroy` runtime cho cue, runner, floating text hoặc FX.
- Tutorial Endurance là nhiều bước có thể bấm qua và lưu `tutorialSeen[SubjectId.Endurance]` bằng store/save hiện có.
- Không chạm các thay đổi dirty ngoài S7: `Assets/_Project/Fonts/Baloo2-ExtraBold.asset`, `README.md`, `Assets/Editor/Task4AssetRepair.cs(.meta)`, `Assets/_Project/ScriptableObjects/Sprint.meta` và các plan S5/S6/S7 khác với file này.
- Mỗi task kết thúc bằng test phù hợp và commit riêng; mọi shell command chạy qua `rtk`.

## File Map

| Area | Files/responsibility |
|---|---|
| Endurance rules/controller | `Assets/_Project/Scripts/Gameplay/Endurance/{EnduranceController,EnduranceRules,LapPattern}.cs`; preserve rule contracts, expose only additive presentation/input seams |
| Shared runtime input | `Assets/_Project/Scripts/Input/{GameplayInputRouter,RhythmBeatInputDetector,HoldInputDetector,SwipeInputDetector}.cs`; detector events and timestamp flow |
| Endurance adapter | `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceInputBridge.cs`; bind shared actions/detectors, enforce active-mode gate, map events to `Tap`, `EndHold`, `Swipe` |
| Endurance presentation | `Assets/_Project/Scripts/Gameplay/Endurance/{EnduranceHud,EnduranceBeatRing,EnduranceObstacleCue,EnduranceParallax}.cs`; read-only beat ring, mode, lap, mini-map, stamina and obstacle feedback |
| Audio/config | `Assets/_Project/Audio/SFX/EnduranceMetronome.wav`, `Assets/_Project/Scripts/Core/AudioManager.cs` for the existing mixer/SFX hookup, `Assets/_Project/Settings/Audio/KMA-AudioMixer.mixer`, `Assets/_Project/Settings/Input/KMA.inputactions` |
| Scene/prefabs | `Assets/_Project/Scenes/MG_Endurance.unity`, `Assets/_Project/Prefabs/UI/HUD_Minigame.prefab`, `Assets/_Project/Prefabs/Gameplay/GameCamera.prefab`; Endurance cue/parallax objects remain scene-local so S7 does not expand the shared prefab surface |
| Tests | `Assets/Tests/PlayMode/Gameplay/Running/{EnduranceControllerTests,EnduranceInputBridgeTests,EnduranceRuntimeInputTests}.cs`, `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`, `Assets/Tests/PlayMode/Presentation/EndurancePresentationGateTests.cs` |
| QA evidence | `docs/qa/s7-endurance-device-gate.md` |

## Interfaces

- `RhythmBeatInputDetector.FeedTap(double inputDsp, double beatDsp)` emits `OnJudge(TimingJudge judge, double deltaMs)`; `HoldInputDetector` emits `OnHoldStart`/`OnHoldEnd(double durationSeconds)` and exposes `ChargeRatio`; `SwipeInputDetector` emits `OnSwipe(SwipeResult)`. None owns Unity time or scene state.
- `GameplayInputRouter` exposes additive detector events for the Endurance adapter: `OnRhythmJudge(TimingJudge, double)`, `OnHoldStarted()`, `OnHoldEnded(double)`, and `OnSwipe(SwipeResult)`. Its existing generic detector tests and `RhythmOffsetMs` behavior remain valid.
- `EnduranceInputBridge.Configure(EnduranceController controller, GameplayInputRouter router)` subscribes once, checks `controller.Phase == MinigamePhase.Play`, then forwards rhythm judgments, hold duration and vertical swipe only when `controller.Rules.Mode` matches the event.
- `EnduranceController.BuildHudState()` remains the common HUD source for timer/progress/stamina/status. `EnduranceHud` additionally reads `Rules.Mode`, `Rules.Combo`, `Rules.Laps`, `Rules.RequiredLaps`, `CurrentBeatDspTime`, `BeatIntervalSeconds`, `ObstacleCueVisible` and `EnduranceCueSchedule.WarningLeadBeats` without writing rules state.
- `EnduranceBeatRing.SetMode(EnduranceInputMode mode)` changes the visual prompt only; `EnduranceObstacleCue.ShowWarning(int warningLeadBeats)` and `.ShowActive()` are visual-only and never alter `EnduranceRules`.
- `EnduranceParallax.Configure(Transform[] layers, float[] speeds)` moves preallocated layers using `unscaledDeltaTime` only when the scene is not paused, with three authored layers and no per-frame component lookup.

---

### Task 1: Lock Endurance input, mode exclusivity and DSP contracts with focused tests

**Files:**
- Modify: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceControllerTests.cs`
- Modify: `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`
- Create: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceInputBridgeTests.cs`
- Create: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceRuntimeInputTests.cs`
- Inspect only: `Assets/_Project/Scripts/Gameplay/Endurance/{EnduranceController,EnduranceInputBridge,EnduranceRules}.cs`, `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`

**Interfaces:** Tests use `EnduranceController.ConfigureLifecycleForTest`, `AdvanceToPlayForTest`, `Dispatch`, `Tap`, `EndHold`, `Swipe`, `ConfigurePatternForTest` and the detector/router seams. They must not require an Android device or change rules internals.

- [ ] **Step 1: Add mode-gating tests.** Build a controller in Play, dispatch `Tap`, `Breath` and `Jump/Slide` beats, then send each detector event through the bridge. Assert rhythm only increments `JudgedCount`, hold only changes stamina, and swipe only changes `ObstacleCleared`; events from inactive modes produce no rules mutation.
- [ ] **Step 2: Add rhythm-boundary tests.** Assert the detector preserves `Perfect` at `±80ms`, `Good` at `±160ms`, `Miss` outside, and that Endurance applies `rhythmOffsetMs` exactly once when a routed tap reaches the controller. Assert obstacle beat does not create a rhythm miss.
- [ ] **Step 3: Add hold/swipe metric tests.** Assert hold `ChargeRatio` clamps to `0..1`, forwards one end event per press, maps vertical swipe direction to `SwipeDirection.Up/Down`, and ignores a second `FeedEnd` or a swipe received during `RhythmTap`/`BreathHold`.
- [ ] **Step 4: Add DSP pause tests.** Start the controller schedule, call `SetPaused(true)` twice, assert the beat time and paused elapsed position remain stable, resume once, and assert the metronome is not duplicated and `CurrentBeatDspTime` continues from the paused beat.
- [ ] **Step 5: Run the focused RED suite.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s7-task1-edit.xml -logFile /tmp/kma-s7-task1-edit.log -testFilter "EnduranceRulesTests|DetectorContractTests" -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s7-task1-play.xml -logFile /tmp/kma-s7-task1-play.log -testFilter "EnduranceControllerTests|EnduranceRuntimeInputTests|GameplayInputRouterTests" -quit
```

Expected: new bridge/event tests fail for missing seams; existing Endurance and input tests compile and remain green.

- [ ] **Step 6: Commit the test contract.** Run `rtk git diff --check`, stage only S7 test files, and commit `test: define endurance input and dsp contract`.

### Task 2: Route shared detectors into Endurance with exactly one active mode

**Files:**
- Modify: `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceInputBridge.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceController.cs`
- Modify: `Assets/_Project/Settings/Input/KMA.inputactions` only if an action required by the bridge is absent
- Modify: `Assets/_Project/Scenes/MG_Endurance.unity`
- Test: `Assets/Tests/EditMode/Input/EnduranceInputBridgeTests.cs`, `Assets/Tests/PlayMode/Gameplay/Running/EnduranceRuntimeInputTests.cs`

**Interfaces:** The scene uses the shared `KMA.inputactions` Endurance map (`Tap`, `Hold`, `SwipeUp`, `SwipeDown`, `TouchPosition`). The bridge owns detector subscriptions and forwards only the detector matching `EnduranceRules.Mode`; no legacy bridge callback may also call the controller.

- [ ] **Step 1: Add mode-agnostic router events.** Emit the detector results after the existing timestamp/offset processing, preserve `SetDetectors`, pointer ownership and keyboard action behavior, and unsubscribe every new event in the same lifecycle paths as existing actions.
- [ ] **Step 2: Refactor `EnduranceInputBridge` into the scene adapter.** Resolve the shared input asset and router in `Awake`/`OnEnable`, subscribe exactly once, route `TimingJudge` to the existing controller timing seam, route hold duration to `EndHold`, and convert `SwipeResult.Direction` to the existing Endurance enum. Keep test-only `ProcessTouchSampleForTest` behavior available until replacement coverage passes.
- [ ] **Step 3: Eliminate duplicate runtime input.** Remove direct controller subscriptions/duplicate touchscreen processing from the scene, ensure only `GameplayInputRouter` receives gameplay pointer events, and keep UI controls owned by EventSystem/`ScreenTapArea` as required by S3.
- [ ] **Step 4: Make calibration single-source.** Pass raw DSP input through the router/detector boundary and apply `rhythmOffsetMs` once; retain `EnduranceController.CalibratedInputTime` as the public additive calculation used by its existing test, with no second offset in the adapter.
- [ ] **Step 5: Run focused GREEN tests.** Re-run Task 1 commands. Expected: keyboard and pointer taps reach the correct mode once, wrong-mode inputs do not change rules, and `GameplayInputRouterTests` still pass its existing exactly-once and offset assertions.
- [ ] **Step 6: Commit** `feat: route endurance through shared input detectors` after `rtk git diff --check`.

### Task 3: Add Endurance HUD, beat ring, obstacle cue and tutorial content

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceHud.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceBeatRing.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceObstacleCue.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceParallax.cs`
- Modify: `Assets/_Project/Scenes/MG_Endurance.unity`
- Modify: `Assets/_Project/Prefabs/UI/HUD_Minigame.prefab` only for shared bindings absent from the prefab
- Create: `Assets/Tests/PlayMode/Presentation/EndurancePresentationGateTests.cs`

**Interfaces:** `EnduranceHud` reads cached references and controller state; the beat ring displays the active prompt (`TAP TO THE BEAT`, `HOLD TO BREATHE`, `SWIPE TO CLEAR`), lap count, stamina and mini-map position; the obstacle cue shows a warning two beats early and an active state on the obstacle beat.

- [ ] **Step 1: Add scene-contract tests.** Load `MG_Endurance` single-mode and assert one `EnduranceController`, one `GameplayInputRouter`, one Endurance bridge, scene-local `MinigameHUD`, beat ring, obstacle cue, tutorial overlay, pause control and `GameCamera`; assert no second component reads Endurance gameplay taps.
- [ ] **Step 2: Implement the HUD read model.** Cache `TMP_Text`, `Image`, ring and cue references in `Awake`/`OnEnable`; update timer, `lap/requiredLaps`, stamina, combo, mode label, beat progress and mini-map marker from controller/rules state. Never call `GetComponent` in `Update`.
- [ ] **Step 3: Implement deterministic beat-ring feedback.** Derive ring fill/color from `CurrentBeatDspTime` and `BeatIntervalSeconds`, switch prompt only from `Rules.Mode`, and show Perfect/Good/Miss feedback from the routed judge without affecting the score.
- [ ] **Step 4: Implement obstacle cue and parallax.** Show the icon when `ObstacleCueVisible`, distinguish warning from active swipe state, hide/resolve it after the authored obstacle event, and move three preallocated background layers with no per-frame allocation. Do not make obstacle landing or direction cosmetic; `EnduranceRules` remains authoritative.
- [ ] **Step 5: Configure tutorial and layout.** Author the steps `Tap on the beat`, `Hold to recover stamina`, and `Swipe up/down to clear obstacles`; wire Next/Back/Skip/Close and Endurance seen-state. Place Pause top-right inside safe area, keep gameplay input clear of UI, and preserve landscape canvas scaling/notch margins.
- [ ] **Step 6: Run presentation tests and commit** `feat: add endurance hud cues and tutorial`.

### Task 4: Replace generated metronome, finish Endurance scene wiring and pause behavior

**Files:**
- Create: `Assets/_Project/Audio/SFX/EnduranceMetronome.wav` and its Unity metadata
- Modify: `Assets/_Project/Scripts/Gameplay/Endurance/EnduranceController.cs`
- Modify: `Assets/_Project/Scenes/MG_Endurance.unity`
- Modify: `Assets/_Project/Scripts/Core/AudioManager.cs` only when the existing mixer group hookup is absent
- Modify: `Assets/_Project/Settings/Audio/KMA-AudioMixer.mixer` only when the existing SFX/Music groups are not already usable
- Test: `Assets/Tests/PlayMode/Gameplay/Running/EnduranceControllerTests.cs`, `Assets/Tests/PlayMode/Presentation/EndurancePresentationGateTests.cs`

**Interfaces:** `EnduranceController` receives an authored `AudioClip` through serialization, schedules it at `MetronomeStartDspTime`, and exposes the same `DspClockScheduled`, `CurrentBeatDspTime`, `SetPaused` and `MetronomeAudioSource` contracts used by existing tests.

- [ ] **Step 1: Add audio asset/import tests.** Assert the scene references a non-null imported metronome clip, the clip is mono and usable by the configured sample rate, and no Endurance production path calls `AudioClip.Create`.
- [ ] **Step 2: Replace runtime clip generation.** Add a serialized clip field, assign it in `MG_Endurance`, remove the production fallback that generates samples, and fail safely with a clear scene/configuration error if the authored clip is missing while preserving test-created AudioSources.
- [ ] **Step 3: Preserve DSP scheduling.** Keep `PlayScheduled(songStartDspTime)` and beat dispatch based on `AudioSettings.dspTime`; ensure `rhythmOffsetMs` affects judgment only, never the metronome start or beat schedule.
- [ ] **Step 4: Verify pause/restart/exit wiring.** Connect the existing `PausePanel` events to S5's restart and Map routes, pause/resume the AudioSource and DSP elapsed state exactly once, restore `Time.timeScale`, and reset all detector pointer/hold/swipe state when leaving or restarting.
- [ ] **Step 5: Run focused tests and commit** `feat: use authored endurance metronome and pause-safe dsp`.

### Task 5: Complete the S7 integration and Android device gate

**Files:**
- Verify: `Assets/_Project/Scenes/MG_Endurance.unity`, `Assets/_Project/Scripts/Gameplay/Endurance/`, `Assets/_Project/Scripts/Input/`, `Assets/_Project/Prefabs/UI/`, `Assets/_Project/Audio/SFX/`
- Modify: `Assets/Tests/PlayMode/Presentation/EndurancePresentationGateTests.cs` only if a device-observed regression needs automated coverage
- Create: `docs/qa/s7-endurance-device-gate.md`
- Do not modify: existing dirty files outside S7

**Interfaces:** The gate proves Endurance works inside S5's session/result flow; it does not claim S8–S16 or the final Definition of Done.

- [ ] **Step 1: Run the complete Unity suites.** Use the pinned editor for full EditMode and PlayMode XML runs; record test totals, compiler output and unexpected errors in `/tmp/kma-s7-*` logs.

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s7-editmode.xml -logFile /tmp/kma-s7-editmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s7-playmode.xml -logFile /tmp/kma-s7-playmode.log -quit
```

- [ ] **Step 2: Build the Android APK.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -buildOutput Builds/Android/kma-s7.apk -logFile /tmp/kma-s7-build.log -quit
```

- [ ] **Step 3: Execute the Endurance device scenario.** Verify tutorial → countdown → tap rhythm → hold recovery → obstacle warning two beats early → vertical swipe; use the calibration offset; observe beat ring, mode prompt, lap, mini-map, stamina and combo; complete a pass and a fail; confirm only the active mechanic changes the result.
- [ ] **Step 4: Execute pause and route scenarios.** Pause during rhythm, hold and obstacle phases; resume without beat drift; restart; exit to Map; fail once into Punishment and complete the result route; confirm one result event and no duplicate input after returning.
- [ ] **Step 5: Measure the S7 performance gate.** On a mid-range Android device, record FPS, frame time, draw calls, audio behavior and input latency; verify 60fps target on mid device, no sustained frame below 30fps on low-end, no visible beat drift after pause, and no duplicate tap/hold/swipe event.
- [ ] **Step 6: Record evidence and commit.** Write device model/API/aspect ratio, build identifier, test results, screenshots or profiler observations, and unavailable checks to `docs/qa/s7-endurance-device-gate.md`; run `rtk git diff --check`; commit `feat: make endurance playable` only after the gate is evidenced.

## Verification Commands

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s7-editmode.xml -logFile /tmp/kma-s7-editmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s7-playmode.xml -logFile /tmp/kma-s7-playmode.log -quit
```

## Plan Self-Review

- Spec coverage: S7 shared detector input, single active mode, calibration, DSP metronome replacement, obstacle warning, beat/lap/stamina HUD, mini-map/parallax, tutorial, pause, tests and Android gate are covered by Tasks 1–5.
- Existing contracts: `EnduranceRules` remains authoritative; controller result/lifecycle APIs remain intact; router changes are additive events and bridge changes are scene-local wiring.
- Calibration consistency: the plan explicitly tests and enforces one offset application across router/detector/controller boundaries while retaining the existing public controller helper.
- Placeholder scan: no `TBD`, `TODO`, or vague "handle edge cases" step remains; every implementation step names files, interface behavior, test expectations and commit boundary.
- Scope: S7 does not add S8 ball presentation, S14 boss work, or S16 balance/art/release claims; its device gate reports only Endurance evidence.
- Dirty-worktree safety: the plan scopes changes to Endurance/input/presentation/audio/QA and preserves all unrelated dirty files listed in Global Constraints.
