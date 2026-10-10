# Hướng dẫn chơi và luật chơi cho mọi minigame

**Ngày:** 2026-10-11

**Trạng thái:** Thiết kế đã được người dùng duyệt từng phần trong hội thoại; đặc tả viết chờ duyệt, sau đó lập kế hoạch triển khai.

## 1. Vấn đề và mục tiêu

Năm minigame đang chạy (Chạy nước rút, Sút phạt đền, Bóng chuyền, Nhảy ếch, Cờ vua thi cuối kỳ) chỉ có một dòng gợi ý ngắn hoặc thẻ intro riêng; không game nào giải thích đủ điều khiển, mục tiêu và luật thắng/thua. Thẻ hướng dẫn dùng chung `TutorialOverlay` có sẵn trong prefab `PhaseOverlay` nhưng là code chết: cả 5 controller đặt `UsesSharedTutorial => false` và `TutorialOverlay.Show(...)` không được gọi ở đâu ngoài test.

**Mục tiêu:** mỗi minigame có bảng hướng dẫn nhiều trang, tự hiện lần đầu vào game, và mở lại được bất cứ lúc nào từ menu tạm dừng.

Quyết định của người dùng:

| Câu hỏi | Quyết định |
|---|---|
| Khi nào hiện | **Tự hiện lần đầu** vào mỗi game; mở lại bằng nút **HƯỚNG DẪN** trong menu tạm dừng |
| Số liệu trong luật | Cách chơi viết cố định; **mục tiêu lấy từ bài đang chơi** (mét, giây, bàn, điểm, nước cờ) |
| Bố cục | **Số trang tuỳ game** |
| Hướng làm | Bảng mới dựng lúc chạy bằng UiKit; gỡ `TutorialOverlay` chết |
| Dòng mục tiêu Sprint lệch số | **Sửa luôn** trong cùng đợt |

Cờ "đã xem" tính **theo game**, không theo bài: mỗi game tự hiện đúng một lần.

**Ngoài phạm vi:** hình minh hoạ/animation trong trang; thay đổi intro riêng của Bóng đá và Cờ vua; thay đổi luật chơi.

## 2. Nội dung

Ký hiệu `{…}` là số lấy từ bài hiện tại. Tiêu đề trang viết HOA; thân trang là câu thường, qua `VietText.Fix` như mọi nhãn khác.

### 2.0 Trang chung "NẾU TRƯỢT"

Thêm vào cuối Sprint, Bóng đá, Bóng chuyền **chỉ khi** `ChallengeKind` là `Practice` hoặc `Exam` (đúng tập `JourneyProgress.IsPenalizedKind`):

> Trượt lần đầu: phải qua Nhảy ếch để giữ mạng. Trượt từ lần hai: mất 1 mạng và vẫn phải nhảy ếch.

### 2.1 Chạy nước rút (3 trang + "Nếu trượt")

Nguồn số: `SprintController.TargetCount`, `TargetDistance`, `TargetTime` (từ `ChallengeDefinition`).

1. **MỤC TIÊU**
   - Learn: "Bấm TRÁI, PHẢI luân phiên đúng {TargetCount} nhịp liên tiếp. Không tính giờ."
   - Practice/Exam: "Chạy {Distance} m trong {TimeLimit} giây. Có 3 bạn chạy cùng."
2. **ĐIỀU KHIỂN:** "Bấm TRÁI rồi PHẢI luân phiên để chạy. Giữ nhịp đều để lên CHUỖI và BỨT TỐC. Ngừng bấm là chậm lại."
3. **LUẬT:** "Bấm sai bên thì mất chuỗi và gần như không tăng tốc. Hết giờ chưa về đích là trượt. Điểm tính theo độ chính xác, thứ hạng và thời gian còn dư."

### 2.2 Sút phạt đền (4 trang + "Nếu trượt")

Nguồn số: `FootballMatchOptions` do `FootballController.OptionsFor` trả về (không đọc trực tiếp asset, vì Learn và Exam đang cố định 3 bàn / 5 lượt trong code).

1. **MỤC TIÊU**
   - Learn: "Ghi {RequiredGoals} bàn. Không có thủ môn, sút bao nhiêu lượt cũng được."
   - Practice/Exam/Final: "Ghi {RequiredGoals} bàn trong {MaxKicks} lượt sút. Có thủ môn."
