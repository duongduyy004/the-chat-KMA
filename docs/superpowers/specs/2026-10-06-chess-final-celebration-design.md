# Bài kiểm tra cuối (cờ vua) và scene ăn mừng

**Ngày:** 2026-10-06

**Trạng thái:** Thiết kế đã được người dùng duyệt từng phần trong hội thoại; đặc tả viết chờ duyệt, sau đó lập kế hoạch triển khai.

**Nguồn yêu cầu:** prompt nhóm “Level cuối cờ vua và scene ăn mừng trong Chạy trốn thể chất” (gọi tắt là *prompt gốc*). Đặc tả này ghi lại cách áp dụng prompt gốc vào repo hiện tại và các quyết định đã chốt.

## 1. Mục tiêu và quyết định đã thống nhất

Thêm chặng cuối “Bài kiểm tra cuối”: Cô Thể Chất đặt một thế cờ, Tân Thủ (người chơi, Trắng) phải chiếu hết trong tối đa 2 nước, 90 giây suy nghĩ, được sửa sai 2 lần. Thắng bài cờ hoàn tất học phần và mở scene ăn mừng “Đã qua thể chất!”.

Quyết định của người dùng:

| Câu hỏi | Quyết định |
|---|---|
| Trượt bài cờ có trừ lượt thi chung không | **Không.** Không trừ lượt, không bật cóc; bảng thất bại và chơi lại tự do |
| Asset boss | Dùng `boss-final-level-assets.zip` ở root repo; chỉ nhập `BossPE/` (30 PNG). Bỏ qua `StudentPenalty/` và các file prompt |
| Xác minh puzzle | `python-chess` + Stockfish khi soạn đề, xuất cây đáp án đầy đủ ra JSON; game chấm bằng cây này |
| Vị trí trên bản đồ | Trạm thứ 4 “Bài kiểm tra cuối” sau Bóng đá; khi hoàn tất, Map có nút “Xem lại lễ mừng” |
| Kiến trúc | Bài cờ là môn thứ tư trong hệ thử thách hiện có (không tạo hệ song song) |
| Tên vai và hình | Giữ nguyên tên vai hiện có; chỉ đổi hình. Cô Thể Chất dùng BossPE ở màn cờ, scene ăn mừng và chân dung hội thoại |

Bối cảnh repo ảnh hưởng thiết kế: Unity 6000.3.23f1, Android landscape, chạy offline, uGUI + TMP. Chạy Stockfish lúc chơi trên Android không phù hợp; vì vậy engine chỉ dùng ở công cụ soạn đề.

### Giả định (chưa được người dùng nêu, dùng làm mặc định)

- Không có AI ngôn ngữ; lời thoại cố định tiếng Việt.
- Chặng hành trình chỉ dùng độ khó Thường. Dữ liệu hỗ trợ trường `difficulty`; đề Dễ/Khó được thêm nếu tìm được đề đạt kiểm tra nhưng không dùng trong hành trình.
- Không có thưởng/mở khóa nào được cấp tại scene ăn mừng; xem lại không có tác dụng phụ ngoài cờ `CelebrationSeen`.

## 2. Lõi cờ (C# thuần)

Thư mục `Assets/_Project/Scripts/Gameplay/Chess/Core/`, asmdef `KMA.Gameplay.Chess.Core`, không tham chiếu `UnityEngine`.

| Đơn vị | Trách nhiệm |
|---|---|
| `ChessPosition` | Bàn cờ, bên đi, quyền nhập thành, ô bắt tốt qua đường, bộ đếm nước. `FromFen`/`ToFen`. Bất biến: `Apply(move)` trả vị trí mới, snapshot/hoàn tác là giữ tham chiếu cũ |
| `ChessMove` | Ô đi, ô đến, quân phong cấp; `ToUci()`/`ParseUci()` (ví dụ `a7a8q`) |
| `MoveGenerator` | Nước hợp lệ đầy đủ: chiếu, ghim, nhập thành (không đi qua/đứng trên ô bị chiếu), bắt tốt qua đường, phong cấp 4 loại. `IsCheck`, `IsCheckmate`, `IsStalemate` |
| `PuzzleDefinition` | Dữ liệu đề đọc từ JSON |
| `PuzzleGrader` | Chấm một nước tại vị trí và nút cây hiện tại |
| `ThinkClock` | Cộng thời gian chỉ khi được bật |
| `ChessFinalStateMachine` | Trạng thái màn chơi, bộ đếm lỗi, snapshot, kết quả |

