# Football Penalty Shootout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Thay Football placeholder bằng penalty 2D năm lượt chơi được trên Android, nối Map, lưu tiến trình và retry có trừ mạng.

**Architecture:** `FootballRules` làm chủ mô phỏng; input chỉ gửi ý định, presentation chỉ đọc trạng thái. `FootballController` nối vòng đời `MinigameBase`, còn router sở hữu giao dịch kết quả/retry. Scene và reference được tạo bằng Editor configurator rồi lưu trên đĩa.

**Tech Stack:** Unity 6000.3.23f1, C#, SpriteRenderer, uGUI/TMP, Input System hiện có, NUnit/Unity Test Framework; Android landscape ARM64/IL2CPP. Không thêm package sản phẩm.

**Spec:** [2026-09-26-football-penalty-design.md](../specs/2026-09-26-football-penalty-design.md), được người dùng duyệt ngày 2026-09-26.

## Global Constraints

- Giữ `SubjectId.Football = 6`, tên môn Bóng đá và scene `MG_Football`.
- Mỗi trận luôn có đúng năm lượt; từ ba bàn thắng trở lên là thắng.
- Không có thao tác swipe, chọn kiểu sút, spin hoặc giới hạn thời gian trận. Chờ AIM hay SHOOT không tự mất lượt.
- Giữ nhãn AIM, SHOOT, GOAL, SAVED, MISS theo yêu cầu; hướng dẫn, lựa chọn và thông báo phụ dùng tiếng Việt phù hợp KMA.
- Thiết kế tham chiếu 1920x1080; vùng chạm mỗi nút tối thiểu 180x140 đơn vị trong canvas tham chiếu, neo vào vùng an toàn.
- Thay đổi tập trung vào Football và phần tích hợp cần thiết; giữ hành vi Sprint và Volleyball.
- Không thêm lưu giữa từng cú sút; không xóa thành tích Football cũ hoặc reset campaign.
- Asset chơi game nằm trong build, có nguồn/license; không thay nhân vật thành hình học placeholder trong bản giao cuối.
- Giữ nguyên hai ZIP chưa tracked ở root; chỉ stage asset được chọn cùng metadata và tài liệu nguồn.
- Mọi lệnh shell bắt đầu bằng `rtk`; không stage toàn bộ workspace. Khi cần workspace cô lập, dùng using-git-worktrees ở lúc thực thi, không đổi working tree trong bước lập kế hoạch.

## Review Focus

1. Một ngón giữ SHOOT, ngón khác thả hoặc app pause: không tự sút/mất lượt; test Task 2.
2. Một frame rất dài, NaN hoặc infinity từ cấu hình/delta: không double resolve và không làm hỏng trạng thái; test Task 1.
3. Nhấn Retry hai lần hoặc scene loader trả null/ném lỗi: chỉ ghi nhận một thất bại, hoặc rollback và có thể bấm lại; test Task 4.
4. Back/thoát ngay trên màn thất bại: không bỏ qua trừ mạng; test Task 4 và Task 6.
5. Chạy configurator/assembler lần hai hoặc reload scene: không nhân đôi UI/listener, mất sprite reference hay quay về placeholder; test Task 5 và Task 6.

## Đường dẫn và hợp đồng xuyên task

Namespace Football giữ `KMA.Gameplay` để không đổi assembly/type ngoài phạm vi. Runtime Football nằm trong `Assets/_Project/Scripts/Gameplay/Football/`; test rules tiếp tục nằm trong assembly Ball EditMode, còn input/controller/scene có assembly Football PlayMode riêng.

| Nhóm | Files và trách nhiệm |
| --- | --- |
| Mô phỏng | `FootballRules.cs`, mới `FootballTypes.cs`, `FootballTuning.cs`, `FootballShotSolver.cs`: trạng thái, tham số, giải cú sút và keeper. Gỡ `GKPatternSet.cs` khi không còn consumer. |
| Input/controller | Mới `FootballHoldButton.cs`, `FootballInputBridge.cs`, `FootballController.cs`: pointer ownership, lifecycle, render binding. |
| View/config | Mới `FootballDifficultyConfig.cs`, `FootballPresentation.cs`, `FootballHud.cs`: Inspector config, sân/nhân vật/bóng, HUD/start. |
| Kết quả | Mới `FootballResultPanel.cs` trong Football; mới `Progression/IRetryResultPreviewPanel.cs`; sửa `Core/SceneRouter.cs`. |
| Scene/assets | Mới `Assets/Editor/FootballSceneConfigurator.cs`; sửa `MG_Football.unity`, `Football.asset`, Map availability; asset dưới `Assets/_Project/Art/Football/`. |
| Chống ghi đè presentation | Mới `Gameplay/Common/MinigamePresentationOwner.cs`, sửa `UI/MinigameUIAssembler.cs`; marker scene chỉ yêu cầu assembler bỏ qua phần UI đã được configurator riêng sở hữu. |
| Kiểm chứng | Test rules/input/controller/result/routes/scene/campaign; tài liệu QA `docs/qa/football-penalty.md`. |

