# Đồng bộ khung cảnh trường, button và sửa lỗi UI toàn game

**Ngày:** 2026-10-06

**Trạng thái:** Thiết kế đã được người dùng duyệt từng phần trong hội thoại; đặc tả viết chờ duyệt, sau đó lập kế hoạch triển khai.

**Nguồn yêu cầu:** “Điều tra end to end game và đồng bộ lại khung cảnh của trường trong tất cả minigame cũng như style button và các lỗi UI hiện có như: nhân vật bị shadowing, thừa element, khoảng cách không phù hợp”.

## 1. Mục tiêu và quyết định đã thống nhất

Mỗi minigame phải đọc ra ngay là đang ở trong trường KMA, với cùng phong cách art; button theo hai họ thống nhất; các lỗi UI tìm thấy trong đợt điều tra được sửa và có ảnh trước/sau làm bằng chứng.

| Câu hỏi | Quyết định |
|---|---|
| Mức đồng bộ khung cảnh | **Một nền trường dùng chung** (trời + skyline trường) phía sau mọi minigame; mỗi môn giữ mặt sân riêng |
| Volleyball | **Bỏ bối cảnh bãi biển**, chuyển vào sân bóng chuyền ngoài trời trong trường |
| Kiến trúc nền | **Hướng A:** component `CampusBackdrop` dùng chung + art sinh bằng script |
| Nguồn art | Dùng asset sẵn có; **được tự tạo art mới** theo style chung (pipeline SVG → PNG bằng `@resvg/resvg-js` như `tools/render-football-goalview-art.js`) |
| Button | **Hai họ:** “Xiên” chỉ ở Menu và GameOver; “Kit” bo tròn ở mọi nơi khác |
| Shadowing | Bản sao cyan phía sau player trong scene Sprint (`SprintPlayerIdentityOutline`) |
| Punishment | **Xóa hẳn** scene, code và input map; save cũ vẫn đọc được |

### Giả định (chưa được người dùng nêu, dùng làm mặc định)

- Không đổi luật chơi, độ khó, toạ độ gameplay (`CourtSpace`, `SprintTrackLayout` về logic, hình học khung thành Football).
- Bootstrap/splash giữ nguyên nội dung.
- Tỉ lệ màn hình cần đạt: 16:9 và 20:9 landscape.

## 2. Hiện trạng (kết quả điều tra)

Ảnh chụp: `Builds/Screenshots/audit/` (không commit).

**Khung cảnh**

| Scene | Hiện trạng |
|---|---|
| Menu / Map | `Art/UI/HomeIllustration.png` — chuẩn tham chiếu về cảm giác “trường KMA” |
| Sprint | `SprintParallax`: Sky (-30), Campus (-20), Track (-10); đường chạy che gần hết Campus |
| Volleyball | Quad `Pixel.png` màu phẳng cát/trời (`Assets/Editor/VolleyballSceneConfigurator.cs:37-40,187-208`); player trái đứng lấn ra ngoài vạch sân |
| Football | `Art/Football/GoalView` (sân, khán đài, cây); không có trường |
| FrogJump | Quad phẳng trời/cỏ/đường; mượn `Pixel.png` của Volleyball và lỗi nếu scene Volleyball chưa build |
| Chess / Celebration | Image `Sky` + `Campus` không giữ tỉ lệ (bị kéo giãn); Celebration không có mặt đất |
| GameOver | Nền navy trơn |

`GameplayPresentation.EnsureCamera()` (`Scripts/Core/GameplayPresentation.cs:81-95`) ép màu nền camera gần đen ở mọi scene, ghi đè màu đã author; lộ ra ở vùng safe-area chưa phủ nền.

Không có backdrop dùng chung: mỗi configurator có bản copy helper Quad/Picture riêng.

**Button:** 7–8 kiểu. Kit (`UiKit.Button`), nút tròn ĐÁNH, plate TRÁI/PHẢI, nút pause kit, nút xiên Menu/GameOver, card/badge Map–Journey (bóng (6,-6) khác kit (0,-4)), nút ad-hoc (confirm new game, Settings back, “BỎ QUA” dialogue) với màu/kích thước viết cứng, và `Btn_Brutal.prefab` không còn được tham chiếu.

**Lỗi UI**