### 2.1. Dữ liệu puzzle

Tệp `Assets/_Project/Resources/Chess/Puzzles/<id>.json`, do script sinh ra, không sửa tay:

```json
{
  "id": "m2_001",
  "sourcePuzzleId": "lichess:XXXXX",
  "startFen": "...",
  "playerColor": "w",
  "objective": "mate",
  "maxPlayerMoves": 2,
  "timeLimitSeconds": 90,
  "maxRecoverableMistakes": 2,
  "difficulty": "Normal",
  "ideaHint": "Câu gợi ý ý tưởng (mức 1)",
  "tree": { "<uci trắng>": { "replies": { "<uci đen>": { "<uci trắng>": {} } } } }
}
```

`tree` là nút cho lượt Trắng: khóa là mọi nước Trắng được chấp nhận. Mỗi nước chưa kết thúc có `replies`: khóa là **mọi** nước hợp lệ của Đen, thứ tự khóa là thứ tự ưu tiên của boss. Nút rỗng `{}` nghĩa là nước Trắng đó chiếu hết. Gợi ý mức 2 và 3 được tính từ cây tại nút hiện tại; `ideaHint` (mức 1) do người soạn viết, một câu cho cả đề.

### 2.2. Quy tắc chấm

Thứ tự kiểm tra khi người chơi gửi một nước:

1. Không thuộc danh sách nước hợp lệ → `Illegal`. Không commit, không tính lỗi. (Bàn cờ chỉ cho chọn ô hợp lệ; đây là chốt chặn.)
2. Sau nước đó bàn cờ chiếu hết → `Solved`, kể cả khi nước không có trong cây.
3. Nước có trong nút cây hiện tại → `Accepted`; boss đáp bằng khóa đầu tiên của `replies`.
4. Còn lại → `Wrong`.

Bước 4 chính xác vì cây được dựng **vét cạn**: tại mỗi nút Trắng, script thử mọi nước hợp lệ và đưa vào cây mọi nước vẫn ép chiếu hết trong số lượt còn lại trước mọi phòng thủ. Vì vậy runtime không có trạng thái “chưa kết luận”, và trạng thái `Recovering` của prompt gốc không cần dùng. Nếu người chơi tới hết số nước mà không chiếu hết, đó là lỗi dữ liệu; test dữ liệu (mục 8) phải chặn trước khi đưa đề vào game, runtime không tuyên bố thắng trong trường hợp này.

Boss luôn đi nước phòng thủ dai nhất theo thứ tự Stockfish, không cố ý đi nước yếu.

### 2.3. Công cụ soạn đề

`tools/chess/verify_puzzles.py` cùng `tools/chess/requirements.txt` (`chess`). Stockfish bản Windows chính thức đặt tại `tools/chess/stockfish/` và được gitignore; README trong `tools/chess/` hướng dẫn tải.

Script nhận danh sách ID Lichess:

1. Lấy `FEN` và `Moves` của đề từ API Lichess.
2. Áp dụng nước đầu trong `Moves` để có `startFen`; xác nhận Trắng đi và nước thứ hai của `Moves` hợp lệ.
3. Dựng cây vét cạn bằng `python-chess` theo định nghĩa ở 2.1 và đối chiếu chéo với lời giải Lichess.
4. Dùng Stockfish (độ sâu cố định, ghi trong JSON đầu ra) để sắp thứ tự `replies` từ phòng thủ dai nhất.
5. Ghi JSON kèm `sourcePuzzleId`; dừng với lỗi nếu bất kỳ kiểm tra nào thất bại.

Bản demo cần ít nhất một đề chiếu hết 2 nước Thường chạy hoàn chỉnh. Không đưa FEN tự bịa vào game.

## 3. Màn chơi `MG_ChessFinal`

### 3.1. Dựng scene

