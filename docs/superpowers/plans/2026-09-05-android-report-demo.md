# Android Report Demo Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task after design approval. Track steps with the checkboxes below.

**Goal:** Hoàn thiện luồng Bootstrap/Splash → Menu → Map → Sprint → Result, có artwork, sprite animation và APK được kiểm tra trên Android thật cho buổi báo cáo 08/09/2026.

**Architecture:** Giữ scene và quyền sở hữu gameplay/session hiện tại. Splash nằm trong Bootstrap; SceneRouter sở hữu việc tải scene và phát tiến độ cho UI. Nhân vật/background chỉ đọc trạng thái gameplay để trình bày.

**Tech Stack:** Unity 6000.3.23f1, C#, uGUI/TextMeshPro, URP 2D, Input System, Android ARM64/IL2CPP.

**Điều chỉnh khi thực thi — 06/09/2026:** Người dùng yêu cầu sử dụng emulator đang chạy. Gate demo chuyển sang Genymotion `127.0.0.1:6555`, API 35, x86_64; tạo APK emulator riêng, giữ APK ARM64 cho điện thoại. Kết quả emulator không được gọi là kiểm thử máy thật. Kích thước PNG sinh thực tế được giữ nguyên và ghi trong `docs/qa/android-report-demo.md`; coverage 25.6×10.8 world units được author bằng Unity. Tiến độ và bằng chứng thực tế ở tài liệu QA, không suy ra hoàn tất từ kế hoạch này.

**Spec:** Yêu cầu người dùng trong cuộc trao đổi 05/09/2026; PLAN.md; docs/superpowers/specs/2026-09-03-asset-pipeline-design.md. Đây là đề xuất mở rộng phạm vi thiết kế asset cũ: thêm background, tích hợp UI và animation đổi sprite. Không coi thiết kế cũ đã phê duyệt các mở rộng này.

## Global Constraints

- Tên sản phẩm mặc định giữ `Thể Chất KMA`; package `com.kma.thechat`.
- Unity 6000.3.23f1; Android ARM64, IL2CPP, minimum API 25, target API 35 theo cấu hình hiện tại. Kiểm tra lại manifest APK thực tế khi build.
- Landscape; UI reference 1920×1080; artwork phủ tối thiểu 2560×1080; safe area trên điện thoại thật.
- Giữ route, lưu tiến trình, xác nhận New Game, Continue và ba instance RivalRunner hiện tại.
- Tạo/sửa scene, prefab, animation clip và import settings bằng Unity Editor/API, giữ GUID và metadata; không viết tay YAML scene.
- Không triển khai Volleyball hay toàn bộ asset bảy môn trong gói demo này. Task 8–9 S1–S9 vẫn là công việc riêng; gate demo không thay thế gate đó.
- Không thay đổi các chỉnh sửa sẵn có trong .gitignore/.worktrees. Chỉ tạo tài liệu trong lượt lập kế hoạch.
- Chưa được cung cấp Slide 20_Module 4.1: nghiệm thu theo yêu cầu background chuyển động đã trích, chưa khẳng định đúng kỹ thuật cụ thể trong slide.

## Hướng thiết kế đề xuất

Chọn 2D cartoon phẳng, viền đen, màu theo UITheme, chủ đề sân chạy trường học. Logo chữ kết hợp biểu tượng đường chạy; dùng biểu tượng tự thiết kế cho trò chơi, không mặc định là huy hiệu chính thức của trường. Giữ text tiếng Việt bằng TMP để dễ sửa và không nhúng chữ vào background.

Ba hướng có thể thực hiện: (1) bộ nhân vật có frame đồng bộ + background/UI cùng palette, khuyến nghị vì ít rủi ro animation; (2) tự vẽ toàn bộ, đồng nhất hơn nhưng tốn thời gian; (3) tạo ảnh bằng AI cho background/intro, vẫn cần chỉnh các lớp và kiểm tra nối nền, không mặc định ảnh AI là sprite sheet dùng được ngay.

## Task 1: Chốt bộ hình và chạy thử APK nền — 2–3 giờ

**Files:** đọc ProjectSettings/ProjectSettings.asset, Assets/Editor/BuildScript.cs, Assets/_Project/Scripts/Core/GameManager.cs, Assets/_Project/Scripts/Shell/S5ShellSceneController.cs; tạo docs/qa/android-report-demo.md khi thực thi.

