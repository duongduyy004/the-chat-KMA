# S8 Ball Presentation Kit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bổ sung bộ trình diễn bóng dùng chung cho 5 môn bóng: dự đoán quỹ đạo khi kéo, bóng đổ theo độ cao và năm `FlightProfile` authored, với kiểm chứng rằng hình ảnh không lệch khỏi physics deterministic hiện có.

**Architecture:** `BallRig` và `Ballistics` vẫn là nguồn sự thật duy nhất cho vị trí, vận tốc, độ cong và điểm rơi. `TrajectoryPreview` chỉ đọc `BallRig.PredictLandingPoint()` để dựng một `LineRenderer` đã cấp phát sẵn; `BallShadow` chỉ đọc snapshot/độ cao để cập nhật transform và alpha của shadow. Hai component không gọi rules, không tự mô phỏng physics khác và không tạo/hủy object trong runtime.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, 2D Physics, `LineRenderer`, `SpriteRenderer`, ScriptableObject `FlightProfile`, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §4 S8, §6 S8, §8, §9 và §10.

## Global Constraints

- Không sửa hành vi/chữ ký đã test của `BallRig`, `Ballistics` hoặc 5 rules engine; chỉ thêm component presentation và asset authored.
- `TrajectoryPreview` phải khớp `BallRig.PredictLandingPoint()`/`Ballistics.PredictGround()`; không duy trì công thức bay thứ hai.
- Preview chỉ hiện khi ball đang attached và người chơi đang kéo; khi đã launch hoặc không có ball thì phải ẩn sạch.
- `BallShadow` không thay đổi `Rigidbody2D`, vị trí thật, velocity hay kết quả rules; chỉ cập nhật visual đã serialize.
- Không `GetComponent` trong `Update`, không `Instantiate/Destroy` runtime; line points, shadow và material/renderer đều phải được gán hoặc cấp phát trước.
- `FlightProfile_Shuttle` phải có `LinearDrag` cao hơn rõ rệt các profile bóng và `BounceDamping == 0`.
- Mọi shell command chạy qua `rtk`; mỗi task có test và commit riêng, đồng thời giữ nguyên các thay đổi dirty ngoài S8.
- Không claim S9–S16, device gate hay Definition of Done toàn game khi kết thúc S8; gate này chỉ chứng minh shared kit.

## File Map

| Area | Files/responsibility |
|---|---|
| Presentation components | `Assets/_Project/Scripts/Gameplay/Ball/TrajectoryPreview.cs` samples the shared landing prediction; `Assets/_Project/Scripts/Gameplay/Ball/BallShadow.cs` maps height to shadow visual |
| Existing physics seam | `Assets/_Project/Scripts/Gameplay/Ball/BallRig.cs`, `BallFlightSnapshot.cs`, `FlightProfile.cs`; inspect and modify only if an additive read-only seam is required |
| Authored data | `Assets/_Project/ScriptableObjects/Ball/FlightProfile_{Volleyball,Basketball,PingPong,Shuttle,Football}.asset` and Unity `.meta` files |
| Reusable scene objects | `Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab` containing preallocated preview line and shadow; no subject-specific rules or controller |
| Tests | `Assets/Tests/EditMode/Gameplay/Ball/TrajectoryPreviewTests.cs`, `FlightProfileTests.cs`; extend `Assets/Tests/PlayMode/Gameplay/Ball/BallRigTests.cs` only for scene/MonoBehaviour integration |
| QA evidence | `docs/qa/s8-ball-presentation-kit.md` |

## Interfaces

