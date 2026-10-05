# Bài kiểm tra cuối (cờ vua) và scene ăn mừng - QA evidence

Tài liệu bàn giao cho tính năng "Bài kiểm tra cuối" (`MG_ChessFinal`) và scene `Celebration`. Spec:
`docs/superpowers/specs/2026-10-06-chess-final-celebration-design.md`; kế hoạch:
`docs/superpowers/plans/2026-10-06-chess-final-celebration.md`.

Trạng thái xác minh: bộ test tự động xanh và có ảnh chụp 1920x1080 cho mọi trạng thái chính. **Chưa có ai chơi tay
toàn bộ vòng lặp trong Editor, và chưa kiểm tra trên thiết bị hay giả lập** (xem mục 5).

## 1. Hành vi cuối cùng và giả định

- Trạm thứ 4 "Bài kiểm tra cuối" nằm sau Bóng đá trên bản đồ. Cô Thể Chất đặt một thế cờ; Tân Thủ (Trắng) phải chiếu
  hết trong tối đa 2 nước, có 90 giây suy nghĩ và được sửa sai 2 lần (lần sai thứ 3 kết thúc lượt chơi).
- Đồng hồ chỉ chạy ở lượt người chơi. Lúc giảng viên đi quân, lúc hiện bảng chọn phong cấp hoặc khi tạm dừng đều không trừ giờ.
- Chấm theo thứ tự: nước đi không hợp lệ chỉ báo "không hợp lệ" (không tính lỗi); nước làm đối phương bị chiếu hết thì
  thắng (kể cả khi nằm ngoài cây đáp án); nước nằm trong cây đáp án thì được nhận và giảng viên đi nước đáp đầu tiên;
  còn lại là sai (tính một lần sửa).
- Thử thách `Final` không trừ lượt thi, không tạo bật cóc, không đụng `failCounts` và có thể bắt đầu khi đang 0 lượt.
  Trượt thì hiện bảng thất bại có nút "CHƠI LẠI", chơi lại tự do. Bật cóc giữ nguyên, không bao giờ được gọi từ màn cờ.
- Thắng lần đầu hoàn tất học phần và mở `Celebration`. Các lần sau không tự mở; trên Map có nút "Xem lại lễ mừng".
- Bản lưu là v9 (`chessBest`, `celebrationSeen`). Bản lưu từ v7 trở lên giữ nguyên hành trình; chỉ dưới v7 mới qua `MigrateLegacy`.
- Giả định mặc định (spec mục 1): không có AI ngôn ngữ, lời thoại cố định tiếng Việt; hành trình chỉ dùng độ khó Thường;
  scene ăn mừng không cấp thưởng nào, chỉ ghi cờ `celebrationSeen`.
- Tên vai không đổi: "Tân Thủ" (`MaleAdventurer`) và "Cô Thể Chất" (`BossPE`).

## 2. File đã tạo hoặc đổi

