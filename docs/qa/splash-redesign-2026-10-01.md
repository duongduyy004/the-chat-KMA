# Splash Screen Thể Chất KMA — 01/10/2026

Giao diện được dựng khi chạy Bootstrap, theo cùng cách Main Menu dựng UI. Tham chiếu ảnh sân vận động trong Bootstrap được tái sử dụng. SceneRouter giữ nguyên luồng tải và phát tiến độ; presenter nội suy phần hiển thị, giữ điều kiện intro tối thiểu rồi fade 0,35 giây sang menu.

## File thay đổi

- `Assets/_Project/Scripts/UI/HomePresentationBuilder.cs`: cho splash gọi cùng bộ dựng badge và sprite bình hành; rasterize thanh theo kích thước thực để giữ góc và viền.
- `Assets/_Project/Scripts/UI/HomeMenuStyle.cs`: dùng chung cỡ badge và font tiêu đề.
- `Assets/_Project/Scripts/UI/HomeMenuResponsive.cs`: chia sẻ phép tính scale hiện có của menu để splash có tiêu đề lớn khoảng 1,3 lần menu sau khi scale.
- `Assets/_Project/Scripts/UI/UITheme.cs` và `Assets/_Project/Settings/UI/UITheme.asset`: cấu hình màu dùng chung, hình học splash, độ mờ, thời gian animation và smoothing.
- `Assets/_Project/Scripts/UI/SplashPresentationView.cs` (kèm `.meta`): nền Envelope Parent, gradient navy 70–85%, nội dung một trục trong Safe Area, badge 160, tiêu đề hai tầng −8°, ba vạch vàng, slogan trắng, thanh 460×20/viền 2 và footer phiên bản.
- `Assets/_Project/Scripts/UI/SplashScreenPresenter.cs`: cập nhật view, nội suy tiến độ bằng thời gian unscaled, phần trăm theo fill, fade khi tải hoàn tất.
- `Assets/Tests/PlayMode/Presentation/SplashLoadingFlowTests.cs`: kiểm tra nội suy khi timeScale=0, fade/input, các tỉ lệ, glyph tiếng Việt và Bootstrap/Menu.
- `Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs`: cập nhật kiểm tra slogan/gradient và dọn splash persistent giữa các fixture.

Typography dùng `VietTypography` cùng Font Asset, `Title_KMA`, `Body` và font HUD của menu. Slogan dùng Be Vietnam Pro Regular theo lựa chọn đã duyệt. Animation dùng Update/coroutine unscaled và `HomeBadgeShine` hiện có. Fill đỏ giữ cạnh nghiêng bằng cách trượt sprite bình hành trong Mask, không vượt viền. Các chuỗi TMP đi qua `VietText.Fix`.

## Kiểm tra hình ảnh

Đã chụp bằng Unity Editor GUI và xem từng PNG:

| Tỉ lệ | Kích thước PNG | Ảnh |
|---|---|---|
| 16:9 | 1920×1080 | [Splash](../../Builds/Screenshots/splash-redesign-16x9.png) |
| 16:10 | 1920×1200 | [Splash](../../Builds/Screenshots/splash-redesign-16x10.png) |
| 18:9 | 2160×1080 | [Splash](../../Builds/Screenshots/splash-redesign-18x9.png) |
| 4:3 | 1440×1080 | [Splash](../../Builds/Screenshots/splash-redesign-4x3.png) |

Nền phủ kín; badge, tiêu đề, slogan, thanh tải và footer đều trong khung. Dấu ở “Đang chuẩn bị...” và “Hành trình rèn luyện thể chất” hiển thị đúng. Fill nằm trong viền. Có ảnh [menu đối chiếu](../../Builds/Screenshots/splash-redesign-menu-reference.png). PNG nằm trong Builds, không được commit.

Công cụ screenshot và scene preview dùng trong QA đã được hoàn nguyên/xóa; cache font và thay đổi scene/prefab do test tự ghi không thuộc bản sửa.

## Kiểm tra tự động cuối

- [EditMode XML](../../Builds/TestResults/splash-final-editmode.xml): 560 test, 557 đạt, 0 lỗi, 3 bỏ qua vì luồng Punishment đã ngừng dùng. Các test bỏ qua: `Controller_ActivatesAuthoredCueAndCounterplayAdapter`, `Controller_RequestsRetryOnceWithoutChangingLivesOrMutatingTheSession`, `NonFiniteProgress_CannotAdvanceOrCompletePunishment`.
- [PlayMode XML](../../Builds/TestResults/splash-final-playmode.xml): 213 test, 212 đạt, 1 lỗi. Toàn bộ test splash và giao diện Festival đạt, bao gồm tiến độ khi timeScale=0, fade giữ chặn input, Bootstrap/Menu, bounds ở bốn tỉ lệ và glyph tiếng Việt.
- Test còn lỗi: `KMA.Tests.Gameplay.Core.AudioGameplayTests.AllPlayableScenesHaveOneListenerAndProduceAudioSamples`, tại MG_Sprint. Peak đo được khoảng `1.26e-8`, thấp hơn ngưỡng `1e-5`. Cần kiểm tra riêng âm thanh/runtime của Sprint; bản sửa splash không thay đổi code audio.
- Kết quả lấy từ XML của Unity Test Runner. Script shell trả mã 1 cả khi EditMode chỉ có test bị bỏ qua, nên số lỗi được xác nhận trực tiếp trong XML.
- Rà soát code độc lập không tìm thấy lỗi chặn trong phần splash.

## Cần kiểm tra thủ công

1. Khởi động lạnh trên thiết bị Android, quan sát fade-in 0,5 giây, KMA scale 0,9→1, shine và fade sang menu sau native Unity splash.
2. Thử thiết bị có notch/cutout và thanh điều hướng: logo, chữ, track và footer nằm trong Safe Area; nền vẫn phủ mép màn hình.
3. Quan sát tiến độ trên máy chậm, lúc kích hoạt scene bị giật và lúc app được đưa ra nền rồi quay lại. Thanh/ phần trăm chuyển mượt; menu nhận input sau fade.
4. Xác nhận footer phiên bản theo bản build thực và hiển thị stencil Mask trên GPU Android.

Ảnh tĩnh trong Editor và kiểm tra tự động chưa chứng minh các điều kiện thiết bị ở trên. Chưa build/export APK trong lượt này.