- `TrajectoryPreview.Configure(BallRig source, LineRenderer line, int sampleCount, float sampleStep)` stores references once; `SetVisible(bool visible)` controls the preview; `Refresh(Vector2 launchDirection, float force, float curvature)` sets preallocated line points and returns the same landing point as `source.PredictLandingPoint()` for the current source state.
- `TrajectoryPreview.SampleLanding(Vector2 position, Vector2 velocity, Vector2 gravity, float groundY, float linearDrag, float curvature, float deltaTime)` is a pure static helper used by EditMode tests and delegates to `Ballistics.PredictGround`; it must not introduce alternate integration.
- `BallShadow.Configure(Transform target, Transform shadow, SpriteRenderer renderer, float groundY, float minScale, float maxScale)` stores references once; `Refresh()` maps target height above `groundY` to a clamped local scale and alpha, with the shadow centered at the target x and `groundY`.
- `FlightProfile` keeps its existing `GravityScale`, `LinearDrag`, `GroundY`, `BounceDamping` properties and `Create(...)` test factory. The five `.asset` files are data only and are referenced by future subject controllers.

### Task 1: Lock the shared presentation contracts with failing tests

**Files:**
- Create: `Assets/Tests/EditMode/Gameplay/Ball/TrajectoryPreviewTests.cs`
- Create: `Assets/Tests/EditMode/Gameplay/Ball/FlightProfileTests.cs`
- Modify: `Assets/Tests/PlayMode/Gameplay/Ball/BallRigTests.cs` only for an additive integration assertion if the EditMode tests cannot cover it
- Inspect only: `Assets/_Project/Scripts/Gameplay/Ball/{BallRig,BallFlightSnapshot,FlightProfile}.cs`

**Interfaces:** Tests use pure `Ballistics.PredictGround`, a `FlightProfile.Create(...)` fixture and the proposed read-only presentation seams. No test may require a volleyball/basketball controller or alter an existing rules test.

- [ ] **Step 1: Add the preview prediction contract test.** Create a ball with a non-zero velocity/curvature and assert that the preview's reported landing point equals `Ballistics.PredictGround(position, velocity, gravity, groundY, linearDrag, curvature, Time.fixedDeltaTime)` within `.001f` on both axes. Include a zero-flight/invalid-sample case that produces no visible line points rather than NaN or Infinity.
- [ ] **Step 2: Add the line ownership and visibility tests.** Assert `SetVisible(false)` disables the preallocated `LineRenderer`, `SetVisible(true)` enables it, and refresh never changes `BallRig.Body.position`, `Body.velocity` or `BallRig.Snapshot`. Assert repeated refreshes reuse the same line object and do not create children.
- [ ] **Step 3: Add the shadow mapping tests.** For target heights at ground, midpoint and above the configured maximum, assert shadow y stays at `groundY`, x follows the target, scale/alpha stay within configured bounds, and values clamp instead of becoming negative or transparent unexpectedly.
- [ ] **Step 4: Add the profile invariant tests.** Load the five expected asset paths after assets exist, assert each has finite non-negative drag, valid ground/bounce values, and assert `FlightProfile_Shuttle.LinearDrag > FlightProfile_Volleyball.LinearDrag` and `FlightProfile_Shuttle.BounceDamping == 0f`.
- [ ] **Step 5: Run the RED suites.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s8-task1-edit.xml -logFile /tmp/kma-s8-task1-edit.log -testFilter "TrajectoryPreviewTests|FlightProfileTests" -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s8-task1-play.xml -logFile /tmp/kma-s8-task1-play.log -testFilter "BallRigTests" -quit
```

Expected: the new tests fail because the presentation components/assets do not exist; the existing `BallRigTests` remain green.
- [ ] **Step 6: Commit the test contract.** Run `rtk git diff --check`, stage only the new S8 test files, and commit `test: define ball presentation contracts`.

### Task 2: Implement deterministic trajectory preview and ball shadow

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Ball/TrajectoryPreview.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Ball/BallShadow.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Ball/BallRig.cs` only if a public additive snapshot/profile seam is needed; preserve all existing methods and tests
- Test: `Assets/Tests/EditMode/Gameplay/Ball/TrajectoryPreviewTests.cs`, `Assets/Tests/PlayMode/Gameplay/Ball/BallRigTests.cs`

**Interfaces:** Implement exactly the `TrajectoryPreview` and `BallShadow` interfaces above. Cache all serialized references in `Awake`/`Configure`; do not call `GetComponent` from `Update`. A missing source/renderer disables the component and logs one actionable configuration error.

