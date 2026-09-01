# S9 Volleyball Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay `PlaceholderMinigameController` trong `MG_Volleyball` bằng một minigame bóng chuyền chơi được: swipe chọn dig/set/spike, bóng bay deterministic, đồng đội và người chơi tự vào vị trí, HUD/tutorial đầy đủ và đối thủ có counterplay được báo trước.

**Architecture:** `VolleyballRules` vẫn là nguồn sự thật cho resolve touch, điểm, combo, phase và kết quả; `VolleyballController` chỉ điều phối input, ball rig, vị trí actor, presentation và gọi rules. `BallRig`/`Ballistics` giữ quyền sở hữu physics; `TrajectoryPreview`, `BallShadow` và `BallPresentation.prefab` từ S8 chỉ được cấu hình và đọc trạng thái. HUD môn bóng chuyền là component riêng, còn `MinigameBase` vẫn cung cấp lifecycle, completion và HUD state chung.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, Unity Input System + `GameplayInputRouter`, 2D Physics, `BallRig`, S8 ball presentation kit, Unity Test Framework `InputTestFixture`, TextMeshPro.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §4 S9, §6 S9, §8, §9 và §10.

## Global Constraints

- Không sửa chữ ký, hành vi hoặc test của `VolleyballRules`, `VolleyReturnPattern`, `BallRig`, `Ballistics` hay rules engine khác; mọi tích hợp mới là additive.
- Input production đi qua `GameplayInputRouter.OnSwipe`; không đọc `Input.touches` hoặc tự tạo detector thứ hai trong controller.
- `BallContext` được tính từ `BallRig` hiện tại và vùng với tới được; `timingAccuracy` được truyền rõ vào `TryResolveAndLaunch`, không suy luận từ kết quả sau khi resolve.
- Điểm rơi, quỹ đạo và spin/fake phải dùng cùng `BallRig.PredictLandingPoint()`; counterplay chỉ thay cue/animation/trail, không đổi quỹ đạo giữa đường bay.
- S8 `TrajectoryPreview.Configure(...)`, `BallShadow.Configure(...)` và prefab `BallPresentation` là hợp đồng đầu vào; không thêm physics owner thứ hai vào prefab presentation.
- Tutorial có ba bước riêng cho `DIG`, `SET`, `SPIKE`, bấm qua được và đánh dấu `tutorialSeen` cho `Volleyball` qua `TutorialOverlay`/save flow hiện có.
- Mọi shell command trong quá trình thực hiện chạy qua `rtk`; mỗi task kết thúc bằng test, `rtk git diff --check` và commit riêng, giữ nguyên dirty files ngoài S9.
- Gate S9 chỉ chứng minh Volleyball; không claim 7 môn, boss, ending hoặc Definition of Done toàn game.

## File Structure

| Area | Files/responsibility |
|---|---|
| Runtime controller | `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs` owns lifecycle bridge, swipe subscription, context/reach calculation, auto-positioning, counterplay cue and result |
| Volleyball HUD | `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs` renders `TOUCH 1/2/3`, player/opponent scores and longest combo without changing rules |
| Input/test assembly | `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs`; add `KMA.Gameplay.Volleyball.PlayMode.Tests.asmdef` with references to `KMA.Gameplay`, `KMA.Gameplay.Ball`, `KMA.Gameplay.Volleyball`, `KMA.Gameplay.UI`, `KMA.Input`, `UnityEngine.TestRunner` |
| Scene | `Assets/_Project/Scenes/MG_Volleyball.unity` replaces placeholder with controller, ball, players, reach zone, `GameCamera`, shared presentation, generic HUD and Volleyball HUD |
| Tutorial | `Assets/_Project/Scripts/UI/PhaseOverlay.cs` additive Volleyball tutorial branch, or the existing tutorial data file if the current implementation has already extracted subject steps |
| Authored assets | `Assets/_Project/Prefabs/Gameplay/VolleyballPlayer.prefab`, `VolleyballTeammate.prefab`, `VolleyballNet.prefab`, plus placeholder-safe sprites/materials under `Assets/_Project/Art/` or existing S8-compatible assets |
| QA evidence | `docs/qa/s9-volleyball-device-gate.md` records tests, scene smoke check, Android result and unavailable checks |

