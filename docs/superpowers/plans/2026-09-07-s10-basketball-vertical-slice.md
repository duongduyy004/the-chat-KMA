# S10 Basketball Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay `PlaceholderMinigameController` trong `MG_Basketball` bằng một minigame bóng rổ chơi được: giữ để nạp lực lob, vuốt để chuyền, đồng đội bay vào bắt bóng, tap đúng đỉnh để kết thúc rổ; HUD apex ring + nhãn `EARLY/PERFECT/LATE`, tutorial 3 bước, và mỗi bậc độ khó chỉ tăng **một** trục.

**Architecture:** `BasketballRules` + `AlleyOopPattern` vẫn là nguồn sự thật duy nhất cho state machine, judge và điểm; `BasketballController` chỉ điều phối input, ball rig, vị trí actor, presentation và gọi rules theo đúng thứ tự `Hold → TryPass → TryLaunchAlleyOop → TapFinish`. `BallRig`/`Ballistics` giữ quyền sở hữu physics; `TrajectoryPreview` và `BallShadow` từ S8 chỉ được `Configure` rồi đọc trạng thái. Input production đi qua đúng một `GameplayInputRouter` với ba detector plain-C# do controller cài qua API additive; controller sở hữu deadline và tự gọi `Finish` để `Completed` phát đúng một lần.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, Unity Input System + EnhancedTouch + `GameplayInputRouter`, 2D Physics, `BallRig`, S8 ball presentation kit, uGUI/TextMeshPro, Unity Test Framework `1.6.0`, Android IL2CPP ARM64.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` — §4 (S10 phụ thuộc S5 + S8), §6 S10, §9, §10. Plan chủ: `docs/superpowers/plans/2026-08-30-kma-game-completion.md`.

## Global Constraints

- Toolchain: Unity `6000.3.23f1` tại `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`. Shell là **PowerShell trên Windows**; `rtk` và mọi đường dẫn Linux trong các plan S1–S9 cũ **không áp dụng**. Project path `D:\project\the-chat-KMA`.
- Không đổi chữ ký, không đổi hành vi đã có test của `BasketballRules`, `AlleyOopPattern.IsApexWindow`, `AlleyOopPattern` constructor, `BallRig`, `Ballistics`, `MinigameBase`, `MinigameLifecycle`, `SceneRouter`, `GameSession`. Ngoại lệ duy nhất được phép, và chỉ trong Task 2: **một hằng số authored** trong `AlleyOopPattern.AuthoredDefault`.
- Mọi thay đổi `GameplayInputRouter` phải là **additive**: chỉ thêm method/event mới, không sửa `SetDetectors`, `SetSwipeDetector` hay bất kỳ dispatch nào đang có.
- Không dùng `PlayerPrefs`. Không dùng random của Unity trong gameplay: mọi biến thiên đến từ authored data (`AlleyOopPattern`, bảng độ khó serialized).
- Chỉ `PrimaryObjective` (5 rổ) được đặt `Pass = true`. Mọi sự kiện bất lợi phải có cue và cửa sổ counterplay deterministic — ở đây là apex ring + dải charge phát sáng, hiện **trước** khi phải phản ứng.
- Giữ `LoadSceneMode.Single`, giữ tên scene `MG_Basketball` và build index hiện tại để `SceneRouter.DefaultSubjectScenes()` (`Assets/_Project/Scripts/Core/SceneRouter.cs:626`) không đổi. `SubjectConfig` không được mang tên scene.
- Android landscape; Canvas reference `1920×1080`, `Match Width Or Height = 1.0`; không realtime light, không post-process; pool mọi FX; **không `GetComponent` trong `Update`** — cache trong `Awake`/`Configure`.
- Repo đang dirty (`.claude/`, `.superpowers/sdd/2026-09-02-s1-s9-stabilization/`). Chạy `git status --short` trước mỗi task, chỉ stage file thuộc task, giữ nguyên mọi path dirty có trước.
- Mỗi task kết thúc bằng: test pass → `git diff --check` → commit riêng. Sau mỗi task chạy lại full EditMode và PlayMode; baseline hiện tại là **EditMode 253/253, PlayMode 193/193** (`docs/qa/s1-s9-stabilization-gate.md`) và không được tụt.
- Gate S10 chỉ chứng minh Basketball. Không claim S11–S16, boss, ending hay Definition of Done toàn game.

## Reference: verified ground truth

Đọc trước khi viết code. Mọi con số dưới đây đã kiểm bằng file thật, không phải suy đoán.

**`BasketballRules` (`Assets/_Project/Scripts/Gameplay/Basketball/BasketballRules.cs`) — API cố định:**

```csharp
public enum BasketballState { Holding, Passing, AlleyOopFlight, Resolved }
public enum FinishJudge { Ignored, Early, Perfect, Late }

public BasketballRules(int targetBaskets = 5, float timeLimit = 30f, MinigameLifecycle lifecycle = null)
public BasketballState State { get; }
public MinigamePhase Phase { get; }          // = lifecycle.Phase
public int Baskets { get; } public int Attempts { get; }
public int Combo { get; } public int BestCombo { get; }
public float Elapsed { get; } public float ApexProgress { get; }   // rules chỉ set 0 khi launch, 1 khi Perfect
public bool PrimaryObjectiveComplete { get; } // Baskets >= targetBaskets
public AlleyOopPattern AuthoredPattern { get; }
public void Hold(float deltaTime)                        // no-op có chủ đích
public bool TryPass(BallRig ball, Vector2 passVector)    // Holding + Play + passVector != 0
public bool TryLaunchAlleyOop(BallRig ball)              // Passing + Play
public FinishJudge TapFinish(float ballY, float velocityY)
public void Tick(float deltaTime)
public MinigameResult BuildResult()
```

**Ba sự thật quyết định thiết kế, đã verify:**

1. `TryPass` gọi `ball.AttachTo(ball.transform)` — bóng bị ghim tại **vị trí hiện tại của chính nó**, nên controller phải đưa bóng vào tay người chơi (`ball.AttachTo(playerHand)`) **trước** khi gọi `TryPass`. Sau `TryPass` bóng vẫn `IsAttached == true`, nên `TrajectoryPreview` vẫn hiện được (nó yêu cầu `source.Snapshot.IsAttached`).
2. `TapFinish` khi đủ 5 rổ tự gọi `lifecycle.BeginResolve()`. `MinigameBase.Finish(result)` chỉ phát `Completed` nếu `Lifecycle.BeginResolve()` trả `true`. Nếu controller **chia sẻ** `Lifecycle` của mình cho rules thì `Finish` sẽ trả về false và `Completed` **không bao giờ phát** → scene treo, không có Result panel. Đây đúng là lỗi S9 đã gặp ở Volleyball (`docs/qa/s1-s9-stabilization-gate.md`). Vì vậy S10 **cố ý** dựng rules với lifecycle riêng (`lifecycle: null` → rules tự tạo `new MinigameLifecycle(0f, 0f)` và tick thẳng vào `Play`), controller giữ lifecycle presentation của riêng nó và tự gọi `Finish`. Test phải dùng **đúng đường dựng production này**, không được inject rules có lifecycle khác — chính sự lệch đó đã che lỗi S9.
3. `AlleyOopPattern.AuthoredDefault(passVector)` = `new AlleyOopPattern(passVector ?? (1, .75), 8f, 0f, 2.8f, 3.2f, .1f)`. Với `Time.fixedDeltaTime = 0.02` và gravity `-9.81`, mỗi bước physics đổi `velocityY` khoảng `0.196`. Cửa sổ `|velocityY| <= .1` do đó mở khoảng `2*.1/9.81 = 0.0204 s` — **dưới một bước physics**, nên `FinishJudge.Perfect` gần như không thể đạt trong build. Task 2 xử lý điều này.

**Ballistics đã sở hữu (`Assets/_Project/Scripts/Gameplay/Ball/BallRig.cs`):**

```csharp
public static Vector2 Ballistics.AdvanceVelocity(Vector2 velocity, Vector2 gravity, float curvature, float linearDrag, float deltaTime)
public Vector2 BallRig.PredictLandingPoint()
public Vector2 BallRig.PredictLandingPoint(Vector2 direction, float force, float curvature)
public void BallRig.Launch(Vector2 direction, float force, float curvature)   // velocity = direction.normalized * force
public void BallRig.AttachTo(Transform target)
public bool BallRig.IsNearApex(float threshold)
public BallFlightSnapshot BallRig.Snapshot { get; }   // Position, Velocity, IsAttached, IsInFlight, Curvature
public FlightProfile BallRig.Profile { get; }
```

`Assets/_Project/ScriptableObjects/Ball/FlightProfile_Basketball.asset`: `gravityScale: 1`, `linearDrag: 0.02`, `groundY: 0`, `bounceDamping: 0.8`.

**Bài toán độ cao — đã tính, dùng làm authored layout:**

- Rổ thật cao `3.05 m`; dải apex authored `[2.8, 3.2]` bao quanh đúng con số đó. Đặt vành rổ tại `y = 3.05`.
- Tay người chơi (điểm phóng) `launchHeight = 1.2`. Lực authored cố định `8`.
- Apex trên điểm phóng = `vy0² / (2 * 9.81)`. Để apex nằm trong `[2.8, 3.2]` cần apex-trên-điểm-phóng `∈ [1.6, 2.0]` → `vy0 ∈ [5.603, 6.264]` → `sin(angle) = vy0/8 ∈ [0.7004, 0.7830]` → **`angle ∈ [44.45°, 51.53°]`**.
- Với `angle` sinh từ charge trên dải `span` quanh tâm `47.5°`: `span = 35` → dải charge đúng rộng `(51.53-44.45)/35 = 0.202` (≈202 ms trên `maxChargeSeconds = 1`); `span = 45` → `0.157`; `span = 55` → `0.129`. Đó là trục "đường alley-oop khó hơn".
- Thời gian tới apex ≈ `6.0/9.81 = 0.61 s`; tầm ngang tới apex ≈ `(8*0.72)*0.61 ≈ 3.5`. Đặt người chơi tại `x = -3.5`, rổ tại `x = 0`.
- `linearDrag = 0.02` làm apex thấp hơn lý thuyết khoảng 1%. Vì vậy controller **không** dùng công thức đóng: nó lặp đúng integrator `Ballistics.AdvanceVelocity` để tìm apex, nên số hiển thị và số physics luôn khớp.

**Presentation kit S8 (chỉ `Configure` + đọc):**

```csharp
public void TrajectoryPreview.Configure(BallRig sourceRig, LineRenderer lineRenderer, int samples, float step)
public Vector2 TrajectoryPreview.Refresh(Vector2 direction, float force, float curvature)
public void TrajectoryPreview.SetVisible(bool visible)   // chỉ hiện khi source.Snapshot.IsAttached && lastForce > minimumForce
public void BallShadow.Configure(Transform target, Transform shadow, SpriteRenderer renderer, float ground, float maxHeight, float minScale, float maxScale, float minAlpha, float maxAlpha)
public void BallShadow.Refresh()
```

Prefab `Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab` chứa `BallShadowVisual` và `TrajectoryPreviewLine`; prefab **không có renderer cho chính quả bóng** — S9 phải thêm renderer trên instance trong scene, S10 cũng phải làm vậy.

**Input layer (`Assets/_Project/Scripts/Input/`):** `GameplayInputRouter` là MonoBehaviour duy nhất đọc Input System/EnhancedTouch. Trong `FeedPointerDown` nó gọi `tapMashDetector?.FeedTap`, `holdDetector?.FeedDown`, `swipeDetector?.FeedSample`; trong `FeedPointerUp` nó gọi `holdDetector?.FeedUp` **trước** rồi mới `swipeDetector?.FeedEnd()`. Thứ tự đó có nghĩa `OnHoldEnd` luôn đến **trước** `OnSwipe` trong cùng một lần nhấc ngón — đúng thứ tự "nạp lực rồi nhả" mà S10 cần. Router hiện phát `OnHoldEnd`, `OnSwipe`, `OnSwipeProgress` nhưng **không có event tap** và **không có setter riêng** cho tap/hold; chỉ có `SetSwipeDetector` và `SetDetectors(...)` (destructive). Task 1 thêm hai setter và một event, additive.

**Shared presentation đã có sẵn trong `MG_Basketball.unity`** (đã verify bằng guid): `GameCamera.prefab`, `HUD_Minigame.prefab` (`MinigameHUD`), `PhaseOverlay.prefab`, `ResultPanel.prefab`, `PausePanel`, `GameplayPresentation`, `EventSystem`, và `Placeholder_MG_Basketball` mang `PlaceholderMinigameController`. **Chưa có** `GameplayInputRouter`, `ScreenTapArea`, ball, actor, court. `GameCamera` bị `MinigameUIAssembler.ConfigureCamera` khoá tại `(0, 0, -10)`, `orthographic`, `orthographicSize = 5.4`.

`Assets/_Project/ScriptableObjects/Subjects/Basketball.asset` đã có: `subjectId: 3`, `displayName: Bóng rổ`, `goalText: Canh lực và thời điểm để ghi rổ.`, `timeLimit: 60`, `unlocked: 1`, `comingSoon: 0`. Node Map đã mở, route đã map — **không cần sửa file nào trong số này**.

Sprite placeholder: các renderer trong `MG_Volleyball` dùng sprite built-in `{fileID: 10905, guid: 0000000000000000f000000000000000}`, bounds `0.16 × 0.16` world unit. Do đó `localScale = worldSize / 0.16f`.

## File Structure

| Area | Files / responsibility |
|---|---|
| Input layer (additive) | `Assets/_Project/Scripts/Input/GameplayInputRouter.cs` — thêm `OnTap`, `SetTapMashDetector`, `SetHoldDetector`; `Assets/_Project/Settings/Input/KMA.inputactions` — thêm map `Basketball` cho keyboard fallback |
| Authored apex data | `Assets/_Project/Scripts/Gameplay/Basketball/AlleyOopPattern.cs` — chỉ hằng số `velocityThreshold` trong `AuthoredDefault` |
| Runtime controller | `Assets/_Project/Scripts/Gameplay/Basketball/BasketballController.cs` — lifecycle bridge, charge→passVector, AI launch delay, apex prediction, cue, auto-position, deadline, result |
| Độ khó authored | `Assets/_Project/Scripts/Gameplay/Basketball/BasketballDifficultyStep.cs` — struct serializable một-trục-một-bậc |
| Basketball HUD | `Assets/_Project/Scripts/Gameplay/Basketball/BasketballHud.cs` — apex ring, vùng apex phát sáng, `EARLY/PERFECT/LATE`, baskets/attempts/combo, dải charge mục tiêu |
| Tutorial | `Assets/_Project/Scripts/UI/PhaseOverlay.cs` — thêm nhánh `BasketballController` (3 bước) |
| Scene authoring | `Assets/Editor/BasketballSceneConfigurator.cs` — script `[MenuItem]` + `-executeMethod` dựng lại `MG_Basketball` một cách tái lập được |
| Scene | `Assets/_Project/Scenes/MG_Basketball.unity` — bỏ placeholder, thêm controller/ball/court/hoop/actors/router/surface/HUD |
| Tests | `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/{BasketballControllerTests,BasketballSceneTests}.cs` + `KMA.Gameplay.Basketball.PlayMode.Tests.asmdef`; `Assets/Tests/EditMode/Gameplay/Ball/AlleyOopWindowTests.cs`; `Assets/Tests/EditMode/Input/InputAssetContractTests.cs` (cập nhật contract); `Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs` |
| Docs | `README.md` (bảng Scenes + Controls), `Assets/_Project/Scripts/Core/GameplayPresentation.cs` (title/controls cho `MG_Basketball`), `docs/qa/s10-basketball-device-gate.md` |
| Evidence | `.superpowers/sdd/2026-09-07-s10-basketball/` — mọi `.xml` và `.log` |