`Assets/Editor/ChessFinalSceneConfigurator.cs`, menu `KMA/Chess Final/Build Scene`, theo mẫu `FrogJumpSceneConfigurator`: dựng scene, gọi `MinigameUIAssembler` để có HUD/pause/`ResultPanel` dùng chung, thêm vào Build Settings. `CharacterArt` được mở rộng để nhập `BossPE` (PPU 200, pivot Bottom Center, Sprite Single); không ghi đè `.meta` hiện có.

12 hình quân cờ được render bằng `tools/render-chess-pieces.js` (cùng cách làm với `render-journey-emoji.js`): phẳng, viền đậm, đọc rõ trên cả hai màu ô, không dùng emoji.

### 3.2. Bố cục (tham chiếu 1920 × 1080, Canvas Scaler + anchor)

| Trái ~18% | Giữa ~64% | Phải ~18% |
|---|---|---|
| “Tân Thủ”, hình `MaleAdventurer`, “Sai: 0/2” | “Bài kiểm tra cuối”; “Chiếu hết trong 2 nước” và “01:30”; bàn cờ vuông ~740 px, tọa độ a–h, 1–8 ngoài bàn; “Lượt của bạn”/“Lượt giảng viên” và nút “Gợi ý” | “Cô Thể Chất”, hình BossPE, bong bóng thoại |

- Ô kem/xanh nhạt; ô chọn viền vàng; ô hợp lệ chấm nhỏ; ô bắt quân vòng tròn; nước vừa đi tô nhẹ; vua bị chiếu nhuốm đỏ vừa đủ.
- Trắng luôn ở dưới. Avatar không che bàn cờ và chỉ số.
- Phong cấp mở popup Hậu/Xe/Tượng/Mã; không tự phong Hậu.
- Nút dùng `BrutalButton`/`UITheme`. “Chơi lại” chỉ có trong menu tạm dừng và bảng kết quả.
- Không hiển thị FEN, UCI, tên engine hoặc thông tin kỹ thuật.

### 3.3. Thành phần Unity

| Thành phần | Trách nhiệm |
|---|---|
| `ChessFinalController : MinigameBase, IChallengeController` | Nạp đề, nối state machine với vòng đời Unity, phát `ChallengeCompleted` đúng một lần kèm metric |
| `ChessBoardView` | 64 ô, quân, highlight, input chạm/click, animation di chuyển; chỉ phản ánh model |
| `BossReactionPresenter` | Sự kiện → pose và câu thoại |
| `HintPresenter` | Mở gợi ý tuần tự mức 1 → 2 → 3 cho nút hiện tại; ghi `hintUsed` |

Màn cờ tự mở cổng bắt đầu bằng nút “Bắt đầu” (không dùng đếm ngược 3-2-1 dùng chung).

### 3.4. State machine

| Trạng thái | Đồng hồ | Input bàn cờ |
|---|---|---|
| `Intro` (đọc đề, chờ “Bắt đầu”) | Dừng | Khóa |
| `PlayerTurn` (gồm chọn quân, popup phong cấp, đọc gợi ý) | Chạy | Mở |
| `Validating` | Dừng | Khóa |
| `BossTurn` (nghĩ ngắn + animation ~0,6 s) | Dừng | Khóa |
| `Paused` | Dừng | Khóa |
| `Completed` / `Failed` | Dừng | Khóa |

Chuyển trạng thái:

- `Intro → PlayerTurn` khi bấm “Bắt đầu”.
- `PlayerTurn → Validating` khi gửi nước. Lệnh gửi ở mọi trạng thái khác bị bỏ qua.
- `Validating`: `Illegal` → `PlayerTurn` (báo ngắn); `Solved` → `Completed`; `Accepted` → `BossTurn`; `Wrong` → khôi phục snapshot, tăng lỗi; nếu lỗi ≤ 2 → `PlayerTurn`, nếu lỗi = 3 → `Failed("Sai quá 2 lần")`.
- `BossTurn → PlayerTurn` khi boss đi xong.
- `PlayerTurn → Failed("Hết giờ")` khi đồng hồ chạm 90 s.
- `PlayerTurn ⇄ Paused` qua menu tạm dừng.
- Chơi lại (từ `Failed` hoặc menu tạm dừng) → `Intro` với cùng đề; reset vị trí, nút cây, lỗi, đồng hồ, gợi ý, pose.

