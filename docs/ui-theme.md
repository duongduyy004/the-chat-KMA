# UI theme

Chỉnh `Assets/_Project/Settings/UI/UITheme.asset` trong Inspector. `UiKitAssets` trong Resources giữ tham chiếu tới asset này để UI tạo bằng code và các prefab đều dùng được trong bản build.

- Palette chung: Primary, Accent, Surface, Text Primary, Scrim. Menu và map có nhóm màu riêng trong cùng asset để giữ diện mạo hiện tại.
- Corner Radius: bán kính panel; nút dùng 1.5 lần, nút pause dùng 5/6 lần, card map dùng 2/3 lần.
- Border Width: viền gốc; UI Kit/card map dùng 3/4 lần, nút menu dùng 1/2 lần.
- Menu: góc tiêu đề, góc nghiêng nút và độ co khi nhấn.
- Motion: thời gian phản hồi nút, fade, các bước hiện kết quả và chu kỳ ánh sáng badge. Animation dùng thời gian unscaled để hoạt động khi pause.

Các màn kế thừa `ScreenBase` tự lấy `UITheme.Shared` khi chưa gán Theme trong Inspector. UI Kit và các builder dùng asset chung; không tạo thêm bản sao theme cho từng scene. Giá trị được áp dụng khi dựng màn; kết quả áp dụng lại khi `Show`. Sau khi chỉnh asset, mở lại màn hoặc chạy lại Play Mode để kiểm tra toàn bộ giao diện. Không cần bake lại prefab kết quả.

Gameplay, điều hướng và vùng nhận input không thuộc theme.