## Interfaces

`BasketballController` kế thừa `MinigameBase`, namespace `KMA.Gameplay`:

```csharp
public sealed class BasketballController : MinigameBase
{
    // Rules + physics owners
    public BasketballRules Rules { get; }
    public BallRig Ball { get; }
    public GameplayInputRouter InputRouter { get; }
    public AlleyOopPattern AuthoredBand { get; }        // = AlleyOopPattern.AuthoredDefault(Vector2.right), chỉ để đọc dải apex

    // Actors + presentation
    public Transform PlayerHand { get; }
    public Transform Finisher { get; }
    public TrajectoryPreview Preview { get; }
    public BallShadow Shadow { get; }

    // Charge / aim
    public bool IsCharging { get; }
    public float ChargeRatio { get; }                   // 0..1, live trong lúc giữ
    public float PassAngleDegrees { get; }              // góc tương ứng ChargeRatio hiện tại
    public Vector2 PendingPassVector { get; }
    public float TargetChargeMin { get; }               // dải charge cho apex trong [AuthoredBand.ApexMin, ApexMax]
    public float TargetChargeMax { get; }

    // Flight / finish
    public Vector2 PredictedApexPoint { get; }
    public float SecondsToApex { get; }
    public float FlightApexProgress { get; }            // 0..1 tới apex; KHÁC Rules.ApexProgress (rules chỉ 0/1)
    public bool FinishCueVisible { get; }
    public FinishJudge LastJudge { get; }
    public Vector2 PredictedLandingPoint { get; }

    // Score mirrors
    public int Baskets { get; } public int Attempts { get; } public int BestCombo { get; }
    public MinigameResult LastResult { get; }

    // Authored difficulty
    public int DifficultyStep { get; }                  // = Mathf.Clamp(Rules.Baskets, 0, steps.Length - 1)
    public float FinishCueLeadSeconds { get; }
    public float ChargeAngleSpanDegrees { get; }
    public System.Collections.Generic.IReadOnlyList<BasketballDifficultyStep> DifficultySteps { get; }
    public float AlleyOopLeadSecondsForTest { get; }

    public bool HasProductionDetectors { get; }

    // Input entry points (router-driven trong build, gọi trực tiếp trong test)
    public void BeginCharge();
    public void CancelCharge();
    public void SubmitPass(float chargeRatio01, SwipeDirection direction);
    public void SubmitFinishTap();

    // Test seams — không tạo lifecycle thứ hai, không thay rules
    public void ConfigureSceneRefsForTest(BallRig ball, GameplayInputRouter router, Transform hand, Transform finisher);
    public void ConfigurePresentationForTest(TrajectoryPreview preview, BallShadow shadow);
    public void SkipTutorialForTest();
    public void SimulateForTest(float dt);

    public MinigameResult BuildResult();
    public MinigameHudState BuildHudState(bool directAccess = true);
    protected override MinigameHudState BuildHudState();
    protected override void TickPlay(float dt);

    // Pure helper — lặp đúng integrator BallRig/Ballistics sở hữu
    public static Vector2 PredictApex(Vector2 position, Vector2 velocity, Vector2 gravity, float linearDrag,
                                      float deltaTime, out float secondsToApex, int maxSteps = 10000);
}
```

```csharp
[System.Serializable]
public struct BasketballDifficultyStep
{
    public float finishCueLeadSeconds;
    public float chargeAngleSpanDegrees;
}
```

```csharp
public sealed class BasketballHud : MonoBehaviour
{
    public BasketballController Controller { get; }
    public string ScoreText { get; }        // "BASKETS 2/5"
    public string AttemptsText { get; }     // "ATTEMPTS 7"
    public string JudgeText { get; }        // "PERFECT" | "EARLY" | "LATE" | ""
    public string ComboText { get; }        // "COMBO 3"
    public string ChargeText { get; }       // "CHARGE 62%" khi giữ, "AIM" khi chưa
    public float ApexRing01 { get; }        // = Controller.FlightApexProgress
    public bool ApexZoneGlowing { get; }    // = Controller.FinishCueVisible
    public void Bind(BasketballController value);
    public void Refresh();
}
```

`GameplayInputRouter` bổ sung (additive, mirror đúng `SetSwipeDetector`):

```csharp
public event System.Action OnTap;
public void SetTapMashDetector(TapMashInputDetector tapMash);
public void SetHoldDetector(HoldInputDetector hold);
```

**Hợp đồng hành vi bắt buộc:**

- `Rules` được dựng **một lần** trong `Awake` bằng `new BasketballRules(targetBaskets, timeLimit)` — không truyền lifecycle, xem Reference §2.
- `SubmitPass` chỉ được gọi `Rules.TryPass(Ball, PendingPassVector)` một lần cho mỗi gesture, và chỉ khi `PresentationPhase == MinigamePhase.Play`, `Rules.State == BasketballState.Holding`, `chargeRatio01 >= minimumChargeRatio`.
- Sau `TryPass` thành công, controller hẹn `alleyOopLeadSeconds` rồi gọi `Rules.TryLaunchAlleyOop(Ball)` — không gọi `Ball.Launch` trực tiếp bao giờ.
- `SubmitFinishTap` gọi đúng `Rules.TapFinish(Ball.Body.position.y, Ball.Body.velocity.y)` — đọc thẳng từ physics body, không làm mượt, không thay số.
- `FlightApexProgress`, `FinishCueVisible`, `TargetChargeMin/Max` và `PredictedApexPoint` đều sinh từ `PredictApex` với đúng gravity/drag/`fixedDeltaTime` của `Ball.Profile`. Không có công thức thứ hai.
- Controller sở hữu deadline: nếu `Rules.Elapsed + dt >= timeLimit` thì resolve terminal **trước khi** `Rules.Tick` có cơ hội tự đổi phase.
- Auto-position chỉ di chuyển `Finisher.position` theo `Ball.PredictLandingPoint()` **trong khi bóng đang bay**, kẹp trong `finisherBounds`; không bao giờ ghi vào `Ball.Body.position` hay `velocity`.

## Execution Order

`Task 1 → Task 2 → Task 3 → Task 4 → Task 5 → Task 6 → Task 7`. Task 3 chỉ viết test (RED); Task 4 làm nó xanh. Không chạy Task 4 trước khi Task 3 đã chứng minh RED thật.

---

### Task 1: Add additive tap/hold detector ownership and a Basketball keyboard map

**Files:**
- Modify: `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- Modify: `Assets/_Project/Settings/Input/KMA.inputactions`
- Modify: `Assets/Tests/EditMode/Input/InputAssetContractTests.cs:14-42`
- Test: `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`

**Interfaces:**
- Consumes: `TapMashInputDetector.FeedTap(double)` + `OnTap`, `HoldInputDetector.FeedDown/FeedUp(double)` + `OnHoldEnd`, `SwipeInputDetector` (đã có).
- Produces: `GameplayInputRouter.OnTap` (`System.Action`), `SetTapMashDetector(TapMashInputDetector)`, `SetHoldDetector(HoldInputDetector)`; map `Basketball` trong `KMA.inputactions` với actions `Tap`, `Hold`, `Left`, `Right`.

Lý do task này tồn tại: Basketball cần cả tap, hold và swipe trên cùng một router, nhưng cách duy nhất hiện có để cài tap/hold là `SetDetectors(...)` — mà `SetDetectors(null, ...)` xoá các detector chủ khác đã cài (chính là "destructive install" mà review S9 đã ghi nhận ở `EnduranceInputBridge`). Hai setter riêng đóng lỗ đó mà không sửa API cũ.

- [ ] **Step 1: Snapshot the dirty worktree.** Chạy `git status --short` và ghi lại danh sách path đang dirty/untracked. Không được commit hay xoá bất kỳ path nào không thuộc S10.

- [ ] **Step 2: Write the failing router tests.** Thêm vào `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`:

```csharp
[Test]
public void SetTapMashDetector_DispatchesOnTapWithoutDroppingOtherDetectors()
{
    var hold = new HoldInputDetector();
    var swipe = new SwipeInputDetector();
    Router.SetDetectors(null, null, hold, null, swipe);

    var tapMash = new TapMashInputDetector();
    Router.SetTapMashDetector(tapMash);

    var taps = 0;
    var holdEnds = 0;
    var swipes = 0;
    Router.OnTap += () => taps++;
    Router.OnHoldEnd += _ => holdEnds++;
    Router.OnSwipe += _ => swipes++;

    Router.FeedPointerDownForTest(new Vector2(100f, 100f), 0d);
    Router.FeedPointerMoveForTest(new Vector2(300f, 100f), .05d);
    Router.FeedPointerUpForTest(new Vector2(340f, 100f), .1d);

    Assert.That(taps, Is.EqualTo(1), "Installing a tap detector must not stop feeding it.");
    Assert.That(holdEnds, Is.EqualTo(1), "SetTapMashDetector must not drop the installed hold detector.");
    Assert.That(swipes, Is.EqualTo(1), "SetTapMashDetector must not drop the installed swipe detector.");
    Assert.That(tapMash.TapsPerSecond, Is.EqualTo(1));
}

[Test]
public void SetHoldDetector_ReplacesOnlyTheHoldSlot()
{
    var tapMash = new TapMashInputDetector();
    var swipe = new SwipeInputDetector();
    Router.SetDetectors(tapMash, null, null, null, swipe);
    Router.SetTapMashDetector(tapMash);

    var replaced = new HoldInputDetector();
    Router.SetHoldDetector(replaced);

    double reportedDuration = -1d;
    var swipes = 0;
    Router.OnHoldEnd += duration => reportedDuration = duration;
    Router.OnSwipe += _ => swipes++;

    Router.FeedPointerDownForTest(new Vector2(100f, 100f), 1d);
    Router.FeedPointerMoveForTest(new Vector2(200f, 100f), 1.4d);
    Router.FeedPointerUpForTest(new Vector2(240f, 100f), 1.5d);

    Assert.That(reportedDuration, Is.EqualTo(.5d).Within(.0001d));
    Assert.That(replaced.ChargeRatio, Is.EqualTo(.5d).Within(.0001d));
    Assert.That(swipes, Is.EqualTo(1), "SetHoldDetector must not drop the installed swipe detector.");
}

// Router.FeedPointerUp feeds hold before swipe, so a controller can read the charge that the
// release gesture belongs to. Basketball depends on that order.
[Test]
public void PointerUp_ReportsHoldEndBeforeSwipe()
{
    var hold = new HoldInputDetector();
    var swipe = new SwipeInputDetector();
    Router.SetHoldDetector(hold);
    Router.SetSwipeDetector(swipe);

    var order = new System.Collections.Generic.List<string>();
    Router.OnHoldEnd += _ => order.Add("hold");
    Router.OnSwipe += _ => order.Add("swipe");

    Router.FeedPointerDownForTest(new Vector2(100f, 100f), 0d);
    Router.FeedPointerMoveForTest(new Vector2(300f, 100f), .2d);
    Router.FeedPointerUpForTest(new Vector2(340f, 100f), .3d);

    Assert.That(order, Is.EqualTo(new[] { "hold", "swipe" }));
}
```

- [ ] **Step 3: Update the input asset contract tests to the six-map shape.** Trong `Assets/Tests/EditMode/Input/InputAssetContractTests.cs`, đổi tên `SharedInputAssetDeclaresExactlyTheFiveS3Maps` thành `SharedInputAssetDeclaresTheS3MapsPlusBasketball` và sửa assertion:

```csharp
Assert.That(asset.actionMaps.Select(map => map.name), Is.EquivalentTo(new[]
{
    "Sprint", "Endurance", "Boss", "Punishment", "UI", "Basketball"
}));
```

và thêm vào `SharedInputAssetDeclaresRequiredActionsAndMeaningfulBindings`:

```csharp
Assert.That(asset.FindActionMap("Basketball").actions.Select(action => action.name),
    Is.EquivalentTo(new[] { "Tap", "Hold", "Left", "Right" }));
```

Đây là mở rộng hợp đồng có chủ đích, không phải sửa test cho khớp code: S10 là section đầu tiên cần một map gameplay thứ sáu, và không thêm map thì `MG_Basketball` không có đường keyboard nào — đúng lỗ mà review S9 đã ghi cho Volleyball.

- [ ] **Step 4: Run the RED suites.** PowerShell:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$evidence = 'D:\project\the-chat-KMA\.superpowers\sdd\2026-09-07-s10-basketball'
New-Item -ItemType Directory -Force $evidence | Out-Null
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Input.GameplayInputRouterTests' -testResults "$evidence\t1-router-red.xml" -logFile "$evidence\t1-router-red.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Input.InputAssetContractTests' -testResults "$evidence\t1-asset-red.xml" -logFile "$evidence\t1-asset-red.log"
```

Expected: router tests fail vì `OnTap`, `SetTapMashDetector`, `SetHoldDetector` chưa tồn tại (compile error được ghi trong log là RED hợp lệ); asset tests fail vì chưa có map `Basketball`. Nếu log chứa `No valid Unity Editor license found` thì đó **không** phải RED — xử lý licensing rồi chạy lại.

- [ ] **Step 5: Implement the two additive setters and the tap event.** Trong `GameplayInputRouter.cs`, ngay sau `SetSwipeDetector`:

```csharp
public event System.Action OnTap;

// Mirrors SetSwipeDetector: replaces only the tap slot, so a caller that owns taps cannot drop
// the hold, rhythm, swipe or alternate-tap detectors another owner installed on the same router.
public void SetTapMashDetector(TapMashInputDetector tapMash)
{
    if (tapMashDetector != null && detectorEventsSubscribed)
        tapMashDetector.OnTap -= DispatchTap;

    tapMashDetector = tapMash;
    if (!isActiveAndEnabled || !detectorEventsSubscribed || tapMashDetector == null)
        return;

    tapMashDetector.OnTap += DispatchTap;
}

public void SetHoldDetector(HoldInputDetector hold)
{
    if (holdDetector != null && detectorEventsSubscribed)
        holdDetector.OnHoldEnd -= DispatchHoldEnd;

    holdDetector = hold;
    if (!isActiveAndEnabled || !detectorEventsSubscribed || holdDetector == null)
        return;

    holdDetector.OnHoldEnd += DispatchHoldEnd;
}

void DispatchTap() => OnTap?.Invoke();
```

Rồi thêm tap vào hai chỗ đăng ký tập trung để `OnEnable`/`OnDisable` không bỏ sót — trong `ConfigureDetectorEvents()` thêm

```csharp
if (tapMashDetector != null)
    tapMashDetector.OnTap += DispatchTap;
```

và trong `UnsubscribeDetectorEvents()` thêm

```csharp
if (tapMashDetector != null)
    tapMashDetector.OnTap -= DispatchTap;
```

Không sửa `SetDetectors`, không sửa `FeedPointerDown/Move/Up`, không sửa bất kỳ dispatch nào đang có.

- [ ] **Step 6: Add the Basketball action map.** Trong `Assets/_Project/Settings/Input/KMA.inputactions`, thêm một map mới cùng shape với map `Boss` (id prefix `6` để không đụng id đang dùng):