2. **NGẮM:** "Kéo thanh hướng sang TRÁI hoặc PHẢI để chọn góc. Đường bay dự kiến hiện ra khi bạn lấy lực."
3. **LỰC:** "Giữ nút SÚT thì lực tăng rồi giảm liên tục. Thả tay đúng lúc để sút. Quá mạnh dễ vọt xà, quá yếu bóng dừng trước khung thành."
4. **LUẬT:** "Trúng cột, trúng xà, chệch khung hay bị thủ môn cản phá đều mất lượt."

### 2.3 Bóng chuyền (4 trang + "Nếu trượt")

Nguồn số: `VolleyballChallengeRules` (`TargetCount`, `PointsToWin`, `TimeLimit`).

1. **MỤC TIÊU**
   - Learn: "Đỡ bóng thành công {TargetCount} lần. Đối thủ luôn giao bóng."
   - Practice: "Ghi {PointsToWin} điểm trước đối thủ trong {TimeLimit} giây."
   - Exam: "Ghi {PointsToWin} điểm trước đối thủ. Hai bên luân phiên giao bóng."
2. **DI CHUYỂN:** "Kéo joystick để chạy tới chỗ bóng rơi."
3. **ĐÁNH & NHẢY:** "Nút ĐÁNH tự chọn giao, đỡ, chuyền, đập hoặc chắn tuỳ tình huống. NHẢY rồi kéo joystick để nhắm hướng đập. Nút NHẢY sáng lên là lúc nên nhảy."
4. **LUẬT:** "Bấm đúng nhịp: HOÀN HẢO, rồi TỐT, rồi SỚM/MUỘN. Mỗi bên chạm tối đa 3 lần. Bóng không qua lưới là mất điểm."

### 2.4 Nhảy ếch (3 trang)

Nguồn số: `FrogJumpTuning` đang dùng (`trackMetres`, thời gian giới hạn, thời gian đứng dậy sau khi ngã) và `session.PendingFrogJump.SavesLife`.

1. **MỤC TIÊU:** "Nhảy hết {trackMetres} m trong {timeLimit} giây. Đây là thử thách bắt buộc sau khi trượt bài."
   - Khi `SavesLife`: thêm "Về đích để giữ mạng."
   - Khi `!SavesLife`: lúc lập kế hoạch đọc `JourneyProgress.TryApplyFrogJump`/`GameSession.TryApplyFrogJump` để ghi đúng hệ quả (hoặc không thêm câu nào nếu kết quả không ảnh hưởng gì).
2. **ĐIỀU KHIỂN:** "Kim chạy qua lại trên thanh lực. Chạm màn hình khi kim ở vùng xanh. Càng gần giữa càng nhảy xa."
3. **LUẬT:** "Chạm vùng đỏ là NGÃ: không tiến được và mất {fallRecovery} giây đứng dậy."

### 2.5 Cờ vua thi cuối kỳ (3 trang)

Nguồn số: `ChessFinalStateMachine` (`MaxPlayerMoves`, `MaxMistakes`, `Clock.Limit`). Thời gian viết bằng chữ qua hàm sẵn có `TimeWords` của `ChessFinalController`.

1. **MỤC TIÊU:** "Chiếu hết trong {MaxPlayerMoves} nước. Thời gian suy nghĩ {TimeWords(limit)}. Bài thi cuối không mất mạng."
2. **ĐIỀU KHIỂN:** "{cách đi quân}. Tốt phong cấp thì chọn quân muốn đổi. Nút GỢI Ý có 3 mức, từ ý tưởng tới nước đi cụ thể."
   - `{cách đi quân}` viết theo thao tác thật trong `ChessBoardView` (chạm-chạm hay kéo-thả), kiểm tra khi lập kế hoạch. Mặc định: "Chọn quân rồi chọn ô để đi."
3. **LUẬT:** "Nước không hợp lệ thì không tính. Đi sai bị tính 1 lỗi, bàn cờ giữ nguyên. Sai quá {MaxMistakes} lần hoặc hết giờ là trượt. Đồng hồ chỉ chạy trong lượt của bạn. Dùng gợi ý, đi sai và đi chậm đều bị trừ điểm."

### 2.6 Sửa dòng mục tiêu Sprint

`objective` trong asset bài lệch với luật thật (`distance`/`timeLimit` của chính asset):

| Asset | Hiện tại | Sửa thành |
|---|---|---|
| `ScriptableObjects/Journey/sprint_practice.asset` | "Chạy 150 m trong tối đa 30 giây. Bài luyện không giới hạn thể lực." | "Chạy 150 m trong tối đa 20 giây." |
| `ScriptableObjects/Journey/sprint_exam.asset` | "Chạy 150 m trong tối đa 22 giây." | "Chạy 150 m trong tối đa 15 giây." |

