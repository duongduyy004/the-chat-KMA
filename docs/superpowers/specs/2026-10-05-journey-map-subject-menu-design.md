# Design — Menu chọn môn theo style Bản đồ hành trình

Ngày: 2026-10-05
Trạng thái: thiết kế đã duyệt qua preview HTML; chờ người dùng duyệt spec viết.

## Mục tiêu

Đổi màn chọn môn (scene `Map`, `S5MapPresentation`) từ 3 thẻ nằm ngang nối bằng
đường thẳng sang một bản đồ hành trình: một con đường uốn lượn nối ba trạm
Chạy nước rút → Bóng chuyền → Bóng đá, kết thúc ở cờ đích. Chỉ đổi bố cục và
hình ảnh. Không đổi luật chơi, route, số lượt hay dữ liệu lưu.

## Ngoài phạm vi

- Luật mở khóa môn. Môn sau vẫn khóa cho tới khi qua bài thi của môn trước
  (`JourneyProgress.IsSubjectUnlocked`: Bóng chuyền cần `sprint_exam`, Bóng đá cần
  bài thi Bóng chuyền). Bản đồ chỉ phải hiển thị đúng trạng thái khóa đó.
- Logic Học → Luyện → Thi, unlock, attempt, `JourneyLessonList` (chỉ đổi tiêu đề
  panel và kiểu hiển thị thẻ, xem bên dưới).
- Màn hình dọc. Giữ mục tiêu landscape 16:9 như hiện tại.

## Tham chiếu

Preview đã duyệt: `journey-map-preview.html` (scratchpad của phiên brainstorm,
khung 1920×1080). Bố cục dưới đây lấy từ preview đó; toạ độ là toạ độ khung
tham chiếu 1920×1080 và phải chuyển thành anchor tương đối khi code.

## Thiết kế

### Header
- Thấp hơn: cao ~84 (hiện 116). Nút back 60, tiêu đề cỡ ~40.
  **Chỉ giữ lại chữ của tiêu đề**: `CHỌN MÔN THI`. Kiểu dáng theo style mới (gọn,
  cỡ chữ nhỏ hơn) và bỏ dòng phụ đề `Chọn một môn để bắt đầu`. Bộ đếm lượt giữ
  nguyên nội dung (`Lượt thi: n/5`) và nút back vẫn gọi `RouteToMenu`.

### Bản đồ
- Một đường cong qua tâm ba trạm: Chạy nước rút (thấp, trái), Bóng chuyền (cao,
  giữa), Bóng đá (thấp, phải). Đường gồm viền navy, nét chấm sáng và nét tiến độ
  vàng nằm **dưới** các huy hiệu.
- Nét tiến độ chỉ vẽ khi đã có môn hoàn thành. Với tiến độ 0 phải ẩn hẳn, không
  để lại dấu chấm ở điểm xuất phát.
- Cờ đích gắn vào góc huy hiệu Bóng đá, không phải phần tử riêng của bản đồ.

### Trạm (node)
- Huy hiệu tròn màu theo môn (`LessonJourney.sprint/volleyball/football`), viền
  trắng + viền navy ngoài, số thứ tự ở góc trên trái, dấu ✓ vàng ở góc trên phải
  khi đã xong. Biểu tượng dùng `SportIconSprite` hiện có.
- Mọi trạm đều có tên, 3 sao và nhãn trạng thái trong một viên thuốc navy. Giữ
  nguyên chuỗi hiện có của `MapNodeView`: `SẴN SÀNG`, `HOÀN THÀNH`,
  `CHƯA MỞ KHÓA`. Số sao lấy từ `MapNodeView.Stars` (theo hạng tốt nhất), không
  mặc định 3. Sao rỗng phải đọc được trên nền đó.
- Trạm **khóa** (môn chưa mở): huy hiệu xám (`MapLockedCard`), biểu tượng xám,
  icon khóa vẽ bằng Image ở góc dưới phải huy hiệu, không bấm được. Khi mới chơi,
  chỉ Chạy nước rút mở; hai môn sau khóa.
- Trạm **hiện tại** (môn đầu tiên đã mở khóa mà chưa qua; không có khi cả khóa học đã xong) lớn hơn (~220 so với ~170), có vòng
  vàng, glow nhấp nháy, thẻ ghim "ĐANG Ở ĐÂY" và tên trong thẻ trắng. Các trạm
  khác dùng chữ trực tiếp trên nền, không có thẻ trắng.
- Trạm đang chọn nhưng không phải trạm hiện tại có vòng vàng, không có glow.
- Tên cách huy hiệu 22–26 và không đè lên vòng tròn.

### Panel bên dưới
- Bắt đầu thấp hơn (cạnh trên ~y=720 trên khung 1080), cao ~320, và không được che bất kỳ
  phần nào của trạm (tên, sao, nhãn trạng thái).
- Tiêu đề panel **chỉ là tên môn** (ví dụ `Chạy nước rút`), bỏ tiền tố
  `Chương 01 ·` đang có trong `JourneyLessonList.Refresh`. Giữ dòng tiến độ
  `n/3 bài hoàn thành`, nút `TIẾP TỤC BÀI HỌC ›` và dòng gợi ý.