```json
{
  "name": "Basketball",
  "id": "60000000000000000000000000000001",
  "actions": [
    { "name": "Tap", "type": "Button", "id": "60000000000000000000000000000011", "expectedControlType": "Button" },
    { "name": "Hold", "type": "Button", "id": "60000000000000000000000000000012", "expectedControlType": "Button" },
    { "name": "Left", "type": "Button", "id": "60000000000000000000000000000013", "expectedControlType": "Button" },
    { "name": "Right", "type": "Button", "id": "60000000000000000000000000000014", "expectedControlType": "Button" }
  ],
  "bindings": [
    { "name": "", "id": "60000000000000000000000000000101", "path": "<Keyboard>/space", "interactions": "", "processors": "", "groups": "", "action": "Tap", "isComposite": false, "isPartOfComposite": false },
    { "name": "", "id": "60000000000000000000000000000102", "path": "<Keyboard>/h", "interactions": "Hold(duration=0.25)", "processors": "", "groups": "", "action": "Hold", "isComposite": false, "isPartOfComposite": false },
    { "name": "", "id": "60000000000000000000000000000103", "path": "<Keyboard>/leftArrow", "interactions": "", "processors": "", "groups": "", "action": "Left", "isComposite": false, "isPartOfComposite": false },
    { "name": "", "id": "60000000000000000000000000000104", "path": "<Keyboard>/rightArrow", "interactions": "", "processors": "", "groups": "", "action": "Right", "isComposite": false, "isPartOfComposite": false }
  ]
}
```

Cố ý **không** bind `<Touchscreen>/primaryTouch/press` ở đây: touch đã đi qua `ScreenTapArea` → `FeedPointerDown/Up`, và bind thêm sẽ làm mỗi lần chạm bị tính hai lần. Đây đúng là quyền sở hữu tap mà S3 đã chốt.

- [ ] **Step 7: Run the GREEN suites plus the full input regression.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Input' -testResults "$evidence\t1-router-green.xml" -logFile "$evidence\t1-router-green.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Input' -testResults "$evidence\t1-asset-green.xml" -logFile "$evidence\t1-asset-green.log"
```

Expected: tất cả pass, bao gồm mọi test S3 đang có (`DetectorContractTests`, `AlternateTapInputDetectorTests`, phần còn lại của `InputAssetContractTests`). Nếu một test S3 cũ đỏ thì đây là regression, không phải "cần cập nhật kỳ vọng".

- [ ] **Step 8: Run both full suites.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testResults "$evidence\t1-full-edit.xml" -logFile "$evidence\t1-full-edit.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testResults "$evidence\t1-full-play.xml" -logFile "$evidence\t1-full-play.log"
```

Expected: EditMode `≥253` pass / 0 fail, PlayMode `≥193` pass / 0 fail. Ghi số thật vào ledger.

- [ ] **Step 9: Commit.**

```powershell
git diff --check
git add Assets/_Project/Scripts/Input/GameplayInputRouter.cs Assets/_Project/Settings/Input/KMA.inputactions Assets/Tests/EditMode/Input/InputAssetContractTests.cs Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs
git commit -m "feat: let one router own tap, hold and swipe detectors"
```

---

### Task 2: Make the authored alley-oop apex window reachable in the build

**Files:**
- Create: `Assets/Tests/EditMode/Gameplay/Ball/AlleyOopWindowTests.cs` (+ `.meta` do Unity sinh)
- Modify: `Assets/_Project/Scripts/Gameplay/Basketball/AlleyOopPattern.cs:32-33` — **chỉ** hằng số `velocityThreshold` trong `AuthoredDefault`
- Do not modify: `BasketballRules.cs`, `AlleyOopPattern` constructor/`IsApexWindow`/`TryLaunch`, `Assets/Tests/EditMode/Gameplay/Ball/BasketballRulesTests.cs`

**Interfaces:**
- Consumes: `AlleyOopPattern.AuthoredDefault(Vector2?)`, `IsApexWindow(float, float)`, `Ballistics.AdvanceVelocity`.
- Produces: `AuthoredDefault(...).VelocityThreshold == 1.5f`, giữ nguyên `PassVector`, `LaunchForce = 8f`, `Curvature = 0f`, `ApexMin = 2.8f`, `ApexMax = 3.2f`.

Đây là lỗi đã đo được, không phải sở thích: với `velocityThreshold = .1f`, cửa sổ `Perfect` mở khoảng `20 ms` — ngắn hơn một bước physics `20 ms`, nên trong build gần như mọi lần tap đều trả `Early` hoặc `Late` và người chơi không thể ghi rổ nào. `1.5f` cho khoảng `2*1.5/9.81 ≈ 306 ms`, và trong cửa sổ đó bóng chỉ rời apex tối đa `1.5²/(2*9.81) = 0.115` unit — vẫn nằm trong dải `[2.8, 3.2]` rộng `0.4`, nên trục "độ cao lob" và trục "thời điểm tap" vẫn độc lập. Con số `1.5` là đúng tiền lệ S9 đã dùng cho `VolleyballController.apexVelocityThreshold` với cùng lý do.

Đã verify không có test nào ghim `AuthoredDefault(...).VelocityThreshold`: `BasketballRulesTests.cs:37` chỉ dùng một pattern rời để chứng minh `TryLaunchAlleyOop(ball, unrelatedReplacement)` trả false, `:127` chỉ assert `IsApexWindow(3f, 0f)` là true (đúng với mọi threshold `>= 0`), và các `[TestCase]` biên `.11f/-.11f` chạy trên `BasketballRules.InFlight(2.8f, 3.2f, .1f)` — truyền threshold tường minh, không qua `AuthoredDefault`.

- [ ] **Step 1: Write the failing window-duration test.** Tạo `Assets/Tests/EditMode/Gameplay/Ball/AlleyOopWindowTests.cs`:

```csharp
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class AlleyOopWindowTests
    {
        const float FixedStep = .02f;

        // The judge needs |velocityY| <= VelocityThreshold, so the window's real length is what a
        // player has to hit. Measuring it in physics steps is the only honest assertion: a
        // threshold that opens for less than one step cannot be hit at all.
        [Test]
        public void AuthoredApexWindow_StaysOpenForAtLeastTwelveFixedSteps()
        {
            AlleyOopPattern pattern = AlleyOopPattern.AuthoredDefault();
            var gravity = new Vector2(0f, -9.81f);

            var openSteps = 0;
            var velocity = new Vector2(0f, 6f);
            for (var step = 0; step < 2000; step++)
            {
                velocity = Ballistics.AdvanceVelocity(velocity, gravity, 0f, .02f, FixedStep);
                if (Mathf.Abs(velocity.y) <= pattern.VelocityThreshold)
                    openSteps++;
                else if (openSteps > 0)
                    break;
            }

            Assert.That(openSteps, Is.GreaterThanOrEqualTo(12),
                "The Perfect window must last long enough for a human tap, not a single physics step. " +
                "Measured steps: " + openSteps);
        }

        // Widening the timing window must not widen the aiming band, or the two difficulty axes
        // collapse into one.
        [Test]
        public void AuthoredApexWindow_KeepsTheAuthoredHeightBandAndLaunchContract()
        {
            AlleyOopPattern pattern = AlleyOopPattern.AuthoredDefault(new Vector2(1f, .75f));

            Assert.That(pattern.ApexMin, Is.EqualTo(2.8f));
            Assert.That(pattern.ApexMax, Is.EqualTo(3.2f));
            Assert.That(pattern.LaunchForce, Is.EqualTo(8f));
            Assert.That(pattern.Curvature, Is.EqualTo(0f));
            Assert.That(pattern.PassVector, Is.EqualTo(new Vector2(1f, .75f)));
            Assert.That(pattern.IsApexWindow(2.79f, 0f), Is.False, "The height band must stay exclusive.");
            Assert.That(pattern.IsApexWindow(3.21f, 0f), Is.False, "The height band must stay exclusive.");
        }

        // A ball whose apex lands outside the authored band must still be judged wrong even when
        // the tap is perfectly timed - that is the aiming axis.
        [Test]
        public void PerfectTiming_OnAMisaimedLob_IsNotPerfect()
        {
            AlleyOopPattern pattern = AlleyOopPattern.AuthoredDefault();

            Assert.That(pattern.IsApexWindow(2.4f, 0f), Is.False, "An under-charged lob apexes below the band.");
            Assert.That(pattern.IsApexWindow(3.6f, 0f), Is.False, "An over-charged lob apexes above the band.");
            Assert.That(pattern.IsApexWindow(3.0f, 0f), Is.True, "A correctly charged lob apexes inside the band.");
        }
    }
}
```

- [ ] **Step 2: Run it and confirm a real RED.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Gameplay.Ball.AlleyOopWindowTests' -testResults "$evidence\t2-window-red.xml" -logFile "$evidence\t2-window-red.log"
```

Expected: `AuthoredApexWindow_StaysOpenForAtLeastTwelveFixedSteps` fail với `Measured steps: 1`; hai test còn lại pass ngay (chúng khoá phần không được đổi). Ghi con số đo được vào ledger — đây là bằng chứng lỗi, không phải giả định.

- [ ] **Step 3: Widen only the authored velocity threshold.** Trong `Assets/_Project/Scripts/Gameplay/Basketball/AlleyOopPattern.cs`, đổi đúng một dòng:

```csharp
        // Sized so the apex window lasts a few hundred milliseconds instead of a single physics
        // step: at 0.02s steps gravity changes velocityY by ~0.196 per step, so the old 0.1
        // threshold made FinishJudge.Perfect unreachable in the build. The 0.4-unit height band
        // is unchanged, so aiming and timing stay independent axes.
        public static AlleyOopPattern AuthoredDefault(Vector2? passVector = null) =>
            new AlleyOopPattern(passVector ?? new Vector2(1f, .75f), 8f, 0f, 2.8f, 3.2f, 1.5f);
```

Không đổi gì khác trong file.

- [ ] **Step 4: Run GREEN plus the Basketball rules regression.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Gameplay.Ball' -testResults "$evidence\t2-window-green.xml" -logFile "$evidence\t2-window-green.log"
```

Expected: `AlleyOopWindowTests` 3/3 pass **và** toàn bộ `BasketballRulesTests` (gồm 6 `[TestCase]` biên) vẫn pass không sửa dòng nào. Nếu `BasketballRulesTests` đỏ thì rollback và báo — hằng số này không được phép đổi hành vi đã có test.

- [ ] **Step 5: Run both full suites** với cùng cặp lệnh ở Task 1 Step 8, kết quả vào `t2-full-edit.xml` / `t2-full-play.xml`. Expected: không tụt so với số ghi ở Task 1.

- [ ] **Step 6: Commit.**

```powershell
git diff --check
git add Assets/_Project/Scripts/Gameplay/Basketball/AlleyOopPattern.cs Assets/Tests/EditMode/Gameplay/Ball/AlleyOopWindowTests.cs
git commit -m "fix: make the authored alley-oop apex window reachable"
```

---

### Task 3: Lock the Basketball controller contract with failing PlayMode tests

**Files:**
- Create: `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/KMA.Gameplay.Basketball.PlayMode.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/BasketballControllerTests.cs`
- Inspect only: `BasketballRules.cs`, `AlleyOopPattern.cs`, `BallRig.cs`, `MinigameBase.cs`, `MinigameLifecycle.cs`, `GameplayInputRouter.cs`, `SwipeInputDetector.cs`

**Interfaces:**
- Consumes: mọi thứ trong **Interfaces** ở đầu plan, cộng `GameplayInputRouter.FeedPointerDownForTest/FeedPointerMoveForTest/FeedPointerUpForTest`.
- Produces: bộ test là hợp đồng mà Task 4 phải làm xanh. Không sửa test nào đang có.

- [ ] **Step 1: Create the test assembly definition.** `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/KMA.Gameplay.Basketball.PlayMode.Tests.asmdef`:

```json
{
    "name": "KMA.Gameplay.Basketball.PlayMode.Tests",
    "rootNamespace": "KMA.Tests.Gameplay.Ball",
    "references": [
        "UnityEngine.TestRunner",
        "KMA.Gameplay",
        "KMA.Gameplay.Ball",
        "KMA.Gameplay.Basketball",
        "KMA.Gameplay.UI",
        "KMA.Input",
        "Unity.InputSystem",
        "Unity.TextMeshPro",
        "Unity.InputSystem.TestFramework"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the fixture and the construction/lifecycle tests.** Tạo `BasketballControllerTests.cs`:

```csharp
using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class BasketballControllerTests
    {
        GameObject root;
        BasketballController controller;
        BallRig ball;
        GameplayInputRouter router;
        Transform hand;
        Transform finisher;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("basketball-controller-fixture");

            var ballObject = new GameObject("ball");
            ballObject.transform.SetParent(root.transform, false);
            ballObject.AddComponent<Rigidbody2D>();
            ball = ballObject.AddComponent<BallRig>();
            ball.SetProfile(FlightProfile.Create(1f, .02f, 0f, .8f));

            var routerObject = new GameObject("router");
            routerObject.transform.SetParent(root.transform, false);
            router = routerObject.AddComponent<GameplayInputRouter>();

            var handObject = new GameObject("hand");
            handObject.transform.SetParent(root.transform, false);
            handObject.transform.position = new Vector3(-3.5f, 1.2f, 0f);
            hand = handObject.transform;

            var finisherObject = new GameObject("finisher");
            finisherObject.transform.SetParent(root.transform, false);
            finisherObject.transform.position = Vector3.zero;
            finisher = finisherObject.transform;

            var controllerObject = new GameObject("controller");
            controllerObject.transform.SetParent(root.transform, false);
            controller = controllerObject.AddComponent<BasketballController>();
            controller.ConfigureSceneRefsForTest(ball, router, hand, finisher);
            controller.SkipTutorialForTest();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void Construction_UsesAPrivateRulesLifecycleSoTheControllerStillOwnsResolve()
        {
            Assert.That(controller.Rules, Is.Not.Null);
            Assert.That(controller.Ball, Is.SameAs(ball));
            Assert.That(controller.InputRouter, Is.SameAs(router));
            Assert.That(controller.PlayerHand, Is.SameAs(hand));
            Assert.That(controller.Finisher, Is.SameAs(finisher));
            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play),
                "SkipTutorialForTest must reach Play without a second lifecycle.");
            Assert.That(controller.Rules.Phase, Is.EqualTo(MinigamePhase.Play),
                "The rules own a private lifecycle that is already in Play.");
            Assert.That(controller.HasProductionDetectors, Is.True,
                "The controller installs its own tap, hold and swipe detectors on the shared router.");
        }

        [Test]
        public void HoldingAttachesTheBallToTheHandSoTheLobLaunchesFromTheAuthoredHeight()
        {
            Assert.That(ball.Snapshot.IsAttached, Is.True);
            Assert.That(ball.Body.position.y, Is.EqualTo(1.2f).Within(.001f));
            Assert.That(ball.Body.position.x, Is.EqualTo(-3.5f).Within(.001f));
        }