Câu "không giới hạn thể lực" bị bỏ vì cơ chế thể lực đã gỡ (commit 48f7849).

## 3. Kiến trúc

### 3.1 Nguồn nội dung

- Interface mới trong assembly `KMA.Gameplay.UI`:

  ```csharp
  public interface IMinigameGuideSource
  {
      string GuideKey { get; }                       // "Sprint", "Football", "Volleyball", "Chess", "FrogJump"
      IReadOnlyList<TutorialStep> BuildGuide();      // gọi lúc mở bảng, dùng số của bài hiện tại
  }
  ```

- 5 controller (`SprintController`, `FootballController`, `VolleyballController`, `FrogJumpController`, `ChessFinalController`) cài interface này. Mọi assembly game đã tham chiếu `KMA.Gameplay.UI`.
- Chữ nằm trong lớp tĩnh thuần của mỗi game: `SprintGuide`, `FootballGuide`, `VolleyballGuide`, `FrogJumpGuide`, `ChessGuide`. Đầu vào là giá trị thuần (kind, số), không phải MonoBehaviour, để test EditMode được.
- Trang "NẾU TRƯỢT" là một hàm dùng chung trong UI (`MinigameGuidePages.FailurePage(ChallengeKind)` trả `null` cho Learn/Final).
- `TutorialStep` giữ nguyên (title, instruction; icon/animationKey để trống).
- Thứ tự gọi đã kiểm: `SceneRouter.OnSceneLoaded` gọi `ConfigureChallenge` trong `sceneLoaded`, trước `Start`, nên khi bảng mở ở frame đầu, số liệu bài đã có.

### 3.2 Bảng `MinigameGuidePanel`

- Dựng lúc chạy bằng UiKit, cùng phong cách menu tạm dừng (scrim + `UiKit.StylePanel` card, nhãn `MinigameUiTheme`). Canvas riêng `overrideSorting`, thứ tự vẽ **950** (menu tạm dừng là 900).
- Card: tiêu đề trang, thân trang (`UiKit.FitLabel`), chỉ số "n / N", hàng nút.
- Logic lật trang tách thành lớp thuần `GuideNavigator` (index, CanBack, CanNext, IsLast), test được không cần scene.
- Hai chế độ:

  | | Lần đầu (`FirstRun`) | Xem lại (`Review`) |
  |---|---|---|
  | Mở bởi | `MinigameGuideHost`, frame đầu | Nút HƯỚNG DẪN trong menu tạm dừng |
  | Nút | BỎ QUA · QUAY LẠI · TIẾP / **BẮT ĐẦU** ở trang cuối | QUAY LẠI · TIẾP / **ĐÓNG** ở trang cuối |
  | Khi đóng | Đánh dấu đã xem; bỏ đóng băng | Không đổi cờ; về lại menu tạm dừng (vẫn đang tạm dừng) |
  | Đóng băng game | Có (mục 3.3) | Không thêm (menu tạm dừng đã đóng băng) |

- BỎ QUA ở chế độ lần đầu cũng đánh dấu đã xem.

### 3.3 Giữ game lại trong lúc đọc

- Tách phần đóng băng trong `PausePanel.Open/Resume` (`IPauseAware.SetPaused` + `Time.timeScale`) thành helper dùng chung `GameFreeze` (UI assembly) **có đếm tham chiếu**: `Acquire()` lần đầu lưu `timeScale` cũ, đặt 0 và báo `SetPaused(true)`; `Release()` cuối cùng khôi phục. `PausePanel` và `MinigameGuidePanel` đều dùng helper này.
- Nhờ vậy không cần sửa cổng bắt đầu của từng game:
  - Bóng chuyền, Nhảy ếch: `MinigameBase.Update` và đếm ngược `PhaseOverlay` dùng `Time.deltaTime` nên đứng yên.
  - Bóng đá, Cờ vua: thẻ Bắt đầu riêng đã chờ người chơi; nằm dưới bảng. Lần đầu người chơi bấm hai lần (BẮT ĐẦU của bảng, rồi Bắt đầu của game) — chấp nhận.
  - **Sprint:** `SprintStartPresentation.Update` dùng `Time.unscaledDeltaTime` nên vẫn đếm khi `timeScale = 0`. Sửa: bỏ qua `Tick` khi `Time.timeScale == 0`. Sửa này cũng chữa lỗi hiện có: tạm dừng trong lúc Sprint đếm ngược không dừng đếm ngược.