- [ ] Xác minh thành phần nào hiện chuyển Bootstrap sang Menu; chọn đúng một chủ sở hữu chuyển scene để splash không bị bỏ qua hoặc tải Menu hai lần.
- [ ] Chụp trạng thái Menu/Sprint ban đầu; ghi SHA và trạng thái Git vào QA.
- [ ] Chốt một bản phác splash, home, gameplay theo palette hiện có trước khi sản xuất cả bộ hình.
- [ ] Xác nhận điện thoại dùng demo: model, Android/API, ABI, độ phân giải, USB debugging. Cài APK nền mới build sớm để phát hiện vấn đề SDK, ký APK, đồ họa và input.
- [ ] Kiểm tra giấy phép bộ nhân vật dự định dùng trước khi nhập; ưu tiên nguồn đã được đề cập trong thiết kế asset, xác minh lại file license và các frame thực có. Ghi nguồn/giấy phép/chỉnh sửa trong CREDITS.md cùng lần thêm asset.

**Đạt:** có thiết bị mục tiêu và build nền cài/mở được, hoặc ghi rõ blocker thiết bị/build; có hướng hình ảnh thống nhất. Không đợi tới cuối mới thử Android.

## Task 2: Logo, icon và artwork — 4–6 giờ

**Files:** tạo Assets/_Project/Art/Brand/{GameLogo,AppIcon}.png; Art/UI/HomeIllustration.png; Art/Environments/Sprint/{Sky,Campus,Track}.png; cập nhật Assets/_Project/CREDITS.md và ProjectSettings/ProjectSettings.asset qua Editor.

- [ ] Logo nền trong suốt; icon nguồn vuông 1024×1024, chủ thể rõ khi thu nhỏ; cấu hình icon Android thích ứng và legacy qua Player Settings.
- [ ] Home/splash dùng chung bộ hình giới thiệu để giữ nhất quán; nút và chữ là UI riêng.
- [ ] Vẽ ba lớp trời, trường/khán đài, đường chạy, mỗi lớp phủ 2560×1080; lớp trước có alpha; cạnh trái/phải nối được khi lặp.
- [ ] Import Sprite, PPU 100 làm điểm xuất phát, Bilinear; kiểm tra kích thước world sau scale thực tế. Không giảm max texture size khiến artwork bị co ngoài ý muốn.
- [ ] Kiểm tra icon trên launcher, alpha viền, tiếng Việt, đọc logo ở kích thước điện thoại và nối nền khi ghép hai tile.

**Đạt:** không còn hình khối mặc định ở các mặt hình ảnh chính của splash/home/Sprint; có nguồn và file sản xuất để chỉnh lại.

## Task 3: Nhân vật và sprite animation — 4–6 giờ

**Files:** tạo Art/Characters/Runner/ với các frame idle/run/hit; tạo Prefabs/Gameplay/PlayerRunnerVisual.prefab và Scripts/Gameplay/Sprint/RunnerVisualPresenter.cs; cập nhật Animations/RivalRunner*.anim, RivalRunner.controller nếu cần, Prefabs/Gameplay/RivalRunner.prefab và Scenes/MG_Sprint.unity.

**Contract:** RunnerVisualPresenter chỉ đọc phase, tốc độ và kết quả của SprintController; không ghi distance/stamina, không điều khiển root gameplay. SpriteRenderer nằm ở Visual; đổi frame chỉ tác động Visual.

- [ ] Chuẩn hóa canvas và pivot ở chân cho mọi frame; tối thiểu 3 frame chạy khác nhau, 1 idle, 1 hit. Nếu bộ hình chỉ có 3 run frame, dùng nhịp 0–1–2–1 để chạy liền mạch.
- [ ] Thay ảnh ở player và prefab rival; phân biệt người chơi/ba AI bằng màu áo hoặc dấu nhận diện, tránh tint cả da không cần thiết.
- [ ] Tạo sprite curves cho Run/Burst; Burst tăng tốc phát. Idle có thở nhẹ; Stumble/Fail dùng hit; Celebrate dùng pose kết hợp chuyển động Visual. Giữ tên state/parameter đang được gameplay sử dụng.
- [ ] Giữ các transform curves có ích nhưng loại chuyển động trùng làm chân nhảy khỏi mặt đường; không animate root/collider.
- [ ] Thêm kiểm tra PlayMode tại Assets/Tests/PlayMode/Presentation/RunnerVisualTests.cs: frame đổi trong Play, Pause đứng hình, Restart về trạng thái đầu; việc animate không đổi snapshot gameplay; ba rival vẫn là prefab instance.
- [ ] Xem trực tiếp ở tốc độ thường/chậm; kiểm tra chân, pivot, thứ tự lane, hình không rung và player nhận diện rõ.

