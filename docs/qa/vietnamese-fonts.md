# Font tiếng Việt — Thể Chất KMA

> Cập nhật 2026-10-01: scene FontTest, 16 material chỉ dùng cho scene này và công cụ tạo scene đã được gỡ. Các số liệu và ảnh FontTest bên dưới là bằng chứng lịch sử của đợt QA 2026-09-30.

## Khảo sát và phạm vi

Unity 6000.3.23f1; URP 17.3.0 / Renderer2D; cấu hình build Android ARM64, công cụ QA Android x86_64. TMP thuộc com.unity.ugui 2.0.0 (TMP Settings version 1.4.0). Khảo sát trước sửa: 8 scene, 9 prefab, 9 UI.Text, 51 TextMeshProUGUI trực tiếp, 0 TextMeshPro 3D; phát hiện thêm 1 TextMesh PLAYER. Có 22 script gán text (18 runtime, 4 Editor); không có hệ thống localization/CSV hiện hữu. MapNodeView đọc tên môn từ ScriptableObject; JSON phục vụ save.

Bốn TTF đã cung cấp trong Assets/Fonts đều là font tĩnh, không có bảng fvar: SairaCondensed-Black, BarlowSemiCondensed-Bold, BeVietnamPro-Regular, BeVietnamPro-Bold. Không dùng font thay thế.

## Thay đổi

- Setup Vietnamese Fonts tạo/reuse 4 TMP font, cấu hình default/fallback, giữ cảnh báo missing character. Saira/Barlow: Static, 90 pt, padding 9, SDFAA, 2048, Optimum. Be Vietnam Regular/Bold: Dynamic, multi atlas, 2048; giữ font nguồn trong build.
- Unity 6 dùng API packing/rendering nội bộ: wrapper reflection kiểm tra chữ ký chính xác từ package đã cài; lỗi rõ nếu Unity tương lai đổi API. Static atlas chứa các ký tự có trong font nguồn thuộc những dải yêu cầu, thêm mũi tên Sprint U+2190/U+2192.
- Title_KMA có gradient vàng, viền navy 0.15, underlay; Button_Primary viền 0.1; Secondary và Body không viền/bóng. Body_Bold riêng để khớp atlas Bold. Màu chữ nút hiện có được giữ theo lựa chọn của người dùng.
- Chuyển legacy trong YAML giữ fileID/GUID và RectTransform; cập nhật tham chiếu/script/factory sang TMP. PLAYER TextMesh chuyển sang TMP 3D giữ vị trí, màu vàng và sorting order.
- Typography gán font theo vai trò, extra padding, line spacing 15, Overflow, Auto Size nút. Bỏ hiệu ứng viền/shadow uGUI trùng với SDF.
- VietText.Fix chuẩn hóa NFC tại các đường gán text; input chuẩn hóa onEndEdit bằng SetTextWithoutNotify, tránh can thiệp composition IME.
- FontTest có 16 tổ hợp font/material, material test khớp texture atlas, input NFC, Camera/EventSystem. CoverageChecker quét scene/prefab, SO, file dữ liệu và string literal C#; các literal nguồn được phân biệt với text đã xác định font.

## Thống kê và kiểm tra

| Hạng mục | Kết quả |
|---|---:|
| UI.Text chuyển sang TMP UGUI | 9 |
| TextMesh chuyển sang TMP 3D | 1 |
| Scene có text được xử lý | 8 |
| Prefab có text được xử lý | 5 |
| Scene / prefab quét cuối | 9 / 9 |
| TMP text serialized quét, gồm prefab instances và FontTest | 218 |
| ScriptableObject / file dữ liệu / file C# quét | 30 / 8 / 161 |
| Ký tự thiếu trong UI/dữ liệu | 0 |
| Literal nguồn chưa được giải quyết | 0 |
| Component ID legacy UI giữ nguyên | 9/9 |
| RectTransform so với snapshot trước sửa | 194, 0 thay đổi |
| Setup lặp lại | 26 asset, cùng đường dẫn/GUID |

Chuỗi mẫu yêu cầu và mọi ký tự Việt dựng sẵn U+1EA0–U+1EF9 đạt trên cả 4 font, không cần fallback cho chuỗi mẫu. Những dải Unicode rộng chứa code point không có trong font nguồn (Saira 456, Barlow 537, Be Vietnam 579 mỗi weight, gồm cả code point chưa gán). Không thể tạo glyph mà TTF không có; đây không phải ký tự thiếu trong nội dung game hiện tại. Input ký tự ngoài phạm vi font vẫn có thể báo missing character, cảnh báo được giữ nguyên.

EditMode cuối: 556 tổng, 553 đạt, 0 lỗi, 3 bỏ qua có chủ đích do Punishment đã nghỉ dùng. PlayMode đầy đủ: 211 tổng, 209 đạt, 2 lỗi: AudioGameplayTests.AllPlayableScenesHaveOneListenerAndProduceAudioSamples (MG_Sprint RMS khoảng 1.27e-8, ngưỡng 1e-5), ScenePresentationContractTests.EveryExistingSceneHasS2CameraAndCanvas (Map chọn scaler 0.5 thay vì 1). Kiểm tra Canvas chạy riêng sau đó đạt 1/1; lỗi có dấu hiệu phụ thuộc thứ tự suite, chưa coi suite đầy đủ là đạt. Không thay đổi âm thanh hoặc làm yếu các assertion này. Các test font, coverage, NFC input và fallback TMP đã đạt. Unity biên dịch không lỗi; không thêm cảnh báo compiler trong các script mới.

Đã mở và xem PNG của Menu, Map, Sprint, Football, Volleyball và FontTest. Không thấy ô vuông hoặc dấu Việt bị cắt trong các ảnh này; FontTest đã sửa xuống dòng sau ảnh đầu. Bằng chứng: Builds/FontQA/*.png, coverage.json, layout-check.json và Builds/TestResults/viet-*.xml/log (artifact cục bộ, không commit).

## Cách sử dụng và việc thủ công

Menu Tools/KMA: Setup Vietnamese Fonts; Apply Vietnamese Fonts to Project; Check Vietnamese Font Coverage. Chạy coverage sau khi thêm nội dung localization mới. Script setup chạy lại không nhân bản asset.

Cần build và thử trên Android thật: Telex/VNI của bàn phím hệ điều hành, ký tự nhập mới, màn hình/safe area, GPU và multi atlas trong player build. Chưa thực hiện build thiết bị trong lượt này. Cần theo dõi hai lỗi full PlayMode kể trên trước khi coi toàn bộ suite xanh.

Không xóa font cũ. Các file cần tự quyết định sau khi rà tham chiếu (kể cả tools cũ): Assets/_Project/Fonts/Baloo2-ExtraBold.ttf, Nunito-Bold.ttf, NotoSansVietnamese.ttf; TMP assets Baloo2-ExtraBold.asset, Nunito-Bold.asset, VietnameseFallback.asset; Baloo2-ExtraBold-TextStrokeDark.mat và meta liên quan. Danh sách file tạo/sửa đầy đủ nằm trong vietnamese-font-files.txt.