| File | Mục đích |
|---|---|
| `Gameplay/Chess/Core/*` (`ChessTypes`, `ChessMove`, `ChessPosition`, `MoveGenerator`, `PuzzleDefinition`, `PuzzleVerifier`, `PuzzleGrader`, `PuzzleHints`, `ThinkClock`, `ChessFinalStateMachine`) | Lõi cờ C# thuần (`noEngineReferences`): FEN, nước đi hợp lệ, chiếu hết, chấm, gợi ý, đồng hồ, máy trạng thái |
| `Gameplay/Chess/*` (`ChessPuzzleLibrary`, `ChessBoardView`, `ChessCastView`, `ChessFinalHud`, `PromotionPicker`, `ChessFinalController`) | Phần Unity của màn cờ: nạp đề, bàn cờ, nhân vật, HUD, chọn phong cấp, controller kế thừa `MinigameBase` |
| `Gameplay/Celebration/*` (`CelebrationTimeline`, `UiConfetti`, `CelebrationSceneController`) | Scene ăn mừng 11 giây, nút Bỏ qua, bảng tổng kết |
| `Progression/Journey/CelebrationSummary.cs` | Tổng kết chỉ đọc dựng từ `GameSession` (có `Sample()` khi mở trực tiếp) |
| `Core/SceneRouter.Celebration.cs`, `SceneRouter.cs`, `SceneRouter.Journey.cs`, `S5RouteBootstrap.cs` | Route `Celebration`, định tuyến thử thách `Final` |
| `SubjectId.cs`, `ChallengeDefinition.cs`, `ChallengeCatalog.cs`, `ChallengeContracts.cs`, `ChallengeAttempt.cs`, `JourneyProgress.cs`, `JourneyStateData.cs`, `JourneySaveMigration.cs`, `GameSession.cs`, `SaveData.cs`, `SaveSystem.cs`, `GameManager.cs`, `JourneyControllerAdapter.cs` | Môn thứ tư (`SubjectId.Chess = 8`, `ChallengeKind.Final`), miễn trừ lượt/bật cóc, kỷ lục cờ, save v9 |
| `ResultPanel.cs`, `JourneyDialoguePresenter.cs` | Bảng kết quả chế độ Final (CHƠI LẠI, chi tiết thời gian/sai/gợi ý), lời thoại kết thúc |
| `MapScreen.cs`, `MapPresentationBuilder.cs`, `MapJourneyPathLayout.cs`, `MapStopBuilder.cs`, `JourneyLessonList.cs`, `JourneyLessonPresentation.cs`, `JourneyCourseSummary.cs`, `UITheme.cs`, `UITheme.asset` | Trạm thứ 4, thẻ bài cuối, nút "Xem lại lễ mừng", màu chủ đề cờ |
| `Assets/Editor/ChessFinalSceneConfigurator.cs`, `CelebrationSceneConfigurator.cs`, `ChessArtImporter.cs`, `CharacterArt.cs` | Dựng `MG_ChessFinal.unity`, `Celebration.unity`, nhập hình BossPE |
| `Assets/Editor/PlayModeScreenshotQaStates.cs` | Các trạng thái QA: `chess-select`, `chess-wrong`, `chess-boss`, `chess-hint`, `chess-promotion`, `chess-win`, `chess-timeout`, `celebration-*`, `map-chess-*`, `map-complete` |
| `Assets/Editor/ShellSceneAuthoring.cs` | Validator: kỳ vọng số nút môn trên Map đổi 3 thành 4 |
| `tools/chess/*`, `tools/render-chess-pieces.js` | Công cụ soạn đề (Lichess + Stockfish + python-chess) và render 12 quân cờ |
| `Assets/_Project/Resources/Chess/Puzzles/*.json` | Đề đã xác minh |
| `README.md`, `PLAN.md` | Bảng scene, vòng tiến trình, trạm thứ 4, save v9 |

## 3. Cách chạy

**Trực tiếp màn cờ:** mở `Assets/_Project/Scenes/MG_ChessFinal.unity` và nhấn Play. Không có `GameManager` nên bảng kết
quả không tự hiện (router mới hiện bảng); dùng để xem bố cục và chơi thử nước đi. Scene ăn mừng mở trực tiếp
(`Celebration.unity`) dùng `CelebrationSummary.Sample()` ("Dữ liệu mẫu") và không ghi save.

**Qua hành trình:** `Bootstrap` -> Menu -> Game mới, hoàn tất 9 thử thách của ba môn, trạm 4 trên Map mở ra và thẻ "CUỐI"
bắt đầu bài cờ. Muốn bỏ qua phần chơi ba môn trong Editor, dùng save gỡ lỗi dựng bằng `JourneyGameplayDriver.CompleteThrough(session, "soccer_exam")`
như các test (`StudentJourneyFlowTests`).

Menu Editor:

| Menu | Việc làm |
|---|---|
| `KMA/Chess Final/Import Art` | Nhập 30 PNG BossPE và 12 quân cờ, đặt import settings |
| `KMA/Chess Final/Build Scene` | Dựng lại `MG_ChessFinal.unity` |
| `KMA/Celebration/Build Scene` | Dựng lại `Celebration.unity` |
| `KMA/Journey/Build All Journey Content` (và các mục con `Build Challenge Assets`, `Build Sprint Balance`, `Build Emoji Sprites`, `Build Dialogue Library`) | Dựng lại dữ liệu hành trình, gồm thử thách `chess_final` |
| `KMA/Presentation/Author Map Lesson Journey` | Bake lại Map (xem mục 5, giới hạn về `Validate`) |

## 4. Đề mẫu

Đề trong hành trình là `m2_001` (độ khó Thường), kèm `m1_001` (Dễ) chỉ để kiểm tra, không dùng trong hành trình.

| Mục | `m2_001` | `m1_001` |
|---|---|---|
| Lichess | `009BH` (rating 1513) | `00T85` (rating 1039) |
| FEN bắt đầu | `3r3k/6p1/4Q3/4B3/1p3P2/4PKP1/3q4/8 w - - 18 52` | `8/8/8/8/8/4K3/5Q2/1qk5 w - - 6 54` |
| Nước chính | Qh6+ Kg8 Qxg7# | Qd2# |
| Số nước / giờ / sửa sai | 2 / 90 s / 2 | 1 / 90 s / 3 |
| `verifiedBy` | `python-chess 1.11.2 + Stockfish 19 depth 18` | `python-chess 1.11.2 + Stockfish 19 depth 18` |

Cách xác minh: `tools/chess/verify_puzzles.py` duyệt đầy đủ cây nước đi của đối phương bằng python-chess, dùng Stockfish
để sắp thứ tự đáp án và ghi JSON; sau đó `PuzzleVerifier` (C#) kiểm tra lại từng file trong `ShippedPuzzleTests` (độ phủ
cây, mọi nhánh kết thúc bằng chiếu hết, không thiếu đáp án thay thế). Hai bên khớp nhau. Đề `m2_001` chỉ có một cách đỡ
duy nhất ở nước 1 (Kg8), nên cây chỉ có 2 nút; các nước Trắng khác đều bị chấm sai.

## 5. Kết quả kiểm thử, ảnh chụp, thiết bị và giới hạn

### Test tự động (batch mode, Editor đóng, assembly `KMA.Tests`)

| Bộ | Tổng | Đạt | Lỗi | Bỏ qua |
|---|---|---|---|---|
| EditMode (`final-edit`) | 799 | 796 | 0 | 3 (ignore có sẵn) |
| PlayMode (`final-play`) | 284 | 284 | 0 | 0 |
| Baseline ban đầu EditMode | 720 | 717 | 0 | 3 |
| Baseline ban đầu PlayMode | 271 | 271 | 0 | 0 |

Test mới đều đạt, không test cũ nào mới hỏng. Vòng đầy đủ Bootstrap -> ba môn -> `MG_ChessFinal` -> `Celebration`
được phủ bởi `StudentJourneyFlowTests.BootstrapToSummaryUsesTheRealControllersAndPersistsCompletion` (controller thật,
router thật, callback lưu thật).

### Ảnh chụp (1920x1080, `tools/qa-screenshot.sh`, `docs/qa/images/`)

