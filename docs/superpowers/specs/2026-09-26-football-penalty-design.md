# Thiết kế Football: sút penalty 2D

Ngày: 2026-09-26

Trạng thái: thiết kế trong hội thoại đã được duyệt; bản spec này chờ người dùng duyệt trước khi lập kế hoạch triển khai.

## 1. Mục tiêu và phạm vi đã thống nhất

Thay Football hiện tại của The Chat KMA bằng minigame penalty 2D offline trên Android, landscape 16:9, phong cách cartoon sáng. Người chơi điều khiển cầu thủ sút vào khung thành có thủ môn AI. Mỗi trận luôn có đúng năm lượt; từ ba bàn thắng trở lên là thắng.

Người dùng đã chọn tích hợp vào game hiện tại và duyệt thiết kế AIM/SHOOT, thủ môn phản ứng sau cú sút, vùng lực rủi ro trên 85%, cùng retry có trừ mạng theo luật KMA.

- Giữ `SubjectId.Football = 6`, tên môn Bóng đá và scene `MG_Football`.
- Bật Bóng đá trên Map; kết quả đi qua hệ thống tiến trình và lưu game hiện có.
- Có màn bắt đầu/chọn độ khó, hướng dẫn ngắn, kết quả từng cú sút, thắng/thua và retry.
- Không có đội bóng, chuyền bóng, di chuyển cầu thủ tự do, đối kháng nhiều người hoặc kết nối mạng.
- Không có thao tác swipe, chọn kiểu sút, spin hoặc giới hạn thời gian trận. Chờ AIM hay SHOOT không tự mất lượt.
- Thay đổi tập trung vào Football và phần tích hợp cần thiết; giữ hành vi Sprint và Volleyball.

## 2. Hiện trạng và phương án kiến trúc

Checkout hiện tại có `FootballRules`, `GKPatternSet` và test cho cơ chế cũ: placement/force/spin, năm mẫu thủ môn định sẵn và giới hạn 30 giây. `MG_Football` chứa placeholder; `MapPresentationBuilder` đặt Football chưa chơi được. `Football.asset` còn mô tả vai trò cản phá, trái với minigame mới.

`MinigameBase.Completed` được `SceneRouter` nối vào màn kết quả. `GameSession.SubmitResult` lưu thành tích khi thắng hoặc trừ một mạng khi thua; thất bại còn mạng trở về Map, hết mạng tới Game Over. `SceneRouter` hiện bỏ qua nội dung action của màn kết quả nên chưa hỗ trợ retry trực tiếp.

Chọn mô phỏng penalty bằng logic thuần và trình diễn sprite theo cùng trạng thái mô phỏng. Phương án Rigidbody2D không được chọn vì trò chơi chỉ cần một đường bóng và một vùng cản phá; việc cân chỉnh va chạm vật lý không đem lại lợi ích tương ứng.

| Thành phần | Trách nhiệm và phụ thuộc |
| --- | --- |
| `FootballRules` | Thay luật cũ bằng trạng thái năm lượt, aim, power, shot, keeper, outcome và điểm trận; không phụ thuộc scene hoặc UI. |
| `FootballDifficultyConfig` | Chứa ba bộ tham số được serialize để cân chỉnh trong Inspector. Khi bắt đầu trận, rules nhận bản giá trị đã kiểm tra hợp lệ. |
| `FootballController : MinigameBase` | Nối vòng đời chung, input, rules, view và kết quả. Chỉ phát `Completed` một lần sau lượt thứ năm. |
| `FootballInputBridge` | Phát ý định AIM, bắt đầu giữ SHOOT, thả SHOOT và hủy giữ; quản lý pointer đang giữ. |
| `FootballPresentation` | Vẽ sân, bóng, cầu thủ, thủ môn và animation theo dữ liệu rules; không tự quyết định GOAL/SAVED/MISS. |
| `FootballHud` và màn kết quả Football | Hiển thị hướng dẫn, difficulty, power, điểm, lượt, start, win/lose và action kết quả. |
| `SceneRouter` / `GameSession` | Ghi nhận kết quả và khởi tạo lần chơi tiếp theo có trừ mạng đúng một lần; giữ đường xử lý mặc định cho môn khác. |

Thay các test của cơ chế Football cũ bằng test hợp đồng mới. Chỉ gỡ kiểu/helper Football cũ sau khi kiểm tra nơi sử dụng; không xóa các thành phần bóng dùng chung của môn khác. Đánh dấu kế hoạch Football ngày 2026-08-25 đã được spec này thay thế, cập nhật README và mô tả môn hiện hành; không sửa hàng loạt tài liệu lịch sử.

