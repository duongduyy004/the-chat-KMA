# S6 Sprint Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay placeholder của `MG_Sprint` bằng một màn chạy nước rút chơi được trên Android, với tap trái/phải, thử thách gió có báo trước, HUD đầy đủ, ba đối thủ authored và kết quả pass/fail nối đúng vào core loop.

**Architecture:** Giữ `SprintRules` và `SprintController` làm nguồn sự thật deterministic; input runtime đi qua `KMA.inputactions`/`GameplayInputRouter` và gọi các method additive của controller. Presentation chỉ đọc snapshot/HUD state, còn `RivalPaceProfileAsset` cung cấp dữ liệu authored cho AI hiển thị và không được thay đổi kết quả rules.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, uGUI/TextMeshPro, Input System + EnhancedTouch, Unity Test Framework, Android landscape, scene-local HUD/pause từ S2–S5.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §4 S6, §6 S6, §9 và §10.

## Global Constraints

- Không sửa hành vi/chữ ký đã test của `SprintRules`, `SprintController`, `MinigameBase` hoặc core route; chỉ thêm adapter/presentation/configuration additive.
- `AlternateTapInputDetector` là detector duy nhất quyết định tap hợp lệ ở runtime; controller tiếp tục nhận `OnLeftTap()`/`OnRightTap()`.
- Hai vùng tap trái/phải ở góc dưới có đường kính tối thiểu `140px @1080p`; vùng giữa phía dưới phải để trống cho Android gesture bar.
- Nút Pause neo góc trên bên phải trong safe area; pause/resume/restart/thoát dùng flow S5 hiện có.
- Wind cue phải xuất hiện trước cửa sổ phản ứng ít nhất `0.8s`; tap đúng wind counterplay giữ đường chạy khả thi, tap sai tạo fail deterministic.
- Player khoá tại `x=35%`; player lane 2, rivals ở lane 1/3/4; tốc độ rival đọc từ authored profile và không gọi random.
- Không dùng `GetComponent` trong `Update`; các hiệu ứng/floating text phải dùng pool đã có.
- Giữ tutorial nhiều bước của `TutorialOverlay` cho Sprint và lưu `tutorialSeen[SubjectId.Sprint]` qua store/save hiện có.
- Không chạm các thay đổi chưa commit ngoài S6, hiện gồm `README.md` và các plan trong `docs/superpowers/plans/`.
- Mỗi task kết thúc bằng test phù hợp và commit riêng; mọi shell command chạy qua `rtk`.

## File Map

| Area | Files/responsibility |
|---|---|
| Sprint rules/controller | `Assets/_Project/Scripts/Gameplay/Sprint/{SprintController,SprintRules,SprintChallengePattern,RivalPaceProfile}.cs`; preserve existing contracts and expose presentation data additively |
| Runtime input | `Assets/_Project/Scripts/Input/{AlternateTapInputDetector,GameplayInputRouter,ScreenTapArea}.cs`, `Assets/_Project/Settings/Input/KMA.inputactions` |
| Sprint presentation | `Assets/_Project/Scripts/Gameplay/Sprint/{SprintHud,SprintWindCue,RivalRunnerAI,SprintParallax}.cs` and existing `MinigameHUD`, `PhaseOverlay`, `PausePanel` |
| Authored data | `Assets/_Project/Scripts/ScriptableObjects/RivalPaceProfileAsset.cs`, `Assets/_Project/ScriptableObjects/Sprint/`, `Assets/_Project/Scripts/Gameplay/Sprint/SprintInputActions.inputactions` only as legacy source to be replaced in scene wiring |
| Scene/prefabs | `Assets/_Project/Scenes/MG_Sprint.unity`, `Assets/_Project/Prefabs/Gameplay/{GameCamera,RivalRunner, SprintLane}.prefab` |
| Tests | `Assets/Tests/EditMode/Input/`, `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs`, `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`, new S6-specific tests under `Assets/Tests/PlayMode/Gameplay/Running/` |

## Interfaces

- `AlternateTapInputDetector.FeedTap(Side side, double timeSeconds)` emits `OnValidTap(Side)` or `OnWrongSide`; it owns no Unity clock or scene state.
- `GameplayInputRouter` owns the detector instance, feeds it timestamps, and invokes `SprintController.OnLeftTap()`/`OnRightTap()` only for valid events.
- `SprintController.BuildHudState()` remains the source for timer, stamina, distance and status; S6 adds a presentation read model for rank, cadence combo and wind cue without changing `MinigameHudState`.
- `RivalRunnerAI.Configure(RivalPaceProfile profile, int lane, Transform visual)` consumes `RivalPaceProfileAsset.ToRuntime()` and updates visual position from controller/rules snapshots.
- `SprintWindCue.ShowCue(float leadSeconds)` and `ShowActiveWindow()` are visual-only notifications driven by `WindCueVisible` and `WindWindowActive`.

---

### Task 1: Lock the S6 runtime contracts with focused tests