`Completed`/`Failed` chỉ phát một lần. Không có trạng thái Penalty; màn cờ không tham chiếu controller, sprite hoặc scene bật cóc.

### 3.5. Phản ứng nhân vật

| Sự kiện | Cô Thể Chất | Thoại mẫu |
|---|---|---|
| Intro | `idleBoss` | “Chiếu hết trong hai nước. Em có một phút rưỡi.” |
| Lượt người chơi | `strictLook` | — |
| Boss nghĩ/đi quân xe | `chessThink` / `chessMove` | — |
| Boss đi quân khác | `chessThink` | — |
| Sai còn được sửa | `whistle0 → whistle1 → strictLook` | “Chưa đúng. Em xem lại.” / “Còn một lần sửa.” |
| Thắng | `cheer0` | “Được, em qua.” |
| Thua | `strictLook` | “Hết giờ.” / “Sai quá số lần rồi.” |

Tân Thủ dùng `idle`, `hurt` khi sai, `cheer0/cheer1` khi thắng. Thoại không chặn input và không dừng thêm đồng hồ.

## 4. Tiến trình, bản lưu và điều hướng

### 4.1. Dữ liệu thử thách

- `SubjectId.Chess = 8` (số mới, không tái sử dụng số cũ).
- `ChallengeKind.Final`.
- `ChallengeCatalog.ExpectedIds` thêm `chess_final` ở vị trí 10. Validation: 9 phần tử đầu giữ quy tắc hiện có; phần tử cuối phải là `Chess`/`Final`.
- Asset `chess_final` (Normal, `timeLimit` 90, `targetCount` 2; số lần sửa sai lấy từ `maxRecoverableMistakes` của đề) do `StudentJourneyContentBuilder` tạo; mục tiêu “Chiếu hết trong 2 nước”.
- Thứ tự học tường minh `[Sprint, Volleyball, Football, Chess]`. `IsSubjectUnlocked(Chess)` khi `soccer_exam` hoàn thành.

### 4.2. Miễn trừ cho thử thách Final trong `JourneyProgress`

- `Apply`: Final trượt không tăng `failCounts`, không trừ `attemptsRemaining`, không tạo `pendingFrogJump`.
- `TryBegin(Journey)`: Final được bắt đầu kể cả khi lượt thi bằng 0.
- `IsPenalized(chess_final)` luôn false, kể cả với save bị sửa tay.
- `CourseComplete` giữ định nghĩa “mọi thử thách trong catalog đã hoàn thành”, nên tự bao gồm bài cờ.
- Thêm cờ lưu `celebrationSeen`.

### 4.3. Điểm và metric

Kết quả dùng thang 0–10 chung:

```text
Điểm = mục tiêu 6 (chiếu hết) + chính xác (2 − số lần sai) + hiệu quả (giây còn lại / 90) + mastery (1 nếu không dùng gợi ý)
```

Thua cho điểm 0 ở phần mục tiêu và không cập nhật thành tích tốt nhất. Kết quả tốt nhất lưu vào `SubjectRecord(Chess)`. `ChallengeMetrics` lưu thêm giây đã dùng, số lần sai và `hintUsed` của lần thắng tốt nhất.

### 4.4. Router và kết quả

- `SceneFor(Chess)` → `MG_ChessFinal`.
- `SessionRoute.Celebration` → scene `Celebration`.
- Kết quả bài cờ hiển thị bằng `ResultPanel` dùng chung:
  - Thắng: “Tiếp tục”. Nếu outcome có `CourseComplete` và `celebrationSeen` còn false → Celebration; còn lại → Map.
  - Thua: lý do cụ thể, “Chơi lại” (hành động `Retry`, attempt mới cùng đề) và “Về bản đồ”. Không bao giờ phát hành động `FrogJump`.
- Lưu hoàn tất trước khi chuyển cảnh, theo `journeyPersist` hiện có. Lỗi lưu dùng phản hồi thử lưu lại hiện có.

### 4.5. Bản lưu v8 → v9