| Ảnh | Quan sát |
|---|---|
| `chess-final-1-intro.png`, `chess-final-2-select.png` | Thẻ mở đầu và chọn quân (Task 10) |
| `chess-final-wrong.png` | Một nước sai: `Sai: 1/2`, lời thoại "Còn một lần sửa.", Tân Thủ đau, Cô Thể Chất thổi còi; `Lượt của bạn` |
| `chess-final-boss.png` | Sau nước Qh6+: nhãn `Lượt giảng viên`, ô vua đen tô đỏ, ô Hậu tô xanh, Cô Thể Chất tư thế suy nghĩ, nút Gợi ý bị mờ. Ảnh bắt đúng lúc suy nghĩ, chưa bắt được khung trượt quân |
| `chess-final-hint.png` | Gợi ý mức 2: "Xem quân Hậu ở e6."; đồng hồ 01:28; Cô Thể Chất đưa tay che mắt |
| `chess-final-promotion.png` | Bảng Hậu / Xe / Tượng / Mã nằm giữa bàn cờ, nút vàng, chữ rõ dấu (mở trực tiếp bằng `promotion.Open` vì đề mẫu không có phong cấp) |
| `chess-final-win.png` | Bảng "CHIẾU HẾT!", XẾP HẠNG S, "Thời gian 00:00 · Sai: 0/2", nút TIẾP TỤC; hai nhân vật reo |
| `chess-final-timeout.png` | Đồng hồ 00:00, nhãn `Hết giờ`, bảng "CHƯA ĐẠT", chi tiết "Hết giờ", hai nút CHƠI LẠI và TIẾP TỤC không chồng nhau |
| `chess-final-map-locked.png`, `chess-final-map-open.png`, `chess-final-map-complete.png` | Bản đồ 4 trạm, trạm cờ khóa/mở/hoàn thành, nút "Xem lại lễ mừng" (Task 12) |
| `celebration-1-cheer.png`, `celebration-2-teacher.png`, `celebration-3-summary.png` | Scene ăn mừng ở giây 3, giây 6 và bảng tổng kết sau khi Bỏ qua (Task 11) |

Ghi chú về cách chụp: scene `MG_ChessFinal` chạy độc lập không có router, nên ở `chess-win` và `chess-timeout` trạng thái
QA tự mở bảng kết quả bằng `ResultPanel.ShowChallenge` ở chế độ Final với kết quả thật của controller (điểm và chi tiết do
`BuildResult`/`BuildMetrics` tính). `chess-final-win.png` có thời gian 00:00 vì trạng thái QA đi nước ngay lập tức. Mọi
ảnh đã được xem lại: chữ tiếng Việt đúng dấu, nhãn `Sai: x/2`, `Lượt giảng viên`, `Hết giờ` có mặt, không chồng chữ trong bố cục chơi.

Lỗi thị giác thấy khi xem, thuộc phần dùng chung (không sửa trong task này):

- Dưới lớp mờ của bảng kết quả vẫn thấy dòng "KẾT QUẢ" của `PhaseOverlay` chồng lên thanh "Chiếu hết trong 2 nước". Trước khi bảng hiện (0,9 s đầu), `PhaseOverlay.prefab` còn hiện chữ "RESOLVE" tiếng Anh ở giữa bàn cờ.
- Nhãn "SCORE" tiếng Anh trên `ResultPanel.prefab` (nhãn dùng chung, đã có trước tính năng này).
- Cả hai prefab này đang ở trạng thái đã sửa chưa commit của người dùng; cần người dùng kiểm tra.

### Validator thủ công `ShellSceneAuthoring.Validate`

- Bootstrap và Menu đạt.
- Map thất bại ở bước kiểm tra "số nút môn" vì kỳ vọng cũ là 3. Đã đổi kỳ vọng thành 4 (một dòng, đúng hành vi mới).
- Sau đó Map vẫn thất bại với "Map builder duplicated UI after reopen": `Map.unity` đã bake sẵn 3 trạm và trạm cờ được
  chèn lúc chạy, nên số đối tượng tăng sau `Build`. Đây là hệ quả đã biết của việc không bake lại Map trong Task 12.
- Đã thử `KMA/Presentation/Author Map Lesson Journey` rồi `Validate`: cả bốn scene đạt (Map 489 đối tượng). Vì lần
  bake lại làm đổi cả `Map.unity` (khoảng 33.000 dòng diff) nên đã hoàn tác và **không commit**. Việc còn lại cho người
  dùng: chạy menu bake lại Map khi thuận tiện, xem diff, rồi commit.

### Build Android

`tools/build-apk.ps1` (arm64) thành công: `Builds/Android/kma-arm64.apk`, 60.304.292 byte, SHA-256
`5c263114597d4dc9de3eb53ddcc669f0ded0604ba50b518daabf0a7825689d2d` (không commit file build). **Chưa cài lên thiết bị hay
giả lập, chưa chạy thử**; không có kiểm tra trên thiết bị nào.

### Chưa xác minh