```

- [ ] **Step 3: Add the charge, target-band and pass tests.** Tiếp trong cùng class:

```csharp
        [Test]
        public void ChargeMapsToTheAuthoredAngleSpanAndReportsItLive()
        {
            controller.BeginCharge();
            Assert.That(controller.IsCharging, Is.True);

            controller.SimulateForTest(.5f);

            float span = controller.ChargeAngleSpanDegrees;
            Assert.That(span, Is.EqualTo(35f).Within(.001f), "Step 0 of the authored table opens at 35 degrees.");
            Assert.That(controller.PassAngleDegrees,
                Is.EqualTo(47.5f - span * .5f + span * controller.ChargeRatio).Within(.01f));
        }

        // The glowing band the HUD draws must be derived from the same authored pattern TryPass
        // will create, or the player is aiming at a lie.
        [Test]
        public void TargetChargeBand_MatchesTheAuthoredApexBandForTheCurrentLaunchHeight()
        {
            AlleyOopPattern authored = AlleyOopPattern.AuthoredDefault(Vector2.right);
            Assert.That(controller.AuthoredBand.ApexMin, Is.EqualTo(authored.ApexMin));
            Assert.That(controller.AuthoredBand.ApexMax, Is.EqualTo(authored.ApexMax));

            Assert.That(controller.TargetChargeMin, Is.GreaterThan(0f));
            Assert.That(controller.TargetChargeMax, Is.LessThan(1f));
            Assert.That(controller.TargetChargeMax, Is.GreaterThan(controller.TargetChargeMin));

            float inside = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;
            Assert.That(ApexHeightFor(inside), Is.InRange(authored.ApexMin, authored.ApexMax));
            Assert.That(ApexHeightFor(controller.TargetChargeMin - .06f), Is.LessThan(authored.ApexMin));
            Assert.That(ApexHeightFor(controller.TargetChargeMax + .06f), Is.GreaterThan(authored.ApexMax));
        }

        [Test]
        public void SubmitPass_BelowTheMinimumCharge_IsRejectedWithoutTouchingTheRules()
        {
            controller.BeginCharge();
            controller.SubmitPass(.02f, SwipeDirection.Right);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding));
            Assert.That(controller.Attempts, Is.Zero);
            Assert.That(ball.Snapshot.IsInFlight, Is.False);
        }

        [Test]
        public void SubmitPass_AuthorsThePatternThenTheAiLaunchesItAfterTheAuthoredLead()
        {
            controller.BeginCharge();
            float charge = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;
            controller.SubmitPass(charge, SwipeDirection.Right);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Passing),
                "The swipe passes; the AI has not launched yet.");
            Assert.That(controller.Rules.AuthoredPattern, Is.Not.Null);
            Assert.That(controller.Rules.AuthoredPattern.PassVector,
                Is.EqualTo(controller.PendingPassVector).Using(Vector2Comparer.Instance));
            Assert.That(ball.Snapshot.IsInFlight, Is.False);

            controller.SimulateForTest(controller.AlleyOopLeadSecondsForTest);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.AlleyOopFlight));
            Assert.That(ball.Snapshot.IsInFlight, Is.True);
            Assert.That(ball.Body.velocity.magnitude, Is.EqualTo(8f).Within(.01f),
                "The authored launch force stays owned by AlleyOopPattern.");
        }
```

`AlleyOopLeadSecondsForTest` là property read-only trên controller (`=> alleyOopLeadSeconds;`) để test không phải hard-code authored delay.

- [ ] **Step 4: Add the apex-prediction, cue and judge tests.**

```csharp
        [UnityTest]
        public IEnumerator ApexPrediction_MatchesTheIntegratorAndClosesTheCueBeforeTheApex()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);

            Assert.That(controller.SecondsToApex, Is.GreaterThan(0f));
            Assert.That(controller.FinishCueVisible, Is.False,
                "The cue must not be up while the apex is still far away.");
            float lead = controller.FinishCueLeadSeconds;
            Assert.That(lead, Is.EqualTo(.6f).Within(.001f), "Step 0 of the authored table leads by 0.6s.");

            float apexHeight = controller.PredictedApexPoint.y;
            Assert.That(apexHeight, Is.InRange(controller.AuthoredBand.ApexMin, controller.AuthoredBand.ApexMax));

            var sawCue = false;
            float guard = Time.unscaledTime + 10f;
            while (ball.Body.velocity.y > 0f && Time.unscaledTime < guard)
            {
                if (controller.FinishCueVisible)
                {
                    sawCue = true;
                    Assert.That(controller.SecondsToApex, Is.LessThanOrEqualTo(lead + .03f));
                }
                yield return new WaitForFixedUpdate();
            }

            Assert.That(sawCue, Is.True, "The finish cue must appear before the apex, not after it.");
            Assert.That(controller.FlightApexProgress, Is.EqualTo(1f).Within(.06f));
            Assert.That(ball.Body.position.y, Is.EqualTo(apexHeight).Within(.12f),
                "The predicted apex must match where the ball actually stops rising.");
        }

        [UnityTest]
        public IEnumerator TapAtTheApexOfACorrectlyChargedLob_ScoresAPerfectBasket()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);
            yield return WaitForApex();

            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Perfect));
            Assert.That(controller.Baskets, Is.EqualTo(1));
            Assert.That(controller.Attempts, Is.EqualTo(1));
            Assert.That(controller.BestCombo, Is.EqualTo(1));
            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding),
                "An unfinished objective returns to Holding for the next attempt.");
            Assert.That(ball.Snapshot.IsAttached, Is.True, "The next attempt starts in the player's hand.");
        }

        [UnityTest]
        public IEnumerator TapAtTheApexOfAnUnderChargedLob_IsEarlyAndBreaksTheCombo()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);
            yield return WaitForApex();
            controller.SubmitFinishTap();
            Assert.That(controller.BestCombo, Is.EqualTo(1));

            yield return LaunchAtCharge(controller.TargetChargeMin - .12f);
            yield return WaitForApex();
            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Early),
                "A lob that apexes below the authored band cannot be finished, however well timed.");
            Assert.That(controller.Baskets, Is.EqualTo(1));
            Assert.That(controller.Attempts, Is.EqualTo(2));
            Assert.That(controller.Rules.Combo, Is.Zero);
            Assert.That(controller.BestCombo, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TapBeforeTheApexWindowOpens_IsEarlyAndCostsAnAttempt()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);

            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Early));
            Assert.That(controller.Attempts, Is.EqualTo(1));
            Assert.That(controller.Baskets, Is.Zero);
        }

        [Test]
        public void FinishTapOutsideFlight_IsIgnoredAndCostsNothing()
        {
            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Ignored));
            Assert.That(controller.Attempts, Is.Zero);
        }
```

- [ ] **Step 5: Add the difficulty-axis, auto-position, HUD and completion tests.**

```csharp
        // Spec S10: each step raises exactly one axis - a narrower timing window OR a harder
        // alley-oop path, never both.
        [Test]
        public void AuthoredDifficultyTable_ChangesExactlyOneAxisPerStep()
        {
            var steps = controller.DifficultySteps;
            Assert.That(steps.Count, Is.EqualTo(5), "One step per basket up to the five-basket objective.");

            for (var index = 1; index < steps.Count; index++)
            {
                bool timingChanged = !Mathf.Approximately(
                    steps[index].finishCueLeadSeconds, steps[index - 1].finishCueLeadSeconds);
                bool pathChanged = !Mathf.Approximately(
                    steps[index].chargeAngleSpanDegrees, steps[index - 1].chargeAngleSpanDegrees);

                Assert.That(timingChanged || pathChanged, Is.True, "Step " + index + " raises no axis.");
                Assert.That(timingChanged && pathChanged, Is.False, "Step " + index + " raises both axes.");
                Assert.That(steps[index].finishCueLeadSeconds,
                    Is.LessThanOrEqualTo(steps[index - 1].finishCueLeadSeconds), "Timing must not get easier.");
                Assert.That(steps[index].chargeAngleSpanDegrees,
                    Is.GreaterThanOrEqualTo(steps[index - 1].chargeAngleSpanDegrees), "The path must not get easier.");
            }
        }

        [UnityTest]
        public IEnumerator DifficultyStepFollowsTheBasketCountAndNarrowsTheTargetBand()
        {
            Assert.That(controller.DifficultyStep, Is.Zero);
            float bandAtStepZero = controller.TargetChargeMax - controller.TargetChargeMin;

            yield return ScoreOneBasket();
            Assert.That(controller.DifficultyStep, Is.EqualTo(1));
            Assert.That(controller.FinishCueLeadSeconds, Is.LessThan(.6f), "Step 1 narrows the timing axis.");
            Assert.That(controller.TargetChargeMax - controller.TargetChargeMin,
                Is.EqualTo(bandAtStepZero).Within(.001f), "Step 1 must not also change the path axis.");

            yield return ScoreOneBasket();
            Assert.That(controller.DifficultyStep, Is.EqualTo(2));
            Assert.That(controller.TargetChargeMax - controller.TargetChargeMin,
                Is.LessThan(bandAtStepZero), "Step 2 narrows the path axis.");
        }

        [UnityTest]
        public IEnumerator FinisherChasesThePredictedLandingWithoutTouchingTheBallBody()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);

            Vector2 predicted = ball.PredictLandingPoint();
            Vector2 velocityBefore = ball.Body.velocity;
            Vector2 positionBefore = ball.Body.position;

            yield return new WaitForFixedUpdate();

            Assert.That(controller.PredictedLandingPoint.x, Is.EqualTo(ball.PredictLandingPoint().x).Within(.001f));
            Assert.That(finisher.position.x, Is.Not.EqualTo(0f).Within(.0001f),
                "The finisher must move toward the predicted landing point.");
            Assert.That(ball.Body.velocity, Is.Not.EqualTo(velocityBefore).Using(Vector2Comparer.Instance),
                "The ball keeps integrating; the assist must not freeze it.");
            Assert.That(ball.Body.position, Is.Not.EqualTo(positionBefore).Using(Vector2Comparer.Instance));
            Assert.That(predicted.y, Is.EqualTo(0f).Within(.001f), "The profile ground plane owns the landing height.");
        }

        [UnityTest]
        public IEnumerator FifthBasket_FinishesOnceWithAPassingResult()
        {
            var completions = 0;
            MinigameResult observed = default;
            controller.Completed += result => { completions++; observed = result; };

            for (var basket = 0; basket < 5; basket++)
                yield return ScoreOneBasket();

            Assert.That(controller.Baskets, Is.EqualTo(5));
            Assert.That(completions, Is.EqualTo(1),
                "Completed must fire exactly once - the rules' own BeginResolve must not swallow it.");
            Assert.That(observed.Pass, Is.True);
            Assert.That(observed.Score, Is.GreaterThan(0f));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));

            controller.SubmitFinishTap();
            controller.SimulateForTest(1f);

            Assert.That(completions, Is.EqualTo(1), "Input after the resolve must not complete the subject twice.");
        }

        [Test]
        public void Deadline_ResolvesOnceWithAFailingResultWhenTheObjectiveIsIncomplete()
        {
            var completions = 0;
            MinigameResult observed = default;
            controller.Completed += result => { completions++; observed = result; };

            controller.SimulateForTest(61f);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(observed.Pass, Is.False);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
        }

        [Test]
        public void HudState_ReportsPhaseTimerBasketProgressAndStatus()
        {
            MinigameHudState state = controller.BuildHudState();

            Assert.That(state.phase, Is.EqualTo(MinigamePhase.Play.ToString()));
            Assert.That(state.timeRemaining, Is.EqualTo(60f).Within(.5f));
            Assert.That(state.progress01, Is.Zero);
            Assert.That(state.statusText, Is.EqualTo("HOLD TO CHARGE"));

            controller.BeginCharge();
            controller.SimulateForTest(.4f);

            Assert.That(controller.BuildHudState().statusText, Is.EqualTo("RELEASE IN THE BAND"));
        }
```

- [ ] **Step 6: Add the production-input routing test.** Đường này phải là đường build thật: `ScreenTapArea` → `router.FeedPointerDown/Move/Up` → detector → controller.

```csharp
        // Basketball has no input bridge, so the controller owns three detectors on the scene's
        // one router. This drives the production dispatch, not the SubmitX helpers.
        [UnityTest]
        public IEnumerator ProductionPointerGesture_ChargesPassesAndFinishesThroughTheRouter()
        {
            var start = new Vector2(600f, 400f);
            router.FeedPointerDownForTest(start, 0d);

            Assert.That(controller.IsCharging, Is.True, "Pointer down must start the charge.");

            float charge = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;
            double release = charge;   // HoldInputDetector default maxChargeSeconds is 1 second.
            router.FeedPointerMoveForTest(start + new Vector2(180f, 0f), release * .5d);
            router.FeedPointerUpForTest(start + new Vector2(240f, 0f), release);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Passing),
                "OnHoldEnd then OnSwipe must complete the pass in one gesture.");
            Assert.That(controller.ChargeRatio, Is.EqualTo(charge).Within(.02f));

            controller.SimulateForTest(controller.AlleyOopLeadSecondsForTest);
            yield return WaitForApex();

            router.FeedPointerDownForTest(start, 10d);
            router.FeedPointerUpForTest(start, 10.05d);

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Perfect),
                "A tap during flight must reach TapFinish through the router's tap detector.");
            Assert.That(controller.Baskets, Is.EqualTo(1));
        }
```

- [ ] **Step 7: Add the shared test helpers.** Đặt ở cuối class rồi đóng class/namespace:

```csharp
        IEnumerator LaunchAtCharge(float charge)
        {
            controller.BeginCharge();
            controller.SubmitPass(charge, SwipeDirection.Right);
            controller.SimulateForTest(controller.AlleyOopLeadSecondsForTest);
            Assert.That(ball.Snapshot.IsInFlight, Is.True, "The AI must have launched the alley-oop.");
            yield return new WaitForFixedUpdate();
        }

        IEnumerator WaitForApex()
        {
            float guard = Time.unscaledTime + 10f;
            while (ball.Body.velocity.y > 0f && Time.unscaledTime < guard)
                yield return new WaitForFixedUpdate();

            Assert.That(ball.Body.velocity.y, Is.LessThanOrEqualTo(0f), "The ball never reached its apex.");
        }

        IEnumerator ScoreOneBasket()
        {
            int before = controller.Baskets;
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);
            yield return WaitForApex();
            controller.SubmitFinishTap();
            Assert.That(controller.Baskets, Is.EqualTo(before + 1),
                "The centred charge plus an apex tap must score. Judge: " + controller.LastJudge);
        }

        // The apex height a given charge produces, simulated with the integrator BallRig owns.
        float ApexHeightFor(float charge01)
        {
            float span = controller.ChargeAngleSpanDegrees;
            float angle = 47.5f - span * .5f + span * Mathf.Clamp01(charge01);
            float radians = angle * Mathf.Deg2Rad;
            var velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 8f;
            return BasketballController.PredictApex(hand.position, velocity,
                Physics2D.gravity * ball.Profile.GravityScale, ball.Profile.LinearDrag,
                Time.fixedDeltaTime, out _).y;
        }

        sealed class Vector2Comparer : System.Collections.Generic.IEqualityComparer<Vector2>
        {
            public static readonly Vector2Comparer Instance = new Vector2Comparer();
            public bool Equals(Vector2 left, Vector2 right) => Vector2.Distance(left, right) < .0001f;
            public int GetHashCode(Vector2 value) => value.GetHashCode();
        }
    }
}
```

- [ ] **Step 8: Run the RED suite.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.Ball.BasketballControllerTests' -testResults "$evidence\t3-controller-red.xml" -logFile "$evidence\t3-controller-red.log"
```

Expected: fail vì `BasketballController` chưa tồn tại — compile error trong log là RED hợp lệ. Kiểm log **không** chứa `No valid Unity Editor license found`. Chạy riêng `KMA.Tests.Gameplay.Ball.BasketballRulesTests` để chứng minh test rules cũ vẫn xanh.

- [ ] **Step 9: Commit the contract tests.**

```powershell
git diff --check
git add Assets/Tests/PlayMode/Gameplay/Ball/Basketball
git commit -m "test: define basketball controller contracts"
```

---

### Task 4: Implement the Basketball controller and difficulty table

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/BasketballDifficultyStep.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/BasketballController.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Basketball/KMA.Gameplay.Basketball.asmdef`
- Test: `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/BasketballControllerTests.cs`
- Do not modify: `BasketballRules.cs`, `AlleyOopPattern.cs`, `BallRig.cs`, hoặc bất kỳ test nào đã tồn tại

**Interfaces:**
- Consumes: `BasketballRules`, `AlleyOopPattern`, `BallRig`, `Ballistics`, `MinigameBase`, `GameplayInputRouter` (+ `OnTap`/`SetTapMashDetector`/`SetHoldDetector` từ Task 1), `TapMashInputDetector`, `HoldInputDetector`, `SwipeInputDetector`, `TrajectoryPreview`, `BallShadow`.
- Produces: đúng bộ API trong **Interfaces** ở đầu plan; Task 5 (HUD + scene) và Task 6 (campaign) chỉ đọc từ đó.

- [ ] **Step 1: Extend the assembly definition.** `KMA.Gameplay.Basketball.asmdef` cần thêm `KMA.Input` và `Unity.TextMeshPro` (khớp `KMA.Gameplay.Volleyball`):

```json
"references": ["KMA.Gameplay", "KMA.Gameplay.Ball", "KMA.Input", "Unity.TextMeshPro"],
```

- [ ] **Step 2: Create the authored difficulty step type.** `BasketballDifficultyStep.cs`:

```csharp
using System;
using UnityEngine;