**Files:**
- Modify: `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs`
- Create: `Assets/Tests/EditMode/Input/AlternateTapInputDetectorTests.cs`
- Create/modify: `Assets/Tests/PlayMode/Gameplay/Running/SprintRuntimeInputTests.cs`
- Inspect only: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`, `Assets/_Project/Scripts/Input/AlternateTapInputDetector.cs`

**Interfaces:** Tests use the existing controller test seams (`ConfigureForTest`, `ConfigureInputForTest`, `OnLeftTap`, `OnRightTap`, `AdvanceToDistance`, `Simulate`) and the S3 detector API. No test should require a real device or alter `SprintRules`.

- [ ] **Step 1: Add detector boundary tests.** Assert the first expected side is valid, the repeated side emits `OnWrongSide`, a valid alternating tap advances exactly once, and timestamps are passed through without reading `Time.time`.

- [ ] **Step 2: Add runtime mapping tests.** Build an in-memory `InputActionAsset` with `SprintLeft` and `SprintRight`, feed a valid sequence through the router, and assert `SprintController.Snapshot.Distance`/`ExpectedSide` change once per valid tap. Feed a wrong-side action and assert no second valid event is generated.

- [ ] **Step 3: Add authored gameplay contract tests.** Assert the wind cue is visible at the authored distance, `WindWindowActive` becomes true only after `0.8s`, correct counterplay emits one passing result, wrong counterplay emits one failing result, and later taps cannot emit another completion.

- [ ] **Step 4: Run the focused RED suite.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-task1-edit.xml -logFile /tmp/kma-s6-task1-edit.log -testFilter "AlternateTapInputDetectorTests" -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task1-play.xml -logFile /tmp/kma-s6-task1-play.log -testFilter "SprintRuntimeInputTests|SprintControllerTests" -quit
```

Expected: new tests fail only for missing S6 seams/behavior; existing Sprint tests must compile and remain green before implementation continues.

- [ ] **Step 5: Commit the test contract.** Run `rtk git diff --check`, stage only the S6 test files, and commit `test: define sprint runtime input contract`.

### Task 2: Wire the shared input asset and detector to Sprint

**Files:**
- Modify: `Assets/_Project/Scripts/Input/AlternateTapInputDetector.cs`
- Modify: `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- Modify: `Assets/_Project/Settings/Input/KMA.inputactions`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Test: `Assets/Tests/EditMode/Input/AlternateTapInputDetectorTests.cs`, `Assets/Tests/PlayMode/Gameplay/Running/SprintRuntimeInputTests.cs`

**Interfaces:** `KMA.inputactions` map `Sprint` exposes `SprintLeft` and `SprintRight`; the scene serializes that asset into `SprintController.inputActions`; `GameplayInputRouter` sends only valid detector events to Sprint.

- [ ] **Step 1: Implement the minimal detector event contract.** Store the expected side, emit `OnValidTap` only on the expected side, emit `OnWrongSide` for the other side, then advance expected side exactly once; do not reference `InputSystem`, `Time`, `MonoBehaviour`, or `GameObject`.

- [ ] **Step 2: Implement Sprint routing.** Bind the router to the shared `Sprint` action map and two `ScreenTapArea` instances; route left/right action callbacks to detector feed calls with the router timestamp, then map detector events to the existing controller tap methods.

- [ ] **Step 3: Rewire the scene asset.** Replace the serialized legacy input asset reference in `MG_Sprint.unity` with `KMA.inputactions`, preserve action names `SprintLeft`/`SprintRight`, and remove any second scene component that can read the same gameplay tap.

- [ ] **Step 4: Run focused tests GREEN.** Run the two Task 1 Unity commands again; expected: detector, action mapping, wind and one-completion tests pass, with no duplicate tap from EventSystem plus Input System.

- [ ] **Step 5: Commit** `feat: wire sprint through shared input layer` after `rtk git diff --check`.

### Task 3: Add Sprint HUD, wind cue and multi-step tutorial content

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintWindCue.cs`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs` or the Sprint-specific tutorial configuration location already used by the scene
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/_Project/Prefabs/UI/HUD_Minigame.prefab` only for shared fields that are absent
- Create/modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs`

**Interfaces:** `SprintHud` reads `SprintController.Snapshot`, `ExpectedSide`, `WindCueVisible`, `WindWindowActive`, `WindChallengeCountered`, and `WindChallengeFailed`; it displays timer, stamina, distance, rank `1st–4th`, cadence combo and wind state without writing rules state.

- [ ] **Step 1: Add presentation tests.** Load `MG_Sprint` with `LoadSceneMode.Single`; assert exactly one Sprint controller, scene-local HUD, pause button and tutorial overlay. Assert the tutorial has the steps `Tap the shown side` and `Counter the wind before the window closes`, and that the overlay can be skipped and remembered for Sprint.

- [ ] **Step 2: Implement the Sprint HUD read-only binding.** Cache references in `Awake`/`OnEnable`, poll `ReadHudState()` and controller properties in `Update`, update text/progress/images, and use event-driven/prefetched references so no `GetComponent` runs in `Update`.

- [ ] **Step 3: Implement wind cue visuals.** Show a clear cue when `WindCueVisible`, switch to active-window state when `WindWindowActive`, show success/failure feedback after resolution, and keep the authored reaction window visible long enough to be actionable; never hide or alter the actual collision/timing state.

- [ ] **Step 4: Configure the two bottom input zones and pause placement.** Set each zone to at least `140px` at reference resolution, leave the center-bottom strip empty, anchor Pause top-right inside `SafeAreaFitter`, and verify the camera/HUD use the project landscape canvas settings.

- [ ] **Step 5: Run presentation tests and commit** `feat: add sprint HUD wind cue and tutorial`.

### Task 4: Add authored rivals, lanes, parallax and Sprint scene polish

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/RivalRunnerAI.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintParallax.cs`
- Create: `Assets/_Project/Prefabs/Gameplay/RivalRunner.prefab`
- Create: `Assets/_Project/Prefabs/Gameplay/SprintLane.prefab`
- Create: `Assets/_Project/ScriptableObjects/Sprint/RivalPaceProfile_{Lane1,Lane3,Lane4}.asset`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Modify: `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs`