Mọi file/folder Unity mới đi kèm `.meta` do Unity tạo; giữ GUID scene Football hiện tại. Các paths runtime trong bảng trên tính từ `Assets/_Project/Scripts/` trừ khi đã ghi đầy đủ.

### Lệnh kiểm tra chung

Unity trên máy đã được xác minh tồn tại tại `/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity`. Task 1 sửa portability của runner, sau đó dùng:

```bash
rtk proxy env UNITY=/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity bash tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Ball.Football football-rules
rtk proxy env UNITY=/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity bash tools/run-unity-tests.sh PlayMode KMA.Tests.Gameplay.Football football-play
rtk proxy env UNITY=/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity bash tools/run-unity-tests.sh PlayMode KMA.Tests.Gameplay.Progression football-progression
```

Mỗi lần chỉ một Editor giữ project. Nếu đang mở Editor, chạy test qua Editor hoặc đóng có lưu trước batch; không xóa lock khi process vẫn sống. Mỗi lần chạy phải tạo XML mới, có test count > 0, failed=0 và result=Passed mới được gọi là pass; lỗi compiler/Package Manager/không có XML là chưa xác minh. Test filter không được vô tình chạy 0 test.

Trước mỗi commit: `rtk git diff --check`, stage đúng file của task và `.meta`, rồi `rtk git diff --cached --name-status` để đối chiếu phạm vi. Commit message được chỉ định cuối từng task.

---

### Task 1: Rules penalty, solver và test runner hoạt động trên Linux

