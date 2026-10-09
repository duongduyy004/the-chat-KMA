# Bóng chuyền: nhảy đập có nhắm hướng và nhảy chắn

**Ngày:** 2026-10-10

**Trạng thái:** Thiết kế đã được người dùng duyệt từng phần trong hội thoại; đặc tả viết chờ duyệt, sau đó lập kế hoạch triển khai.

## 1. Vấn đề và mục tiêu

Hiện NPC đỡ được **mọi** quả bóng nó chạy tới kịp (trong `OpponentReach` = 0,8 m); lần hụt duy nhất đến từ các bước kịch bản `MissesReceive` trong `OpponentPlan`. Chất lượng cú đánh của người chơi không ảnh hưởng tới việc NPC đỡ được hay không. Cú đập là cú duy nhất có hướng, nhưng `AimAtOpponent` chỉ có 2 độ sâu (x = 3 hoặc 7) và lấy hướng từ chính joystick di chuyển, nên vừa chạy vừa nhắm bị dính nhau.

**Mục tiêu:** người chơi ghi điểm nhờ kỹ năng: **nhảy lên, chọn hướng đập vào chỗ trống xa NPC**, và **nhảy ở lưới để chắn** cú đập của NPC.

Quyết định của người dùng:

| Câu hỏi | Quyết định |
|---|---|
| Kiểu điều khiển | **Nút NHẢY riêng.** Trên không không di chuyển; joystick chuyển sang nhắm; nhấn ĐÁNH để đập về đích |
| Nhảy dùng cho | **Đập + chắn.** Phát bóng, đỡ, chuyền, đỡ cứu giữ nguyên |

Giả định (người dùng không phản đối): người chơi phổ thông trên điện thoại; giữ phong cách không ngẫu nhiên của code hiện tại; NPC giữ tốc độ và luật đỡ hiện tại (đập trúng chỗ trống là điểm).

**Ngoài phạm vi:** nhảy phát bóng; nhắm hướng cho cú không phải đập (free ball, đỡ cứu chạm cuối vẫn bay về (5, 0)); sprite nhảy mới; thay đổi `OpponentPlan`.

## 2. Luật chơi

### 2.1 Nhảy

- Nhấn NHẢY khi: bóng `BallState.InPlay`, người chơi đang dưới đất và không bị khóa hành động (`!IsLocked`). Ngoài các trường hợp đó, nhấn NHẢY không có tác dụng.
- Thời gian trên không `JumpSeconds = 0.7` giây. Độ cao hiển thị `JumpPeak * sin(π · t / JumpSeconds)` với `JumpPeak = 0.8` (đơn vị sân), chỉ dùng cho hình ảnh; luật không phụ thuộc độ cao này.
- Trên không: **không di chuyển** (`Move` bỏ qua đầu vào). Joystick dùng để nhắm.
- Nhấn NHẢY khi đang trên không: không có tác dụng.

### 2.2 Nhắm và đập

- Đích đập `SmashAim(stick)` (hàm tĩnh, liên tục, luôn trong sân NPC):
  - Joystick được kẹp độ dài ≤ 1 trước.
  - `x = 5.25 + stick.x * 2.25` → kéo trái = bỏ nhỏ (3), kéo phải = đánh sâu (7,5). Mép ngắn là 3 vì đó là đích ngắn nhất mà code hiện tại đã chứng minh là qua được lưới cao 2,24 m.
  - `y = stick.y * 5`, nên chỉ cần kéo chéo là đã chạm biên. Nếu dùng hệ số 4, cú đập ngắn và rộng nhất chỉ làm đường bóng qua lưới lệch khoảng 0,8 m, nhỏ hơn tầm chắn 1 m của NPC, nghĩa là nhắm thế nào cũng không né được chắn.
  - Kẹp trong sân NPC với lề 0,5 m: `x ∈ [3, 7.5]`, `|y| ≤ 3.5` (`CourtSpace.HalfLength = 8`, `HalfWidth = 4`).
  - Joystick thả (0, 0) → đích (5.25, 0), gần chỗ đứng chờ của NPC: không nhắm thì khó ăn điểm (cố ý).
- `PlayerAim` = `SmashAim(move)` khi người chơi đang trên không.
- **Điều kiện đập** = các điều kiện hiện tại của `ResolveTouch` (chạm thứ 2–3, `|x| ≤ SmashNetDistance`, bóng đủ cao, trong `Reach`, cửa sổ thời gian smash) **cộng thêm người chơi đang trên không**.
- Kết quả theo điểm thời gian giữ nguyên:
  - Perfect/Good: bay về `SmashAim(move)` với apex `startHeight + SmashRise`.
  - Late muộn: bay về `SmashAim(move)` với apex `SetApexHeight` (bóng bổng).
  - Late sớm: rúc lưới như hiện tại.
