# Thay sprite nhân vật bằng bộ asset bổ sung "chay-tron-the-chat-sports-supplement"

**Ngày:** 2026-10-10

**Trạng thái:** Thiết kế đã được người dùng duyệt trong hội thoại; đặc tả chờ duyệt, sau đó lập kế hoạch triển khai.

## 1. Vấn đề và mục tiêu

Các minigame đang mượn tạm tư thế không đúng ngữ cảnh: bóng đá dùng `back`/`climb0`/`climb1` cho người sút và `MalePerson` làm thủ môn; bóng chuyền dùng `duck`/`hold`/`jump`/`attack1`; bật cóc dùng `duck`/`jump`/`fallDown`; cờ vua và màn ăn mừng chỉ có `idle`/`hurt`/`cheer`.

File `chay-tron-the-chat-sports-supplement.zip` (gốc repo) có 56 tư thế mới đúng ngữ cảnh cho nhân vật chính, cô giáo và một thủ môn riêng (`StudentKeeper`), cùng 12 chân dung.

**Mục tiêu:** mọi minigame hiển thị tư thế đúng ngữ cảnh từ bộ mới, **chỉ bằng cách đổi sprite** mà configurator nạp vào scene.

Quyết định của người dùng:

| Câu hỏi | Quyết định |
|---|---|
| Phạm vi | **Chỉ đổi sprite.** Thay những tư thế ánh xạ 1-1 vào trạng thái gameplay hiện có; không thêm trạng thái, chuỗi animation hay logic mới |
| Chân dung 256 × 256 | **Giữ thoại toàn thân.** Đổi biểu cảm Cheer/Hurt sang sprite toàn thân mới; nhập chân dung vào dự án nhưng chưa dùng |

**Ngoài phạm vi:** chuỗi animation mới mà hướng dẫn của bộ asset gợi ý (xuất phát → lao lên, bật cóc 4 pha, đập 3 pha, thủ môn bắt bóng có bóng trong hình, cô giáo ghi sổ, vỗ tay); bộ sprite bóng đá 255 × 320; đổi bố cục thoại sang chân dung; đổi sprite của đối thủ/bạn chạy (`MalePerson`, `FemalePerson`, `FemaleAdventurer`).

## 2. Nhập file

Nguồn là thư mục `Assets/_Project/...` trong zip. Gói không có `.meta`; Unity tự sinh GUID mới khi nhập.

| Nguồn trong zip | Đích trong dự án | Số file |
|---|---|---:|
| `Art/Characters/HeroExtra/*.png` | `Art/Characters/MaleAdventurer/` | 36 |
| `Art/Characters/BossPEExtra/*.png` | `Art/Characters/BossPE/` | 8 |
| `Art/Characters/StudentKeeper/*.png` | `Art/Characters/StudentKeeper/` (thư mục mới) | 12 |
| `Art/UI/Portraits/*.png` | `Art/UI/Portraits/` (thư mục mới) | 12 |

**Không chép:**

- `ReusedHero/`, `ReusedBossPE/`: trùng từng byte với file đang có (đã kiểm tra bằng `cmp`).
- `Art/Football/Characters/` (255 × 320): cảnh bóng đá đã vẽ nhân vật qua `CharacterArt` ở 192 × 256, PPU 200; dùng chung một kích thước cho mọi scene.
- `Previews/`, `Sources/`, `Docs/`, `README.md`.

**Không xóa** sprite cũ hay `.meta` nào; tư thế cũ vẫn được đối thủ và các trạng thái chưa đổi dùng.

Tên file giữ nguyên quy ước `<Nhân vật>_<tư thế>.png`, nên `CharacterArt.PosePath` và `IsPoseOf` dùng được ngay, không cần sửa.

## 3. `CharacterArt` (`Assets/Editor/CharacterArt.cs`)