- `SaveData.CurrentVersion` từ 8 lên 9.
- Save cũ đã hoàn thành cả 9 thử thách: checkpoint chuyển sang `chess_final`; học phần chưa hoàn tất cho đến khi thắng bài cờ. Điểm/hạng tốt nhất giữ nguyên.
- Save cũ chưa hoàn thành: không đổi.
- Save chỉ có cài đặt, save hỏng: giữ cơ chế hiện có.
- `celebrationSeen` mặc định false.

### 4.6. Map

- `MapJourneyPathLayout`/`MapStopBuilder` thêm trạm 4 “Bài kiểm tra cuối” với icon Material Symbols phù hợp; khóa có ghi chú “Đạt Bóng đá để mở”. Danh sách bài của trạm có một mục.
- Khi `CourseComplete`, vị trí `JourneyCourseSummary` hiển thị nút “Xem lại lễ mừng” mở Celebration ở chế độ xem lại.
- Bài cờ đã đạt cho phép chơi lại như các bài thi đã đạt; không đổi trạng thái hoàn tất.

### 4.7. Hình nhân vật

Giữ nguyên tên vai. `StudentJourneyContentBuilder` đổi chân dung Cô Thể Chất từ `FemaleAdventurer` sang `BossPE`; các vai khác giữ nguyên.

## 5. Scene ăn mừng `Celebration`

### 5.1. Dữ liệu

`CelebrationSummary` do `GameSession` dựng khi `CourseComplete`:

- Tên các chặng theo thứ tự catalog.
- Điểm/hạng tốt nhất của Chạy nước rút, Bóng chuyền, Bóng đá từ `SubjectRecord`.
- Bài cờ: giây đã dùng, số lần sai, đã dùng gợi ý.
- Số đợt thi bổ sung.

Scene chỉ đọc. Thao tác ghi duy nhất là đặt `celebrationSeen = true` và lưu ngay khi scene mở, nên bỏ qua hay thoát giữa chừng đều không làm scene tự mở lại. Mở trực tiếp trong Editor khi không có session dùng `CelebrationSummary.Sample()` và không ghi save. Session chưa `CourseComplete` mà vào route Celebration bị router từ chối.

### 5.2. Sân khấu

Dựng bằng `CelebrationSceneConfigurator`. Nền `Environments/Sprint/Campus.png` và `Sky.png`, camera cố định. Tân Thủ (`MaleAdventurer`) đứng giữa, Mai Toang (`FemalePerson`) bên cạnh, Cô Thể Chất (BossPE) lệch phải. Confetti `ParticleSystem` 2D dùng màu `UITheme`. Không có bàn cờ, timer hay input di chuyển.

### 5.3. Timeline (`CelebrationTimeline`, C# thuần)

| Giây | Diễn biến |
|---|---|
| 0–2 | Fade vào; Tân Thủ `idle` |
| 2–5 | Tân Thủ `cheer0 ↔ cheer1`; Mai Toang cheer; confetti |
| 5–8 | Cô Thể Chất `idleBoss → taunt → cheer0`; “Được, em qua.” |
| 8–11 | “Đã qua thể chất!” và bảng tổng kết trượt vào |

Nút “Bỏ qua” có từ giây 0 và gọi cùng hàm `ShowSummary()` mà timeline gọi khi chạy hết. BossPE không có pose gật đầu; `cheer0` là pose tạm cho thái độ bớt nghiêm, cần asset bổ sung nếu muốn gật đầu thật.

### 5.4. Bảng tổng kết và điều hướng

- Bốn dòng: Chạy nước rút, Bóng chuyền, Bóng đá (dấu hoàn thành, điểm, hạng); Bài kiểm tra cuối (thời gian đã dùng, “Sai: x”, “Có/Không dùng gợi ý”). Không hiển thị thông số game không lưu.
- Nút chính “Về menu” → Menu. Nút phụ “Chơi lại” → Map, nơi mọi trạm mở; không xóa tiến trình.

## 6. Ngoài phạm vi

- Bật cóc: giữ nguyên module, không sửa và không gọi từ màn cờ.
- Hình phạt bổ sung (thu dọn dụng cụ, đếm nhịp, đứng tấn) ở mục 19 của prompt gốc chỉ là đề xuất; không tạo code/scene.
- Không có AI ngôn ngữ, backend, đấu cả ván, multiplayer, leaderboard, mua gợi ý.
- Không dùng độ khó Dễ/Khó trong hành trình.

