# Menu bắt đầu — 2026-09-26

## Hành vi

- Menu chính có đúng bốn nút theo thứ tự: Tiếp tục, Chơi mới, Cài đặt, Thoát.
- Tiếp tục chỉ bật khi tải được một bản lưu hành trình hợp lệ; khôi phục qua SceneRouter.ResumeCampaign.
- Chơi mới vào Map ngay khi chưa có hành trình; nếu có thì yêu cầu xác nhận thay thế. Hủy giữ nguyên tiến trình. Reset giữ cài đặt và cờ hướng dẫn hiện có.
- Cài đặt có hai thanh âm lượng nhạc/hiệu ứng, công tắc rung và nút quay lại. Giá trị áp dụng và lưu ngay.
- Thoát gọi Application.Quit trên bản build.

## Lưu dữ liệu

Trường bổ sung `settingsOnly` phân biệt bản lưu tùy chọn trước lượt chơi đầu tiên. Không tăng phiên bản save vì trường này tương thích bổ sung: bản lưu cũ thiếu trường được hiểu là một hành trình. GameManager chỉ bật trạng thái hành trình khi router thay đổi phiên chơi; lưu cài đặt hoặc tạm dừng ứng dụng không tự tạo hành trình. SaveSystem ghi nhận lần tải hợp lệ để file hỏng không bật Tiếp tục.

## Kiểm thử

- Trước khi sửa: nhóm test menu 46 đạt / 5 lỗi, đúng các hành vi chưa có.
- Sau khi sửa: nhóm test menu 51/51 đạt.
- Toàn bộ EditMode: 470 đạt, 0 lỗi, 3 bỏ qua (các test ChallengeSequence/Punishment đã có Ignore từ trước).
- Toàn bộ PlayMode: 195/195 đạt, gồm bản lưu chỉ có cài đặt, mở lại ứng dụng, chơi mới, tiếp tục, điều khiển cài đặt và gameplay.
- Kết quả: `Builds/TestResults/menu-red.xml`, `menu-green.xml`, `menu-all-edit.xml`, `menu-all-play.xml`.
- Review độc lập phạm vi menu/lưu dữ liệu: không có phát hiện cần sửa.

## Kiểm tra giao diện Editor

- Đã mở và xem trực tiếp `Builds/Screenshots/menu-four-buttons.png`: bốn nút đúng thứ tự, không chồng lấn/cắt chữ.
- `Builds/Screenshots/menu-settings.png`: cài đặt hiển thị đầy đủ; con trỏ qua EventSystem/raycast mở được màn hình, chỉnh nhạc ~24%, hiệu ứng 50%, tắt rung. Log: `menu-pointer-result.txt`, `menu-pointer-hits.txt` cùng thư mục.
- Công cụ QA tạm cần chờ màn cài đặt render xong trước khi bấm; lượt bấm ngay trong khung hình mở màn hình chưa có raycast hợp lệ. Lượt chạy có chờ render đã xác nhận thao tác thành công.
- Đã xem `Builds/Screenshots/menu-new-game-confirm.png`: hộp xác nhận và hai nút rõ, nằm trên lớp phủ che menu. Hộp xác nhận dùng trạng thái có hành trình được thiết lập riêng cho preview, không ghi đè save thật; hành vi lưu/reset được xác minh bằng PlayMode tests.
- Chưa kiểm tra bản Android hoặc hành vi đóng ứng dụng trên thiết bị trong lần thay đổi này.