- Thêm `HeroPoses` = 18 tư thế chung + 36 tư thế mới của nhân vật chính (tên lấy đúng phần sau `MaleAdventurer_` của file trong `HeroExtra`).
- `BossPoses` thêm 8 tư thế: `angry`, `clap0`, `clap1`, `congratulate`, `handsOnHips`, `scoreHold`, `scoreWrite`, `think`.
- Thêm `public const string Keeper = "StudentKeeper"` và `KeeperPoses` gồm 12 tư thế: `idle`, `ready`, `reachLeftEmpty`, `reachRightEmpty`, `diveLeftEmpty`, `diveRightEmpty`, `catchLeftBall`, `catchRightBall`, `holdBall`, `highCatchBall`, `recover`, `cheer`.
- `ImportAll` nhập: `HeroPoses` cho `Hero`, `Poses` cho ba nhân vật phụ, `BossPoses` cho `Boss`, `KeeperPoses` cho `Keeper`. Cài đặt nhập giữ nguyên (Sprite Single, PPU 200, pivot đáy giữa, Bilinear, không mipmap, Max Size 512).
- `Characters` vẫn chỉ gồm 4 nhân vật có đủ 18 tư thế chung; `StudentKeeper` không thuộc danh sách đó vì không có bộ 18 tư thế.

Chân dung nhập bằng một hàm riêng (trong `CharacterArt` hoặc file nhỏ cạnh đó): Sprite (2D and UI), Single, pivot giữa, Bilinear, không mipmap, Max Size 256. Không có tham chiếu nào tới chân dung trong lần này.

## 4. Ánh xạ tư thế theo scene

Không đổi trạng thái hay logic; chỉ đổi file mà configurator nạp vào. Đối thủ và bạn chạy giữ nguyên.

### 4.1 Bóng đá (`FootballSceneConfigurator.cs`)

| Slot `FootballPoseSprites` | Cũ | Mới |
|---|---|---|
| `kickerReady` | Hero `back` | Hero `footballBackIdle` |
| `kickerRunUp` | Hero `climb0` | Hero `footballBackApproach` |
| `kickerStrike` | Hero `climb1` | Hero `footballBackKick` |
| `kickerCelebrate` | Hero `cheer1` | Hero `footballBackCelebrate` |
| `keeperReady` | MalePerson `cheer1` | StudentKeeper `ready` |
| `keeperSave` | MalePerson `hold` | StudentKeeper `cheer` |
| `keeperBeaten` | MalePerson `hurt` | StudentKeeper `recover` |

`KeeperCharacter` đổi thành `CharacterArt.Keeper`, `KeeperReadyPose` thành `"ready"`.

Thủ môn **chỉ dùng tư thế không vẽ bóng**: `FootballPresentation` không ẩn quả bóng riêng khi cản phá, nên `holdBall`/`catch*Ball` sẽ hiện hai quả bóng. Pha nghiêng/bay hiện có do `KeeperAngle` xoay renderer, không cần sprite theo hướng.

**Vùng cản phá khớp lại theo hình mới (người dùng chọn `ready` khi triển khai).** `KeeperCapsules` trong `FootballFlightSimulation.cs` mô phỏng hình thủ môn đang vẽ; `KeeperSilhouetteTests` yêu cầu độ khớp ≥ 0,85. Bộ capsule cũ chỉ khớp `ready` ở mức 0,63/0,55, nên được thay bằng bộ khớp lại theo `ready` (0,91/0,91, ngang thủ môn cũ). Thủ môn khom, không còn tay giơ cao, nên vùng cản thấp hơn thủ môn cũ khoảng 14 px màn hình. Hệ quả cân bằng (người dùng đã được báo và vẫn chọn `ready`): cú sút giữa (aim 0) ở mức Normal chỉ bị cản khi lực khoảng 0,35–0,45 (cao 0,5–1,5 m); từ lực 0,5 trở lên bóng bay qua đầu và thành bàn thắng (trước đây lực 0,5 vẫn bị cản). Hai test pin hành vi cũ được đổi sang cú sút trúng thân người: `FootballShotSolverTests.KeeperReactsAfterDelayAndSavesActualContact` dùng lực 0,4 thay vì 0,5, và `FootballControllerTests.FiveResolvedShotsEmitOneCampaignResultAfterTheFeedbackPhase` giữ nút sút 0,9 s thay vì 0,7 s. Đây là thay đổi cân bằng duy nhất và là thay đổi script runtime duy nhất.

### 4.2 Bóng chuyền (`VolleyballSceneConfigurator.Athlete`)