## 3. Luồng trận và điều khiển

Luồng bên trong Football: `Start -> Aiming -> AimLocked -> Charging -> Kicking -> Flying -> ShotResult -> Aiming`, hoặc `MatchResult` sau lần thứ năm.

1. **Start:** hiển thị hướng dẫn “AIM để khóa hướng, giữ SHOOT và thả để sút”, chọn Easy/Normal/Hard; mặc định Normal. Nút bắt đầu mở tutorial gate và dùng countdown chung một lần cho trận.
2. **Aiming:** tâm di chuyển qua lại tuyến tính, phản xạ tại hai biên. SHOOT bị vô hiệu hóa. Mỗi lượt tâm bắt đầu ở giữa và đi sang phải.
3. **AimLocked:** bấm AIM chụp vị trí hiện tại; tâm đổi sang màu vàng, hiện biểu tượng khóa. AIM bị vô hiệu hóa tới lượt tiếp theo; SHOOT được bật.
4. **Charging:** pointer-down SHOOT bắt đầu từ 0%; lực chạy tuyến tính 0–100–0 liên tục khi giữ. Pointer-up của đúng ngón tay đang giữ chụp lực và tạo đúng một cú sút.
5. **Kicking:** động tác đá kéo dài 0,18 giây; hướng, lực, sai lệch và tham số thủ môn đã được chụp, bóng chưa rời chân. Cả hai nút bị vô hiệu hóa.
6. **Flying:** bóng rời chân; bắt đầu đồng hồ phản ứng thủ môn. Mô phỏng và hình ảnh dùng chung thời gian cú sút.
7. **ShotResult:** tăng số lượt đã đá đúng một lần, hiện GOAL/SAVED/MISS trong 1 giây, cập nhật dấu lượt và điểm. Sau đó reset cầu thủ, bóng, thủ môn và input cho lượt mới.
8. **MatchResult:** sau feedback lượt thứ năm, phát kết quả trận và mở màn thắng/thua. Không kết thúc sớm khi đã ghi ba bàn hoặc không còn khả năng thắng.

Không chấp nhận SHOOT trước khi khóa AIM, AIM khi đang giữ lực, input trong animation, hoặc hai pointer cùng điều khiển SHOOT. Thả ngón tay ngoài vùng nút vẫn sút nếu pointer đó đã bắt đầu giữ trong nút.

Khi hệ điều hành hủy touch, app mất focus hoặc bị pause: hủy lần giữ đang diễn ra, giữ hướng khóa và trở về `AimLocked` với lực 0; không tự sút hay mất lượt. Nếu đã đá, tạm dừng rồi tiếp tục cùng mô phỏng khi app hoạt động lại; không bù thời gian chạy nền.

## 4. Mô hình cú sút và thủ môn

### Không gian và lực

Mặt phẳng khung thành dùng tọa độ chuẩn hóa: trái `x=-1`, phải `x=1`, dưới `y=0`, trên `y=1`. Tâm chạy trong `x=[-0,9; 0,9]`, ở `y=0,45`. Bán kính bóng tại mặt phẳng khung thành là 0,04 theo đơn vị nửa chiều rộng khung thành; render và kiểm tra ngang cùng dùng giá trị này. Tỷ lệ chiều dọc của sân là phần trình diễn, không tạo thêm thao tác ngắm dọc.

Lực `p` nằm trong `[0; 1]`:

- `p < 0,12`: MISS vì bóng không tới khung thành; chạy một animation bóng lăn hụt trong 0,9 giây, không kiểm tra save.
- `p >= 0,12`: thời gian tới khung thành `T = Lerp(0,90; 0,40; (p - 0,12) / 0,88)` giây. Lực tăng luôn làm giảm thời gian bay.
- `p <= 0,85`: điểm tới ngang chính xác bằng vị trí AIM đã khóa.
- `p > 0,85`: cộng sai lệch đều trong `[-e; e]`, với `e = 0,30 * (p - 0,85) / 0,15`. Không clamp điểm tới trở lại khung thành.

Sai lệch được lấy mẫu đúng một lần lúc thả SHOOT bằng nguồn random có thể inject seed cho test. Không lấy mẫu lại theo frame. UI tô đỏ vùng trên 85% và ghi “Lực cao dễ lệch”; lực cao không mặc định biến thành MISS.