namespace KMA.Gameplay
{
    // Spec S10: each step raises exactly one axis. finishCueLeadSeconds is the timing axis - how
    // much warning the finisher gets before the apex. chargeAngleSpanDegrees is the path axis -
    // widening the span shrinks the fraction of the charge that lands the lob in the authored
    // apex band, without touching the band itself.
    [Serializable]
    public struct BasketballDifficultyStep
    {
        [Min(.05f)] public float finishCueLeadSeconds;
        [Min(5f)] public float chargeAngleSpanDegrees;

        public BasketballDifficultyStep(float finishCueLeadSeconds, float chargeAngleSpanDegrees)
        {
            this.finishCueLeadSeconds = finishCueLeadSeconds;
            this.chargeAngleSpanDegrees = chargeAngleSpanDegrees;
        }
    }
}
```

- [ ] **Step 3: Implement fields, construction and detector ownership.** Đầu `BasketballController.cs`:

```csharp
using System.Collections.Generic;
using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class BasketballController : MinigameBase
    {
        const float DefaultTutorialSeconds = 2f;
        const float DefaultCountdownSeconds = 3f;
        const int ChargeBandSamples = 201;

        [SerializeField] int targetBaskets = 5;
        [SerializeField] float timeLimit = 60f;
        [SerializeField] BallRig ball;
        [SerializeField] GameplayInputRouter inputRouter;
        [SerializeField] Transform playerHand;
        [SerializeField] Transform finisher;
        [SerializeField] BoxCollider2D finisherBounds;
        [SerializeField] TrajectoryPreview trajectoryPreview;
        [SerializeField] BallShadow ballShadow;
        [SerializeField] float passAngleCentreDegrees = 47.5f;
        [SerializeField, Range(0f, 1f)] float minimumChargeRatio = .05f;
        [SerializeField] float alleyOopLeadSeconds = .35f;
        [SerializeField, Min(0f)] float minimumSwipeLengthPixels = .5f;
        [SerializeField] float finisherLandingOffset = -.4f;
        [SerializeField] BasketballDifficultyStep[] difficultySteps =
        {
            new BasketballDifficultyStep(.60f, 35f),
            new BasketballDifficultyStep(.45f, 35f),
            new BasketballDifficultyStep(.45f, 45f),
            new BasketballDifficultyStep(.30f, 45f),
            new BasketballDifficultyStep(.30f, 55f)
        };

        TapMashInputDetector productionTapDetector;
        HoldInputDetector productionHoldDetector;
        SwipeInputDetector productionSwipeDetector;
        bool inputRouterSubscribed;
        bool terminalResolved;
        bool pendingAlleyOop;
        float alleyOopDelay;
        float chargeElapsed;
        float launchSecondsToApex;
        int cachedBandStep = -1;
        float cachedBandLaunchY = float.NaN;

        public BasketballRules Rules { get; private set; }
        public AlleyOopPattern AuthoredBand { get; private set; }
        public BallRig Ball => ball;
        public GameplayInputRouter InputRouter => inputRouter;
        public Transform PlayerHand => playerHand;
        public Transform Finisher => finisher;
        public TrajectoryPreview Preview => trajectoryPreview;
        public BallShadow Shadow => ballShadow;

        public bool IsCharging { get; private set; }
        public float ChargeRatio { get; private set; }
        public float PassAngleDegrees => AngleForCharge(ChargeRatio);
        public Vector2 PendingPassVector { get; private set; }
        public float TargetChargeMin { get; private set; }
        public float TargetChargeMax { get; private set; }

        public Vector2 PredictedApexPoint { get; private set; }
        public float SecondsToApex { get; private set; }
        public float FlightApexProgress { get; private set; }
        public bool FinishCueVisible { get; private set; }
        public FinishJudge LastJudge { get; private set; } = FinishJudge.Ignored;
        public Vector2 PredictedLandingPoint { get; private set; }

        public int Baskets => Rules == null ? 0 : Rules.Baskets;
        public int Attempts => Rules == null ? 0 : Rules.Attempts;
        public int BestCombo => Rules == null ? 0 : Rules.BestCombo;
        public MinigameResult LastResult { get; private set; }

        public int DifficultyStep => Mathf.Clamp(Baskets, 0, Mathf.Max(0, difficultySteps.Length - 1));
        public float FinishCueLeadSeconds => CurrentStep.finishCueLeadSeconds;
        public float ChargeAngleSpanDegrees => CurrentStep.chargeAngleSpanDegrees;
        public IReadOnlyList<BasketballDifficultyStep> DifficultySteps => difficultySteps;
        public float AlleyOopLeadSecondsForTest => alleyOopLeadSeconds;
        public bool HasProductionDetectors =>
            productionTapDetector != null && productionHoldDetector != null && productionSwipeDetector != null;

        BasketballDifficultyStep CurrentStep => difficultySteps.Length == 0
            ? new BasketballDifficultyStep(.6f, 35f)
            : difficultySteps[DifficultyStep];

        protected override void Awake()
        {
            base.Awake();
            CacheReferences();
            InstallProductionDetectors();
            // Rules get their own lifecycle on purpose: TapFinish calls BeginResolve itself when
            // the objective completes, and a shared lifecycle would make MinigameBase.Finish
            // return false so Completed would never fire. See the plan's Reference section.
            Rules = new BasketballRules(targetBaskets, timeLimit);
            AuthoredBand = AlleyOopPattern.AuthoredDefault(Vector2.right);
            ResetRuntimeState();
        }

        void OnEnable()
        {
            CacheReferences();
            SubscribeInputRouter();
        }

        void OnDisable() => UnsubscribeInputRouter();
        void OnDestroy() => UnsubscribeInputRouter();

        void CacheReferences()
        {
            if (inputRouter == null) inputRouter = GetComponent<GameplayInputRouter>();
            if (inputRouter == null) inputRouter = FindFirstObjectByType<GameplayInputRouter>();
            if (finisherBounds == null) finisherBounds = GetComponent<BoxCollider2D>();
            if (inputRouter == null)
                Debug.LogError("BasketballController found no GameplayInputRouter; the scene accepts no gestures.", this);
        }

        // The Basketball scene has no input bridge, so the controller owns the three detectors the
        // shared router feeds. Each slot is replaced through its own additive setter, so a
        // detector another owner installed on the same router survives.
        void InstallProductionDetectors()
        {
            if (inputRouter == null || HasProductionDetectors) return;
            productionTapDetector = new TapMashInputDetector();
            productionHoldDetector = new HoldInputDetector();
            productionSwipeDetector = new SwipeInputDetector();
            inputRouter.SetTapMashDetector(productionTapDetector);
            inputRouter.SetHoldDetector(productionHoldDetector);
            inputRouter.SetSwipeDetector(productionSwipeDetector);
        }

        void SubscribeInputRouter()
        {
            if (inputRouter == null || inputRouterSubscribed) return;
            inputRouter.OnTap += OnRouterTap;
            inputRouter.OnHoldEnd += OnRouterHoldEnd;
            inputRouter.OnSwipe += OnRouterSwipe;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed) return;
            inputRouter.OnTap -= OnRouterTap;
            inputRouter.OnHoldEnd -= OnRouterHoldEnd;
            inputRouter.OnSwipe -= OnRouterSwipe;
            inputRouterSubscribed = false;
        }
```

- [ ] **Step 4: Implement the router dispatch.** Thứ tự `OnHoldEnd` trước `OnSwipe` (đã verify ở Reference) là điều làm một cử chỉ giữ-rồi-nhả trở thành một cú chuyền:

```csharp
        // Pointer down starts the charge. In flight the same pointer down is the finishing tap,
        // which TapFinish already separates by state, so no extra mode flag is needed.
        void OnRouterTap()
        {
            if (PresentationPhase != MinigamePhase.Play) return;
            if (Rules != null && Rules.State == BasketballState.AlleyOopFlight)
            {
                SubmitFinishTap();
                return;
            }

            BeginCharge();
        }

        // The hold always resolves before the swipe on the same pointer up, so the charge this
        // release belongs to is already final when the swipe arrives.
        void OnRouterHoldEnd(double duration)
        {
            if (!IsCharging || productionHoldDetector == null) return;
            ChargeRatio = Mathf.Clamp01((float)productionHoldDetector.ChargeRatio);
        }

        // A stationary press reports a zero delta, which the detector calls a Right swipe. The
        // pass already requires a real charge, so the swipe only has to say the finger left.
        void OnRouterSwipe(SwipeResult swipe)
        {
            if (PresentationPhase != MinigamePhase.Play || !IsCharging) return;
            if (swipe.Length < minimumSwipeLengthPixels)
            {
                CancelCharge();
                return;
            }

            SubmitPass(ChargeRatio, swipe.Direction);
        }
```

- [ ] **Step 5: Implement charge, target band and pass.**

```csharp
        public void BeginCharge()
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play ||
                Rules.State != BasketballState.Holding) return;

            IsCharging = true;
            chargeElapsed = 0f;
            ChargeRatio = 0f;
            RefreshTargetChargeBand();
        }

        public void CancelCharge()
        {
            IsCharging = false;
            chargeElapsed = 0f;
            ChargeRatio = 0f;
            trajectoryPreview?.SetVisible(false);
        }

        public void SubmitPass(float chargeRatio01, SwipeDirection direction)
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play ||
                Rules.State != BasketballState.Holding) return;

            float charge = Mathf.Clamp01(chargeRatio01);
            if (charge < minimumChargeRatio)
            {
                CancelCharge();
                return;
            }

            ChargeRatio = charge;
            PendingPassVector = PassVectorForCharge(charge, direction);
            if (!Rules.TryPass(ball, PendingPassVector))
            {
                CancelCharge();
                return;
            }

            IsCharging = false;
            pendingAlleyOop = true;
            alleyOopDelay = alleyOopLeadSeconds;
            trajectoryPreview?.SetVisible(false);
        }

        float AngleForCharge(float charge01)
        {
            float span = ChargeAngleSpanDegrees;
            return passAngleCentreDegrees - span * .5f + span * Mathf.Clamp01(charge01);
        }

        // The horizontal sign follows the swipe so a left-handed layout still works; the elevation
        // is the charge. Force stays owned by AlleyOopPattern, which normalises this vector.
        Vector2 PassVectorForCharge(float charge01, SwipeDirection direction)
        {
            float radians = AngleForCharge(charge01) * Mathf.Deg2Rad;
            float sign = direction == SwipeDirection.Left ? -1f : 1f;
            return new Vector2(sign * Mathf.Cos(radians), Mathf.Sin(radians));
        }

        // Scans the charge range for the sub-range whose apex lands inside the authored band, so
        // the glowing HUD band is derived from the same pattern TryPass will create. Recomputed
        // only when the difficulty step or the launch height changes - never per frame.
        void RefreshTargetChargeBand()
        {
            float launchY = LaunchOrigin.y;
            if (cachedBandStep == DifficultyStep && Mathf.Approximately(cachedBandLaunchY, launchY)) return;

            cachedBandStep = DifficultyStep;
            cachedBandLaunchY = launchY;
            var min = float.NaN;
            var max = float.NaN;
            for (var sample = 0; sample < ChargeBandSamples; sample++)
            {
                float charge = sample / (float)(ChargeBandSamples - 1);
                float apexY = ApexHeightForCharge(charge);
                if (apexY < AuthoredBand.ApexMin || apexY > AuthoredBand.ApexMax) continue;
                if (float.IsNaN(min)) min = charge;
                max = charge;
            }

            TargetChargeMin = float.IsNaN(min) ? 0f : min;
            TargetChargeMax = float.IsNaN(max) ? 0f : max;
        }

        float ApexHeightForCharge(float charge01)
        {
            float radians = AngleForCharge(charge01) * Mathf.Deg2Rad;
            var velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * AuthoredBand.LaunchForce;
            return PredictApex(LaunchOrigin, velocity, ProfileGravity, ProfileDrag,
                Time.fixedDeltaTime, out _).y;
        }

        Vector2 LaunchOrigin => playerHand != null
            ? (Vector2)playerHand.position
            : ball != null ? ball.Body.position : Vector2.zero;

        Vector2 ProfileGravity => Physics2D.gravity * (ball != null && ball.Profile != null ? ball.Profile.GravityScale : 1f);
        float ProfileDrag => ball != null && ball.Profile != null ? ball.Profile.LinearDrag : 0f;
```

- [ ] **Step 6: Implement the shared apex predictor.** Đây là nguồn duy nhất cho apex, dùng cùng integrator với physics nên số hiển thị không bao giờ lệch số mô phỏng:

```csharp
        // Mirrors the integrator BallRig/Ballistics own, so the ring closes on the frame the ball
        // actually stops rising instead of on a closed-form estimate that linear drag invalidates.
        public static Vector2 PredictApex(Vector2 position, Vector2 velocity, Vector2 gravity,
            float linearDrag, float deltaTime, out float secondsToApex, int maxSteps = 10000)
        {
            secondsToApex = 0f;
            if (deltaTime <= 0f || maxSteps <= 0 || velocity.y <= 0f) return position;

            Vector2 current = velocity;
            Vector2 currentPosition = position;
            for (var step = 0; step < maxSteps; step++)
            {
                Vector2 next = Ballistics.AdvanceVelocity(current, gravity, 0f, linearDrag, deltaTime);
                if (next.y <= 0f) return currentPosition;
                currentPosition += next * deltaTime;
                secondsToApex += deltaTime;
                current = next;
            }

            return currentPosition;
        }