Chỉ cầu thủ (`CharacterArt.Hero`); đối thủ `OpponentCharacter` giữ bộ cũ. Cần tách danh sách tư thế theo nhân vật, ví dụ truyền bộ tên tư thế vào `Athlete`.

| Tham số `VolleyAthleteView.Configure` | Cũ | Mới (cầu thủ) |
|---|---|---|
| `idleFrames` | `idle` | `volleyReady` |
| `runFrames` | `run0, run1, run2, run1` | `volleyShuffleRight, volleyShuffleLeft` |
| `receiveFrames` | `duck, hold` | `volleyDig, volleyRecover` |
| `smashFrames` | `jump, attack1` | `volleySpikeWindup, volleySpikeContact` |
| `blockFrames` | `jump, cheer1` | `volleyJumpLoad, volleySet` |
| `diveFrames` | `fall, slide` | `fallForwardRight, fallSitRight` |

`MarkerWorldHeight` 3,1 → 3,25: `volleySpikeContact` cao 246 px (2,83 đơn vị ở `AthleteScale` 2,3), cao hơn điểm thấp nhất của dấu chỉ người chơi hiện tại (2,73).

**Rủi ro:** hai khung shuffle xen kẽ có thể giật khi chạy. Nếu ảnh chụp kiểm tra cho thấy xấu, `runFrames` quay về `run0, run1, run2, run1` và ghi lại trong commit.

### 4.3 Bật cóc (`FrogJumpSceneConfigurator.cs`)

| Tham số | Cũ | Mới |
|---|---|---|
| `squat` | `duck` | `frogReadyRight` |
| `jump` | `jump` | `frogAirRight` |
| `fall` | `fallDown` | `fallSitRight` |

`FrogJumpView` vẫn tự co giãn scale và tính quỹ đạo; không đổi.

### 4.4 Chạy ngắn (`DemoSprintArtConfigurator.LoadCharacter`)

Chỉ nhân vật chính:

| Trường `RunnerArt` | Cũ | Mới |
|---|---|---|
| `Hit` (clip Stumble) | `hurt` | `fallForwardRight` |
| `FallDown` (clip Fail) | `fallDown` | `fallSitRight` |
| `Idle`, `Run`, `Cheer` | giữ nguyên | giữ nguyên |

`Idle` **không** đổi sang `sprintStart`: `RunnerVisualPresenter` cũng phát Idle khi người chơi ngừng chạm giữa cuộc đua (`resting`), tư thế ngồi xuất phát sẽ sai ở đó.

`LoadCharacter` nhận thêm tên tư thế cho Hit/FallDown (hoặc rẽ nhánh theo `folder == CharacterArt.Hero`); ba bạn chạy giữ `hurt`/`fallDown`.

### 4.5 Cờ vua (`ChessFinalSceneConfigurator.cs`)

`ChessFinalController` gọi `SetStudent`/`SetTeacher` bằng **khóa** tư thế; khóa giữ nguyên. Configurator thêm bảng khóa → tên file; khóa không có trong bảng dùng tên file trùng khóa như hiện nay.

| Vai | Khóa | File cũ | File mới |
|---|---|---|---|
| Sinh viên | `idle` | `idle` | `think` |
| Sinh viên | `hurt` | `hurt` | `disappointed` |
| Sinh viên | `cheer0` | `cheer0` | `celebrate` |
| Sinh viên | `cheer1` | `cheer1` | `happy` |
| Cô giáo | `idleBoss` | `idleBoss` | `handsOnHips` |
| Cô giáo | `taunt` | `taunt` | `angry` |
| Cô giáo | `chessThink` | `chessThink` | `think` |
| Cô giáo | `cheer0` | `cheer0` | `congratulate` |
| Cô giáo | `chessMove`, `strictLook` | giữ nguyên | giữ nguyên |

Ảnh mặc định của hai `Image` khi dựng scene lấy qua cùng bảng (sinh viên `think`, cô giáo `handsOnHips`).

### 4.6 Màn ăn mừng (`CelebrationSceneConfigurator.cs`)

`CelebrationSceneController` nhận mảng sprite theo vị trí (đến, cổ vũ A, cổ vũ B); configurator tách bộ tên riêng cho sinh viên, bạn cùng lớp và cô giáo.