- Đang **dưới đất**: bóng cao chỉ được đỡ/chuyền như thường (không còn đập tự động).
- Đang **trên không** mà không thỏa điều kiện đập: nhấn ĐÁNH trả `ActionDecision.None` (không đỡ trên không, không tính vào độ chính xác).
- NPC chắn cú đập của người chơi: giữ luật `OpponentBlocks`; phần NPC dự đoán đường đập (`PositionOpponentWhileDefending`) chuyển từ `AimAtOpponent(move)` sang `SmashAim(move)`.
- `AimAtOpponent` **giữ nguyên** cho cú phát bóng Perfect.

### 2.3 Chắn

- Bỏ chắn bằng nút ĐÁNH (`ActionResolver.ResolveBlock` bị gỡ; khi không có quyền chạm bóng, ĐÁNH trả `None`).
- `PressJump` khi `OpponentSmashTell` đang bật và `|Player.Position.x| ≤ BlockNetDistance` → người chơi nhảy với hành động `AthleteAction.Block` và match phát `PlayerActed(ActionDecision(Block, Miss, 0))` (để HUD/âm thanh hiện tại vẫn phản ứng). Nhảy ở nơi khác là nhảy thường (`AthleteAction.Smash` làm khung hình trên không).
- `PlayerBlocks(target)` = người chơi **đang trên không** + `|x| ≤ BlockNetDistance` + `|y − netCrossY| ≤ BlockLateral`. Kết quả chắn thành công giữ nguyên (điểm cho người chơi, bóng dội lại).

### 2.4 Không đổi

Phát bóng, đỡ, chuyền, đỡ cứu, `OpponentPlan`, tốc độ/phản ứng NPC, cách tính điểm và `BuildResult`. Practice vẫn đếm combo Receive → Receive → Smash (`ActionKind.Smash` giữ nguyên). Learn chỉ đỡ, không bị ảnh hưởng.

## 3. Điều khiển và gợi ý

- **Nút:** góc phải dưới có 2 nút tròn cùng cỡ: **ĐÁNH** ở vị trí hiện tại (góc ngoài), **NHẢY** bên trong và chếch lên. Cùng kiểu `UiKit.RoundButton`, cùng `JoystickScale`.
- **Bàn phím:** Space = ĐÁNH, **J** = NHẢY.
- **Vòng đích người chơi:** chỉ hiện khi người chơi trên không, đặt tại `PlayerAim` trên sân NPC, cùng sprite vòng với `aimMarker` nhưng màu khác để không lẫn với vòng báo đập của NPC.
- **Nhắc "NHẢY!":** `TryGetJumpCue(out float secondsToIdeal)` đúng khi người chơi đang dưới đất và:
  - (cơ hội đập) bên mình giữ bóng, chạm thứ 1–2 đã xảy ra, `|x| ≤ SmashNetDistance`, apex bóng > `SmashContactHeight`, và còn ≤ `JumpCueLead = 0.5` giây tới thời điểm đập lý tưởng; hoặc
  - (cơ hội chắn) `OpponentSmashTell` đang bật và `|x| ≤ BlockNetDistance`.

  Khi cue đúng, nút NHẢY phóng to theo nhịp. Không vẽ chữ lên vòng đứng: việc đó cần thêm một TMP world-space, trong khi nút nhấp nháy đã đủ báo hiệu.
- **Hướng dẫn HUD:**
  - Mặc định/Exam: "Joystick: di chuyển · NHẢY rồi kéo joystick để nhắm · ĐÁNH để đập"
  - Practice: "ĐỠ → CHUYỀN → NHẢY ĐẬP · {n}/{N} ĐIỂM"
  - Learn: giữ nguyên.
- **Phản hồi:** Perfect/Good/Late giữ nguyên. Chắn thành công thì hiện "CHẮN!" thông qua sự kiện mới `VolleyballMatch.PlayerBlocked`. Sự kiện này phát sau khi đã cộng điểm, nên chữ "CHẮN!" đè lên chữ "GHI ĐIỂM!".

## 4. Cấu trúc code

### 4.1 Model (EditMode test được)

- `VolleyAthlete`
  - `const float JumpSeconds = .7f`, `JumpPeak = .8f`.
  - `bool IsAirborne`, `float AirTimeLeft`, `float JumpHeight` (tính từ `AirTimeLeft`).
  - `bool TryJump(AthleteAction pose)`: thất bại nếu đang trên không hoặc bị khóa; thành công thì đặt `AirTimeLeft = JumpSeconds`, `Action = pose`.
  - `Move` không làm gì khi trên không. `Tick` đếm ngược `AirTimeLeft`; về 0 thì `Action = Idle` (nếu không bị khóa bởi hành động khác).
  - `PlaceAt` xóa trạng thái trên không.
- `VolleyballMatch`
  - `public static Vector2 SmashAim(Vector2 stick)`.
  - `public Vector2 PlayerAim`.
  - `public bool PressJump()`: kiểm tra `IsOver`, `BallState.InPlay`; chọn pose Block hoặc Smash theo §2.3; phát `PlayerActed(Block)` khi là nhảy chắn.
  - `PlayerHit` nhánh Smash dùng `SmashAim(move)`; nhánh Late sớm giữ nguyên.
  - `PlayerBlocks` dùng `Player.IsAirborne`.
  - `PositionOpponentWhileDefending` dùng `SmashAim(move)`.
  - `public bool TryGetJumpCue(out float secondsToIdeal)`.
  - `TryGetPlayerContactCue` giữ nguyên: hàm này vốn đã chỉ điểm đập khi người chơi đứng gần lưới, và chuyển sang điểm đỡ khi hết cửa sổ đập.
  - `public event Action PlayerBlocked`.