- [ ] **Step 1: Implement `TrajectoryPreview` with fixed preallocated samples.** Add serialized/default values `sampleCount = 16`, `sampleStep = 0.04f`, `minimumForce = 0.01f`; cache the source and line renderer; set `positionCount` once; use `BallRig.PredictLandingPoint()` for the final point and `Ballistics.AdvanceVelocity` only to interpolate the already-authoritative path samples. Keep the last point exactly at the predicted landing position so the visible endpoint cannot diverge.
- [ ] **Step 2: Implement dashed rendering without runtime allocation.** Configure the existing line material/texture for a dashed stroke, write points into a reusable `Vector3[]`, and hide unused points by setting `positionCount` to zero or a predeclared active count. Do not construct a new material, array, child object or GameObject per refresh.
- [ ] **Step 3: Implement drag/release visibility behavior.** `SetVisible(true)` only works when the source is attached and the launch force exceeds `minimumForce`; `SetVisible(false)` clears/hides the line. `Refresh(...)` must re-check those conditions, use the supplied direction/force/curvature only for the prospective preview, and never call `BallRig.Launch()`.
- [ ] **Step 4: Implement `BallShadow`.** Cache target/shadow/renderer; compute `height = max(0, target.position.y - groundY)`, `height01` against an explicit `maxHeight`, then map height to scale and alpha using clamped `Mathf.Lerp`. Keep the shadow on the ground plane and optionally flip its local x scale only through serialized settings, never by reading physics components each frame.
- [ ] **Step 5: Run focused GREEN tests.** Re-run Task 1 commands. Expected: landing endpoint matches `Ballistics.PredictGround`, line/shadow remain finite and bounded, and all existing `BallRigTests` pass without modifications to physics behavior.
- [ ] **Step 6: Commit the components.** Run `rtk git diff --check` and commit `feat: add deterministic ball trajectory and shadow presentation`.

### Task 3: Author the five reusable flight profiles and presentation prefab

**Files:**
- Create: `Assets/_Project/ScriptableObjects/Ball/FlightProfile_{Volleyball,Basketball,PingPong,Shuttle,Football}.asset` and `.meta` files
- Create: `Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab` and `.meta`
- Modify: `Assets/Tests/EditMode/Gameplay/Ball/FlightProfileTests.cs` if asset-loading assertions need the final GUID/path
- Do not modify: the five subject scenes or any subject rules/controller; S9–S13 will consume this kit

**Interfaces:** Each asset is a `FlightProfile` only. `BallPresentation.prefab` contains one inactive `TrajectoryPreview` with a preconfigured `LineRenderer`, one inactive `BallShadow` with a preconfigured shadow renderer, and no subject-specific input or scoring logic.

- [ ] **Step 1: Create the Ball folder and assets through Unity authoring.** Use the `KMA/Gameplay/Ball Flight Profile` asset type and assign these initial values, which later subject tuning may adjust without changing the component contract:

| Asset | GravityScale | LinearDrag | GroundY | BounceDamping |
|---|---:|---:|---:|---:|
| `FlightProfile_Volleyball` | `1.00` | `0.05` | `0.00` | `0.75` |
| `FlightProfile_Basketball` | `1.00` | `0.02` | `0.00` | `0.80` |
| `FlightProfile_PingPong` | `0.85` | `0.08` | `0.00` | `0.65` |
| `FlightProfile_Shuttle` | `0.90` | `4.00` | `0.00` | `0.00` |
| `FlightProfile_Football` | `1.10` | `0.03` | `0.00` | `0.60` |