## Interfaces

- `VolleyballController` derives from `MinigameBase` and exposes read-only runtime state:

```csharp
public VolleyballRules Rules { get; }
public BallRig Ball { get; }
public BallContext CurrentContext { get; }
public bool InReachZone { get; }
public int TouchCount { get; }
public int PlayerScore { get; }
public int OpponentScore { get; }
public int LongestCombo { get; }
public bool OpponentCounterCueVisible { get; }
public bool OpponentFakeCueVisible { get; }
public Vector2 PredictedLandingPoint { get; }
public void SubmitSwipe(Vector2 swipe, bool inReachZone, float timingAccuracy);
public void ConfigureForTest(VolleyballRules rules, BallRig ball);
```

- `SwipeResult` maps `Left/Right/Up/Down` to a normalized `Vector2`; length, duration and curvature are retained for preview/telemetry, while `timingAccuracy` is supplied by the authored touch-window calculation. `SubmitSwipe` calls exactly `Rules.TryResolveAndLaunch(Ball, CurrentContext, swipe, inReachZone, timingAccuracy)` and only increments controller-side counters after a valid resolve.
- `VolleyballHud.Refresh(VolleyballController source)` reads `TouchCount`, `CurrentContext`, `PlayerScore`, `OpponentScore`, `LongestCombo` and cue flags; it never calls rules methods.
- `VolleyballController.BuildHudState()` returns the existing generic state with phase, `Rules.Elapsed`, normalized score progress, and status text (`TOUCH 1/2/3`, `MOVE INTO REACH`, `COUNTER THE FAKE`, or `READY`).
- `BallContext` calculation is deterministic: `Low` when the ball is descending and not near the authored net apex window; `Rising` when `Ball.Body.velocity.y > 0`; `ApexNearNet` when `Ball.IsNearApex(apexVelocityThreshold)` and ball x is within `netApexWindow` of `netX`. `InReachZone` is computed from the serialized `BoxCollider2D`/bounds around the player, not from an invisible hard-coded second position.
- Auto-positioning samples `Ball.PredictLandingPoint()` and moves the player/teammate toward authored x offsets around that point. It may move transforms only; it must not modify ball body position/velocity or replace `BallRig` physics.

### Task 1: Lock controller, input and counterplay contracts with failing tests

**Files:**
- Create: `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs`
- Create: `Assets/Tests/PlayMode/Gameplay/Ball/KMA.Gameplay.Volleyball.PlayMode.Tests.asmdef` and `.meta`
- Inspect only: `Assets/_Project/Scripts/Gameplay/Volleyball/{VolleyballRules,VolleyReturnPattern}.cs`, `Assets/_Project/Scripts/Gameplay/Ball/{BallRig,BallFlightSnapshot}.cs`, `Assets/_Project/Scripts/Input/{GameplayInputRouter,SwipeInputDetector}.cs`

**Interfaces:** Tests use the public interfaces above and a `BallRig` fixture with an explicit `FlightProfile`; no existing rules test is changed.