**Files:**
- Modify: `tools/run-unity-tests.sh`.
- Replace: `Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs`.
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballTypes.cs`, `FootballTuning.cs`, `FootballShotSolver.cs`.
- Remove after reference scan: `Assets/_Project/Scripts/Gameplay/Football/GKPatternSet.cs` và `.meta`.
- Replace test: `Assets/Tests/EditMode/Gameplay/Ball/FootballRulesTests.cs`.
- Create tests: `Assets/Tests/EditMode/Gameplay/Ball/FootballShotSolverTests.cs`, `FootballTuningTests.cs`.

**Interfaces:**
- `enum FootballDifficulty { Easy, Normal, Hard }`; `enum FootballState { Start, Aiming, AimLocked, Charging, Kicking, Flying, ShotResult, MatchResult }`; `enum FootballOutcome { Goal, Saved, Miss }`.
- `readonly struct FootballTuning`: fields `AimTraverseSeconds`, `PowerRiseSeconds`, `KeeperReactionSeconds`, `KeeperSpeed`; constructor nhận bốn float, `static FootballTuning For(FootballDifficulty difficulty)`; constructor từ chối non-finite, thời gian/tốc độ <= 0.
- `readonly struct FootballShot`: read-only `AimX`, `Power`, `TargetX`, `FlightSeconds`, `IsShort`; constructor nội bộ do solver dùng, thay type FootballShot cũ cùng commit.
- `static FootballShotSolver.Create(float aimX, float power, float signedNoise) -> FootballShot`; `KeeperX(FootballShot shot, FootballTuning tuning, float flightTime) -> float`; `Resolve(FootballShot shot, FootballTuning tuning) -> FootballOutcome`. `signedNoise` hợp lệ trong [-1,1].
- `FootballRules(FootballTuning tuning, Func<float> signedNoise = null)`; `Start()`, `LockAim()`, `BeginCharge()`, `ReleaseShot()` trả bool báo có nhận thao tác; `CancelCharge()`, `Tick(float dt)` trả void; `BuildResult() -> MinigameResult` chỉ hợp lệ ở MatchResult.
- Rules exposes read-only `State`, `AimX`, `Power`, `KeeperX`, `FlightElapsed`, `StateElapsed`, `LastShot`, `LastOutcome` (nullable), `Kicks`, `Goals`, `Outcomes` (`IReadOnlyList<FootballOutcome>`). `Start()` chỉ nhận ở Start.

- [x] **1. Sửa runner trước red test.** Giữ CLI `<EditMode|PlayMode> <filter> <name>`, phân biệt thiếu argument với filter rỗng: `''` nghĩa chạy toàn suite và bỏ cờ -testFilter. Dùng đường dẫn project phù hợp host (`pwd` trên Linux, `pwd -W` chỉ trên MSYS); tôn trọng `UNITY`, fallback Linux theo version file. Parse XML bằng Python 3 và trả nonzero khi failure/no XML/0 tests; không suy pass từ exit code Unity. Chạy `rtk proxy bash -n tools/run-unity-tests.sh`, rồi một filter đang tồn tại: `KMA.Tests.Gameplay.Ball.FlightProfileTests`; kỳ vọng XML có test và không lỗi runner.
- [x] **2. Viết test đỏ cho hợp đồng mới.** Thay toàn bộ test GKPattern/swipe cũ. Các assertion quyết định:

```csharp
var r = new FootballRules(FootballTuning.For(FootballDifficulty.Normal), () => 0f);
Assert.That(r.BeginCharge(), Is.False);
Assert.That(r.Start(), Is.True);
r.Tick(.85f); // một nửa thời gian traverse = giữa tới biên phải
Assert.That(r.AimX, Is.EqualTo(.9f).Within(.0001f));
Assert.That(r.LockAim(), Is.True);
r.Tick(60f);
Assert.That(r.AimX, Is.EqualTo(.9f).Within(.0001f));
Assert.That(r.BeginCharge(), Is.True);
r.Tick(1.4f);
Assert.That(r.Power, Is.EqualTo(1f).Within(.0001f));
r.Tick(1.4f);
Assert.That(r.Power, Is.EqualTo(0f).Within(.0001f));
Assert.That(r.ReleaseShot(), Is.True);
Assert.That(r.ReleaseShot(), Is.False);
```

Thêm `FiveKicksRequireThreeGoals` điều khiển rules qua input/tick thật: sút góc p=.85 để goal, sút p=0 để miss; các ca (2,false,0), (3,true,6), (4,true,8), (5,true,10). Sau bốn lượt dù đã có ba goal, State khác MatchResult; sau feedback lượt thứ năm đúng MatchResult, không nhận cú thứ sáu. Không dùng setter điểm test-only.

Solver tests: p=.119 short; p=.12 T=.9; p=1 T=.4; x=.9,p=.85,noise=1 vẫn target=.9; p=1 target=1.2 và MISS; x=0,p=.85 SAVED. Tạo cấu hình test reaction dài để cô lập biên khung: target=.96 trong khung, >.96 MISS. Keeper chưa di chuyển trước delay; tại biên tầm với <=.22 là SAVED. Test số ngẫu nhiên chỉ được yêu cầu một lần/cú quá lực.

`LongFrameAndSmallFramesAgree` so sánh một tick vượt windup/flight/result với nhiều tick cùng tổng thời gian: cùng Kicks, Goals, State và aim tiếp theo. `InvalidDeltaDoesNotMutate` yêu cầu Tick(NaN/infinity/negative) không đổi trạng thái; `InvalidTuningRejected` yêu cầu constructor ném ArgumentOutOfRangeException.

- [x] **3. Chạy lệnh rules ở phần lệnh chung.** Kỳ vọng fail do API mới chưa có hoặc assertion cơ chế mới; lỗi môi trường không tính là red hợp lệ.
- [x] **4. Implement rules và solver.** Copy chính xác công thức, timing .18/1 giây và bảng tuning từ spec. Dùng thời gian trạng thái, tiêu thụ dt còn dư qua các transition, không làm rơi dt sau resolve; chặn MatchResult. Khởi đầu aim ở 0 đi phải, biên ±.9. Noise mặc định bằng RNG cục bộ; chỉ sample ở release quá .85. Dữ liệu solver không hợp lệ bị từ chối, input lặp trả false. Rules không sử dụng MinigameLifecycle; controller sẽ sở hữu vòng đời này. Reference-scan các kiểu cũ trước khi xóa: `rtk proxy rg -n 'GKPattern|ShotKind|FootballPhase|ResolveAuthoredShot|FootballRules.ForTest' Assets`; sửa consumer thực sự trong phạm vi Football để project compile, không giữ luật cũ chỉ vì test cũ.
- [x] **5. Chạy lại rules filter và nhóm Ball EditMode (`KMA.Tests.Gameplay.Ball`).** Kỳ vọng test thực sự được chạy và không failure; xác nhận các bài Ball dùng chung không hỏng.
- [x] **6. Commit:** `feat: replace Football rules with five-kick penalty simulation`.

### Task 2: Input AIM và giữ/thả SHOOT có pointer ownership

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballHoldButton.cs`, `FootballInputBridge.cs`.
- Modify: `Assets/_Project/Scripts/Gameplay/Football/KMA.Gameplay.Football.asmdef` (thêm UnityEngine.UI và Unity.InputSystem).
- Create: `Assets/Tests/PlayMode/Gameplay/Football/KMA.Gameplay.Football.PlayMode.Tests.asmdef`, `FootballInputTests.cs`.

**Interfaces:**
- `FootballHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler`; events `Pressed`, `Released`, `Cancelled`; `SetInteractable(bool value)`, `Cancel()`; pointer id được quản lý nội bộ, pointer-exit không hủy hold.
- `FootballInputBridge.Configure(Button aim, FootballHoldButton shoot)`; events `AimPressed`, `ShootPressed`, `ShootReleased`, `ShootCancelled`; `SetEnabled(bool aimEnabled, bool shootEnabled)`, `CancelActivePointer()`.
- Consumes rules Task 1 ở test harness; bridge không tự điều khiển rules hay phụ thuộc controller. PlayMode test asmdef tham chiếu Gameplay, Football, UnityEngine.UI, Unity.InputSystem và test runner/NUnit.