```

- [ ] **Step 7: Implement the play tick, AI launch, cue and finish.**

```csharp
        protected override void TickPlay(float dt)
        {
            if (Rules == null) return;
            float deltaTime = Mathf.Max(0f, dt);

            // The controller owns the deadline. Rules would otherwise flip their own lifecycle to
            // Resolve and the terminal path would then skip its Play guard, leaving the scene with
            // no Result panel on a timeout - exactly the S9 Volleyball failure.
            if (Rules.Phase == MinigamePhase.Play)
            {
                if (Rules.Elapsed + deltaTime >= timeLimit)
                {
                    ResolveTerminal();
                    return;
                }

                Rules.Tick(deltaTime);
            }

            if (IsCharging)
            {
                chargeElapsed += deltaTime;
                ChargeRatio = Mathf.Clamp01(chargeElapsed);   // HoldInputDetector maxChargeSeconds is 1
                Rules.Hold(deltaTime);
                RefreshChargePreview();
            }

            TickAlleyOopLaunch(deltaTime);
            RefreshFlightState();
            ResolveTerminalState();
        }

        void TickAlleyOopLaunch(float deltaTime)
        {
            if (!pendingAlleyOop || Rules.State != BasketballState.Passing) return;
            alleyOopDelay -= deltaTime;
            if (alleyOopDelay > 0f) return;

            pendingAlleyOop = false;
            if (!Rules.TryLaunchAlleyOop(ball)) return;

            PredictApex(ball.Body.position, ball.Body.velocity, ProfileGravity, ProfileDrag,
                Time.fixedDeltaTime, out launchSecondsToApex);
            launchSecondsToApex = Mathf.Max(launchSecondsToApex, Mathf.Epsilon);
        }

        void RefreshFlightState()
        {
            if (ball == null) return;
            PredictedLandingPoint = ball.PredictLandingPoint();

            if (Rules.State != BasketballState.AlleyOopFlight || !ball.Snapshot.IsInFlight)
            {
                FinishCueVisible = false;
                FlightApexProgress = 0f;
                SecondsToApex = 0f;
                ballShadow?.Refresh();
                return;
            }

            PredictedApexPoint = PredictApex(ball.Body.position, ball.Body.velocity, ProfileGravity,
                ProfileDrag, Time.fixedDeltaTime, out float remaining);
            SecondsToApex = remaining;
            FlightApexProgress = Mathf.Clamp01(1f - remaining / launchSecondsToApex);
            FinishCueVisible = remaining <= FinishCueLeadSeconds;
            MoveFinisherToPrediction();
            ballShadow?.Refresh();
        }

        // Transforms only. The assist must never write to the ball body, or it fights the physics
        // BallRig owns and walks both the actor and the ball off the court.
        void MoveFinisherToPrediction()
        {
            if (finisher == null) return;
            float target = PredictedLandingPoint.x + finisherLandingOffset;
            if (finisherBounds != null)
            {
                Bounds bounds = finisherBounds.bounds;
                target = Mathf.Clamp(target, bounds.min.x, bounds.max.x);
            }

            finisher.position = new Vector3(target, finisher.position.y, finisher.position.z);
        }

        public void SubmitFinishTap()
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play)
            {
                LastJudge = FinishJudge.Ignored;
                return;
            }

            LastJudge = Rules.TapFinish(ball.Body.position.y, ball.Body.velocity.y);
            if (LastJudge == FinishJudge.Ignored) return;

            FinishCueVisible = false;
            FlightApexProgress = 0f;
            SecondsToApex = 0f;
            if (Rules.State == BasketballState.Holding) AttachForNextAttempt();
            ResolveTerminalState();
        }

        void AttachForNextAttempt()
        {
            CancelCharge();
            cachedBandStep = -1;                 // the difficulty step just moved
            RefreshTargetChargeBand();
            ball.AttachTo(playerHand != null ? playerHand : transform);
        }

        void RefreshChargePreview()
        {
            if (trajectoryPreview == null) return;
            Vector2 direction = PassVectorForCharge(ChargeRatio, SwipeDirection.Right);
            trajectoryPreview.Refresh(direction, AuthoredBand.LaunchForce, AuthoredBand.Curvature);
            trajectoryPreview.SetVisible(true);
        }

        void ResolveTerminalState()
        {
            if (terminalResolved || Rules == null || PresentationPhase != MinigamePhase.Play) return;
            if (!Rules.PrimaryObjectiveComplete) return;
            ResolveTerminal();
        }

        void ResolveTerminal()
        {
            if (terminalResolved || PresentationPhase != MinigamePhase.Play) return;
            terminalResolved = true;
            pendingAlleyOop = false;
            CancelCharge();
            FinishCueVisible = false;
            LastResult = Rules.BuildResult();
            Finish(LastResult);
        }

        void ResetRuntimeState()
        {
            terminalResolved = false;
            pendingAlleyOop = false;
            IsCharging = false;
            chargeElapsed = 0f;
            ChargeRatio = 0f;
            PendingPassVector = Vector2.zero;
            LastJudge = FinishJudge.Ignored;
            FinishCueVisible = false;
            FlightApexProgress = 0f;
            SecondsToApex = 0f;
            launchSecondsToApex = 1f;
            PredictedApexPoint = Vector2.zero;
            PredictedLandingPoint = Vector2.zero;
            LastResult = default;
            cachedBandStep = -1;
            RefreshTargetChargeBand();
            if (ball != null) ball.AttachTo(playerHand != null ? playerHand : transform);
        }
```

- [ ] **Step 8: Implement the HUD state and the test seams.**

```csharp
        public MinigameResult BuildResult() =>
            terminalResolved && LastResult.Score > 0f ? LastResult : Rules == null ? default : Rules.BuildResult();

        public MinigameHudState BuildHudState(bool directAccess = true) => CreateHudState();
        protected override MinigameHudState BuildHudState() => CreateHudState();

        MinigameHudState CreateHudState()
        {
            if (Rules == null) return MinigameHudState.Empty;
            string status = FinishCueVisible ? "TAP AT THE APEX"
                : Rules.State == BasketballState.AlleyOopFlight ? "WAIT FOR THE APEX"
                : IsCharging ? "RELEASE IN THE BAND"
                : "HOLD TO CHARGE";
            return new MinigameHudState(
                PresentationPhase.ToString(),
                Mathf.Max(0f, timeLimit - Rules.Elapsed),
                Mathf.Clamp01(Baskets / (float)Mathf.Max(1, targetBaskets)),
                Mathf.Clamp01(ChargeRatio),
                Rules.BuildResult().Score,
                status);
        }

        public void ConfigureSceneRefsForTest(BallRig configuredBall, GameplayInputRouter router,
            Transform hand, Transform finisherActor)
        {
            ball = configuredBall;
            inputRouter = router;
            playerHand = hand;
            finisher = finisherActor;
            InstallProductionDetectors();
            SubscribeInputRouter();
            ResetRuntimeState();
        }

        public void ConfigurePresentationForTest(TrajectoryPreview preview, BallShadow shadow)
        {
            trajectoryPreview = preview;
            ballShadow = shadow;
        }

        // Closes the tutorial gate the same way PhaseOverlay does in the build, so the test path
        // and the production path share one lifecycle.
        public void SkipTutorialForTest()
        {
            SetTutorialGate(false);
            Lifecycle.Tick(DefaultCountdownSeconds);
        }

        public void SimulateForTest(float dt)
        {
            float deltaTime = Mathf.Max(0f, dt);
            Lifecycle.Tick(deltaTime);
            if (PresentationPhase == MinigamePhase.Play) TickPlay(deltaTime);
        }
    }
}
```

- [ ] **Step 9: Run the focused GREEN suite.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.Ball.BasketballControllerTests' -testResults "$evidence\t4-controller-green.xml" -logFile "$evidence\t4-controller-green.log"
```

Expected: mọi test Task 3 pass. Nếu `TapAtTheApexOfACorrectlyChargedLob_ScoresAPerfectBasket` đỏ, kiểm theo thứ tự: (a) `AuthoredBand` có threshold `1.5` chưa (Task 2), (b) `launchSecondsToApex` có được đặt sau launch chưa, (c) `PredictApex` có dùng `Time.fixedDeltaTime` chưa. Không nới lỏng assertion để cho qua.

- [ ] **Step 10: Run the ball and rules regression, then both full suites.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Gameplay.Ball' -testResults "$evidence\t4-ball-edit.xml" -logFile "$evidence\t4-ball-edit.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testResults "$evidence\t4-full-edit.xml" -logFile "$evidence\t4-full-edit.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testResults "$evidence\t4-full-play.xml" -logFile "$evidence\t4-full-play.log"
```

Expected: `BallRigTests`, `TrajectoryPreview*`, `VolleyballControllerTests` và toàn bộ suite không tụt.

- [ ] **Step 11: Commit.**

```powershell
git diff --check
git add Assets/_Project/Scripts/Gameplay/Basketball
git commit -m "feat: add basketball alley-oop gameplay controller"
```

---

### Task 5: Build the Basketball HUD, tutorial and production scene

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Basketball/BasketballHud.cs`
- Create: `Assets/Editor/BasketballSceneConfigurator.cs`
- Create: `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/BasketballSceneTests.cs`
- Modify: `Assets/_Project/Scripts/UI/PhaseOverlay.cs:113-127` (thêm nhánh, không sửa nhánh cũ)
- Modify: `Assets/_Project/Scenes/MG_Basketball.unity` (qua script authoring, không sửa YAML bằng tay)

**Interfaces:**
- Consumes: `BasketballController` (Task 4), `MinigameHUD`, `PhaseOverlay`, `TutorialOverlay`, `ResultPanel`, `PausePanel`, `GameCamera.prefab`, `BallPresentation.prefab`, `FlightProfile_Basketball.asset`, `ScreenTapArea`, `GameplayInputRouter`.
- Produces: scene `MG_Basketball` có đúng một `BasketballController`, một `BallRig`, một `GameplayInputRouter`, một `ScreenTapArea`, một `BasketballHud`, không còn `PlaceholderMinigameController`; các object có tên chính xác mà Task 6 và gate dựa vào.

Tên object bắt buộc (test assert theo tên): `BasketballCourt`, `BasketballHoop`, `BasketballBackboard`, `BasketballPlayer`, `BasketballPlayerHand`, `BasketballFinisher`, `BasketballDefender`, `FullScreenGameplayInput`, `BasketballHudCanvas`, và các label `BasketballScoreLabel`, `BasketballAttemptsLabel`, `BasketballJudgeLabel`, `BasketballComboLabel`, `BasketballChargeLabel`, cùng `BasketballApexRing`, `BasketballApexZone`, `BasketballChargeFill`, `BasketballChargeTargetBand`.

Layout authored (world unit, ground `y = 0` theo `FlightProfile_Basketball.groundY`):

| Object | Position | World size |
|---|---|---|
| `BasketballCourt` | `(0, -0.15, 1)` | `18 × 0.3` |
| `BasketballPlayer` | `(-3.5, 0.6, 0)` | `0.9 × 1.2` |
| `BasketballPlayerHand` | `(-3.5, 1.2, 0)` | không renderer |
| `BasketballHoop` (vành) | `(0, 3.05, 0)` | `0.9 × 0.1` |
| `BasketballBackboard` | `(0.6, 3.6, 1)` | `0.15 × 1.2` |
| `BasketballFinisher` | `(0, 0.7, 0)` | `0.9 × 1.4` |
| `BasketballDefender` | `(1.6, 0.65, 0)` | `0.9 × 1.3` |
| `finisherBounds` (`BoxCollider2D` trên controller) | offset `(-1.5, 0)` | size `(7, 4)` |

- [ ] **Step 1: Write the failing scene tests.** `BasketballSceneTests.cs` — sao đúng bộ helper `SceneObjects<T>`/`LabelText`/`AssertVisible` của `Assets/Tests/PlayMode/Gameplay/Ball/Volleyball/VolleyballSceneTests.cs` (lặp lại code, đừng tham chiếu chéo file test), rồi:

```csharp
        const string SceneName = "MG_Basketball";

        [UnityTest]
        public IEnumerator BasketballScene_HasPlayableControllerAndSinglePhysicsBall()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<BasketballController>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BallRig>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BasketballHud>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<PlaceholderMinigameController>(scene), Is.Empty);
            Assert.That(GameObject.Find("BasketballCourt"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballHoop"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballPlayer"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballFinisher"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballDefender"), Is.Not.Null);

            var controller = SceneObjects<BasketballController>(scene)[0];
            Assert.That(controller.Ball, Is.SameAs(SceneObjects<BallRig>(scene)[0]));
            Assert.That(controller.Ball.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(controller.PlayerHand, Is.Not.Null);
            Assert.That(controller.Finisher, Is.Not.Null);
            Assert.That(controller.Ball.Profile, Is.Not.Null);
            Assert.That(controller.Ball.Profile.name, Is.EqualTo("FlightProfile_Basketball"));
        }

        // The authored rim height and the authored apex band must agree, or the visible hoop is
        // not where the scoring window is.
        [UnityTest]
        public IEnumerator BasketballScene_PutsTheRimInsideTheAuthoredApexBand()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var controller = SceneObjects<BasketballController>(SceneManager.GetActiveScene())[0];
            var hoop = GameObject.Find("BasketballHoop");

            Assert.That(hoop.transform.position.y,
                Is.InRange(controller.AuthoredBand.ApexMin, controller.AuthoredBand.ApexMax),
                "The drawn rim must sit inside the apex band the judge scores in.");
            Assert.That(controller.PlayerHand.position.y, Is.EqualTo(1.2f).Within(.01f));
            Assert.That(controller.TargetChargeMin, Is.GreaterThan(0f));
            Assert.That(controller.TargetChargeMax, Is.GreaterThan(controller.TargetChargeMin),
                "The authored launch height must leave a reachable charge band.");
        }

        [UnityTest]
        public IEnumerator BasketballScene_RoutesGameplayThroughOneSharedInputRouterAndSurface()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<GameplayInputRouter>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<ScreenTapArea>(scene), Has.Length.EqualTo(1));

            var controller = SceneObjects<BasketballController>(scene)[0];
            var router = SceneObjects<GameplayInputRouter>(scene)[0];
            var surface = SceneObjects<ScreenTapArea>(scene)[0];
            Assert.That(controller.InputRouter, Is.SameAs(router));
            Assert.That(surface.Router, Is.SameAs(router));
            Assert.That(controller.HasProductionDetectors, Is.True);
            Assert.That(router.InputActions, Is.Not.Null,
                "Basketball must have a keyboard fallback, unlike the S9 Volleyball scene.");

            Assert.That(EventSystem.current, Is.Not.Null);
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * .5f, Screen.height * .5f)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);

            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<ScreenTapArea>(), Is.SameAs(surface),
                "The top raycast hit at screen centre must belong to the gameplay surface. Blocker: " + hits[0].gameObject.name);
        }

        [UnityTest]
        public IEnumerator BasketballScene_ReferencesTheS8PresentationKitWithoutDuplicatingIt()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<TrajectoryPreview>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BallShadow>(scene), Has.Length.EqualTo(1));

            var controller = SceneObjects<BasketballController>(scene)[0];
            Assert.That(controller.Preview, Is.SameAs(SceneObjects<TrajectoryPreview>(scene)[0]));
            Assert.That(controller.Shadow, Is.SameAs(SceneObjects<BallShadow>(scene)[0]));
            Assert.That(controller.Preview.Source, Is.SameAs(controller.Ball));
            Assert.That(controller.Preview.Line, Is.Not.Null);
            Assert.That(controller.Shadow.Target, Is.SameAs(controller.Ball.transform));
        }

        [UnityTest]
        public IEnumerator BasketballScene_ShowsGenericAndBasketballHudLabels()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<MinigameHUD>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<PausePanel>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<ResultPanel>(scene), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("GameCamera"), Is.Not.Null);

            var controller = SceneObjects<BasketballController>(scene)[0];
            var hud = SceneObjects<BasketballHud>(scene)[0];
            var sharedHud = SceneObjects<MinigameHUD>(scene)[0];
            Assert.That(hud.Controller, Is.SameAs(controller));

            yield return null;

            Assert.That(sharedHud.LastState.phase, Is.EqualTo(controller.PresentationPhase.ToString()));
            Assert.That(sharedHud.LastState.timeRemaining, Is.GreaterThan(0f));

            hud.Refresh();

            Assert.That(hud.ScoreText, Is.EqualTo("BASKETS 0/5"));
            Assert.That(hud.AttemptsText, Is.EqualTo("ATTEMPTS 0"));
            Assert.That(hud.ComboText, Is.EqualTo("COMBO 0"));
            Assert.That(hud.JudgeText, Is.Empty);
            Assert.That(hud.ChargeText, Is.EqualTo("AIM"));
            Assert.That(LabelText("BasketballScoreLabel"), Is.EqualTo(hud.ScoreText));
            Assert.That(LabelText("BasketballAttemptsLabel"), Is.EqualTo(hud.AttemptsText));
            Assert.That(LabelText("BasketballComboLabel"), Is.EqualTo(hud.ComboText));
            Assert.That(LabelText("BasketballJudgeLabel"), Is.EqualTo(hud.JudgeText));
            Assert.That(LabelText("BasketballChargeLabel"), Is.EqualTo(hud.ChargeText));
            Assert.That(GameObject.Find("BasketballApexRing"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballApexZone"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballChargeTargetBand"), Is.Not.Null);
        }

        // S9 shipped a scene whose colliders were invisible; do not repeat it.
        [UnityTest]
        public IEnumerator BasketballScene_RendersTheCourtHoopActorsAndBall()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var controller = SceneObjects<BasketballController>(SceneManager.GetActiveScene())[0];
            AssertVisible("BasketballCourt");
            AssertVisible("BasketballHoop");
            AssertVisible("BasketballBackboard");
            AssertVisible("BasketballPlayer");
            AssertVisible("BasketballFinisher");
            AssertVisible("BasketballDefender");

            var ballIsDrawn = false;
            foreach (SpriteRenderer renderer in controller.Ball.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.enabled && renderer.sprite != null &&
                    renderer.gameObject != controller.Shadow.Shadow.gameObject)
                    ballIsDrawn = true;
            }

            Assert.That(ballIsDrawn, Is.True, "The ball itself must be drawn, not only its shadow.");

            var court = GameObject.Find("BasketballCourt").GetComponent<SpriteRenderer>();
            Assert.That(court.bounds.size.x, Is.EqualTo(18f).Within(.05f),
                "Authored sizes are world units; the built-in sprite is 0.16 units, so localScale = size / 0.16.");
        }

        [UnityTest]
        public IEnumerator BasketballScene_TeachesHoldAimFinishThroughTheSharedTutorialOverlay()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<PhaseOverlay>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<TutorialOverlay>(scene), Has.Length.EqualTo(1));

            var overlay = SceneObjects<TutorialOverlay>(scene)[0];
            Assert.That(overlay.CurrentStep, Is.Not.Null);
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("HOLD"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Hold to charge the lob."));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("AIM"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Release inside the glowing charge band."));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("FINISH"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Tap when the ball reaches the apex ring."));
            Assert.That(overlay.CanGoNext, Is.False, "Three steps, as the spec requires for a complex mechanic.");
        }
```