- [ ] **Step 1: Add the controller construction and lifecycle tests.** Create a GameObject with `VolleyballController`, call `ConfigureForTest` with `new VolleyballRules(targetScore: 2, timeLimit: 60f)` and a ball fixture, advance lifecycle to `Play`, and assert `Rules`, `Ball`, `CurrentContext`, `PlayerScore`, `OpponentScore` and `LongestCombo` are exposed without null state.
- [ ] **Step 2: Add swipe-to-rules tests.** Submit a valid low-ball downward swipe, rising-ball swipe and apex-near-net right/down swipe; assert the controller selects `Dig`, `Set`, `Spike` through the resulting `VolleyReturnPattern`, launches once, and rejects an out-of-reach or below-threshold timing swipe without changing score/touch count. Include all four `SwipeResult` directions through an `InputTestFixture`/`GameplayInputRouter` fixture so production `OnSwipe` dispatch is covered.
- [ ] **Step 3: Add context/reach and prediction tests.** Set ball height/velocity and player reach bounds for each context; assert the exact context and reach result. Capture `Ball.PredictLandingPoint()` before controller auto-positioning, simulate one frame, and assert the controller's `PredictedLandingPoint` is equal within `.001f` and the ball body position/velocity are unchanged.
- [ ] **Step 4: Add rally-three counterplay tests.** Resolve three valid touches, assert the opponent cue becomes visible before the next launch, assert hand-animation/trail state is represented by public cue properties, and compare predicted landing before/after the cue. The points must remain unchanged until the rules award a rally point, and the ball prediction must not change merely because spin/fake is shown.
- [ ] **Step 5: Add HUD/result tests.** Assert `BuildHudState().statusText` reports touch guidance and `BuildHudState().score` follows the rules result. Complete the primary objective and assert `Completed` fires once; repeated swipes/ticks after resolve do not fire it again.
- [ ] **Step 6: Run the RED suite.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s9-task1-play.xml -logFile /tmp/kma-s9-task1-play.log -testFilter "VolleyballControllerTests" -quit
```

Expected: the new tests fail because `VolleyballController` and its test assembly do not exist; existing `VolleyballRulesTests` remain green when run separately.
- [ ] **Step 7: Commit the contract tests.** Run `rtk git diff --check`, stage only the new S9 test/asmdef files, and commit `test: define volleyball controller contracts`.

### Task 2: Implement the controller and deterministic ball integration

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs` and `.meta`
- Modify: `Assets/_Project/Scripts/Gameplay/Volleyball/KMA.Gameplay.Volleyball.asmdef` only if the controller requires the existing `KMA.Input` or `KMA.Gameplay.UI` assembly references
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs`
- Do not modify: `VolleyballRules.cs`, `VolleyReturnPattern.cs`, `BallRig.cs` or their tests

**Interfaces:** Implement the exact controller interface above. Cache serialized references in `Awake`/configuration; subscribe/unsubscribe to `GameplayInputRouter.OnSwipe` in `OnEnable`/`OnDisable` and never subscribe twice.

- [ ] **Step 1: Implement authored fields and initialization.** Add serialized `VolleyballRules` parameters (`targetScore = 5`, `timeLimit = 60f`), `BallRig`, `GameplayInputRouter`, player/teammate transforms, reach-zone collider, net x, apex threshold/window and timing threshold. Construct rules in `Awake` using the serialized values and preserve `MinigameBase` lifecycle defaults.
- [ ] **Step 2: Implement swipe dispatch and action mapping.** Convert `SwipeResult.Direction` to a vector, reject non-Play phases, calculate the authored timing accuracy from the touch window, and call `SubmitSwipe`. `SubmitSwipe` must call `Rules.TryResolveAndLaunch` once; on success update touch count, selected action/cue state and attach/configure the shared preview before the launch. Do not call `Launch` directly from the controller.
- [ ] **Step 3: Implement context and reach calculation.** Read `Ball.Snapshot`/`Ball.Body.velocity`, apply the exact context order from Interfaces, and use the serialized reach-zone bounds for `InReachZone`. Expose `CurrentContext` and `InReachZone` for HUD/tests.
- [ ] **Step 4: Implement auto-positioning and preview/shadow wiring.** Cache `TrajectoryPreview` and `BallShadow` references from the scene/prefab once, call `Configure` once, refresh the preview only while the ball is attached and a swipe is being prepared, and update player/teammate target positions from `Ball.PredictLandingPoint()`. Never alter the ball's body transform or velocity during repositioning.
- [ ] **Step 5: Implement rally-three cue without physics mutation.** Track valid touches/rally phase, enable `OpponentCounterCueVisible` and `OpponentFakeCueVisible` before the next opponent return after rally 3, select only visual hand/trail states, and leave `BallRig` launch direction/force/curvature owned by `VolleyReturnPattern`. Clear cues at the authored launch/point boundary.
- [ ] **Step 6: Implement HUD state and terminal result.** Return generic `MinigameHudState`, tick `Rules.Tick(dt)` during Play, call `Finish(Rules.BuildResult())` once when `Rules.Phase` enters Resolve, and ensure `BuildResult` is stable after completion.
- [ ] **Step 7: Run the GREEN suite and regression.** Re-run the focused S9 PlayMode command, then run the existing Volleyball EditMode tests and BallRig PlayMode tests. Expected: all pass with no changes to rules/physics behavior.
- [ ] **Step 8: Commit runtime integration.** Run `rtk git diff --check` and commit `feat: add volleyball gameplay controller`.

### Task 3: Build the Volleyball HUD, scene and tutorial content

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs` and `.meta`
- Create: `Assets/_Project/Prefabs/Gameplay/VolleyballPlayer.prefab`, `VolleyballTeammate.prefab`, `VolleyballNet.prefab` and `.meta` files
- Modify: `Assets/_Project/Scenes/MG_Volleyball.unity`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs` add the Volleyball tutorial branch and Vietnamese-safe labels
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/VolleyballControllerTests.cs` plus scene presentation assertions in the S9 test assembly