- [x] **1. Viết `FootballInputTests`.** Dùng EventSystem/ExecuteEvents với PointerEventData thật: down id=10, up id=11 => Released count=0; up id=10 ngoài rect => count=1; up id=10 lần nữa => vẫn 1. Disable khi đang giữ => Cancelled=1, Released=0; re-enable cho pointer mới được giữ. SHOOT disabled trước AIM không phát Pressed; hai lần enable/disable bridge không nhân đôi listener. `PauseCancelsChargeWithoutKick` nối events vào rules đã AimLocked rồi CancelActivePointer: assert State= AimLocked, Power=0, Kicks=0.
- [x] **2. Chạy `football-play` bằng lệnh chung; kỳ vọng API mới thiếu hoặc assertion thất bại.**
- [x] **3. Implement hai component và asmdef.** Lưu đúng pointer sở hữu, chỉ release một lần; reset pointer trước khi gọi callback. Awake/OnEnable/OnDisable đăng ký/hủy listener đối xứng. OnApplicationPause(true)/OnApplicationFocus(false) gọi CancelActivePointer, không phát Released. Chuột Editor đi cùng đường pointer với touch; không thêm keyboard gameplay.
- [x] **4. Chạy lại `football-play`; kỳ vọng toàn bộ input tests pass, không log lỗi hoặc listener tồn sau Destroy.**
- [x] **5. Commit:** `feat: add penalty aim and hold-release input`.

### Task 3: Nhập asset, view và HUD theo trạng thái mô phỏng

**Files:**
- Create: `Assets/_Project/Art/Football/` với các thư mục `Environment/`, `Characters/`, `UI/`, `Sources/`; manifest `Assets/_Project/Art/Football/ASSET_SOURCES.md`.
- Create runtime: `FootballDifficultyConfig.cs`, `FootballPresentation.cs`, `FootballHud.cs` trong thư mục Football.
- Create config: `Assets/_Project/ScriptableObjects/Football/FootballDifficulty.asset`.
- Modify Football asmdef: thêm Unity.TextMeshPro.
- Create tests: `Assets/Tests/PlayMode/Gameplay/Football/FootballPresentationTests.cs`.

**Interfaces:**
- `FootballDifficultyConfig : ScriptableObject`, `Get(FootballDifficulty difficulty) -> FootballTuning`, ba bộ giá trị Inspector bằng đúng bảng spec.
- `FootballPresentation.Render(FootballRules rules)` và `ValidateReferences() -> bool`; serialized references sân/goal, ball/shadow, player parts, keeper parts. Chuyển đổi `GoalXToWorld(float x) -> Vector3` dùng chính hai mốc cột thật đã serialize; bóng tại t=T và keeper dùng phép ánh xạ này.
- `FootballHud`: event `StartRequested(FootballDifficulty)`; `ShowStart(FootballDifficulty selected)`, `Render(FootballRules rules)`, `ValidateReferences() -> bool`. Exposes `AimButton` và `ShootButton` để cấu hình bridge; UI controls được serialize. Hud không tự mở kết quả trận.

- [x] **1. Kiểm tra và nhập asset.** Đọc archive/SVG thật từ bốn link spec, xem ảnh bằng view_image trước khi chọn; không suy đoán góc nhìn từ tên file. Trích riêng tài nguyên cần dùng từ ZIP, loại __MACOSX và metadata Unity cũ; giữ bản SVG nguồn và ghi tên nguồn, tác giả, license, URL, file gốc, file dẫn xuất. Chọn PNG dùng tên ổn định: Environment/goal.png, field.png, ball.png; Characters/player.png, keeper.png và các phần chi cần hoạt họa; UI/button.png, panel.png, power-fill.png, crosshair.png. Đây là tên output được chuẩn hóa, không phải khẳng định gói nguồn có những tên đó. Nếu nguồn không có đủ pose, xuất phần SVG cần thiết và dựng animation transform; không tự thay bằng art khác. Bản gốc ZIP không đưa vào commit.
- [x] **2. Viết `FootballPresentationTests`.** Dùng references tối thiểu trong fixture: sau Render Aiming => SHOOT disabled; AimLocked => aim disabled/shoot enabled; p=.9 => phần trăm 90 và cảnh báo quá lực bật; ShotResult => số lượt/điểm cập nhật từ rules. AtGoalPlane test assert vị trí ball bằng GoalXToWorld(LastShot.TargetX), keeper bằng GoalXToWorld(rules.KeeperX); thay renderer không làm đổi outcome. Parametrize difficulty config exact values theo spec. Đây là contract dữ liệu hiển thị, không xem là visual QA.
- [x] **3. Chạy `football-play`, xác nhận fail do component/HUD/config chưa thực hiện.**
- [x] **4. Implement config/view/HUD.** Dùng sprite thật đã chọn, bilinear + alpha transparency/no mipmaps phù hợp cartoon. HUD reference 1920x1080, giữ aspect sân và safe area của controls; mỗi nút >=180x140. Hàng top score/remaining/five markers; mặc định Normal. Giữ start mở tới khi bấm Start. Cầu thủ windup .18 giây; ball trajectory tới đúng target và co theo chiều sâu; short shot lăn hụt .9 giây; keeper delay/position/tay lấy từ rules; nét animation thể hiện save/goal/miss đúng lúc. Tâm viền tối và khóa vàng có icon. Toàn bộ reference/config để Inspector gán được, không chỉ sinh khi Play.
- [x] **5. Chạy lại `football-play`, xác minh asset import không lỗi và tests pass.** Kiểm tra manifest liên kết tới mọi file nguồn/derived được sử dụng; kiểm tra ảnh atlas/export thấy rõ cầu thủ và thủ môn theo góc nhìn đã duyệt. Visual QA scene đầy đủ ở Task 7.
- [x] **6. Commit:** `feat: add sourced penalty artwork and state-driven presentation`.