## 7. Rủi ro đã biết

- Sửa `ChallengeCatalog` từ 9 lên 10 bài ảnh hưởng khoảng 14 file có giả định ba môn; test hiện có về Journey/FrogJump/Volleyball phải chạy lại và xanh.
- Thay đổi chưa commit trong working tree (Volleyball, FrogJump, Map…) thuộc người dùng; triển khai không được ghi đè chúng.
- Pose BossPE chưa thử trong scene; cần xem tỷ lệ, pivot và nhịp đổi khung qua ảnh chụp.

## 8. Kiểm thử và nghiệm thu

### 8.1. EditMode

| Nhóm | Ca kiểm thử |
|---|---|
| Luật cờ | Perft vị trí đầu độ sâu 1–4 (20 / 400 / 8 902 / 197 281); Kiwipete độ sâu 1–3 (48 / 2 039 / 97 862); nhập thành bị chặn khi đi qua ô bị chiếu; bắt tốt qua đường bị ghim ngang; phong cấp đủ 4 quân; chiếu hết; hòa bí; FEN đọc rồi ghi lại không đổi |
| Dữ liệu đề | Với mỗi JSON: `startFen` hợp lệ, Trắng đi; mọi nước trong cây hợp lệ; mọi nước hợp lệ của Đen có trong `replies`; mọi lá là chiếu hết trong `maxPlayerMoves` |
| Chấm | Chiếu hết ngoài cây được công nhận; `Wrong` không đổi vị trí; `Illegal` không tính lỗi; boss đáp khóa đầu tiên |
| State machine | Gửi nước ngoài `PlayerTurn` bị bỏ qua; sai 1 và 2 hoàn tác rồi mở lại, sai 3 ra `Failed`; kết quả phát một lần; hết giờ chỉ trong `PlayerTurn`; đồng hồ không chạy ở Intro/BossTurn/Paused/kết quả; chơi lại reset đủ |
| Tiến trình | `chess_final` khóa đến khi đạt `soccer_exam`; trượt không trừ lượt và không tạo bật cóc, kể cả trượt nhiều lần; chơi được khi lượt thi bằng 0; `CourseComplete` chỉ đúng sau khi thắng; kết quả lặp không ghi hai lần; migration v8→v9 cho save đủ 9 bài, đang dở và chỉ có cài đặt; `celebrationSeen` ngăn tự mở lại; route Celebration bị từ chối khi chưa hoàn tất |
| Ăn mừng | Bỏ qua ở 0 s, ở 10,9 s và chạy hết hội tụ cùng trạng thái tổng kết; tổng kết dựng từ session; chế độ mẫu không ghi save |

Toàn bộ test EditMode hiện có phải xanh sau thay đổi.

### 8.2. Hình ảnh và build

- Chụp bằng skill `testing-unity-ui-with-screenshots` / `PlayModeScreenshotQaStates`: Intro, chọn quân, phong cấp, sai (còi), lượt boss, thắng, thua hết giờ, thua sai 3 lần, trạm 4 trên Map khi khóa và khi mở, timeline ăn mừng, bảng tổng kết. Lưu `docs/qa/images/chess-final-*.png`, báo cáo `docs/qa/chess-final-celebration.md`. Xem từng ảnh: chữ tiếng Việt, bàn cờ không bị che, tọa độ đọc được.
- Thử build APK bằng `tools/build-apk`; nếu không chạy được trên emulator/thiết bị, ghi rõ lý do. Báo cáo tách riêng kết quả Editor, tự động, emulator và thiết bị.

### 8.3. Tài liệu bàn giao

- Hướng dẫn thêm đề: chạy `verify_puzzles.py` với ID Lichess mới, viết `ideaHint`, chạy test dữ liệu.
- Hướng dẫn thay asset boss/quân cờ mà không sửa logic.
- Hướng dẫn thử điều kiện mở scene ăn mừng (save mẫu thiếu một môn, chưa thắng cờ, vừa thắng, xem lại).
- Thông tin đề demo: nguồn, mục tiêu, cách xác minh.