**Interfaces:** The scene contains one `VolleyballController`, one `BallRig` physics owner, the S8 presentation prefab, one generic `MinigameHUD`, one `VolleyballHud`, `GameCamera`, reach-zone collider, net, player and teammate. No `PlaceholderMinigameController` remains in this scene.

- [ ] **Step 1: Add failing scene/HUD assertions.** Load `MG_Volleyball` and assert the scene has a camera, `VolleyballController`, `BallRig`, `TrajectoryPreview`, `BallShadow`, generic HUD, Volleyball HUD, exactly one active physics ball and no `PlaceholderMinigameController`. Assert the custom HUD exposes labels for `TOUCH 1/2/3`, player/opponent score and longest combo.
- [ ] **Step 2: Implement `VolleyballHud`.** Serialize TMP labels/fills and optional cue text; refresh from the controller each frame or through an explicit `Refresh` call. Render touch progress as `TOUCH {clamped count}/3`, scores as `player-opponent`, longest combo, current context and counter cue. Keep the generic timer/phase overlay owned by `MinigameHUD`/`PhaseOverlay`.
- [ ] **Step 3: Author safe placeholder scene objects.** Use the existing `GameCamera.prefab` and `BallPresentation.prefab`; add a ball Rigidbody2D + BallRig, player/teammate targets, a visible net and a reachable court background. Set serialized profile to `FlightProfile_Volleyball`, configure preview/shadow references, and use unlit placeholder materials/sprites that remain visible on a low-end Android device.
- [ ] **Step 4: Replace the placeholder controller and wire references.** Remove only the Volleyball placeholder object/component from `MG_Volleyball`, attach the controller and HUD, assign `KMA.inputactions`/`GameplayInputRouter`, assign reach zone and actor transforms, and keep the scene name/build index unchanged for `SceneRouter`.
- [ ] **Step 5: Add the three tutorial steps.** Configure `TutorialOverlay.Show("Volleyball", ...)` with:

```csharp
new TutorialStep("DIG", "Vuốt khi bóng thấp để đỡ bóng"),
new TutorialStep("SET", "Vuốt khi bóng đang bay lên để chuyền bóng"),
new TutorialStep("SPIKE", "Vuốt phải + xuống gần đỉnh lưới để đập bóng")
```

Use existing next/back/skip controls, preserve the per-subject seen key/save behavior, and make the overlay disappear only after the player completes or skips it.
- [ ] **Step 6: Run scene and HUD tests.** Expected: scene loads without missing references, all required labels/components exist, tutorial text is present, and the controller tests remain green.
- [ ] **Step 7: Commit authored scene and presentation.** Run `rtk git diff --check`, stage only S9 runtime/HUD/scene/prefab/tutorial files, and commit `feat: author volleyball scene and HUD`.