| Vai | Ô | Mới |
|---|---|---|
| Sinh viên | 0 (`idle`) | `happy` |
| Sinh viên | 1, 2 (`cheer0`, `cheer1`) | giữ nguyên: hai ô luân phiên mỗi 0,35 giây, đổi một ô sang `celebrate` sẽ nháy giữa hai hình không liên quan |
| Cô giáo | 1 (`taunt`) | `clap0` |
| Cô giáo | 2 (`cheer0`) | `congratulate` |
| Cô giáo | 0 (`idleBoss`) | giữ nguyên |
| Bạn cùng lớp (`FemalePerson`) | tất cả | giữ nguyên |

### 4.7 Cô giáo thổi còi đầu màn (`StartLecturerAuthoring.cs`)

| Tham số `ConfigureGestures` | Cũ | Mới |
|---|---|---|
| `tauntPose` | `taunt` | `angry` |
| `cheerPose` | `cheer0` | `congratulate` |
| còn lại (count, command, strictLook, cheerBothArms, penalty, penaltySide) | giữ nguyên | giữ nguyên |

`Configure` (idleBoss, whistle0, whistle1) giữ nguyên; chiều cao đặt chỗ đọc từ `idleBoss` nên không đổi.

### 4.8 Thoại hành trình (`StudentJourneyContentBuilder.cs`)

`PoseFiles` hiện áp dụng chung cho mọi nhân vật. Thêm ghi đè theo nhân vật:

| Nhân vật | `DialoguePose.Cheer` | `DialoguePose.Hurt` |
|---|---|---|
| `anh_khoa_tren` (MaleAdventurer) | `celebrate` | `disappointed` |
| `co_the_chat` (BossPE) | `congratulate` | `angry` |
| `tan_thu`, `mai_toang` | giữ `cheer0` | giữ `hurt` |

Idle, Jump, Duck giữ nguyên cho mọi nhân vật.

`BuildDialogues` gọi `CharacterArt.ImportAll()` trước tiên, vì file mới phải được nhập thành sprite trước khi `AssetDatabase.LoadAssetAtPath<Sprite>` tìm thấy.

## 5. Tạo lại scene và asset

Sau khi sửa configurator, chạy lại các configurator bị ảnh hưởng ở Unity batchmode để ghi lại scene/asset: `MG_Football`, `MG_Volleyball`, `MG_FrogJump`, `MG_Sprint` (clip `MaleAdventurer_*.anim`), `MG_ChessFinal`, `Celebration`, scene có `StartLecturer`, và thư viện thoại hành trình. Commit cả `.png.meta` mới sinh.

## 6. Kiểm thử

- **EditMode:** cập nhật các test đang khẳng định tên tư thế/đường dẫn (`CharacterArtTests`, `FootballSceneConfiguratorTests`, `FrogJumpSceneTests`, `BossArtImportTests` và test nào khác hỏng sau khi đổi). Thêm test: mọi tư thế trong `HeroPoses`/`BossPoses`/`KeeperPoses` tồn tại và nhập với PPU 200, pivot đáy giữa; thủ môn bóng đá là `StudentKeeper`; slot thủ môn không dùng tư thế có bóng (`*Ball`). Chạy toàn bộ EditMode, phải xanh.
- **Hình ảnh:** chụp Play mode (skill `testing-unity-ui-with-screenshots`) cho bóng đá (chờ sút, sút, ghi bàn, bị cản), bóng chuyền (đứng, chạy, đỡ, đập), bật cóc (ngồi, bay, ngã), chạy ngắn (vấp, thua), cờ vua, màn ăn mừng, một đoạn thoại. Kiểm tra: không cắt mép, chân chạm đất (pivot), kích thước nhân vật không nhảy giữa các tư thế, chỉ một quả bóng ở bóng đá.

## 7. Tiêu chí hoàn thành

- 68 PNG (56 tư thế + 12 chân dung) nằm đúng thư mục ở mục 2, có `.meta`, nhập đúng cài đặt.
- Mọi scene ở mục 4 dùng tư thế mới như bảng; đối thủ và bạn chạy không đổi.
- Không file cũ nào bị xóa; script runtime chỉ đổi `KeeperCapsules` (mục 4.1).
- EditMode xanh; ảnh chụp đạt các điểm ở mục 6.