### 3.4 `MinigameGuideHost`

- Cài tự động khi scene load (cùng kiểu `GameplayPauseFlowController`), chỉ khi scene có `IMinigameGuideSource` đang bật.
- Frame đầu (`Start`): nếu `seenStore.HasSeen(GuideKey)` là false thì mở bảng chế độ `FirstRun`.
- Giữ tham chiếu bảng để menu tạm dừng mở chế độ `Review`.

### 3.5 Menu tạm dừng

- `PausePanel.EnsureMenu` thêm nút **HƯỚNG DẪN** (Secondary), chỉ hiện khi scene có `MinigameGuideHost`; card cao thêm một hàng. Bấm → host mở bảng `Review`.
- `SetLeaveOptionsVisible(false)` (Nhảy ếch) giữ nút HƯỚNG DẪN: menu rút gọn gồm TIẾP TỤC + HƯỚNG DẪN.
- Sprint tạo `PausePanel` lúc chạy (`SprintFestivalPresentation.EnsurePause`); nút được thêm trong `EnsureMenu` nên áp dụng luôn.

### 3.6 Cờ "đã xem"

- Sprint, Football, Volleyball, Chess: dùng lại `GameManager.HasSeenTutorial/MarkTutorialSeen(SubjectId)` qua `SaveDataTutorialSeenStore` như hiện tại.
- Nhảy ếch: thêm `bool frogJumpTutorialSeen` vào `SaveData` (và bản sao trong `SaveSystem`, các chỗ clone/migrate ở `GameManager`, `JourneySaveMigration`). `SaveDataTutorialSeenStore` nhận khoá `"FrogJump"` và đọc/ghi qua `GameManager.HasSeenFrogJumpTutorial/MarkFrogJumpTutorialSeen`. Khi lập kế hoạch kiểm `SaveSystem` validate để save cũ (thiếu trường, mặc định false) vẫn tải được; không đổi `version` nếu không bắt buộc.
- Save cũ nào lỡ có cờ `tutorialSeen` = true sẽ không tự hiện lần đầu; người chơi vẫn mở được từ menu tạm dừng. Chấp nhận.

### 3.7 Dọn dẹp

- Xoá `TutorialOverlay` (component) và cây con của nó (`tutorialRoot`) khỏi `Prefabs/UI/PhaseOverlay.prefab`; bỏ trường `tutorialOverlay`, `tutorialRoot` và phần đăng ký `Completed` trong `PhaseOverlay.cs`.
- `PhaseOverlay.ConfigureTutorial` còn lại: bind Sprint start presentation (nếu `OwnsStartGate`), và nhả cổng khi `!OwnsStartGate`.
- Xoá lớp `TutorialOverlay`; giữ `TutorialStep`, `ITutorialSeenStore` và các store. Cập nhật/xoá test dùng `TutorialOverlay.ConfigureForTest`.
- Sửa prefab bằng Unity Editor (qua `unity-cli`) hoặc chỉnh YAML cẩn thận, rồi mở 5 scene để chắc không còn tham chiếu thiếu.

## 4. Kiểm thử

EditMode:

- Mỗi builder nội dung: đúng số trang và đúng số liệu theo Learn/Practice/Exam/Final; trang "NẾU TRƯỢT" chỉ có ở Practice/Exam; Nhảy ếch có/không câu giữ mạng theo `SavesLife`.
- `GuideNavigator`: biên trang đầu/cuối, nút cuối là BẮT ĐẦU/ĐÓNG theo chế độ.
- `GameFreeze`: hai lần `Acquire`, một `Release` vẫn đóng băng; `Release` cuối khôi phục `timeScale` cũ.
- `SaveDataTutorialSeenStore` với khoá `"FrogJump"` lưu qua `GameManager`; save cũ thiếu trường tải được.
- `SprintStartPresentation`: không tiến cổng/đếm ngược khi `timeScale == 0`.
- `PausePanel`: có nút HƯỚNG DẪN khi có host; vẫn có khi `SetLeaveOptionsVisible(false)`.
- Asset Sprint: `objective` khớp `distance`/`timeLimit`.

Trực quan: dùng skill `testing-unity-ui-with-screenshots` chụp bảng hướng dẫn (trang 1 và trang cuối) ở cả 5 game, và menu tạm dừng có nút HƯỚNG DẪN (bản đầy đủ và bản Nhảy ếch), ở tỉ lệ màn hình điện thoại dọc/ngang mà game đang dùng.