- Giữ nội dung thật của thẻ bài (tiêu đề HỌC/LUYỆN/THI, mục tiêu, trạng thái,
  hành động `BẮT ĐẦU ›` / `ÔN LẠI ›` / `CHƠI LẠI ›`); preview chỉ minh họa.
- Ba thẻ Học / Luyện / Thi thấp hơn (còn khoảng 45–55% chiều cao hiện nay; con
  số cuối cùng chốt bằng ảnh chụp và các test không-cắt-chữ). Dùng chung một màu nhấn vàng cho
  "đang chọn" với trạm và nút.
  - Đang mở: viền vàng và nút **BẮT ĐẦU** nổi bật.
  - Khóa: nền xám đậm hơn, chữ tối, đọc được; icon khóa là sprite/vẽ bằng Image,
    không dùng emoji.
  - Đã xong: nền xanh nhạt, dấu ✓ và nút nhỏ **CHƠI LẠI**.

## Thay đổi code

| File | Thay đổi |
| --- | --- |
| `UI/MapJourneyPathLayout.cs` | Đường cong qua ba trạm (nhiều đoạn thẳng/ảnh xoay hoặc mesh), vị trí trạm theo anchor, trạm hiện tại to hơn, nét tiến độ ẩn khi chưa có tiến độ. |
| `UI/MapPresentationBuilder.cs` | Header thấp hơn; `Card` dựng huy hiệu tròn thay thẻ chữ nhật; thêm cờ đích, số thứ tự, ✓, thẻ "ĐANG Ở ĐÂY". |
| `UI/MapNodeView.cs` | Thêm sao, trạng thái current/selected/completed/locked; giữ API `Configure`/`Bind`/`SetAvailability` hiện tại. |
| `UI/MapScreen.cs`, `UI/JourneyLessonList.cs` | `MapScreen` tính trạm hiện tại/đang chọn và đẩy xuống `MapNodeView`; `JourneyLessonList` lộ `SelectedSubject`. |
| `UI/JourneyLessonPresentation.cs`, `UI/JourneyLessonList.cs` | Tiêu đề chỉ tên môn; thẻ thấp hơn; màu khóa/đang mở/đã xong; nút BẮT ĐẦU / CHƠI LẠI. |
| `UI/UITheme.cs` (`LessonJourneyStyle`) | Cập nhật anchor panel/map và thêm các hằng kích thước/màu mới ở đây, không hard-code. |
| `Editor/ShellSceneAuthoring.cs`, `Scenes/Map.unity` | `Map.unity` đã được author sẵn (`S5MapPresentation` tồn tại, nhánh `existing` của builder). Phải chạy lại authoring để scene nhận cấu trúc mới và không nhân đôi UI. |

Không thêm package hay bitmap mới; dùng uGUI, `UiKit`, `VietTypography` và sprite
runtime như code hiện tại.

## Kiểm thử

- Cập nhật các kiểm tra hình học hiện có trong `UIComponentTests`,
  `FestivalUiExperienceTests` và `S5NewGameTests` (chúng đang đo thẻ ngang).
- Thêm kiểm tra: ở 1280×720, 1920×1080, 1440×1080 và 2400×1080
  1. không có phần nào của trạm (tên, sao, nhãn) giao với panel dưới;
  2. không có chữ nào bị cắt hoặc giao với huy hiệu;
  3. ba trạm đều có sao và nhãn trạng thái;
  4. nét tiến độ ẩn khi tiến độ 0, và đầy đúng đoạn khi xong 1/2/3 môn;
  5. trạm hiện tại lớn hơn các trạm khác và là môn đầu tiên chưa xong;
  6. mở lại scene không nhân đôi UI.
- Chụp ảnh 16:9 và 4:3 theo `testing-unity-ui-with-screenshots` cho các trạng
  thái 0/1/2/3 môn hoàn thành rồi xem trực tiếp trước khi kết luận.

## Tiêu chí hoàn thành

1. Màn `Map` hiển thị bản đồ hành trình đúng như preview, ở cả bốn tỉ lệ trên.
2. Hành vi chọn môn, quay lại menu, số lượt và gameplay không đổi.
3. Mọi kiểm thử mới và hiện có (EditMode + PlayMode) pass.
4. `git diff --check` sạch; không đụng các thay đổi chưa commit không liên quan
   trong working tree.

## Rủi ro

- `Map.unity` là scene author sẵn: sửa builder mà không chạy lại authoring sẽ cho
  kết quả khác nhau giữa lần build mới và scene đã lưu.
- Preview mô phỏng trạng thái "Mới bắt đầu" với cả ba môn mở. Trong game thật
  hai môn sau đang khóa, nên ảnh chụp thật sẽ khác preview ở điểm này.
- Preview dùng emoji cho biểu tượng môn; bản Unity dùng `SportIconSprite`, nên
  cảm giác huy hiệu có thể khác và cần chụp ảnh để so lại.
