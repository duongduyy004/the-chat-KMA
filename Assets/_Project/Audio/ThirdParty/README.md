# Audio assets — tải ngày 2026-09-26

Các file nằm trong `Assets/_Project/Audio/ThirdParty/`. Đã có cấu hình tích hợp tại `Assets/_Project/Resources/GameAudioLibrary.asset`. Bản gốc được giữ nguyên; các cue ngắn nằm trong `Assets/_Project/Audio/Prepared/`.

## Đã tải

| Thư mục/file | Nội dung | Giấy phép |
|---|---|---|
| Kenney/interface-sounds | 100 hiệu ứng OGG | CC0 |
| Kenney/music-jingles | 85 jingle + Preview.ogg (file nghe thử, không dùng làm cue) | CC0 |
| Kenney/impact-sounds | 130 hiệu ứng OGG | CC0 |
| KevinMacLeod/Move Forward.mp3 | Menu/Map | CC BY 4.0 |
| KevinMacLeod/Monkeys Spinning Monkeys.mp3 | Nhạc menu thay thế | CC BY 4.0 |
| KevinMacLeod/Cipher2.mp3 | Chạy nhanh | CC BY 4.0 |
| KevinMacLeod/Beachfront Celebration.mp3 | Bóng chuyền | CC BY 4.0 |
| KevinMacLeod/Winner Winner.mp3 | Bóng đá | CC BY 4.0 |

Giữ nguyên âm thanh gốc. Kenney có `License.txt` đi kèm từng pack; nhạc có `KevinMacLeod/CREDITS.txt`. Nguồn và link tải chính xác nằm trong `download-manifest.json`. Các ZIP gốc được kiểm tra CRC khi giải nén. `audio-validation.json` ghi thời lượng giải mã, sample rate, số kênh và SHA-256 của từng file. Kiểm tra giải mã không thay thế nghe thử hoặc kiểm tra trong Unity.

## Freesound — đã được người dùng tải đủ và kiểm tra

| Asset | Trang tải | Tên file lưu đề xuất | Giấy phép |
|---|---|---|---|
| Referee whistle | [Referee whistle](https://freesound.org/people/Rosa-Orenes256/sounds/538422/) | `referee-whistle.wav` | CC0 |
| Crowd Cheer | [Crowd Cheer](https://freesound.org/people/FoolBoyMedia/sounds/397434/) | `crowd-cheer.wav` | CC0 |
| Running footsteps | [Running footsteps](https://freesound.org/people/ralph.whitehead/sounds/565708/) | `running-footsteps.wav` | CC0 |
| Volleyball Hit | [Volleyball Hit](https://freesound.org/people/designerschoice/sounds/845535/) | `volleyball-hit.wav` | CC BY 4.0 |
| Sand footsteps | [Sand footsteps](https://freesound.org/people/fthgurdy/sounds/528948/) | `sand-footsteps.wav` | CC0 |
| SoccerBallKick | [SoccerBallKick](https://freesound.org/people/purchasing102/sounds/521825/) | `soccer-ball-kick.wav` | CC0 |

Các WAV hiện nằm trực tiếp trong thư mục ThirdParty. `running-footsteps.wav` đã được thay đúng bản 33,5 giây / 48 kHz / 24-bit; không trùng bản bước chân trên cát.

### Các bước tải lại khi cần

1. Mở một link trong bảng. Chọn **Login to download**.
2. Đăng nhập Freesound; nếu chưa có tài khoản, chọn **Join now**, đăng ký và xác minh email nếu được yêu cầu.
3. Quay lại trang âm thanh và chọn **Download** để lấy file WAV gốc. Không dùng thao tác lưu trang web hoặc lưu waveform.
4. Chép các file vào `Assets/_Project/Audio/ThirdParty/` và đổi tên đúng theo bảng để công cụ chuẩn bị âm thanh nhận diện được.
5. Với **Volleyball Hit**, lưu attribution của Nicholas Judy / designerschoice, URL nguồn và link https://creativecommons.org/licenses/by/4.0/ vào Credits. Trang riêng của file ghi CC BY 4.0, dù mô tả pack có câu không bắt buộc ghi công; ưu tiên giấy phép của file.
6. Năm file còn lại ghi CC0; nên giữ tên tác giả/nguồn để truy xuất sau này.

### Ghi chú khi dùng

- Chưa tải preview Freesound thay cho WAV gốc.
- Footsteps Running On Concrete là bước chạy trên bê tông, cần cắt từng bước và kiểm tra phù hợp với mặt sân.
- Footsteps on sand and gravel có cả cát/sỏi, cần chọn đoạn thích hợp.
- SoccerBallKick thực chất là 10 mẫu bóng rơi xuống sàn gỗ; cần biên tập/nghe thử trước khi dùng làm tiếng sút.
- Xem `docs/qa/audio-integration.md` để biết phạm vi kiểm tra tích hợp. Chưa có xác nhận nghe thử trên loa thiết bị Android.