Đường bóng dùng nội suy từ chân cầu thủ tới điểm tới, thêm cung cong nhẹ, bóng thu nhỏ theo chiều sâu và có bóng đổ. Điểm tới và thời điểm bóng chạm mặt phẳng khung thành luôn khớp rules. Vệt chuyển động và độ nảy là trình diễn.

### Quyết định thủ môn và kết quả

Thủ môn bắt đầu mỗi lượt tại `x=0`. Khi bóng rời chân, chờ reaction delay rồi di chuyển về điểm tới thực tế với tốc độ hữu hạn. Với bài toán một cú sút cố định, đây là AI đuổi mục tiêu có độ trễ; không đổi kết quả bằng tỷ lệ save ngẫu nhiên và không phản ứng trước lúc bóng rời chân.

Tại thời điểm `t`, vị trí thủ môn được tính từ thời gian tuyệt đối của cú sút: tiến từ 0 về mục tiêu một đoạn tối đa `speed * Max(0; t - reactionDelay)`. Tâm thủ môn được giới hạn trong `[-0,82; 0,82]`; tầm với ngang là 0,18. Sprite dùng cùng vị trí tâm, nghiêng/ngả theo hướng di chuyển và có tay hướng về bóng.

Tại `t=T`, phân loại theo thứ tự:

1. Nếu `Abs(targetX) + 0,04 > 1`: MISS. Bóng lấn ra ngoài lòng khung hoặc vào vùng cột được tính là MISS; không mô phỏng bật cột quay vào lưới.
2. Nếu bóng nằm trọn trong khung và `Abs(targetX - keeperX(T)) <= 0,18 + 0,04`: SAVED. Điểm bắt/chạm bóng phải nằm trong vùng tay với được khi render.
3. Các trường hợp còn lại: GOAL, bóng đi vào lưới và lưới nhấp nháy/nảy nhẹ.

Tại biên khung đúng bằng 1, bóng được coi là trong khung; tại biên tầm với đúng bằng tổng bán kính, thủ môn cản được. Kết quả cập nhật đúng một lần ngay cả khi một frame vượt qua mốc T.

### Tham số khởi đầu để cân chỉnh

| Tham số | Easy | Normal | Hard |
| --- | ---: | ---: | ---: |
| Thời gian tâm đi từ biên trái tới biên phải | 2,4 s | 1,7 s | 1,1 s |
| Thời gian lực tăng 0% tới 100% | 1,4 s | 1,4 s | 0,9 s |
| Độ trễ thủ môn sau lúc bóng rời chân | 0,40 s | 0,27 s | 0,15 s |
| Tốc độ thủ môn, đơn vị nửa chiều rộng khung/giây | 0,75 | 1,20 | 1,70 |

Lực giảm dùng cùng thời gian với lực tăng. Tầm với, luật năm lượt, ngưỡng thắng và giới hạn 85% giống nhau ở ba độ khó. Các thông số trên là giá trị ban đầu, được cân chỉnh sau playtest trong quan hệ thứ tự này. Góc sút tốt với lực dưới vùng đỏ phải có khả năng ghi bàn ở cả ba mức.

## 5. Giao diện, asset và animation

- Thiết kế tham chiếu 1920x1080. Khung thành và thủ môn ở giữa nửa trên, cầu thủ và bóng ở giữa phía dưới; không che hai nút.
- SHOOT ở góc dưới trái, AIM ở góc dưới phải. Vùng chạm mỗi nút tối thiểu 180x140 đơn vị trong canvas tham chiếu, neo vào vùng an toàn.
- Thanh lực nằm trên SHOOT, có số phần trăm và vùng đỏ 85–100%. Khi chưa thể sút, nút có trạng thái disabled rõ ràng.
- Hàng trên có “Bàn thắng n/5”, “Còn k lượt” và năm dấu kết quả. `k=5-kicksCompleted`; lượt đang bay chưa bị trừ khỏi k tới khi chốt outcome.
- Tâm tương phản cao có viền tối, đổi màu và biểu tượng khi khóa; feedback không chỉ dựa vào màu sắc.
- Giữ nhãn AIM, SHOOT, GOAL, SAVED, MISS theo yêu cầu; hướng dẫn, lựa chọn và thông báo phụ dùng tiếng Việt phù hợp KMA.
- Ở màn hình rộng hơn 16:9, giữ sân chơi trong vùng 16:9 giữa màn hình, mở rộng nền trang trí; HUD và nút nằm trong safe area. Màn hình hẹp hơn dùng fit để không cắt nội dung tương tác.