- [ ] **Step 2: Build the reusable presentation prefab.** Add a child line object with a `LineRenderer` using a dashed, unlit material and a child shadow object with a white/soft circular sprite or approved placeholder. Serialize both references, use the existing `GameCamera` coordinate scale, and leave the prefab inactive until a subject controller attaches a `BallRig`.
- [ ] **Step 3: Validate prefab ownership.** Add a test or Editor validation that the prefab has exactly one `TrajectoryPreview`, one `BallShadow`, one `LineRenderer`, one shadow `SpriteRenderer`, no `Rigidbody2D`, and no `Update`-time component lookup path. This prevents the shared visual prefab from becoming a second physics owner.
- [ ] **Step 4: Run asset/profile tests.** Run `FlightProfileTests` and the existing `BallRigTests`; expected: all five assets load, Shuttle has high drag/zero bounce, and the profile values are available through the existing public properties.
- [ ] **Step 5: Commit authored data and prefab.** Run `rtk git diff --check`, stage only the Ball assets/prefab/tests owned by S8, and commit `feat: author shared ball flight profiles`.

### Task 4: Complete the S8 integration gate and document handoff

**Files:**
- Verify: `Assets/_Project/Scripts/Gameplay/Ball/`, `Assets/_Project/ScriptableObjects/Ball/`, `Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab`
- Create: `docs/qa/s8-ball-presentation-kit.md`
- Do not modify: `Assets/_Project/Scenes/MG_{Volleyball,Basketball,PingPong,Badminton,Football}.unity` except if a dedicated prefab import check requires a non-behavioral reference update approved by the task owner

**Interfaces:** The handoff documents the kit contract for S9–S13: attach a `BallRig`, call `TrajectoryPreview.Configure`, call `BallShadow.Configure`, and feed current drag state to `Refresh`; the future controller remains the owner of input and rules calls.

- [ ] **Step 1: Run all ball tests and the regression suites.** Run:

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s8-editmode.xml -logFile /tmp/kma-s8-editmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s8-playmode.xml -logFile /tmp/kma-s8-playmode.log -quit
```

Expected: the pre-existing suite and new S8 tests pass; no existing rules/controller contract is changed.
- [ ] **Step 2: Inspect runtime allocation and ownership.** Review the Unity Profiler/Frame Debugger on a minimal scene containing the prefab; confirm refresh does not instantiate/destroy objects, line points/materials are reused, and no `GetComponent` call occurs in the component update path.
- [ ] **Step 3: Run the visual smoke check.** In a temporary or existing ball test scene, attach the preview to a held ball, drag in at least two directions, release, and verify the dashed endpoint lands on the same ground x as `PredictLandingPoint`; move the ball through low/apex/high positions and verify the shadow remains under it and shrinks/fades monotonically.
- [ ] **Step 4: Write the handoff evidence.** Record Unity version, test XML result paths, the five profile values, prefab contents, visual smoke result, and any unavailable device checks. Explicitly state that subject scenes/controllers are intentionally not wired until S9–S13.
- [ ] **Step 5: Commit the QA evidence.** Run `rtk git diff --check`; stage only `docs/qa/s8-ball-presentation-kit.md` and commit `test: verify ball presentation kit`.

## Verification Commands

```bash
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s8-editmode.xml -logFile /tmp/kma-s8-editmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s8-playmode.xml -logFile /tmp/kma-s8-playmode.log -quit
rtk git diff --check
```

## Plan Self-Review

- Spec coverage: S8's trajectory preview, height-driven shadow, five profiles, shuttle high-drag/zero-bounce invariant and ballistics prediction test are covered by Tasks 1–4.
- Contract safety: `BallRig`, `Ballistics` and all existing rules remain authoritative; no S9–S13 controller is introduced by this plan.
- Runtime safety: preallocated line/shadow objects, cached references and no per-frame component lookup are explicit in Tasks 2–4.
- Type consistency: `Configure`, `SetVisible`, `Refresh` and `SampleLanding` are defined once in Interfaces and used consistently in tests and handoff.
- Scope: this plan does not claim Android gameplay, subject scene wiring, art/audio replacement or final performance/balance gates.
- Dirty-worktree safety: only S8-owned scripts, assets, prefab, tests and QA evidence are staged; the existing dirty files and prior S5–S7 plan files are preserved.
