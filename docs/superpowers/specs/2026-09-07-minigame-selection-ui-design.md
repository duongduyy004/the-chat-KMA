# Design — Sửa giao diện chọn minigame

Ngày: 2026-09-07  
Trạng thái: đã được người dùng duyệt sau bước investigate.

## Vấn đề

`Map.unity` đang chứa presentation của minigame (`S2_HUD_Minigame`,
`S2_PhaseOverlay`, `S2_ResultPanel`) dù không có `MinigameBase`. Trạng thái mặc
định của các prefab này làm tutorial Sprint và HUD xuất hiện trên màn hình chọn
minigame. Đồng thời `S5ShellSceneController` tạo node bằng `Image`/`Text` chưa
cấu hình font, màu và anchor, nên kết quả là các khối trắng đặt bằng tọa độ cố
định như ảnh lỗi.

## Thiết kế đã duyệt

- `Map` là shell scene: có camera, canvas, safe area và event system nhưng không
  có HUD, phase overlay, result panel hay pause UI của gameplay.
- Màn hình có tiêu đề `CHỌN MÔN THI`, bộ đếm `LƯỢT: n/5`, grid responsive và
  nút boss tách riêng.
- Ba môn hiện có gameplay hoàn chỉnh — Sprint, Endurance, Volleyball — ở trạng
  thái `SẴN SÀNG` và có thể chọn.
- Basketball, PingPong, Badminton và Football hiển thị `ĐANG PHÁT TRIỂN`, dùng
  màu muted và không tương tác. PushUps, Rhythm và Swimming tiếp tục là nội dung
  sắp ra mắt, không tạo route campaign.
- Node đã hoàn thành hiển thị rank và số sao; boss chỉ tương tác khi
  `GameSession.BossUnlocked`.
- UI dùng uGUI hiện có, `LegacyRuntime.ttf`, màu từ `UITheme`, layout group và
  safe-area anchors; không thêm package hoặc bitmap mới.
- Thay đổi phải có PlayMode regression test tái hiện lỗi chồng lớp và kiểm tra
  trạng thái node. Không thay đổi rules engine hoặc hợp đồng route.

## Phạm vi file

- Tách việc dựng Map khỏi `S5ShellSceneController` sang một builder chuyên trách.
- Bổ sung trạng thái có thể quan sát/kiểm thử cho `MapNodeView` mà không phá API
  `Configure`/`Bind` hiện tại.
- Sửa `MinigameUIAssembler` để shell scene Map không nhận presentation gameplay,
  rồi chạy assembler để sửa `Map.unity` hiện tại.
- Mở rộng `S5NewGameTests` bằng kiểm thử scene thật và kiểm thử trạng thái node.

## Tiêu chí hoàn thành

1. Load `Map` không có component `MinigameHUD`, `PhaseOverlay`, `ResultPanel` hay
   text tutorial/play đang active.
2. `S5MapPresentation` phủ safe area, có tiêu đề, counter lượt và grid không dùng
   tọa độ node tuyệt đối.
3. Chính xác ba node production tương tác được; bốn node placeholder bị khóa và
   có nhãn tiếng Việt; boss phản ánh đúng trạng thái session.
4. Test Map tập trung, test presentation liên quan và toàn bộ PlayMode đều pass.
5. `git diff --check` sạch và không đụng các thay đổi `.worktrees` có sẵn.