**Đạt:** người xem thấy chân/tay đổi tư thế khi chạy; không chỉ nảy một sprite tĩnh.

## Task 4: Tích hợp background parallax — 2–3 giờ

**Files:** cập nhật Scenes/MG_Sprint.unity, Scripts/Gameplay/Sprint/SprintParallax.cs chỉ nếu cần; dùng Art/Environments/Sprint/*.png.

- [ ] Gắn hai tile cho mỗi lớp vào các binding hiện có; tốc độ đề xuất trời 0.15, trường 0.4, đường chạy 1.0 lần quãng đường hiển thị.
- [ ] Đặt loopWidth bằng chiều rộng tile world thực tế sau import/scale, không mặc định mọi ảnh rộng 25.6 world units.
- [ ] Giữ cơ chế lấy Snapshot.Distance: background dừng khi gameplay dừng; kiểm tra reset origin khi chơi lại.
- [ ] Kiểm tra nhiều vòng lặp và quãng đường tăng lớn, không khe hở/nhảy tile; xem cả 16:9 và 20:9, không che nhân vật/HUD.

**Đạt:** ba lớp chuyển động khác tốc độ, nền kín màn hình, Pause/Restart hoạt động đúng. Đây là bằng chứng trực tiếp cho yêu cầu background chuyển động.

## Task 5: Splash, home và loading bar thật — 4–6 giờ

**Files:** cập nhật Scenes/Bootstrap.unity, Scenes/Menu.unity, Scripts/Core/SceneRouter.cs, Scripts/UI/SceneTransitionOverlay.cs; tạo Scripts/UI/SplashScreenPresenter.cs; cập nhật chủ sở hữu bootstrap đã xác định ở Task 1; giữ MainMenuScreen.cs và S5ShellSceneController.cs làm chủ các hành động menu hiện tại.

**Interfaces đề xuất:** SceneRouter bổ sung `event Action<float> SceneLoadProgressChanged`; giữ `SceneLoadStarted`/`SceneLoadCompleted`. Mọi đường tải Menu/gameplay cần cùng phát tiến độ. SplashScreenPresenter quản lý hiển thị và thời gian intro, không tự tạo GameSession mới.

- [ ] Tạo splash trong Bootstrap: logo, ảnh giới thiệu, dòng chuẩn bị và loading bar; intro tối thiểu khoảng 1.5 giây là thời gian trình bày, tách khỏi tiến độ tải thật.
- [ ] Khi tải async, ánh xạ `Mathf.Clamp01(operation.progress / 0.9f)` cho phần tải; dành bước hoàn tất/100% cho scene được kích hoạt. Không dùng timer giả làm phần trăm tải.
- [ ] Home có logo, ảnh nền và các nút hiện có; Việt hóa nhãn nhưng giữ tên GameObject được code dùng để tìm nút. Continue vẫn phụ thuộc save hợp lệ, New Game vẫn xác nhận.
- [ ] Bổ sung GraphicRaycaster/CanvasGroup phù hợp cho overlay để chặn input thật khi tải; cập nhật UI bằng unscaled time để tải từ Pause không treo.
- [ ] Kết thúc tải phải bỏ chặn input; load scene không hợp lệ phải báo lỗi và trả quyền thao tác, không để overlay treo hoặc session bị thay đổi một nửa.
- [ ] Thêm PlayMode coverage tại Assets/Tests/PlayMode/Presentation/SplashLoadingFlowTests.cs: Bootstrap tới Menu đúng một lần; không duplicate manager; double click không tạo hai transition; loading từ Pause; Continue và New Game không thay đổi ngữ nghĩa.
- [ ] Xem trực tiếp cold start, thanh tải, transition và nút trên điện thoại; không chỉ dựa test tự động.

**Đạt:** mở app thấy logo/intro/loading, sau đó có home dùng được; thanh tải phản ánh quá trình thực và không nhận tap xuyên overlay.

## Task 6: Build cuối, kiểm tra máy thật và bộ bằng chứng — 4–6 giờ

**Files:** dùng Assets/Editor/BuildScript.cs; tạo Builds/Android/kma-report.apk (artifact, không mặc định commit binary); ghi docs/qa/android-report-demo.md, cập nhật README.md với trạng thái đã xác minh.

- [ ] Chạy các test mới theo từng task; sau tích hợp chạy full EditMode/PlayMode, kiểm tra diff và thiếu script/reference. Ghi XML/log và SHA, không lấy số test cũ làm bằng chứng mới.
- [ ] Build APK ARM64 mới; kiểm tra manifest: package, label, min/target SDK, ABI, launcher; ghi SHA256 và dung lượng.
- [ ] Cài bằng `adb install -r`; nếu khác chữ ký, không tự gỡ app/xóa save: ghi lỗi và lựa chọn dùng khóa đúng hoặc xin phép reset dữ liệu demo.
- [ ] Chạy cold start → Splash → Menu → Map → Sprint → Result ít nhất 3 lượt; kiểm tra L/R luân phiên, multi-touch, tutorial, Pause/Resume/Restart/Exit và Continue sau mở lại.
- [ ] Chạy landscape cả hai hướng, safe area, chữ tiếng Việt, icon launcher; Home rồi quay lại ứng dụng; chơi lặp 10 phút để tìm crash/treo và nóng máy.
- [ ] Ghi FPS trên đúng thiết bị/công cụ đo: mục tiêu 60, ngưỡng demo ổn định tối thiểu 30 FPS; ghi cách đo và các đoạn giật. Không coi Editor FPS là Android FPS.
- [ ] Lưu ảnh splash/home/gameplay/result và video 60–90 giây trên chính APK cuối; giữ APK + hash + video dự phòng. Nếu chỉnh code/asset sau quay, build và kiểm tra lại phần ảnh hưởng.

**Đạt:** APK hash xác định chạy trên điện thoại đã ghi model/API/ABI; checklist có kết quả quan sát. Không có điện thoại thì task này chưa đạt, emulator không thay thế.

## Lệnh thực thi dự kiến

Chạy từ repository root; đóng Editor đang mở project trước batch run.

```bash
KMA_UNITY_EDITOR=/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-report-edit.xml -logFile /tmp/kma-report-edit.log
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-report-play.xml -logFile /tmp/kma-report-play.log
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -buildOutput Builds/Android/kma-report.apk -logFile /tmp/kma-report-build.log -quit
rtk proxy /home/duongduy/Android/Sdk/build-tools/35.0.0/aapt dump badging Builds/Android/kma-report.apk
rtk proxy sha256sum Builds/Android/kma-report.apk
rtk adb devices -l
rtk adb shell getprop ro.product.model
rtk adb shell getprop ro.build.version.sdk
rtk adb shell getprop ro.product.cpu.abilist
rtk adb install -r Builds/Android/kma-report.apk
rtk adb shell am start -n com.kma.thechat/com.unity3d.player.UnityPlayerGameActivity
rtk git diff --check
rtk git status --short
```

## Thứ tự và lịch dự kiến

Ước lượng 20–30 giờ làm việc, phụ thuộc sửa art và lỗi build/device, không phải cam kết thời gian.

- 05/09: Task 1, chốt hướng hình ảnh, thử thiết bị và bắt đầu Task 2.
- 06/09: hoàn tất Task 2–4, có Sprint với hình/animation thật.
- 07/09: Task 5–6; đóng băng bản demo cuối ngày, lưu video dự phòng.
- 08/09: chỉ kiểm tra mở app, pin/cáp, trình chiếu và diễn tập; tránh nâng Unity/thay dependency.

Nếu thiếu thời gian: giảm số frame phụ và hiệu ứng trang trí, tái sử dụng hình splash/home. Giữ đủ logo/intro, loading bar, home buttons, ít nhất một nhân vật đổi frame, background chuyển động và thử điện thoại thật. Không bắt đầu thêm minigame để bù cho phần trình bày còn thiếu.

## Kịch bản báo cáo 90 giây

0–15 giây: mở từ icon, trình bày splash/logo/intro/loading. 15–30 giây: home và nút, chọn Sprint qua Map. 30–70 giây: chạy, chỉ rõ ba lớp parallax và animation nhân vật/touch/HUD. 70–90 giây: Pause/Resume và Result nếu đã kết thúc lượt; nếu thời lượng lượt dài hơn, demo Result riêng sau đó, không sửa scoring chỉ để khớp video.

## Tự rà soát kế hoạch

- Logo/icon/intro: Task 2 và 5; background: Task 2 và 4; nhân vật/animation: Task 3; loading bar: Task 5; máy thật/APK: Task 1 và 6.
- Các path tạo mới là đề xuất, không khẳng định đã tồn tại. Asset pipeline cũ chỉ là nguồn thiết kế, không giả định generator đã được triển khai.
- Cần người dùng cung cấp điện thoại có thể kết nối khi thực thi; nội dung Slide 20 cần đối chiếu nếu được cung cấp.
- Kế hoạch chưa triển khai code, tạo artwork, build APK mới hoặc chứng nhận thiết bị.