**Interfaces:** Three `RivalRunnerAI` instances consume `RivalPaceProfileAsset` values for lanes 1, 3 and 4; Sprint player occupies lane 2 and stays at `x=35%`. The visual race position is derived from the existing `SprintRules.RivalDistances` and never feeds back into rules.

- [ ] **Step 1: Add authored profile tests.** Assert each profile converts to a non-null `RivalPaceProfile` with opening/sustained speeds, the scene contains exactly three rival instances in lanes 1/3/4, and no rival is assigned player lane 2.

- [ ] **Step 2: Implement rival visual motion.** Configure each rival from its asset, map `RivalDistances` to lane-local positions, transition between idle/run/burst/stumble/celebrate/fail animation states from controller phase/result, and avoid creating/destroying objects during play.

- [ ] **Step 3: Implement three-layer parallax.** Use background layers at authored scroll multipliers, provide a minimum background width of `2560×1080` for 21:9 coverage, and recycle/loop layers without runtime allocation spikes.

- [ ] **Step 4: Add the 70% burst cue.** Drive the authored rival sprint visual from progress at `70%`; keep it cosmetic and deterministic, with no change to `SprintRules` rank or pass computation.

- [ ] **Step 5: Run controller, presentation and scene-contract tests, then commit** `feat: add sprint rivals and parallax presentation`.

### Task 5: Complete the S6 integration and device gate

**Files:**
- Verify: `Assets/_Project/Scenes/MG_Sprint.unity`, `Assets/_Project/Prefabs/Gameplay/`, `Assets/_Project/Settings/Input/KMA.inputactions`, `Assets/_Project/Scripts/Gameplay/Sprint/`
- Modify: `Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs` if device-observed contracts need regression coverage
- Create: `docs/qa/s6-sprint-device-gate.md`
- Do not modify: existing dirty files outside S6

**Interfaces:** The gate proves Sprint works as one routed minigame inside S5's session/result flow; it does not claim S7–S16 or the final Definition of Done.

- [ ] **Step 1: Run the complete Unity suites.** Use the pinned editor for full EditMode and PlayMode XML runs; record test totals, compiler output and unexpected errors in `/tmp/kma-s6-*` logs.

- [ ] **Step 2: Build the Android APK.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -buildOutput Builds/Android/kma-s6.apk -logFile /tmp/kma-s6-build.log -quit
```

- [ ] **Step 3: Execute the Sprint device scenario.** On a mid-range Android device, verify tutorial → countdown → play; tap alternating sides; observe timer/stamina/distance and rank; trigger wind and counter it; trigger wrong-side failure; pause/resume/restart/exit; confirm exactly one result panel and correct return route.

- [ ] **Step 4: Measure the S6 performance gate.** With Unity Profiler on the real device, record FPS, frame time and draw calls; verify 60fps target on a mid device, no sustained frame below 30fps during parallax/wind effects, and no duplicate input event.

- [ ] **Step 5: Record evidence.** Write device model/API, aspect ratio, build identifier, test results, screenshots or profiler observations, and any unavailable checks to `docs/qa/s6-sprint-device-gate.md`. Explicitly state that final balance pass criteria remain an S16 responsibility.

- [ ] **Step 6: Run `rtk git diff --check` and commit** `feat: make sprint playable` only after the device gate and all automated tests are evidenced.

## Verification Commands

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-editmode.xml -logFile /tmp/kma-s6-editmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-playmode.xml -logFile /tmp/kma-s6-playmode.log -quit
```

## Plan Self-Review

- Spec coverage: S6 input mapping, wind cue lead, 40% wrong-side impulse, HUD metrics, player/rival lanes, 70% rival burst, parallax, tutorial, pause, tests and Android 60fps gate are covered by Tasks 1–5.
- Existing contracts: rules and controller result computation remain authoritative; all new runtime behavior is adapter or presentation work.
- Placeholder scan: no `TBD`, `TODO`, or unassigned implementation step remains; S16 balance thresholds are explicitly outside this S6 plan.
- Dirty-worktree safety: the plan preserves the existing `README.md` and untracked completion/S5 plans and scopes S6 edits to Sprint/input/presentation/QA files.