- [ ] **Step 2: Run the scene RED.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.Ball.BasketballSceneTests' -testResults "$evidence\t5-scene-red.xml" -logFile "$evidence\t5-scene-red.log"
```

Expected: fail — `BasketballHud` chưa tồn tại và scene vẫn là placeholder.

- [ ] **Step 3: Implement `BasketballHud`.**

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class BasketballHud : MonoBehaviour
    {
        [SerializeField] BasketballController controller;
        [SerializeField] int targetBaskets = 5;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text attemptsLabel;
        [SerializeField] TMP_Text judgeLabel;
        [SerializeField] TMP_Text comboLabel;
        [SerializeField] TMP_Text chargeLabel;
        [SerializeField] Image apexRingFill;
        [SerializeField] Image apexZoneGlow;
        [SerializeField] Image chargeFill;
        [SerializeField] RectTransform chargeTargetBand;
        [SerializeField] RectTransform chargeTrack;

        public BasketballController Controller => controller;
        public string ScoreText { get; private set; } = string.Empty;
        public string AttemptsText { get; private set; } = string.Empty;
        public string JudgeText { get; private set; } = string.Empty;
        public string ComboText { get; private set; } = string.Empty;
        public string ChargeText { get; private set; } = string.Empty;
        public float ApexRing01 { get; private set; }
        public bool ApexZoneGlowing { get; private set; }

        public void Bind(BasketballController value)
        {
            controller = value;
            Refresh();
        }

        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null) return;

            ScoreText = $"BASKETS {controller.Baskets}/{targetBaskets}";
            AttemptsText = $"ATTEMPTS {controller.Attempts}";
            ComboText = $"COMBO {controller.BestCombo}";
            JudgeText = controller.LastJudge == FinishJudge.Ignored
                ? string.Empty
                : controller.LastJudge.ToString().ToUpperInvariant();
            ChargeText = controller.IsCharging
                ? $"CHARGE {Mathf.RoundToInt(controller.ChargeRatio * 100f)}%"
                : "AIM";
            ApexRing01 = controller.FlightApexProgress;
            ApexZoneGlowing = controller.FinishCueVisible;

            SetText(scoreLabel, ScoreText);
            SetText(attemptsLabel, AttemptsText);
            SetText(judgeLabel, JudgeText);
            SetText(comboLabel, ComboText);
            SetText(chargeLabel, ChargeText);

            // The ring closes around the ball as the apex approaches; the zone lights up only
            // inside the authored cue lead, so the player is warned before they must act.
            if (apexRingFill != null) apexRingFill.fillAmount = 1f - Mathf.Clamp01(ApexRing01);
            if (apexZoneGlow != null) apexZoneGlow.enabled = ApexZoneGlowing;
            if (chargeFill != null) chargeFill.fillAmount = Mathf.Clamp01(controller.ChargeRatio);
            RefreshTargetBand();
        }

        // The glowing band is the charge range that lands the lob inside the authored apex band,
        // so it moves whenever the difficulty step widens the angle span.
        void RefreshTargetBand()
        {
            if (chargeTargetBand == null || chargeTrack == null) return;
            float min = Mathf.Clamp01(controller.TargetChargeMin);
            float max = Mathf.Clamp01(controller.TargetChargeMax);
            chargeTargetBand.anchorMin = new Vector2(chargeTargetBand.anchorMin.x, min);
            chargeTargetBand.anchorMax = new Vector2(chargeTargetBand.anchorMax.x, max);
            chargeTargetBand.offsetMin = Vector2.zero;
            chargeTargetBand.offsetMax = Vector2.zero;
        }

        static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
```

- [ ] **Step 4: Add the tutorial branch.** Trong `Assets/_Project/Scripts/UI/PhaseOverlay.cs`, thêm nhánh mới ngay sau nhánh `VolleyballController` (so sánh bằng `GetType().Name` như code hiện tại, để `KMA.Gameplay.UI` không cần tham chiếu assembly Basketball):

```csharp
            else if (source.GetType().Name == "BasketballController")
            {
                tutorialOverlay.Show("Basketball", new List<TutorialStep>
                {
                    new TutorialStep("HOLD", "Hold to charge the lob."),
                    new TutorialStep("AIM", "Release inside the glowing charge band."),
                    new TutorialStep("FINISH", "Tap when the ball reaches the apex ring.")
                });
            }
```

Khoá `"Basketball"` phải parse được thành `SubjectId.Basketball` cho `TutorialSeenStore` — verify bằng test ở Task 6 Step 2.

- [ ] **Step 5: Write the scene authoring script.** `Assets/Editor/BasketballSceneConfigurator.cs`, bọc `#if UNITY_EDITOR`, namespace `KMA.EditorTools`, entry point `[MenuItem("KMA/S10/Author Basketball Scene")] public static void Author()`. Nó phải, theo thứ tự:

1. `EditorSceneManager.OpenScene("Assets/_Project/Scenes/MG_Basketball.unity", OpenSceneMode.Single)`.
2. Xoá **chỉ** object mang `PlaceholderMinigameController` (`Placeholder_MG_Basketball`); giữ nguyên `GameCamera`, `S2_HUD_Minigame`, `S2_PhaseOverlay`, `S2_ResultPanel`, `PausePanel`, `EventSystem`, `GameplayPresentation`.
3. Lấy sprite placeholder: `var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");` — nếu `null` thì `throw new InvalidOperationException(...)`; không im lặng dựng renderer không sprite (đó là lỗi S9).
4. Dựng world object theo bảng layout ở trên, dùng đúng helper này để world size là world size:

```csharp
static SpriteRenderer AddVisual(GameObject parent, string name, Sprite sprite, Color color,
    Vector3 position, float worldWidth, float worldHeight, int sortingOrder)
{
    var visual = new GameObject(name);
    visual.transform.SetParent(parent == null ? null : parent.transform, false);
    visual.transform.position = position;
    // The built-in UISprite is 0.16 x 0.16 world units, so a world size becomes a local scale.
    visual.transform.localScale = new Vector3(worldWidth / .16f, worldHeight / .16f, 1f);
    var renderer = visual.AddComponent<SpriteRenderer>();
    renderer.sprite = sprite;
    renderer.color = color;
    renderer.sortingOrder = sortingOrder;
    return renderer;
}
```

5. Instantiate `Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab`, thêm `Rigidbody2D` + `BallRig` lên root của nó, `SetProfile(AssetDatabase.LoadAssetAtPath<FlightProfile>("Assets/_Project/ScriptableObjects/Ball/FlightProfile_Basketball.asset"))`, và **thêm một `SpriteRenderer` cho chính quả bóng** (prefab không có) với world size `0.5 × 0.5`, màu `(1, .45, .15, 1)`. Đặt `Rigidbody2D.bodyType = Kinematic`, `gravityScale = 0` (BallRig tự quản).
6. `Configure` presentation kit: `preview.Configure(ballRig, lineRenderer, 16, .04f)` với `lineRenderer` lấy từ `TrajectoryPreviewLine` trong prefab; `shadow.Configure(ballRig.transform, shadowTransform, shadowRenderer, 0f, 4f, .35f, 1f, .2f, .75f)`.
7. Dựng `FullScreenGameplayInput` **giống nguyên xi** khối trong `MG_Volleyball.unity:1045-1180`: một `Canvas` (`ScreenSpaceOverlay`, `sortingOrder` **thấp hơn** canvas HUD), `CanvasScaler` (`ScaleWithScreenSize`, `1920×1080`, `matchWidthOrHeight = 1`), `GraphicRaycaster`, một `Image` trong suốt (`color.a = 0`) với `raycastTarget = true`, `GameplayInputRouter` và `ScreenTapArea`. Trên router set `inputActions` = `Assets/_Project/Settings/Input/KMA.inputactions` và `gameplayActionMapName` = `"Basketball"` (dùng `SerializedObject`, xem điểm 10). Gọi `screenTapArea.Configure(router, imageRectTransform)`.
8. Thêm object `BasketballController` mang `BasketballController` + `BoxCollider2D` (`isTrigger = true`, offset `(-1.5, 0)`, size `(7, 4)`) làm `finisherBounds`, rồi gán `ball`, `inputRouter`, `playerHand`, `finisher`, `finisherBounds`, `trajectoryPreview`, `ballShadow` bằng `SerializedObject`.
9. Dựng `BasketballHudCanvas` (`ScreenSpaceOverlay`, `sortingOrder` cao hơn canvas input, `CanvasScaler` `1920×1080`/`match 1`, một `SafeAreaFitter` con) chứa `BasketballHud` và các label/Image theo tên bắt buộc. Mọi `Graphic` trong canvas này phải `raycastTarget = false` — S9 review đã bắt đúng lỗi một backdrop full-screen ăn hết gesture.
10. Rebind shared presentation về controller mới bằng cùng cách `MinigameUIAssembler` làm:

```csharp
static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
{
    var serializedObject = new SerializedObject(target);
    var property = serializedObject.FindProperty(propertyName);
    if (property == null)
        throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{propertyName}'.");
    property.objectReferenceValue = value;
    serializedObject.ApplyModifiedPropertiesWithoutUndo();
}
```

Gán `minigameSource` cho `MinigameHUD` và cho `PhaseOverlay`, `theme` cho `MinigameHUD` từ `Assets/_Project/Settings/UI/UITheme.asset`.
11. `EditorSceneManager.MarkSceneDirty(scene)`, `EditorSceneManager.SaveScene(scene)`, `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()`.

- [ ] **Step 6: Run the authoring script headlessly.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -executeMethod KMA.EditorTools.BasketballSceneConfigurator.Author -logFile "$evidence\t5-author.log" -quit
```

Expected: exit code 0 và `git status --short` cho thấy `Assets/_Project/Scenes/MG_Basketball.unity` đã đổi. Nếu log có exception thì sửa script, **không** sửa YAML bằng tay — scene phải dựng lại được.

- [ ] **Step 7: Run the scene GREEN and the controller regression.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.Ball' -testResults "$evidence\t5-scene-green.xml" -logFile "$evidence\t5-scene-green.log"
```

Expected: `BasketballSceneTests` toàn xanh, `BasketballControllerTests` và `VolleyballSceneTests` không tụt.