1. Sprint: `SprintPlayerIdentityOutline` copy sprite player mỗi frame, scale 1.12×, tint cyan, order 11 — thành bóng ma; trùng order 11 với runner làn 1.
2. Football: StartPanel bán trong suốt đè lên kicker; chip “CÒN 5 LƯỢT” sát nút pause.
3. Chess: IntroCard bán trong suốt, thấy quân cờ phía sau; mục tiêu lặp 3 lần (chip, bong bóng thoại, IntroCard).
4. Sprint: mũi tên vàng dưới đáy chồng lên plate TRÁI/PHẢI; mây đè lên panel HUD; object rỗng `Rank`, `FX`.
5. FrogJump: chữ HUD trơn (Timer/Progress/Status) trộn với chip kit.
6. GameOver: còn `S2_HUD_Minigame` (active), `S2_PhaseOverlay`, `S2_ResultPanel`.
7. Màn kết quả (cần xác nhận bằng ảnh): có thể chồng “KẾT QUẢ” + chip “RESOLVE” (tiếng Anh) + StatusLabel.
8. Punishment: chữ placeholder tiếng Anh, “TUTORIAL” chồng “PLAY”, nền đen — scene không còn trong luồng.

## 3. Khung cảnh trường

### 3.1 Art mới

Script `tools/render-campus-art.js` (Node + `@resvg/resvg-js`), cùng bảng màu và độ dày nét với `render-football-goalview-art.js` (ink `#1c2546`, viền navy, màu phẳng, không gradient). Xuất cả `.svg` và `.png` vào `Assets/_Project/Art/Environments/Campus/`:

| File | Nội dung | Ghi chú |
|---|---|---|
| `CampusSky` | Trời xanh + mây | Lặp ngang liền mạch |
| `CampusSkyline` | Tòa nhà KMA (gợi lại khối nhà trong `HomeIllustration`: thân nhà nhiều tầng, sảnh kính, cột cờ), cây, khán đài, đèn sân | Nền trong suốt; lặp ngang liền mạch |
| `SchoolCourt` | Sân bóng chuyền ngoài trời: nền bê tông, mặt sân sơn, vạch sân, cột và lưới | Hình học khớp `CourtSpace`; thay bộ quad cát |
| `SchoolTrack` | Dải đường chạy + cỏ cùng style | Dùng cho FrogJump |

`Environments/Sprint/Campus.png` giữ lại cho đến khi mọi tham chiếu chuyển sang `CampusSkyline`, sau đó xóa. Ghi nguồn gốc art vào `CREDITS.md`.

### 3.2 `CampusBackdrop`

`CampusBackdropArt`, `CampusBackdropLayout`, `CampusBackdropWorld` ở `Scripts/Gameplay/Common/Backdrop/`; `CampusBackdropUi` ở `Scripts/UI/`; `CampusBackdropAuthoring` ở `Assets/Editor/`.