- Chơi tay toàn bộ vòng lặp trong Editor (Menu -> Game mới -> Map -> trạm 4, thua một lần, thử lại, thắng, xem ăn mừng,
  bỏ qua ở lần thứ hai, về Menu). Chỉ có `StudentJourneyFlowTests` phủ vòng này bằng controller thật.
- Chạy APK trên thiết bị/giả lập, cảm giác chạm, âm thanh còi/thắng trên máy thật.
- Khung hình trượt quân của giảng viên giữa chừng (ảnh `boss` bắt lúc suy nghĩ).

### Giới hạn đã biết

- Chưa sinh đề Khó; hành trình chỉ dùng độ khó Thường. Đề Thường hiện chỉ có một đề (`m2_001`).
- BossPE không có pose gật đầu; `cheer0` đóng vai thái độ bớt nghiêm.
- Mức gợi ý được giữ lại sau khi giảng viên đáp (mức 3 sẽ tự lộ nước kế tiếp).
- Thẻ bài cuối trên Map dùng biểu tượng sách của mục "Học", chưa có biểu tượng riêng cho bài thi.
- Chữ mở đầu ("2 nước", "90 giây", "2 lần sửa") được ghi cứng trong thẻ giới thiệu.
- Confetti vẽ đè lên lời thoại và vẫn chạy phía sau bảng tổng kết.

## 6. Thêm đề, đổi hình

- Thêm đề: xem `tools/chess/README.md` (`fetch_candidates.py` -> thêm vào `puzzle_sources.json` -> `verify_puzzles.py`), rồi
  chạy EditMode `KMA.Tests.Gameplay.Chess`. Không sửa tay file JSON. Hành trình dùng đề Thường đầu tiên theo tên file.
- Đổi hình boss hoặc quân cờ: render lại (`node tools/render-chess-pieces.js`) hoặc thay các PNG, rồi chạy
  `KMA/Chess Final/Import Art`. Nếu đổi bố cục thì chạy thêm `KMA/Chess Final/Build Scene`.

## 7. Kiểm tra cổng ăn mừng

`SceneRouter.RouteToCelebration` chỉ cho phép khi `Journey.CourseComplete` và không có môn đang chơi. Cần thử:

| Tình huống | Kỳ vọng |
|---|---|
| Thiếu một môn (ví dụ chưa qua Bóng đá) | Trạm cờ khóa, không thể vào bài cờ; route `Celebration` bị từ chối |
| Đã đủ ba môn nhưng chưa thắng cờ | Trạm cờ mở, không có nút "Xem lại lễ mừng", route `Celebration` bị từ chối |
| Thắng cờ lần đầu | Bảng kết quả -> TIẾP TỤC -> `Celebration`; `celebrationSeen` được lưu ngay khi scene mở |
| Thắng cờ lần sau (đã `celebrationSeen`) | TIẾP TỤC về Map, không tự mở lại |
| Replay từ Map | Nút "Xem lại lễ mừng" mở `Celebration`, không xóa tiến trình, không có tác dụng phụ |
| Bỏ qua ở giây 0 | Bảng tổng kết hiện ngay, đủ 4 dòng |
| Bỏ qua gần cuối (khoảng giây 10) | Cùng kết quả, không hiện hai lần |
| "Về menu" | Về Menu; "Chơi lại" -> Map với mọi trạm mở |

Các test tự động liên quan: `ChessFinalRoutingTests`, `ChessFinalSaveTests`, `CelebrationTimelineTests`,
`CelebrationSceneTests`, `JourneyMapChessTests`, `StudentJourneyFlowTests`. Các dòng trong bảng trên chưa được ai chơi tay.

## 8. Hình phạt bổ sung (chỉ là đề xuất)

Các hình phạt ở spec mục 6 (thu dọn dụng cụ, đếm nhịp, đứng tấn) chỉ là đề xuất; **không có code hay scene nào được tạo
cho chúng**. Bật cóc giữ nguyên, không được sửa và không bao giờ được gọi từ màn cờ (không code cờ nào tham chiếu kiểu,
sprite hay scene của FrogJump).