- [ ] **Step 8: Verify the shared PhaseOverlay change did not disturb the other scenes.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.UI' -testResults "$evidence\t5-ui-play.xml" -logFile "$evidence\t5-ui-play.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Gameplay.UI' -testResults "$evidence\t5-ui-edit.xml" -logFile "$evidence\t5-ui-edit.log"
```

Expected: tutorial của Sprint, Endurance, Volleyball không đổi.

- [ ] **Step 9: Run both full suites** (cùng lệnh Task 1 Step 8, kết quả `t5-full-*`). Expected: không tụt.

- [ ] **Step 10: Commit.**

```powershell
git diff --check
git add Assets/_Project/Scripts/Gameplay/Basketball/BasketballHud.cs Assets/_Project/Scripts/UI/PhaseOverlay.cs Assets/Editor/BasketballSceneConfigurator.cs Assets/_Project/Scenes/MG_Basketball.unity Assets/Tests/PlayMode/Gameplay/Ball/Basketball/BasketballSceneTests.cs
git commit -m "feat: author basketball scene, HUD and tutorial"
```

---

### Task 6: Prove the Map-to-Basketball campaign route end to end

**Files:**
- Create: `Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs`
- Modify: `Assets/_Project/Scripts/Core/GameplayPresentation.cs:105-125` (thêm entry `MG_Basketball` vào `SceneTitle` và `Controls`)
- Modify: `README.md:41-63` (bảng Scenes + bảng Controls + câu về touch input)
- Verify only: `Assets/_Project/ScriptableObjects/Subjects/Basketball.asset`, `Assets/_Project/Scripts/Core/SceneRouter.cs:626`

**Interfaces:**
- Consumes: `SceneRouter.EnsurePersistentInstance()`, `StartSubject(SubjectId)`, `SubmitSubjectResult`, `CompletePunishment`, `Session.GetRecord`, `Session.ToSaveData()`, `ResultPanel.CurrentResult/PreviewRoute/Continue()`, `BasketballController` (Task 4), `GameplayInputRouter.FeedPointer*ForTest`.
- Produces: bằng chứng route thật; không thêm API production nào.

- [ ] **Step 1: Write the campaign tests.** Sao đúng khung `Assets/Tests/PlayMode/Progression/VolleyballCampaignTests.cs` (`TearDown` xoá mọi `SceneRouter`, helper `WaitForRoute`, `SkipTutorialAndReachPlay`, `RecordFor`) và thay phần chơi bằng đường Basketball:

```csharp
        const string SceneName = "MG_Basketball";

        [UnityTest]
        public IEnumerator MapToBasketballRoute_LoadsTheProductionScene()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(Object.FindFirstObjectByType<BasketballController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlaceholderMinigameController>(FindObjectsInactive.Include), Is.Null);
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(SubjectId.Basketball));
            Assert.That(router.Session.ToSaveData().activeSubject, Is.EqualTo(SubjectId.Basketball));
        }

        [UnityTest]
        public IEnumerator BasketballPass_PreviewsMapThenContinuesAndPersistsTheRecord()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            var controller = Object.FindFirstObjectByType<BasketballController>();
            var panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            Assert.That(panel, Is.Not.Null, "The Basketball scene must own a ResultPanel to continue a result.");

            yield return SkipTutorialAndReachPlay(controller);

            for (var basket = 0; basket < 5; basket++)
                yield return ScoreOneBasketThroughTheRouter(controller);

            Assert.That(controller.Baskets, Is.EqualTo(5));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(panel.CurrentResult, Is.Not.Null);
            Assert.That(panel.CurrentResult.Pass, Is.True);
            Assert.That(panel.PreviewRoute, Is.EqualTo(SessionRoute.Map.ToString()));

            panel.Continue();
            yield return WaitForRoute(router, "Map");

            SubjectRecord record = router.Session.GetRecord(SubjectId.Basketball);
            Assert.That(record.Passed, Is.True);
            Assert.That(record.FailedVisits, Is.Zero);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(RecordFor(router.Session.ToSaveData(), SubjectId.Basketball).passed, Is.True);
        }

        [UnityTest]
        public IEnumerator BasketballFirstFailure_RoutesToPunishmentAndKeepsTheAttemptActive()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            Assert.That(router.StartSubject(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(router.SubmitSubjectResult(SubjectId.Basketball,
                new MinigameResult(false, 0f, Rank.F)), Is.True);
            yield return WaitForRoute(router, "Punishment");

            Assert.That(router.Session.PendingPunishmentSubject, Is.EqualTo(SubjectId.Basketball));
            Assert.That(router.Session.VisitAttempt, Is.EqualTo(2));
            SaveData saved = router.Session.ToSaveData();
            Assert.That(saved.awaitingPunishment, Is.True);

            Assert.That(router.CompletePunishment(SubjectId.Basketball), Is.True);
            yield return WaitForRoute(router, SceneName);

            Assert.That(router.Session.PendingPunishmentSubject, Is.Null);
            Assert.That(router.Session.Lives, Is.EqualTo(GameSession.MaxLives));
        }

        // Plays a basket the way the build does: one pointer gesture through the scene's own
        // router for the lob, then one pointer tap at the apex.
        static IEnumerator ScoreOneBasketThroughTheRouter(BasketballController controller)
        {
            int before = controller.Baskets;
            GameplayInputRouter router = controller.InputRouter;
            var start = new Vector2(600f, 400f);
            double charge = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;

            router.FeedPointerDownForTest(start, 0d);
            router.FeedPointerMoveForTest(start + new Vector2(180f, 0f), charge * .5d);
            router.FeedPointerUpForTest(start + new Vector2(240f, 0f), charge);

            float guard = Time.unscaledTime + 20f;
            while (!controller.Ball.Snapshot.IsInFlight && Time.unscaledTime < guard)
                yield return new WaitForFixedUpdate();
            Assert.That(controller.Ball.Snapshot.IsInFlight, Is.True, "The AI never launched the alley-oop.");

            while (controller.Ball.Body.velocity.y > 0f && Time.unscaledTime < guard)
                yield return new WaitForFixedUpdate();

            router.FeedPointerDownForTest(start, 30d);
            router.FeedPointerUpForTest(start, 30.05d);

            Assert.That(controller.Baskets, Is.EqualTo(before + 1),
                "The routed gesture must score. Judge: " + controller.LastJudge);
            yield return new WaitForFixedUpdate();
        }
```

- [ ] **Step 2: Add the tutorial-key test to the same file.** Khoá tutorial phải map được sang `SubjectId`, nếu không `TutorialSeenStore` sẽ âm thầm không ghi nhớ:

```csharp
        [Test]
        public void BasketballTutorialKeyParsesToItsSubjectId()
        {
            Assert.That(System.Enum.TryParse("Basketball", out SubjectId parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(SubjectId.Basketball));
        }
```

- [ ] **Step 3: Run the campaign tests.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.Progression.BasketballCampaignTests' -testResults "$evidence\t6-campaign.xml" -logFile "$evidence\t6-campaign.log"
```

Expected: xanh hết. Nếu `ScoreOneBasketThroughTheRouter` không ghi được rổ, ghi `controller.LastJudge`, `TargetChargeMin/Max`, `PredictedApexPoint.y` vào ledger rồi tune **`passAngleCentreDegrees` hoặc `BasketballPlayerHand.y`** trong scene — không tune bằng cách nới assertion, không sửa rules.

- [ ] **Step 4: Add the scene text for the demo build.** Trong `GameplayPresentation.cs`, thêm vào `SceneTitle`: `"MG_Basketball" => "KMA — Basketball",` và vào `Controls`: `"MG_Basketball" => "Basketball: hold to charge, release to pass, Space to finish",`. Không sửa entry nào đang có.

- [ ] **Step 5: Reconcile the README.** Thêm vào bảng Scenes:

```markdown
| `MG_Basketball` | Charged alley-oop lob with an apex finish window |
```

Thêm vào bảng Controls:

```markdown
| Basketball | `H` hold to charge, Left/Right arrows to release, `Space` to finish |
```

Và sửa câu cuối mục Controls thành: `Touch input is supported by Endurance, Boss, and Punishment input bridges where the scene requires it. Volleyball and Basketball have no bridge: their controllers own detectors on the scene's shared GameplayInputRouter, fed by the one full-screen ScreenTapArea. Volleyball is gesture-only; Basketball also has a keyboard path through the Basketball action map.` Không sửa số test trong README ở task này — Task 7 sở hữu việc đó.

- [ ] **Step 6: Run both full suites** (`t6-full-*`). Expected: EditMode và PlayMode đều tăng đúng số test mới, 0 fail.

- [ ] **Step 7: Commit.**

```powershell
git diff --check
git add Assets/Tests/PlayMode/Progression/BasketballCampaignTests.cs Assets/_Project/Scripts/Core/GameplayPresentation.cs README.md
git commit -m "test: verify the basketball campaign route"
```

---

### Task 7: Execute the S10 gate and write the QA evidence

**Files:**
- Create: `docs/qa/s10-basketball-device-gate.md`
- Modify: `README.md` (mục Verification — cập nhật số suite; mục trạng thái S10)
- Verify: mọi file S10; không sửa scene/controller môn khác

**Interfaces:** Tài liệu QA ghi lệnh thật, số thật, hợp đồng scene, đường input, quan sát thiết bị, và nói rõ item nào **unavailable**. Nó không được claim S11–S16 hay Definition of Done toàn game.

- [ ] **Step 1: Run both full suites twice from a clean tracked status.**

```powershell
git status --short | Out-File -Encoding utf8 "$evidence\t7-status-1.txt"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testResults "$evidence\t7-gate-edit-1.xml" -logFile "$evidence\t7-gate-edit-1.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testResults "$evidence\t7-gate-play-1.xml" -logFile "$evidence\t7-gate-play-1.log"
git status --short | Out-File -Encoding utf8 "$evidence\t7-status-2.txt"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testResults "$evidence\t7-gate-edit-2.xml" -logFile "$evidence\t7-gate-edit-2.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testResults "$evidence\t7-gate-play-2.xml" -logFile "$evidence\t7-gate-play-2.log"
```

Expected: hai lần chạy cho cùng số pass, 0 fail, và `t7-status-1.txt` == `t7-status-2.txt` (test không được làm bẩn worktree). Đọc `total`/`passed`/`failed` từ attribute root của XML, đừng đếm bằng mắt.

- [ ] **Step 2: Build the Android APK.**

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -logFile "$evidence\t7-android-build.log" -quit
```

Trước khi chạy, đọc `Assets/Editor/BuildScript.cs` để lấy đúng tên method và đường dẫn output; nếu tên khác thì dùng tên thật và ghi vào QA doc. Ghi lại đường dẫn APK, số byte, SHA-256 (`Get-FileHash`), và các trường manifest (`package`, `label`, minSdk, targetSdk, ABI) qua `aapt`/`aapt2` nếu có. Nếu build thất bại vì môi trường, ghi **unavailable** kèm log — không claim đã pass.

- [ ] **Step 3: Run the device smoke flow.** Trên máy Android thật (ARM64), kiểm và ghi từng mục: Menu → Map → node `Bóng rổ` mở → vào `MG_Basketball`; tutorial 3 bước next/back/skip; giữ thấy vòng nạp lực + dải phát sáng; nhả trong dải → bóng lob tới rổ; apex ring thu lại và vùng apex sáng **trước** apex; tap ra `PERFECT`; tap sớm/muộn ra `EARLY`/`LATE`; `BASKETS`/`ATTEMPTS`/`COMBO` cập nhật; 5 rổ → Result panel → Continue → về Map, node có rank/sao; thoát app rồi mở lại → record còn; Pause Resume/Restart/Exit. Ghi model máy, phiên bản Android, và **tỉ lệ pass lượt 1 trên 10 lượt chơi** — mục tiêu spec §16 là `40–60%`.

- [ ] **Step 4: Check the visual and performance invariants.** Trên 16:9 và một máy có notch: không HUD element nào bị cắt, bóng/preview/shadow luôn thấy được, không màn đen. Dùng Profiler **trên máy thật** ghi FPS và draw call cho `MG_Basketball`. Mục nào không đo được thì ghi **unavailable — not verified, not passed**, đúng cách `docs/qa/s1-s9-stabilization-gate.md` §4 đã làm.

- [ ] **Step 5: Tune the balance through authored data only.** Nếu tỉ lệ pass ngoài dải `40–60%`, điều chỉnh **một** trong: `difficultySteps[*].chargeAngleSpanDegrees`, `difficultySteps[*].finishCueLeadSeconds`, `BasketballPlayerHand.y`, `alleyOopLeadSeconds`. Ghi số trước/sau và lý do. Không đổi `AlleyOopPattern`, không đổi `BasketballRules`, không nới test. Sau mỗi lần tune, chạy lại `KMA.Tests.Gameplay.Ball` và `KMA.Tests.Gameplay.Progression.BasketballCampaignTests`.

- [ ] **Step 6: Write `docs/qa/s10-basketball-device-gate.md`.** Bắt buộc có các mục: Baseline (commit, Unity, số suite kèm đường dẫn XML/log); Scene contract (danh sách object + component, ai sở hữu physics, ai sở hữu input); Input route (`ScreenTapArea` → router → 3 detector → controller → rules, cộng đường keyboard qua map `Basketball`); Difficulty axes (bảng 5 bậc, chỉ rõ bậc nào đổi trục nào); **Ruling** về `AlleyOopPattern.AuthoredDefault` velocity threshold kèm số đo trước/sau; Balance (tỉ lệ pass đo được, số đã tune); Known gaps (art là primitive built-in tô màu chứ không phải art authored — S16 sở hữu; không có anim cầu thủ/giảng viên; `PhaseOverlay` backdrop dùng chung vẫn làm tối gameplay ở mọi scene minigame); Physical-device gate (bảng từng mục pass/unavailable); Kết luận nói rõ **S10 chỉ chứng minh Basketball**.

- [ ] **Step 7: Update the README verification numbers.** Sửa `README.md:107` sang số suite mới đã đo hai lần, và thêm link `docs/qa/s10-basketball-device-gate.md`. Sửa `README.md:12` để nói S10 đã xong và S11–S16 vẫn ngoài phạm vi. Sửa `README.md:14` để Basketball nằm trong nhóm scene chơi được (Sprint, Endurance, Volleyball, Basketball) và nhóm "chỉ có rules" còn lại là PingPong, Badminton, Football.

- [ ] **Step 8: Commit.**

```powershell
git diff --check
git add docs/qa/s10-basketball-device-gate.md README.md
git commit -m "test: verify the S10 basketball gate"
```

---

## Verification Commands

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$evidence = 'D:\project\the-chat-KMA\.superpowers\sdd\2026-09-07-s10-basketball'

# Full suites
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testResults "$evidence\editmode.xml" -logFile "$evidence\editmode.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testResults "$evidence\playmode.xml" -logFile "$evidence\playmode.log"

# S10 focused
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode -testFilter 'KMA.Tests.Gameplay.Ball.Basketball' -testResults "$evidence\s10-play.xml" -logFile "$evidence\s10-play.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode -testFilter 'KMA.Tests.Gameplay.Ball' -testResults "$evidence\s10-edit.xml" -logFile "$evidence\s10-edit.log"

# Re-author the scene
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -executeMethod KMA.EditorTools.BasketballSceneConfigurator.Author -logFile "$evidence\author.log" -quit

git diff --check
git status --short
```

Đọc kết quả từ attribute `total`/`passed`/`failed` ở root node của XML. Một log chứa `No valid Unity Editor license found` **không phải** kết quả test — xử lý licensing rồi chạy lại.

## Plan Self-Review

- **Spec coverage (§6 S10):** chuỗi `Hold(dt) → TryPass → TryLaunchAlleyOop → TapFinish → FinishJudge` ở Task 4 Steps 4–7; HUD apex ring + vùng apex phát sáng + nhãn `EARLY/PERFECT/LATE` + `Baskets`/`Attempts`/`ApexProgress`/`BestCombo` ở Task 5 Step 3; "mỗi phase một trục" ở Task 4 Step 2 và test `AuthoredDifficultyTable_ChangesExactlyOneAxisPerStep` (Task 3 Step 5); nội dung tutorial 3 bước (yêu cầu chung của §6 cho S6–S13) ở Task 5 Step 4. Template §6 `input → rules API → HUD → scene → test → gate` map đúng Task 1 → 4 → 5 → 5 → 3/6 → 7. §9 (test bổ sung) ở Task 3/5/6; §10 chỉ được claim phần Basketball, ghi rõ ở Task 7 Step 6.
- **Gap the spec did not anticipate:** brief S10 giả định "S9–S13 là việc nối dây, không phải thiết kế luật". Đúng với state machine và scoring, nhưng **không** đúng với hai con số: cửa sổ `velocityThreshold = .1f` mở dưới một bước physics, và lực authored `8` không thể đưa bóng lên dải `[2.8, 3.2]` nếu điểm phóng ở mặt sân. Task 2 xử lý cái thứ nhất bằng một hằng số authored có đo lường; cái thứ hai xử lý hoàn toàn phía controller bằng `launchHeight = 1.2` + charge→góc, không sửa rules. Cả hai đều được ghi vào QA doc.
- **Placeholder scan:** không có `TBD`, `TODO`, "implement later", "add appropriate error handling", hay "write tests for the above". Mọi step code đều có code block; mọi step verify đều có lệnh và kỳ vọng cụ thể.
- **Type consistency:** `BasketballController.PredictApex(Vector2, Vector2, Vector2, float, float, out float, int)` khai báo ở Interfaces, dùng ở Task 3 Step 7 (`ApexHeightFor`), hiện thực ở Task 4 Step 6, gọi lại ở Task 4 Steps 5/7 — cùng một chữ ký. `TargetChargeMin/Max`, `FlightApexProgress`, `FinishCueVisible`, `LastJudge`, `DifficultySteps`, `AlleyOopLeadSecondsForTest`, `HasProductionDetectors`, `AuthoredBand` xuất hiện trong test (Task 3) trước khi hiện thực (Task 4) với đúng tên và kiểu. `BasketballHud` property khớp assertion Task 5 Step 1. `SetTapMashDetector`/`SetHoldDetector`/`OnTap` khai báo Task 1, dùng Task 4.
- **Contract changes made deliberately, not silently:** (a) `AlleyOopPattern.AuthoredDefault` velocityThreshold `.1f → 1.5f` — Task 2, đã verify không test nào ghim; (b) `InputAssetContractTests` mở từ 5 lên 6 map — Task 1 Step 3, có lý do và có test cho map mới. Không có thay đổi hợp đồng nào khác.
- **Lifecycle hazard closed:** rules dùng lifecycle riêng nên `TapFinish`'s `BeginResolve` không nuốt `Completed`; controller sở hữu deadline. Test `FifthBasket_FinishesOnceWithAPassingResult` và `Deadline_ResolvesOnceWithAFailingResultWhenTheObjectiveIsIncomplete` chạy qua đúng đường dựng production — chính chỗ lệch test/production đã che lỗi S9.
- **Dirty-worktree safety:** `.claude/` và `.superpowers/sdd/2026-09-02-s1-s9-stabilization/` phải còn nguyên; mỗi task chỉ stage file của mình; bằng chứng S10 nằm dưới `.superpowers/sdd/2026-09-07-s10-basketball/`.
- **Scope:** plan không chạm S11 (PingPong), S12 (Badminton), S13 (Football), S14 (Boss/Punishment polish), S15 (ending/credits) hay S16 (art/audio/release). Bốn scene placeholder còn lại vẫn là placeholder, và QA doc phải nói vậy.