### Task 4: Verify the S9 gate on the full regression suite and Android

**Files:**
- Create: `docs/qa/s9-volleyball-device-gate.md`
- Verify: S9 controller/HUD/scene files and `Assets/_Project/ScriptableObjects/Subjects/Volleyball.asset`
- Do not modify: other subject scenes/controllers or unrelated dirty files

**Interfaces:** The QA note records the exact build/test commands, scene contract, input path, and device observations; it does not declare completion of later sections.

- [ ] **Step 1: Run focused S9 tests.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s9-volleyball-play.xml -logFile /tmp/kma-s9-volleyball-play.log -testFilter "VolleyballControllerTests" -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s9-volleyball-edit.xml -logFile /tmp/kma-s9-volleyball-edit.log -testFilter "VolleyballRulesTests|Ballistics|FlightProfile" -quit
```

Expected: focused S9 tests and all existing Volleyball/Ball tests pass.
- [ ] **Step 2: Run the full regression suites.** Run the project EditMode and PlayMode suites with results/logs under `/tmp/kma-s9-full-*`; record test counts and any environment limitation. Passing S9 tests does not excuse a regression in the pre-existing 158-test contract.
- [ ] **Step 3: Run the Android build and device smoke flow.** Build the configured APK, install on a real Android device, and verify: Menu → Map → Volleyball; tutorial next/back/skip; touch guidance; low/rising/apex swipe actions; predicted landing and auto-positioning; rally-3 cue before launch; score/combo updates; pass/fail result; pause Resume/Restart/Exit routes. Capture the device model, Android version, Unity build command, and APK size.
- [ ] **Step 4: Check visual and performance invariants.** On 16:9 and a tall/notched device, verify no required HUD element is cut off, the ball/preview/shadow remain visible, the cue appears before counterplay, and there is no black screen. Use Profiler on the real device to record FPS and draw calls for `MG_Volleyball`; note unavailable measurements explicitly.
- [ ] **Step 5: Write the QA evidence.** Include test result paths, scene object contract, tutorial strings, input route, counterplay observation, save/tutorial-seen observation and device/performance results. State clearly which checks were not available if hardware/tooling blocks them.
- [ ] **Step 6: Commit the gate evidence.** Run `rtk git diff --check`, stage only `docs/qa/s9-volleyball-device-gate.md`, and commit `test: verify volleyball vertical slice`.

## Verification Commands

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s9-editmode.xml -logFile /tmp/kma-s9-editmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s9-playmode.xml -logFile /tmp/kma-s9-playmode.log -quit
rtk git diff --check
```

## Plan Self-Review

- **Spec coverage:** S9 swipe mapping, `BallContext`/reach, `PredictLandingPoint` auto-positioning, `TOUCH 1/2/3` + score/combo HUD, rally-3 spin/fake cue, tutorial content, scene replacement, PlayMode input tests and Android gate are covered by Tasks 1–4.
- **Contract safety:** Existing Volleyball rules and BallRig remain authoritative; controller calls `TryResolveAndLaunch` and never reproduces scoring/ballistics.
- **Placeholder scan:** No `TBD`, `TODO`, “implement later”, or vague “write tests for the above” steps are used; every implementation step names a file, behavior and verification.
- **Type consistency:** `SwipeResult`, `BallContext`, `VolleyballRules.TryResolveAndLaunch`, `BallRig.PredictLandingPoint`, `VolleyballController.SubmitSwipe`, and `VolleyballHud.Refresh` are defined once and reused consistently.
- **Dirty-worktree safety:** Existing modified fonts/README/editor assets and other S5–S8 plan files are outside the S9 staging scope and must remain untouched.
- **Scope:** The plan does not claim Basketball/PingPong/Badminton/Football, Boss, S15 ending or S16 release balance; those remain later sections.