### Task 4: Màn kết quả và giao dịch retry Football

**Files:**
- Create: `Assets/_Project/Scripts/Progression/IRetryResultPreviewPanel.cs`.
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`; `Assets/_Project/Scripts/Gameplay/Football/KMA.Gameplay.Football.asmdef` (thêm Progression).
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballResultPanel.cs`.
- Create tests: `Assets/Tests/PlayMode/Progression/FootballResultRoutingTests.cs`, `Assets/Tests/PlayMode/Gameplay/Football/FootballResultPanelTests.cs`.
- Modify: `Assets/Tests/PlayMode/Progression/KMA.Gameplay.Progression.PlayMode.Tests.asmdef` (Football, UnityEngine.UI nếu dùng trực tiếp).

**Interfaces:**
- `IRetryResultPreviewPanel : IResultPreviewPanel`: `ConfigureRetry(int remainingLives)`, `SetActionPending(bool pending, string error)`; đặt cùng Progression namespace KMA.Gameplay. `remainingLives` là số sau trừ nếu thất bại.
- `ResultPanelActions` static class cùng file: constants `Continue = "Continue"`, `Retry = "Retry"`. Giữ string event cũ để môn khác không phải đổi interface.
- `FootballResultPanel : MonoBehaviour, IRetryResultPreviewPanel`: event `ActionRequested(string)`; `Show(MinigameResult result,string previewRoute)`, hai method mở rộng ở trên; `SetGoals(int goals)`, `ValidateReferences() -> bool`; `Continue()`, `Retry()` gọi cùng đường như UI; `IsVisible` read-only. Retry chỉ hiển thị khi thua và remainingLives>0.
- Router giữ `SubmitSubjectResult(SubjectId,MinigameResult) -> bool` công khai. Thêm private `TrySubmitSubjectResult(SubjectId subject, MinigameResult result, bool retry) -> bool`; callback Football sử dụng method này. Retry chỉ hợp lệ cho Football thất bại và panel hỗ trợ mở rộng. Core không tham chiếu assembly Football.

- [x] **1. Viết test đỏ router/panel.** Dùng controller stub và panel fixture nối `BindSubject`, cùng scene loader seam đã có. Các ca: Football fail từ 2 mạng + Retry => 1 mạng, active=Football, transition scene=MG_Football, không Map trung gian; fail từ 1 mạng => GameOver, active=null, không retry. Emit Retry hai lần trong transition => một LifeLost và một SessionChanged. Loader trả null/ném exception => lives/active/record bằng snapshot trước, panel vẫn bấm được; lần sau loader hoạt động => chỉ trừ một mạng. `BackOnFailureCommitsResult` gọi `ExitActiveSubjectToMap` khi pending Football result => nhận thất bại một lần. Parametrize Continue cho Sprint/Volleyball => vẫn route hiện tại, không expose retry. Panel tests xác minh nhãn, remaining lives, disabled khi pending và enabled trở lại khi có error.
- [x] **2. Chạy `football-progression` và `football-play`; xác nhận các test mới fail đúng hành vi thiếu.**
- [x] **3. Implement giao dịch trong router.** Trước mutation chụp SaveData; SubmitResult rồi nếu retry và còn mạng gọi StartSubject(Football); chuyển thẳng scene một lần bằng TryRouteMutatedSession. Với hết mạng dùng route GameOver đã có. Chặn reentrancy cả trong event callback. Chỉ unbind panel sau route được nhận; rejected/exception khôi phục session và reset pending action + thông báo để bấm lại. LifeLost/SessionChanged phát sau khi route được nhận, dựa trên snapshot; GameManager đang save qua SessionChanged nên không tạo save writer mới. Giữ entrypoint public SubmitSubjectResult chuyển tiếp bool retry=false.
- [x] **4. Implement panel và Back interception.** Football panel hiển thị số bàn do controller gán qua `SetGoals(int goals)` trước Completed; Show hiển thị điểm/hạng từ MinigameResult và không trừ mạng. Trong `ExitActiveSubjectToMap`, nếu có pending Football result thì chạy cùng action Continue; phần còn lại giữ hành vi cũ. Panel nhận Escape/Android Back bằng Input System và gọi Continue, đồng thời chặn pause overlay trên result. Retry giữ difficulty qua scene reload bằng cấu hình phiên ở Task 5. Không sửa SaveData version.
- [x] **5. Chạy lại cả hai filter; thêm EditMode `KMA.Tests.Gameplay.Progression` để kiểm tra session/save hiện có.** Kỳ vọng rollback, retry, default Continue đều pass; lỗi route giả lập phải được LogAssert.Expect.
- [x] **6. Commit:** `feat: integrate Football results and transactional retry`.

