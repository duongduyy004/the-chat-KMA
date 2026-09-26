# Football: góc nhìn goal và đường bóng vật lý

Người dùng đã duyệt preview `docs/proposals/football-goal-view.html` và yêu cầu triển khai vào game ngày 2026-09-26.

- Camera nhìn từ sau cầu thủ về goal, cầu thủ lệch trái; bố cục theo preview.
- Slider ngang hướng -1..1, đổi góc bằng atan(direction * 4.3 / 11). Không AIM tự chạy/khóa hướng riêng.
- Giữ SÚT: lực dao động cosine 0..1; chỉ trong Charging mới hiện quỹ đạo và tâm ngắm. Thả: ẩn preview và sút. Hủy, pause, mất focus, disable: về Aiming, không mất lượt; ẩn preview ngay.
- Tốc độ 6 + 20 * power m/s, góc nâng 22 độ, trọng lực 9.81 m/s²; bóng bán kính .11 m, goal cách 11 m, rộng 7.32 m, cao 2.44 m. Fixed step 1/240 s, trình diễn thời gian .8x như preview. Không random lệch bóng.
- Nảy/lăn mặt sân, cột/xà bật bóng, thủ môn phản ứng trễ và chỉ cứu khi chạm bóng; goal khi toàn bộ bóng qua vạch trong goal. Cùng hướng/lực cho cùng quỹ đạo.
- Preview dùng cùng solver nhưng bỏ thủ môn, chỉ khi giữ SÚT. Chọn hướng trước khi giữ, khóa slider trong Charging/Flying.
- 5 lượt, ít nhất 3 bàn qua môn; giữ scoring 6/8/10, route, mạng, retry và save hiện hành.
- Dùng artwork vector của preview làm nguồn cho sprite nhìn thẳng; giữ UI có nguồn gốc đã có. Không cần dịch vụ tải lúc chơi.
- Normal giữ thông số vật lý như preview; Easy/Hard chỉ thay tốc độ nạp lực và phản ứng/tốc độ thủ môn. Không sửa môn khác.
- Scene/prefab/reference phải lưu trên đĩa. Xác minh EditMode, PlayMode, screenshot được xem thực tế; không suy ra Android QA từ ảnh Editor.