Nguồn asset:

| Nguồn | Cách dùng |
| --- | --- |
| [Basic Soccer Pack](https://opengameart.org/content/basic-soccer-pack) | Sân, khung thành, bóng và các phần nhân vật/thủ môn có sẵn. `supergoalkeeper.zip` đã có ở root, chưa được import. |
| [Football Players](https://opengameart.org/content/football-players) | Nhân vật cầu thủ; nguồn cung cấp SVG. Chọn hình phù hợp góc nhìn sau cầu thủ và xuất sprite PNG cho Unity. |
| [Kenney UI Pack](https://kenney.nl/assets/ui-pack) | Nền nút, panel, thanh lực và bảng điểm; thêm nhãn bằng TMP. |
| [Kenney Game Icons](https://kenney.nl/assets/game-icons) | Icon UI; kiểm tra gói thực tế để chọn tâm phù hợp, bổ sung tâm đơn giản nếu không có. |

Bốn trang nguồn đã được kiểm tra ngày 2026-09-26 và đều ghi CC0. Khi import, lưu manifest nguồn/license cùng các file cần dùng; đối chiếu nội dung gói và license đi kèm. Không coi danh sách tên asset trên trang là bằng chứng đã có đủ động tác. `Free assets.zip` hiện có ở root chưa được xác minh xuất xứ nên không mặc định sử dụng.

Ưu tiên sprite cùng bộ looneybits cho sân và nhân vật, phối UI Kenney bằng màu xanh lá, xanh dương, vàng và đường viền thống nhất. Nếu thiếu animation, dựng chuyển động ngắn bằng các phần sprite/SVG: chân đá, thân nghiêng, tay vươn, thủ môn lao trái/phải và hồi vị. Không thay nhân vật thành hình học placeholder trong bản giao cuối. Không cần tải asset lúc chạy game; toàn bộ tài nguyên dùng khi chơi nằm trong build.

## 6. Kết quả, mạng, retry và lưu tiến trình

Kết quả trận được dựng sau đúng năm outcome: 0–2 bàn là `Pass=false`, điểm 0, hạng F; 3/4/5 bàn lần lượt là điểm 6/8/10, hạng do `ScoreUtil.ToRank` quyết định. Cả ba độ khó cùng cách chấm điểm. Không dùng vị trí trái/phải làm điểm accuracy như cơ chế cũ.

Giữ thời điểm commit kết quả hiện tại của KMA: màn kết quả là preview, action đầu tiên được chấp nhận mới ghi nhận trận. Nút back/thoát của màn này cũng phải đi qua action kết quả, không dùng lối bỏ trận đang chơi để tránh ghi nhận thất bại.

- **Thắng:** hiện số bàn và điểm, nút “VỀ MAP”; action ghi nhận thành tích rồi trở về Map.
- **Thua, còn mạng sau khi trừ:** hiện “Mất 1 mạng”, số mạng còn lại dự kiến, nút “THỬ LẠI” và “VỀ MAP”. Cả hai đều ghi nhận thất bại một lần. Retry mở một trận Football mới tại màn Start, giữ lựa chọn độ khó trong phiên chạy; về Map chỉ điều hướng về Map.
- **Thua, hết mạng sau khi trừ:** hiện thông báo hết mạng và nút “TIẾP TỤC” tới Game Over. Chơi lại toàn campaign dùng luồng Game Over hiện có, không tạo một lần chơi Football với 0 mạng.

Mở rộng xử lý action kết quả trong router để phân biệt Continue và Retry cho Football; không đổi hợp đồng action mặc định của các môn khác. Với retry, thực hiện trong một giao dịch chuyển scene: chụp session cũ, submit thất bại, kiểm tra mạng, start Football mới khi hợp lệ, lưu/chuyển scene theo cơ chế hiện tại. Không chuyển về Map ở giữa rồi mô phỏng thêm một lần bấm môn.

Nếu cơ chế route hiện tại từ chối hoặc báo lỗi, khôi phục session qua đường rollback sẵn có, mở lại action màn kết quả để thử tiếp; không mất thêm mạng hay tạo hai lần chơi đang active. Chặn nhấn lặp trong khi chuyển scene. Phát sự kiện LifeLost/SessionChanged và cập nhật save theo kết quả giao dịch được chấp nhận; không tự viết file save riêng từ controller.

Giữ khóa subject/save hiện tại; việc thay gameplay không tự xóa thành tích Football cũ hoặc reset campaign. Không thêm lưu giữa từng cú sút. Khôi phục một Football đang active từ save dùng luồng campaign hiện hành và bắt đầu trận mới ở Start; không tiếp tục một quả bóng đang bay. Spec này không bổ sung cơ chế chống việc đóng ứng dụng để tránh kết quả chưa commit.

## 7. Lỗi và giới hạn kỹ thuật

- Scene phải có reference Inspector đầy đủ; thiếu view, input, config hoặc panel kết quả thì ghi lỗi rõ ràng và vô hiệu hóa điều khiển, không chạy trận dở dang.
- Dữ liệu cấu hình phải hữu hạn, thời gian/tốc độ dương; range aim, bán kính, tầm với và ngưỡng lực được kiểm tra khi khởi tạo. Cấu hình sai bị từ chối trước trận.
- Logic cập nhật từ delta thời gian hợp lệ; tính ping-pong và vị trí thủ môn bằng thời gian tuyệt đối để một frame dài không đổi lực hoặc outcome so với nhiều frame ngắn cùng tổng thời gian.
- UI đăng ký/hủy đăng ký input và callback theo vòng đời; unload/retry không giữ listener hoặc pointer từ scene trước.
- Đưa object, sprite và reference cần chỉnh vào scene/prefab lưu trên đĩa; cấu hình quan trọng có thể kiểm tra trong Inspector, không chỉ tồn tại trong Play Mode.

## 8. Tiêu chí nghiệm thu và xác minh

### Rules và input

- AIM phản xạ ở đúng biên; khóa đứng yên. Power ping-pong giữ đúng range sau nhiều chu kỳ và frame dài.
- SHOOT không hoạt động trước AIM; release tạo một cú sút; input lặp và pointer khác không tạo cú sút phụ.
- Force dưới 12% là MISS; tại 12% bắt đầu tới khung. Tại 85% không có lệch; trên 85% lệch được sample một lần và có thể ra ngoài khung.
- Test biên goal/save/miss và seed cố định; tốc độ bóng tăng theo lực; keeper không di chuyển trước reaction delay.
- Chia nhỏ timestep hay dùng một timestep vượt mốc tới khung cho cùng outcome, số lượt và số bàn.
- Đủ năm lượt mới kết thúc, gồm cả khi thắng/thua đã chắc chắn; 2 bàn thua, 3 bàn thắng; lần resolve thứ sáu bị từ chối.
- Hủy touch/pause không sút, không mất lượt; callback hoàn tất trận chỉ phát một lần.

### Scene, tiến trình và lưu game

- Từ Map vào scene Football thật, chọn độ khó và chơi đủ năm lượt bằng input UI.
- Thắng ghi đúng môn/điểm; đọc lại save xác nhận tiến trình. Save trước khi thay gameplay vẫn tải được với subject ID cũ.
- Thua rồi Retry mất đúng một mạng, khởi tạo năm lượt mới, không đi qua Map. Thua rồi về Map cũng mất đúng một mạng.
- Hết mạng không cho retry Football; đi tới Game Over. Double tap, route từ chối và lỗi chuyển scene không làm mất mạng hai lần.
- Các test route/result hiện có của Sprint và Volleyball tiếp tục qua sau thay đổi shared router.

### Hình ảnh và Android

- Kiểm tra ảnh render thật ở 1920x1080 và một màn Android rộng có safe area: sân, cầu thủ, thủ môn, tâm, hai nút, điểm và lực đều đọc được, không chồng lấn.
- Xem ít nhất các trạng thái Start, Aiming, khóa hướng, lực đỏ, sút, thủ môn lao, GOAL/SAVED/MISS và thắng/thua.
- Kiểm tra thao tác press/hold/release, thả ngoài nút, hai ngón và pause/resume trên runtime; ảnh tĩnh không được xem là bằng chứng input hoạt động.
- Chạy kiểm tra EditMode/PlayMode phù hợp và build Android theo công cụ hiện có. Ghi riêng kết quả test, build APK, cài/chạy thiết bị, thao tác và QA hình ảnh. Bước nào không chạy được phải báo chưa xác minh.

## 9. Bước tiếp theo

Người dùng duyệt bản spec này. Sau đó dùng skill writing-plans để viết kế hoạch triển khai có file, dependency, test và cách xác minh cụ thể; người dùng duyệt kế hoạch và chọn cách thực hiện trước khi viết code sản phẩm.