### Task 5: Controller và scene Football được lưu, vào chơi từ Map

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/Football/FootballController.cs`.
- Create: `Assets/_Project/Scripts/Gameplay/Common/MinigamePresentationOwner.cs`.
- Create: `Assets/Editor/FootballSceneConfigurator.cs`; modify `Assets/Editor/KMA.EditorTools.asmdef` (Football).
- Modify: `Assets/_Project/Scenes/MG_Football.unity`, `Assets/_Project/ScriptableObjects/Subjects/Football.asset`, `Assets/_Project/Scripts/UI/MapPresentationBuilder.cs`, `Assets/_Project/Scripts/UI/MinigameUIAssembler.cs`.
- Create tests: `Assets/Tests/PlayMode/Gameplay/Football/FootballControllerTests.cs`, `FootballSceneTests.cs`; `Assets/Tests/EditMode/EditorTools/FootballSceneConfiguratorTests.cs`.
- Modify editor test asmdef `Assets/Tests/EditMode/EditorTools/KMA.EditorTools.EditMode.Tests.asmdef` với Football/UI/Progression reference khi cần assertions kiểu cụ thể.

**Interfaces:**
- `FootballController.Configure(FootballDifficultyConfig config, FootballInputBridge input, FootballPresentation view, FootballHud hud, FootballResultPanel resultPanel)`; `Rules`, `LastResult` read-only; `BeginMatch(FootballDifficulty difficulty) -> bool` chỉ nhận ở màn Start.
- `FootballController` override Awake/TickPlay/BuildHudState; giữ rules tại Start tới khi common Lifecycle chuyển Play, rồi Start rules một lần. Lifecycle do MinigameBase sở hữu, rules không gọi BeginResolve. Controller gọi `Finish(LastResult)` đúng một lần.
- Static session preference bên trong controller giữ difficulty qua reload; reset về Normal bằng RuntimeInitializeOnLoadMethod SubsystemRegistration để không giữ giá trị từ Play session trước. Không serialize vào save.
- `MinigamePresentationOwner : MonoBehaviour` là marker không có runtime logic; UI assembler nhận marker ở scene thì bỏ qua việc ghép UI chung, không thêm result panel thứ hai.
- `KMA.EditorTools.FootballSceneConfigurator.BuildScene() -> void`, MenuItem `KMA/Football/Build Scene`; có thể gọi bằng executeMethod. Xây scene lặp lại an toàn, giữ GUID scene.

- [x] **1. Viết test đỏ controller/scene.** Start gate không tự bỏ qua sau 10 giây; BeginMatch một lần mở countdown; chưa Play thì input không sút; đủ 5 shots + feedback mới Completed count=1. Pause sau kick giữ nguyên mô phỏng khi background; pause khi charge hủy charge. Missing references => log rõ ràng một lần, controller disabled. Scene tests yêu cầu đúng một FootballController, không PlaceholderMinigameController, đúng một IResultPreviewPanel, input EventSystem, sprite references thật, font có dấu tiếng Việt. Configurator test tạo/reload hai lần rồi gọi MinigameUIAssembler.AssembleScenePath: giữ một controller/result panel/marker, không mất sprite hoặc bị thay bằng UI chung.
- [x] **2. Chạy `football-play` và EditMode filter `KMA.Tests.EditorTools.FootballSceneConfiguratorTests` (đặt namespace test đúng filter).** Kỳ vọng thiếu controller/scene thật hoặc assertions fail.
- [x] **3. Implement controller nối Tasks 1–4.** Configure đầy đủ trước Awake trong test fixture. Gate tutorial đóng ngay Awake, hud start không bị generic tutorial tự đóng; BeginMatch lấy config rồi mở gate. Render sau tick, đặt panel.SetGoals trước Finish, vô hiệu hóa input ở result. OnDisable unsubscribe và cancel pointer; pause/focus chặn cả TickPlay, không cộng thời gian nền. Missing reference dừng có lỗi. HUD chung BuildHudState không hiển thị timer giả; custom FootballHud là nguồn UI chính.
- [x] **4. Implement configurator và Map availability.** Dựng camera orthographic, SpriteRenderer thật, HUD safe area 1920x1080, marker, InputSystemUIInputModule, controller, Start/HUD/Result, pause/back phù hợp. Gán reference bằng SerializedObject hoặc public Configure rồi save scene/assets ngoài Play Mode. Dùng font Baloo2 hiện có. Thay placeholder trong cùng scene, giữ build settings scene đã có; chỉ thêm/bật entry nếu thiếu. Football.asset goalText đổi thành “Ghi ít nhất 3 bàn sau 5 lượt sút.”, timeLimit=0 (controller không dùng timer), unlocked=1, comingSoon=0. Map Football Available=true và màu xanh lá phù hợp môn.
- [x] **5. Chạy configurator bằng Editor hoặc lệnh:**

```bash
rtk proxy /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -quit -projectPath . -executeMethod KMA.EditorTools.FootballSceneConfigurator.BuildScene -logFile /tmp/kma-football-scene.log
```

Đọc log xác nhận scene đã lưu và không compiler/import error; chạy lại các filters bước 2 và nhóm scene presentation hiện có. Sửa assertion Football-placeholder/disabled cũ khi nó xung đột yêu cầu mới; không làm yếu assertion của Sprint/Volleyball.
- [x] **6. Commit:** `feat: replace Football placeholder with playable penalty scene`.

### Task 6: Kiểm chứng campaign, save và vòng chơi liên tiếp

**Files:**
- Create: `Assets/Tests/PlayMode/Progression/FootballCampaignTests.cs`.
- Modify: `Assets/Tests/EditMode/Progression/SaveSystemTests.cs`, `Assets/Tests/PlayMode/Presentation/ScenePresentationContractTests.cs` nếu contract Football trước đây là disabled.
- Update current documentation: `README.md`, `docs/superpowers/plans/2026-08-25-football-vertical-slice.md` (ghi superseded và link spec mới).
- Create: `docs/qa/football-penalty.md` với bảng evidence; chưa ghi pass trước khi chạy.

**Interfaces:**
- Consumes production Map selection, controller.BeginMatch, EventSystem AIM/SHOOT, panel actions, router/session và SaveSystem hiện có. Không thêm API setter goal/force-win vào production.
- Tests lưu vào thư mục tạm riêng theo pattern SaveSystemTests; không ghi đè campaign người dùng.

- [x] **1. Viết `MapToFootballToWinPersists`, `FailureRetryThenWinUsesOneLife`, `LastLifeFailureRoutesGameOver`, `ResultBackConsumesOneLife`, `ReloadedActiveFootballStartsAtStart`.** Lái input UI thật bằng EventSystem; dùng thời gian từ rules và config để aim góc + force .85, dùng force 0 cho MISS. Assert đủ năm outcome trước result; win đúng subject 6 và điểm 6/8/10 sau đọc lại save; retry reset Kicks=0, không tạo listener thừa; mất mạng chính xác. Save compatibility test nạp fixture schema hiện tại có Football best score và confirm giữ nguyên sau save/load mới.
- [x] **2. Chạy `football-progression` và EditMode `KMA.Tests.Gameplay.Progression`; nếu test fail, sửa đúng component sở hữu lỗi trước khi tiếp tục.** Test coverage ở giai đoạn này có thể pass ngay nếu Tasks 1–5 đúng; không tạo bug giả để lấy red.
- [x] **3. Cập nhật README và đánh dấu kế hoạch cũ superseded.** README mô tả ba môn chơi được, năm lượt penalty, hai nút, ngưỡng 3/5, difficulty và retry trừ mạng. Không sửa các lịch sử thiết kế không liên quan. Tạo QA report ghi tên XML/log, môi trường và trạng thái từng gate hiện có.
- [x] **4. Chạy bộ hồi quy phù hợp một lần:** toàn EditMode (`filter ''`) và toàn PlayMode (`filter ''`) bằng runner đã sửa cho phép chuỗi filter rỗng nghĩa chạy toàn suite. Nếu có failure baseline ngoài phạm vi, ghi rõ fixture/lỗi, xác định liên quan trước khi sửa. Không lặp full suite khi chưa có thay đổi/lý do mới.
- [x] **5. Commit:** `test: verify Football campaign retry and save compatibility`.

### Task 7: QA hình ảnh, thao tác thực và Android build

**Files:**
- Update: `docs/qa/football-penalty.md`; scene/view/tuning chỉ khi QA phát hiện lỗi cụ thể.
- Evidence outputs: `Builds/Screenshots/football/`, `Builds/TestResults/`, `Builds/Android/` (generated, không mặc định commit).

**Interfaces:**
- Dùng `.claude/skills/testing-unity-ui-with-screenshots/SKILL.md`, `tools/qa-screenshot.sh` và Editor GUI đang mở. Tool screenshot hiện tại chỉ hiểu capture tĩnh; không gửi actions vào request rồi cho rằng nó đã chạy input.
- Input runtime bằng Editor UI/control khả dụng hoặc fixture PlayMode; thiết bị Android để xác minh touch, pause, safe area thật. `UnityQaScenario.cs` có validator chưa chứng minh có runner hoạt động; phải probe khả năng trước khi dùng.

- [x] **1. Capture và xem màn Start ở 1920x1080.** Nếu chưa có GUI Editor, mở đúng version, không batchmode. Chạy:

```bash
rtk proxy bash tools/qa-screenshot.sh Builds/Screenshots/football/start.png Assets/_Project/Scenes/MG_Football.unity 3
```

Đọc done.json đúng request id và status=ok, sau đó mở PNG bằng view_image. Kiểm tra sân/goal/thủ môn/cầu thủ, tương phản, font có dấu, hai nút và safe area; ghi lỗi cụ thể, sửa rồi chụp lại khi cần.
- [x] **2. Kiểm tra các trạng thái còn lại trong một phiên Play điều khiển được.** Start -> AIM lock -> lực đỏ -> kick -> keeper dive -> GOAL/SAVED/MISS -> victory/defeat. Dùng capture trên phiên hiện hành hoặc ScreenCapture từ fixture trạng thái; lưu evidence và xem từng ảnh. Capture helper cũ tự thoát Play sau ảnh nên không coi các request riêng là một phiên liên tục. Ảnh từ trạng thái dựng trong fixture ghi rõ là fixture, không thay thế kiểm tra thao tác thật.
- [x] **3. Thử một trận đầy đủ bằng thao tác runtime.** Thả ngoài nút, hai ngón, chờ lực qua nhiều chu kỳ, pause khi charge và khi bóng đang bay, retry liên tiếp. Kiểm tra cooldown không nuốt lượt, thủ môn không save bóng ngoài tay, ba độ khó có khác biệt và góc tốt p<=.85 có thể ghi bàn. Nếu công cụ không thể điều khiển runtime, báo gate này chưa xác minh thay vì suy từ unit tests.
- [x] **4. Build ARM64 bằng công cụ hiện có sau khi Editor đã đóng có lưu:**

```bash
rtk proxy bash tools/build-apk.sh --arm64 --name kma-penalty --unity /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity
```

Kỳ vọng log build thành công và `Builds/Android/kma-penalty-arm64.apk` mới, ghi hash/size. Nếu thiết bị chỉ x86_64, build thêm `--x86_64` để thử emulator, ghi rõ khác biệt ABI. Dùng `rtk proxy adb devices -l` trước cài; cài `adb install -r` vào đúng thiết bị và chạy offline. Kiểm tra landscape, screen ratio rộng/safe area và pause/resume. Không có thiết bị thì APK build và device runtime là hai trạng thái khác nhau trong report.
- [x] **5. Hoàn thiện QA report và review toàn diff.** Ghi tests/build/install/runtime/visual QA riêng, ảnh đã xem, mọi limitation. Nếu QA sửa code, chạy lại đúng tests liên quan; nếu sửa tuning, chạy solver/rules và playtest độ khó. Kiểm tra không có package mới, asset placeholder, curve/swipe/timer trong Football hiện hành, hoặc thay đổi ngoài phạm vi.
- [x] **6. Commit các sửa QA và report:** `test: record penalty visual and Android validation`. Chỉ tuyên bố hoàn tất những gate có evidence; một gate môi trường bị chặn phải được nêu rõ khi bàn giao.

## Self-review và handoff

- Spec §§1–4 -> Tasks 1, 2, 5; §5 -> Tasks 3, 5, 7; §6 -> Tasks 4, 6; §7 -> Tasks 1, 2, 4, 5; §8 -> Tasks 1–7.
- Thứ tự dependency: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7. Mỗi commit phải compile; view/HUD ở Task 3 chỉ phụ thuộc rules/input đã có, result/router ở Task 4 chưa cần scene/controller.
- Kiểm tra signature xuyên task trước coding; `FootballResultPanel.SetGoals(int)` được tạo trong Task 4 và gọi trong Task 5. References assembly chỉ đi từ Football tới Progression, không từ Core về Football.
- Plan chờ người dùng review và chọn cách thực thi. Đề xuất Native vì bảy task nối tiếp dùng chung scene và router; subagent-driven là lựa chọn review riêng từng task nếu người dùng muốn.
