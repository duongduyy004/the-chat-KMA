# Tích hợp âm thanh — 2026-09-26

## Phạm vi

| Màn chơi | Nhạc nền | Hiệu ứng riêng |
|---|---|---|
| Menu / Map | Move Forward | Click nút, kể cả nút được tạo động |
| Sprint | Cipher | 4 biến thể bước chạy theo quãng đường |
| Volleyball | Beachfront Celebration | 4 bước trên cát, chạm bóng, được/mất điểm |
| Football | Winner Winner | 3 tiếng sút, thủ môn cản, cột/xà, bàn thắng hoặc trượt |

Hiệu ứng chung gồm đếm ngược, còi bắt đầu, thắng/thua và cổ vũ. Tổng cộng có 14 loại cue; mỗi loại có thể có nhiều clip. Music/SFX dùng hai nhóm mixer riêng và thiết lập âm lượng hiện có. Nhạc tiếp tục cùng vị trí khi đi Menu → Map, đổi bài theo môn. Pause tạm dừng nhạc và dừng hiệu ứng gameplay; click menu pause vẫn phát. Nhạc giảm nhẹ khi có kết quả/cổ vũ. Pool giới hạn 8 tiếng gameplay đồng thời, với cooldown từng cue để tránh phát dồn.

Các hook chỉ quan sát trạng thái gameplay; không thay đổi luật, input hoặc kết quả. AudioManager tự khởi tạo khi mở riêng scene và cung cấp AudioListener dự phòng khi scene chưa có listener.

## Asset và cấu hình

- Thư viện: `Assets/_Project/Resources/GameAudioLibrary.asset` — 4 bài nhạc và 14 loại cue.
- Nguồn: `Assets/_Project/Audio/ThirdParty/`, 327 file âm thanh đã giải mã và ghi SHA-256 trong `audio-validation.json`. Các bản gốc được giữ nguyên.
- Clip đã biên tập: `Assets/_Project/Audio/Prepared/`, 14 WAV mono, peak tối đa khoảng -6 dBFS, fade ở hai đầu. `provenance.json` lưu hash nguồn, khoảng cắt và hệ số gain.
- Tạo lại clip: `rtk proxy python3 tools/prepare-audio.py` (cần thư viện hệ thống libsndfile).
- Sau đó chọn **KMA → Audio → Configure Downloaded Audio** trong Unity để cập nhật thư viện và importer. Nhạc dùng Streaming/Vorbis; SFX dùng PCM/DecompressOnLoad, mono.
- Attribution trong `Assets/_Project/CREDITS.md`; bản đóng gói cùng ứng dụng nằm tại `Assets/StreamingAssets/AudioCredits.txt`. Link nguồn và giấy phép nằm trong manifest/README của ThirdParty.

## Kiểm tra

- 12/12 bài kiểm tra âm thanh PlayMode đạt trong `Builds/TestResults/audio-final-focused.xml`.
- Kiểm tra bao gồm route nhạc, giữ vị trí Menu → Map, pause/resume, cooldown, âm lượng hai nhóm độc lập, dọn tiếng khi đổi scene, hủy đăng ký sự kiện, click nút đóng chính nó và click ngay khi mở pause.
- Kiểm tra gameplay dùng scene thật: bước chạy chỉ khi di chuyển, bóng chuyền chỉ phát hit khi đánh, bóng đá phát một lần cho cú sút và một lần cho kết quả.
- Mỗi scene minigame có đúng một listener hoạt động. `AudioListener.GetOutputData` trong cửa sổ 2 giây đo được peak khác 0: Sprint 0.3943, Volleyball 0.2180, Football 0.1529. Cipher có khoảng 0,8 giây đầu im lặng trong bản gốc.
- Toàn bộ EditMode: 470 đạt, 0 lỗi, 3 bỏ qua trên 473 bài (`audio-full-edit.xml`). Ba bài bỏ qua thuộc Punishment đã ngừng sử dụng; script trả exit 1 vì kết quả gốc NUnit là `Skipped:Ignored`, không phải vì có test thất bại.
- Toàn bộ PlayMode: 207/207 đạt, 0 lỗi, 0 bỏ qua (`Builds/TestResults/audio-full-play.xml`), bao gồm 12 bài kiểm tra âm thanh.

Hai regression về click nút bị ẩn sau handler và click bị cắt khi mở pause đã được tái hiện bằng test thất bại trước khi sửa. Kiểm tra listener cũng thất bại trước khi bổ sung listener dự phòng.

## Giới hạn

Đã kiểm tra import, logic phát tiếng và dữ liệu đầu ra trong Unity Editor. Chưa build APK cho thay đổi này, chưa nghe thử trên loa/tai nghe Android; kiểm tra tín hiệu không đánh giá chủ quan độ hợp tai hoặc chất lượng vòng lặp. Tiếng sút được cắt từ nguồn bóng rơi trên sàn gỗ, cần nghe thử trong game trước khi chốt bản phát hành.