| Đơn vị | Trách nhiệm |
|---|---|
| `CampusBackdropArt` (ScriptableObject, 1 asset) | Tham chiếu sprite các lớp + màu trời/đất fallback lấy từ `UITheme` |
| `CampusBackdropLayout` (C# thuần) | Từ kích thước viewport, tỉ lệ sprite và tham số neo (đường chân trời, chiều cao skyline) tính rect từng lớp; luôn giữ tỉ lệ, phủ kín chiều ngang, lặp tile khi cần |
| `CampusBackdropWorld` (MonoBehaviour) | Đặt SpriteRenderer theo camera orthographic; cập nhật khi đổi kích thước màn hình; order cố định: sky -40, skyline -35 |
| `CampusBackdropUi` (MonoBehaviour) | Cùng layout cho Image trong canvas; đặt ngoài `SafeAreaRoot` để phủ cả vùng safe-area |
| `CampusBackdropAuthoring` (Editor) | Một hàm dùng chung cho mọi scene configurator, thay các helper Quad/Picture copy riêng |

`SprintParallax` giữ cơ chế cuộn; chỉ đổi sprite và vị trí lớp.

### 3.3 Từng scene

| Scene | Thay đổi |
|---|---|
| Sprint | Skyline mới; hạ/thu hẹp vùng đường chạy để thấy rõ trường, giữ 4 làn đọc được |
| Volleyball | `CampusBackdropWorld` + `SchoolCourt` thay Sky/Sand/Court/lines; giữ toạ độ `CourtSpace`; sửa vị trí khởi đầu player để đứng trong vạch. Âm thanh: thay nhạc “Beachfront Celebration” bằng bài nhạc minigame sẵn có, `SandStep` → bước chân sân cứng (dùng clip sẵn có; FrogJump cũng đang dùng `SandStep`). Cập nhật `VolleyballSceneConfiguratorTests` cho danh sách sprite môi trường mới |
| FrogJump | `CampusBackdropWorld` + `SchoolTrack`; bỏ phụ thuộc `Pixel.png` của Volleyball |
| Football | Thay dải cây phía sau khán đài trong `field` bằng skyline trường (sửa `render-football-goalview-art.js`); giữ hình học khung thành/vạch |
| Chess | `CampusBackdropUi` thay Sky/Campus kéo giãn; giữ lớp Wash để bàn cờ nổi |
| Celebration | `CampusBackdropUi` + dải mặt đất để nhân vật đứng |
| GameOver | `HomeIllustration` + wash navy (cùng cách Map) |
| Toàn cục | `GameplayPresentation` không ép màu camera; camera clear bằng màu trời trong `CampusBackdropArt` (scene không có backdrop giữ màu đã author) |

## 4. Button — hai họ

**Token** (trong `UITheme`): bán kính control, viền, `ShadowOffset`, màu bóng, màu disabled (nền + chữ), cỡ chữ nút. `MinigameUiTheme` lấy `ShadowOffset` từ `UITheme` thay vì viết cứng `(0,-4)` (`MinigameUiTheme.cs:48`). Giá trị chung được chốt khi triển khai bằng ảnh so sánh, mặc định giữ `(0,-4)` của kit.

**Họ Kit** — mọi nơi trừ Menu/GameOver:

- Giữ nguyên: nút minigame, pause, PausePanel, ResultPanel, Chess, Football, Celebration.
- Chuyển sang token kit: nút “‹” Map, card bài học, “TIẾP TỤC BÀI HỌC”, badge chặng (`MapStopBuilder` bỏ Outline/Shadow viết cứng).
- Chuyển sang `UiKit.Button`: confirm new game (`S5ShellSceneController.cs:236-272`), Settings back (`SettingsPresentationBuilder.cs:49-58`), “BỎ QUA” (`JourneyDialoguePresenter.cs:430-444`).
- Nút điều khiển giữ hình dạng nhưng dùng token: ĐÁNH (`UiKit.RoundButton`), plate TRÁI/PHẢI (`UiKit.ControlPlate`) đổi sang nền đặc, đủ tương phản trên đường chạy đỏ.
- Một style disabled chung cho Kit (Chess “Gợi ý”, Football “GIỮ ĐỂ SÚT”): rõ là khóa, chữ vẫn đọc được.

**Họ Xiên** — Menu và GameOver: giữ hình dạng; `HomeMenuButton.cs:78-90` lấy màu disabled từ token; cỡ chữ GameOver lấy từ token, bỏ ép 28 (`GameOverPresentationBuilder.cs:132-148`).

**Dọn dẹp:** xóa `Prefabs/UI/Btn_Brutal.prefab`; xóa `BrutalButton` nếu sau khi dọn `MapNodeView`/`MapStopBuilder` không còn ai dùng (nếu `MapStopBuilder` cần hiệu ứng nhấn, dùng `KitPressFeedback`).

## 5. Sửa lỗi UI

1. **Sprint shadowing:** xóa `SprintPlayerIdentityOutline`. Nhận diện player bằng tag “PLAYER” sẵn có + một vòng cyan dưới chân (sprite từ pipeline art). Player order 14, rival 10–13 không trùng.
2. **Football:** StartPanel nền đặc và không đè lên kicker (thu nhỏ/dời lên trên); chip lượt cách pause theo spacing chuẩn.
3. **Chess:** IntroCard nền đặc; bỏ dòng mục tiêu trong IntroCard (giữ thời gian và số lần sửa sai), mục tiêu chỉ ở chip và bong bóng thoại.
4. **Sprint layout:** dành vùng đáy riêng cho plate TRÁI/PHẢI, mũi tên không chồng plate; mây không đè panel HUD; xóa `Rank`, `FX` rỗng.
5. **FrogJump HUD:** Timer/Progress/Status chuyển sang chip kit giống các minigame khác.
6. **GameOver:** xóa `S2_HUD_Minigame`, `S2_PhaseOverlay`, `S2_ResultPanel` khỏi scene.
7. **Màn kết quả:** chụp xác nhận; nếu chồng tiêu đề, bỏ chip “RESOLVE” và chỉ giữ một tiêu đề.
8. **Spacing:** thêm thang spacing vào `UITheme` (8/16/24/32) và lề HUD góc màn hình; HUD góc của mọi minigame dùng chung lề.

## 6. Xóa Punishment

- Xóa: `Scenes/Punishment.unity`, `Scripts/Core/PunishmentSceneController.cs`, `Scripts/Progression/PunishmentController.cs`, mục trong `EditorBuildSettings`, nhánh route trong `SceneRouter` (`punishmentScene`, `CompletePunishment`), action map “Punishment” trong input asset và `GameplayInputRouter.PunishmentActionMapName`, các chuỗi trong `AudioManager`, `GameAudioLibrary`, `GameplayPresentation`, xử lý trong `MinigameUIAssembler`/`MinigamePrefabStyler`/`PlayModeScreenshot`.
- Giữ tương thích save: giá trị số của `SessionRoute` không đổi — `Punishment` đổi tên thành `RetiredPunishment` (giữ vị trí, có comment). Save cũ có route này hoặc `awaitingPunishment = true` được migration đưa về `Map` và xóa cờ; trường `awaitingPunishment` chỉ còn được đọc trong migration.
- Test: xóa `PunishmentRouteTests`; sửa các test còn tham chiếu (danh sách scene, input map, save) — thêm test migration cho save cũ có `RetiredPunishment`.

## 7. Kiểm chứng

- **Ảnh trước/sau** mọi scene qua `tools/qa-screenshot.sh` (Menu, Map, 5 minigame tutorial + giữa trận, Pause, Result, Celebration, GameOver) ở 16:9 và 20:9. Thêm qaState kết quả cho Volleyball và Football. Lưu ảnh chọn lọc vào `docs/qa/images/` + báo cáo `docs/qa/campus-ui-unification.md`.
- **EditMode:** `CampusBackdropLayout` (giữ tỉ lệ, phủ kín, tile, nhiều tỉ lệ màn hình); token button (kit/xiên lấy từ `UITheme`, không còn màu viết cứng ở các nút đã chuyển); configurator Volleyball/FrogJump dùng sprite mới, không phụ thuộc chéo; migration save Punishment.
- **PlayMode:** Sprint không còn renderer bản sao player; GameOver không còn object minigame UI; luồng thua không bao giờ tải scene Punishment.
- Chạy lại toàn bộ EditMode và PlayMode hiện có.

## 8. Ngoài phạm vi

Gameplay và độ khó; nội dung Bootstrap/splash; art nhân vật; build Android (kiểm tra thiết bị làm riêng nếu cần).

## 9. Rủi ro và lưu ý

- `MG_Football.unity`, `MG_Volleyball.unity`, prefab UI (`HUD_Minigame`, `PhaseOverlay`, `ResultPanel`) và `EditorBuildSettings` đang có thay đổi chưa commit; cần commit/stash trước khi triển khai.
- `KMA/Volleyball/Build Scene` dựng lại scene từ rỗng; mọi chỉnh tay trong scene sẽ mất — thay đổi phải nằm trong configurator.
- Art sinh bằng script phải được người dùng duyệt bằng ảnh trước khi áp vào tất cả scene.

## 10. Điều chỉnh khi lập kế hoạch

Đọc code chi tiết khi viết kế hoạch cho thấy một số mục ở trên cần sửa:

- **Volleyball "player đứng ngoài vạch"** không phải lỗi: đó là vị trí giao bóng sau đường biên cuối (`VolleyballMatch.PlayerServeSpot = (-8.5, 0)`, nửa sân dài 8 m). Giữ nguyên.
- **Sprint:** chỉ bỏ `SprintPlayerIdentityOutline`. Sorting order bị trùng chỉ do chính bản sao này gây ra, và tag PLAYER + chevron đã đủ để nhận diện người chơi, nên không đổi order và không thêm vòng dưới chân.
- **`BrutalButton`** vẫn còn được các điểm dừng trên Map dùng, nên chỉ xóa `Btn_Brutal.prefab`.
- **Cỡ chữ GameOver** đã lấy từ token `MinigameUiTheme.MinimumFontSize`, không cần đổi.
- **Thang spacing** đã có sẵn (`MinigameUiTheme.SpaceXs/Sm/Md/Lg`). Kế hoạch dùng lại thang này thay vì thêm vào `UITheme`, và sửa lề nút pause của FrogJump cho khớp.
- **Volleyball:** camera chuyển lên `y = 1`, ortho `6.2` để có chỗ cho skyline phía trên biên xa. Đây chỉ là thay đổi trình bày, không ảnh hưởng input, vì điều khiển nằm trên UI.
- **Save cũ:** trường `SaveData.awaitingPunishment` được giữ lại trong schema (luôn ghi `false`), `SessionRoute.Punishment` đổi tên thành `RetiredPunishment` và giữ nguyên giá trị số.