- `ActionContext` không đổi: resolver đọc `context.Athlete.IsAirborne`.
- `ActionResolver`
  - Nhánh Smash yêu cầu `context.Athlete.IsAirborne`.
  - Đang trên không mà không đập được → `None`.
  - Gỡ `ResolveBlock`; không có quyền chạm bóng → `None`.

### 4.2 Presentation

- `VolleyAthleteView.Render`: `transform.position = CourtSpace.ToWorld(athlete.Position, athlete.JumpHeight)`. Khung hình theo `Action` như hiện có (Smash/Block).
- `VolleyBallView`: thêm `playerAimMarker` (SpriteRenderer) + màu `PlayerAimColor`; hiện khi `match.Player.IsAirborne`.
- `JumpButton` (mới, giống `ActionButton`: `IPointerDownHandler`, sự kiện `Pressed`) + hàm làm nhịp đập (scale) khi được báo cue.
- `VolleyballInputBridge`: thêm `jumpButton`, `InputAction` phím J, `ConsumeJumps()`, `FeedJumpForTest()`; `Configure(stick, button, jump)`.
- `VolleyballController`: mỗi frame, sau `SetMove`, xử lý `ConsumeJumps()` → `PressJump()` trước `ConsumePresses()` → `PressAction()`; truyền `TryGetJumpCue` cho `JumpButton`. Đường đi qua `VolleyballChallengeRules` có thêm `PressJump()`.
- `VolleyballHud`: các câu hướng dẫn ở §3; feedback "CHẮN!" khi `PlayerActed(Block)`.
- `VolleyballSceneConfigurator` (Editor): tạo `JumpButton` ("NHẢY"), `PlayerAimMarker`, nối vào bridge/view; chạy lại để sinh `MG_Volleyball.unity`. File scene đang có thay đổi chưa commit trong working tree: phải hỏi người dùng trước khi ghi đè.

## 5. Kiểm thử

TDD: viết test thất bại trước rồi mới code.

**Test mới (EditMode)**
- `VolleyAthlete`: nhảy → trên không trong `JumpSeconds` rồi đáp; `Move` vô hiệu khi trên không; `TryJump` thất bại khi đang trên không hoặc bị khóa.
- `ActionResolver`: bóng cao ở lưới + dưới đất → Receive/FreeBall, không phải Smash; + trên không → Smash; trên không không có bóng → None; không có quyền chạm bóng → None.
- `VolleyballMatch`:
  - `SmashAim` các góc và trung tâm, luôn trong sân.
  - Nhảy đập với stick (1, 1) bay tới góc xa và người chơi được điểm khi NPC không với tới.
  - `PressJump` khi cầm bóng phát / đang tung / match kết thúc → false.
  - Nhảy chắn ở lưới chặn cú đập NPC (bước `OpponentPlan` có Smash) → điểm cho người chơi; nhảy sai vị trí thì không chặn.
  - `TryGetJumpCue` đúng/sai theo §3.
- `VolleyballChallengeRules`: combo Practice với nhảy đập vẫn được tính.
- `VolleyballHud`: các câu hướng dẫn mới.
- `VolleyballSceneConfiguratorTests`: có `JumpButton` nhãn "NHẢY", có player aim marker, bridge đã được nối.

**Test PlayMode:** `VolleyballInputBridgeTests` cho nút NHẢY và phím J; `VolleyballControllerTests` cho việc nhảy rồi đánh trong cùng frame.

**Sửa test cũ:** các test đập hoặc chắn bằng nút ĐÁNH (`ActionResolverTests`, `OpponentAiTests`, `VolleyballMatchTests`, `AssistedRallyTests`, test Practice). Thêm helper `MatchDriver.JumpSmash(match, stick)` và `MatchDriver.JumpBlock(match)`.

**Kiểm tra trực quan:** chụp scene bằng skill `testing-unity-ui-with-screenshots` để kiểm tra vị trí nút NHẢY/ĐÁNH, vòng đích, nhấp nháy cue.

## 6. Rủi ro

- Nhảy rồi nhấn ĐÁNH là 2 lần bấm liên tiếp, có thể khó với người chơi phổ thông. Giảm thiểu: cửa sổ trên không 0,7 giây bao trọn cửa sổ smash, cộng thêm cue "NHẢY!". Nếu chơi thử vẫn khó thì chỉnh `JumpSeconds` hoặc `JumpCueLead` (hằng số, không đổi cấu trúc).
- Lúc nhảy sớm người chơi bị đứng yên và có thể lệch khỏi điểm chạm. Vòng đứng (contact ring) vẫn hiện màu ngoài tầm/trong tầm để người chơi biết.
