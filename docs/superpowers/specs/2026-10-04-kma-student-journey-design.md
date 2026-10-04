# Hành trình tân sinh viên KMA vượt môn thể chất

**Ngày:** 2026-10-04

**Trạng thái:** Đặc tả đã được người dùng duyệt trong hội thoại; kế hoạch triển khai đang chờ duyệt và chọn cách thực thi.

## 1. Mục tiêu và các quyết định đã thống nhất

Người chơi là một tân sinh viên KMA, nghe anh chị khóa trên kể rằng môn thể chất là “ác mộng”. Ban đầu nhân vật chỉ muốn qua môn; qua luyện tập và thi, nhân vật dần hiểu kỹ năng, tự tin hơn và có bạn đồng hành. Câu chuyện dùng tông hài hước, gần gũi đời sinh viên; giảng viên nghiêm nhưng công bằng.

Hành trình ngắn có mở đầu và kết thúc rõ ràng, dùng ba môn hiện có. Mục tiêu thời lượng là khoảng 20–30 phút cho người mới, bao gồm hội thoại, học thao tác và thử lại. Đây là mục tiêu thiết kế, cần đo bằng chơi thử.

Các quyết định của người dùng:

- Cấu trúc “học kỳ thu gọn”: nhập học → học và thi từng môn → nhận kết quả học phần.
- Thứ tự bắt buộc: **Sprint → Bóng chuyền → Soccer → Qua học phần**.
- Mỗi môn có ba thử thách: làm quen, luyện tập có mục tiêu, thi đánh giá.
- Bài luyện thứ hai của Sprint yêu cầu **100 m trong 20 giây**; bài thi yêu cầu **100 m trong 14 giây**.
- Có **năm lượt thi chung cho toàn học phần**. Thi trượt mất một lượt; luyện tập được thử lại tự do.
- Hết lượt mở đợt thi bổ sung: hoàn thành lại bài luyện thứ hai của môn đang trượt, nhận năm lượt mới, rồi tiếp tục thi. Giữ kết quả các môn đã đạt.

## 2. Cấu trúc hành trình

| Chặng | Câu chuyện | Kết quả cần đạt |
|---|---|---|
| Nhập học | Lời đồn từ khóa trên, bạn cùng lớp rủ ra sân, giảng viên giới thiệu học phần | Mở thử thách đầu của Sprint |
| Chương 1: Chạy trước đã, tính sau | Những lần bấm vụng về, học giữ nhịp, bước vào bài thi đầu tiên | Thi đạt Sprint để mở phần học bóng chuyền |
| Chương 2: Bóng chưa rơi, đừng hoảng | Đánh hụt, hiểu điểm rơi và thời điểm chạm bóng, thi với bạn cùng lớp | Thi đạt bóng chuyền để mở phần học Soccer |
| Chương 3: Năm cú sút định đoạt | Học hướng và lực, quan sát thủ môn, bạn cùng lớp cổ vũ bài thi cuối | Thi đạt Soccer |
| Qua môn | Bảng điểm và hội thoại kết ghi nhận sự tiến bộ | Hoàn tất hành trình, mở chơi lại cải thiện thành tích |

Thứ tự trong từng môn cũng bắt buộc: bài 1 → bài 2 → bài 3. Một thử thách hoàn thành mở thử thách tiếp theo trong cùng môn. **Chỉ bài thi thứ ba đạt mới mở môn tiếp theo**, bao gồm cả các bài luyện của môn đó.

Môn đã đạt cho phép ôn tập và chơi lại. Những lượt chơi này không thay đổi vị trí tiếp tục của hành trình và không tiêu tốn lượt thi. Điểm bài thi chơi lại có thể cải thiện thành tích tốt nhất.

## 3. Chín thử thách và điều kiện hoàn thành

ID thử thách ổn định trong bản lưu; tên hiển thị có thể chỉnh sửa mà không đổi ID.

| ID | Môn / loại | Thử thách | Điều kiện hoàn thành |
|---|---|---|---|
| `sprint_learn` | Sprint / làm quen | Bắt nhịp chân | 12 lần bấm trái–phải đúng luân phiên liên tiếp; bấm sai đặt chuỗi liên tiếp về 0 |
| `sprint_practice` | Sprint / luyện tập | Luyện chạy 100 m | Hoàn thành 100 m trong thời gian không quá 20 giây |
| `sprint_exam` | Sprint / thi | Thi chạy 100 m | Hoàn thành 100 m trong thời gian không quá 14 giây, chạy cùng ba đối thủ |
| `volleyball_learn` | Bóng chuyền / làm quen | Đỡ được đã | Đỡ thành công ba đường bóng trong một lượt luyện; không cần liên tiếp |
| `volleyball_practice` | Bóng chuyền / luyện tập | Chuyền rồi đập | Ghi hai điểm trong một lượt luyện bằng chuỗi đỡ → chuyền → đập; các điểm từ cách đánh khác không tính vào mục tiêu này |
| `volleyball_exam` | Bóng chuyền / thi | Thi với bạn cùng lớp | Đạt năm điểm trước đối thủ trong thời gian không quá 120 giây |
| `soccer_learn` | Soccer / làm quen | Trúng khung trước đã | Ghi ba bàn trong một lượt luyện, tắt thủ môn; số cú thử không giới hạn |
| `soccer_practice` | Soccer / luyện tập | Đọc thủ môn | Ghi hai bàn trong một lượt luyện với thủ môn ở cấu hình Normal; số cú thử không giới hạn |
| `soccer_exam` | Soccer / thi | Năm cú sút định đoạt | Ghi ít nhất ba bàn sau đủ năm lượt sút, thủ môn ở cấu hình Normal |

Các ngưỡng 12 lần, 20 giây và 14 giây là mốc xuất phát cho cân bằng. Việc điều chỉnh phải dựa trên chơi thử; bài luyện Sprint luôn có giới hạn thời gian dài hơn bài thi. Bài luyện Sprint không có điều kiện thể lực tối thiểu.

Các bộ đếm bài luyện được khởi tạo lại khi bắt đầu lượt luyện mới. Trong các bài luyện không giới hạn số lần thử, lỗi thao tác tạo phản hồi và cấp lại tình huống tiếp theo; người chơi có thể chủ động bắt đầu lại bài. Bài luyện có giới hạn thời gian tạo kết quả chưa đạt khi hết giờ mà chưa hoàn thành mục tiêu.

## 4. Chiều sâu từng môn

### 4.1. Sprint

Người chơi học luân phiên đúng, giữ nhịp và tăng tốc dưới áp lực thời gian. Bài làm quen ưu tiên tín hiệu trái/phải và chuỗi đúng; bài luyện dùng đúng cự ly thi với giới hạn 20 giây; bài thi dùng 14 giây và ba đối thủ để tạo không khí cạnh tranh.

Thể lực tác động đến khả năng tăng tốc. Luân phiên đúng với nhịp ổn định có hiệu quả tốt; thao tác sai và dồn thao tác quá mức hao sức hơn. Thể lực thấp giảm hiệu quả tăng tốc nhưng vẫn cho phép tiếp tục chạy. Cấu hình khởi đầu:

| Tham số | Giá trị khởi đầu |
|---|---|
| Thể lực ban đầu / tối đa | 100 / 100 |
| Xung tăng tốc khi đúng | 18, như quy tắc hiện tại |
| Xung khi sai | 40% xung đúng |
| Hao thể lực mỗi lần đúng / sai | 0,25 / 1,5 |
| Bấm nhanh hơn 6 lần/giây | Hao thêm 0,75 mỗi lần, dựa trên khoảng cách giữa hai lần bấm |
| Khi tốc độ lớn hơn 20 | Hao `tốc độ × 0,02` thể lực/giây |
| Khi tốc độ không quá 20 | Hồi 6 thể lực/giây |
| Ngưỡng mệt | Thể lực dưới 30 |
| Khi mệt | Xung tăng tốc còn 75%; trần tốc độ 90 thay vì 120 |
| Giảm tốc tự nhiên / quy đổi tiến quãng đường | 15/giây; `tốc độ × thời gian × 0,08`, như hiện tại |

Thể lực và tốc độ được chặn trong giới hạn cấu hình. Trần tốc độ khi mệt áp dụng cả trong Tick và khi nhận thao tác. Mô phỏng sơ bộ công thức ở bước 1/240 giây cho nhịp luân phiên đều 2, 4 và 6 lần/giây hoàn thành 100 m lần lượt khoảng 13,11; 11,47 và 11,07 giây. Nhịp 8–10 lần/giây cạn thể lực và chậm hơn nhịp 6 lần/giây. Đây là kiểm tra tính khả thi của cấu hình đề xuất, chưa phải kiểm chứng controller/input Unity hoặc chơi thử. Các hệ số được đưa vào asset cân bằng để điều chỉnh theo dữ liệu thực tế, giữ mục tiêu luyện 20 giây và thi 14 giây làm mốc xuất phát.

Điều kiện qua môn là cự ly và thời gian. Thứ hạng, độ chính xác và hiệu quả thể lực đóng góp thành tích tốt nhất; thứ hạng không khóa tiến trình. Hướng dẫn, cấu hình mục tiêu và bộ đánh giá phải cùng mô tả điều kiện này. Giữ thang điểm 0–10 và các hạng hiện có cho bài thi.

### 4.2. Bóng chuyền

Bài làm quen đưa từng đường bóng dễ đọc, cho thấy điểm rơi và tín hiệu thời điểm đỡ. Một lần đỡ được tính khi hành động Receive hợp lệ trả bóng lên thành công. Nếu bóng rơi hoặc đánh hụt, game phản hồi rồi cấp đường bóng mới.

Bài luyện hỗ trợ dựng chuỗi hành động và cung cấp đối thủ dễ tạo cơ hội. Hai điểm mục tiêu phải kết thúc bằng Smash ghi điểm sau đỡ và chuyền trong cùng pha bóng; lỗi hoặc điểm kết thúc bằng cách khác không cộng bộ đếm mục tiêu. Trong mô hình một người mỗi bên hiện có, “đỡ” là Receive ở chạm đầu, “chuyền” là Receive ở chạm thứ hai đưa bóng lên, rồi Smash ở chạm thứ ba. Bộ đánh giá theo dõi số chạm và các hành động thực tế trong cùng quyền kiểm soát bóng. Hướng dẫn giúp người chơi đợi bóng ở độ cao đỡ/chuyền trước khi chuyển sang đập.

Bài thi dùng đối thủ phối hợp đập mạnh, bỏ nhỏ và bóng sâu theo các mẫu hiện có. Hỗ trợ thời điểm giảm so với bài luyện; điểm rơi/bóng/bóng đổ vẫn đủ đọc để phản ứng. Đối thủ được điều chỉnh qua cấu hình mẫu đánh, tốc độ và độ trễ phản ứng.

Đạt năm điểm trước đối thủ trong 120 giây là điều kiện duy nhất để thi đạt. Hết giờ khi hai bên đều chưa đạt năm điểm tạo kết quả chưa đạt, kể cả người chơi đang dẫn điểm. Bước mô phỏng cuối chỉ chạy đến mốc 120 giây; nếu pha bóng cuối đạt điểm thứ năm đúng mốc này, xử lý điểm trước khi kiểm tra hết giờ. Đây là thay đổi có chủ đích so với điều kiện hiện tại cho phép đạt khi dẫn điểm lúc hết giờ.

### 4.3. Soccer

Soccer là môn bóng đá hiện có. Các màn tiếng Việt dùng tên “Bóng đá”; bản đặc tả dùng Soccer để khớp cách gọi trong hội thoại. Định danh nội bộ tiếp tục dùng `SubjectId.Football = 6`, scene `MG_Football` và lớp Football hiện có để giữ tương thích dữ liệu.

Bài đầu học hướng, lực và đường bay với thủ môn tắt. Theo điều chỉnh của người dùng ngày 2026-10-04, cả ba bài và mọi lần chơi lại đều dùng Normal: giữ lực khoảng 2,042035 giây để đạt tối đa, thủ môn phản ứng sau 0,23 giây và tốc độ 2,5. Thủ môn vẫn tắt ở bài học đầu. Dấu hiệu chuẩn bị và phản hồi kết quả giúp người chơi hiểu cách thủ môn bắt bóng.

Hai bài luyện liên tục cấp lượt sút mới đến khi đạt số bàn mục tiêu. Bài thi kết thúc sau đủ năm cú sút, tính đạt khi có ít nhất ba bàn. Điểm bài thi là số bàn × 2 như hiện có. Bóng đá dùng Normal cố định ở cả hành trình, ôn tập và chơi tự do; không có bộ chọn độ khó. Kết quả chơi lại bài thi vẫn có thể cải thiện bảng điểm học phần theo hợp đồng hiện có.

Dùng chung mô phỏng đường bay cho preview và cú sút thật. Preview hiển thị khi giữ nút và ẩn khi thả. Kết quả chạm khung, ra ngoài, bắt được và ghi bàn tiếp tục dựa trên mô phỏng hiện có.

## 5. Lượt thi, thất bại và đợt thi bổ sung

Hành trình mới bắt đầu với năm lượt thi. Lượt này dùng chung cho cả ba bài thi; qua một môn không tự nạp lại lượt. Luyện tập và ôn tập không tiêu tốn lượt.

Một kết quả thi chưa đạt trừ đúng một lượt và giữ nguyên các thử thách, môn đã hoàn thành. Người chơi chọn thi lại ngay hoặc quay về luyện tập. Với số lượt còn lại lớn hơn 0, cả hai hành động đều hợp lệ.

Khi lượt giảm từ 1 xuống 0:

1. Lưu kết quả thất bại và chuyển hành trình sang trạng thái chờ thi bổ sung tại môn đang học.
2. Màn lộ trình hiển thị đợt thi bổ sung và dẫn tới bài luyện thứ hai của môn đó.
3. Người chơi phải hoàn thành **một lượt luyện mới** của đúng bài này, được đánh dấu là lượt ôn cho thi bổ sung.
4. Thành công đặt lượt thi về 5, tăng số đợt thi bổ sung một lần, xóa trạng thái chờ và mở lại bài thi hiện tại.
5. Thất bại hoặc rời lượt luyện giữ trạng thái chờ; có thể thử lại tự do.

Dấu hoàn thành bài luyện từ trước không thỏa bước 3. Kết quả lặp lại của cùng lượt luyện không được nạp thêm lượt hay tăng số đợt. Các môn tương lai tiếp tục bị khóa cho đến khi bài thi hiện tại đạt.

Tạm dừng đóng băng gameplay. Rời một lượt chưa có kết quả hoặc đóng ứng dụng giữa lượt sẽ đưa người chơi trở lại cùng thử thách để bắt đầu mới; chỉ một kết quả thi chưa đạt được xác nhận mới tiêu tốn lượt. Kết quả đã xác nhận được lưu ngay, nên đóng game tại màn kết quả không hoàn lại lượt.

## 6. Nhân vật, hội thoại và phản hồi

| Vai trò | Vai trò trong câu chuyện |
|---|---|
| Bạn / tân sinh viên | Nhân vật chính hiện có, từ lo lắng đến tự tin |
| Bạn cùng lớp | Giúp luyện tập, trêu nhẹ khi vụng về, làm đối thủ bóng chuyền và cổ vũ bài cuối |
| Anh/chị khóa trên | Tạo lời đồn mở đầu và các bình luận cường điệu giữa chương |
| Giảng viên | Giới thiệu mục tiêu, nhận xét có ích và xác nhận kết quả học phần |

Dùng nhân vật từ bộ Toon Characters hiện có. Hội thoại thể hiện vai trò bằng chân dung, tên vai trò và nội dung. Các diễn viên giao diện được cấu hình riêng với nhân vật điều khiển trong sân.

Mỗi nút hội thoại có 2–4 lượt nói ngắn, xuất hiện trước/sau thử thách và có thể bỏ qua. Thoại chính phát khi mở đầu hành trình, bắt đầu mỗi môn, trước mỗi bài thi, lần đầu thi đạt từng môn, bắt đầu đợt thi bổ sung và kết thúc học phần. Các mốc đã xem được lưu; chơi lại dùng phản hồi ngắn thay vì lặp hội thoại mở chương.

Ví dụ nội dung để xác định tông:

- Khóa trên, mở đầu: “Muốn biết sân trường dài bao nhiêu, cứ đợi buổi thể chất đầu tiên.”
- Bạn cùng lớp: “Nghe dọa đủ rồi. Ra sân tập thử đã.”
- Tân sinh viên: “Qua môn trước. Ngầu tính sau.”
- Giảng viên: “Học lần lượt: chạy nước rút, bóng chuyền, rồi bóng đá. Đạt môn trước mới học môn sau.”
- Bạn cùng lớp, mở Soccer: “Hai môn rồi. Giờ bình tĩnh, bóng không có deadline đâu.”
- Giảng viên, thi bổ sung: “Ôn lại bài luyện, rồi vào thi tiếp. Các phần đã đạt vẫn được ghi nhận.”
- Bạn cùng lớp, kết: “Qua rồi! Lần sau nhớ kể nhẹ tay cho khóa dưới nhé.”

Phản hồi kỹ năng phải gắn với nguyên nhân quan sát được: bấm sai nhịp, hết giờ, đứng ngoài tầm đỡ, chạm bóng sớm/muộn, sút ra ngoài hoặc bị thủ môn bắt. Hiển thị một gợi ý cải thiện cùng số đo liên quan. Các câu hài bổ sung sắc thái cho phản hồi này.

## 7. Lộ trình, kết quả và chơi lại

Map trở thành lộ trình ba môn có thứ tự rõ ràng. Môn hiện tại nổi bật; môn đã đạt có dấu hoàn thành và điểm/hạng tốt nhất; môn tương lai ghi điều kiện mở khóa. Chọn môn hiện tại mở các bài đã học và thử thách tiếp theo; bài chưa mở có trạng thái khóa rõ ràng.

Đầu màn hiển thị số lượt thi còn lại bằng nhãn “Lượt thi: X/5”. Khi chờ bổ sung, dùng thông báo đợt thi và nút ôn bài tương ứng. Nút Tiếp tục đưa tới checkpoint hiện tại của hành trình; lựa chọn ôn môn cũ không đổi checkpoint này.

Kết quả bài luyện hiển thị mục tiêu và kết quả thực hiện, cùng hành động học tiếp hoặc thử lại. Kết quả bài thi hiển thị điểm/hạng, lượt thi còn lại và hành động tiếp theo phù hợp. Bài luyện không bị gán điểm học phần hoặc hạng F chỉ vì không có điểm thi.

Sau khi Soccer thi đạt, màn tổng kết trên Map hiển thị ba bài thi, điểm/hạng tốt nhất, số đợt thi bổ sung và hội thoại kết. Hành trình hoàn thành được lưu; mở lại game đưa về tổng kết/lộ trình hoàn thành. Chơi lại mở cả ba môn, các bài luyện và bài thi; kết quả cải thiện bảng thành tích nhưng không thay đổi trạng thái đã qua học phần.

## 8. Kiến trúc và tích hợp

### 8.1. Trách nhiệm các phần

| Thành phần | Trách nhiệm | Liên hệ với mã hiện tại |
|---|---|---|
| Danh mục thử thách | Lưu ID, môn, thứ tự, loại, mục tiêu, cấu hình độ khó, trợ giúp và hội thoại | Các asset thử thách tham chiếu cấu hình Subject hiện có |
| Tiến trình hành trình | Kiểm tra mở khóa, hoàn thành thử thách, lượt thi, checkpoint, thi bổ sung và hoàn tất | Mở rộng `GameSession`, tách quy tắc hành trình khỏi điều hướng scene |
| Ngữ cảnh lượt chơi | Xác định thử thách, chế độ học/thi/ôn/bổ sung/chơi lại và ID lượt chơi | Truyền từ session qua `SceneRouter` tới controller |
| Gameplay từng môn | Chạy cấu hình thử thách và xuất kết quả/metric | `SprintRules/Controller`, `VolleyballMatch/Controller`, `FootballRules/Controller` |
| Lưu dữ liệu | Lưu trạng thái hành trình, kết quả, hội thoại và chuyển đổi bản lưu cũ | `SaveData`, `SaveSystem`, `GameManager` |
| Trình bày | Hiển thị lộ trình, hướng dẫn, hội thoại, kết quả và tổng kết | `MapScreen`, các builder, tutorial/result UI và shell hiện có |

Ba scene `MG_Sprint`, `MG_Volleyball`, `MG_Football` chạy các cấu hình thử thách tương ứng. Lộ trình, thi bổ sung và tổng kết dùng Map cùng các màn/panel phù hợp. Trạng thái thi bổ sung là trạng thái của học phần, dùng bài luyện hiện có của môn đang học.

Thứ tự học là danh sách tường minh `[Sprint, Volleyball, Football]`. Giá trị enum hiện tại là `[0, 7, 6]` theo thứ tự này; không suy ra thứ tự học từ số enum hoặc `Enum.GetValues`.

### 8.2. Luồng kết quả và điều hướng

1. Session xác nhận thử thách được mở và tạo ngữ cảnh lượt chơi với ID mới.
2. Router tải scene môn tương ứng; controller áp dụng cấu hình và chạy vòng tutorial/countdown/play/resolve.
3. Controller phát kết quả thử thách đúng một lần, kèm metric và điểm bài thi nếu là thi đánh giá.
4. Session kiểm tra ID lượt đang chạy, ghi nhận kết quả, cập nhật mở khóa/lượt thi/checkpoint và yêu cầu lưu.
5. UI hiển thị kết quả đã được ghi nhận. Các nút tiếp tục/thi lại điều hướng từ trạng thái này.

Kết quả luyện và kết quả thi có ý nghĩa riêng: bài luyện cập nhật tiến trình thử thách; bài thi cập nhật điểm học phần. Các bộ đánh giá dùng metric gameplay thực tế. Mở khóa được kiểm tra tại session và router, đồng thời phản ánh trên UI; lời gọi vào môn bị khóa phải bị từ chối trước khi sửa trạng thái.

Các thao tác ghi kết quả và nạp lượt thi bổ sung phải có tính chất một lần theo ID lượt chơi. Nút bấm lặp, sự kiện Completed lặp hoặc thất bại tải scene không được tạo hai lần mất lượt, mở hai bài, hay nạp hai lần lượt thi. Lưu hoàn tất trước khi thực hiện chuyển cảnh sau kết quả; lỗi ghi tệp cần có phản hồi thử lưu lại.

## 9. Bản lưu, chuyển đổi và khôi phục

Tăng `SaveData.CurrentVersion` từ 6 lên 7 khi triển khai. Bổ sung trạng thái thử thách đã hoàn thành, checkpoint, hội thoại đã xem, ngữ cảnh lượt đang chạy, trạng thái chờ thi bổ sung, môn cần ôn, số đợt bổ sung và trạng thái kết quả vừa ghi nhận. Tiếp tục dùng dữ liệu điểm/hạng tốt nhất từng môn và trường `lives` cho số lượt thi.

Mở khóa và hoàn tất học phần được suy ra từ chuỗi thử thách hợp lệ. Không lưu thêm các cờ mở khóa độc lập có thể mâu thuẫn với kết quả. Chỉ trạng thái chờ bổ sung và các mốc hội thoại cần cờ riêng.

- Lưu sau bắt đầu hành trình, bắt đầu lượt chơi, ghi kết quả, hoàn thành ôn bổ sung và đánh dấu mốc hội thoại đã xem.
- Bản lưu chỉ có cài đặt vẫn giữ `settingsOnly`; việc chỉnh cài đặt không bật Tiếp tục hành trình.
- Chơi mới đặt lại tiến trình, điểm học phần, hội thoại và lượt thi của hành trình; giữ cài đặt và chính sách hướng dẫn chung hiện có.
- Khôi phục một lượt chưa có kết quả về đúng thử thách để bắt đầu mới; một kết quả đã ghi nhận được khôi phục từ checkpoint sau kết quả, tránh ghi lại.
- Số lượt nằm trong 0–5. Nếu hết lượt tại môn chưa đạt, khôi phục trạng thái chờ bổ sung; các ID/thứ tự không hợp lệ được đưa về checkpoint hợp lệ sớm nhất.

Với bản lưu phiên bản cũ, giữ cài đặt, điểm/hạng tốt nhất và lịch sử thất bại. Chuyển các môn đã đạt thành tiến trình chương hoàn thành **chỉ theo tiền tố liên tục của thứ tự Sprint → Volleyball → Football**. Ví dụ: Sprint đã đạt, Volleyball chưa đạt, Football đã đạt → giữ Sprint hoàn thành; bắt đầu học Volleyball; giữ thành tích Football để tham khảo nhưng Football của hành trình mới còn khóa. Khi chuyển đổi, cờ `Passed` của record được đồng bộ với các bài thi nhập vào hành trình; điểm/hạng tốt nhất của các môn ngoài tiền tố vẫn được bảo lưu.

Bản lưu cũ đạt đủ ba môn được nhập thành hành trình hoàn thành. Bản lưu chỉ có cài đặt không tạo tiến trình. Bản lưu hỏng tiếp tục theo cơ chế đọc an toàn hiện có, với thông báo thích hợp nếu không thể tiếp tục hành trình đã lưu.

## 10. Điều kiện nghiệm thu và kiểm chứng

### Tiến trình và bản lưu

- Khởi đầu chỉ Sprint được học; hoàn thành hai bài luyện chưa mở Volleyball.
- Sprint thi đạt mở bài đầu Volleyball; Volleyball thi đạt mở bài đầu Soccer; Soccer thi đạt mở tổng kết.
- UI và lời gọi route trực tiếp đều tôn trọng khóa môn và khóa bài.
- Luyện/ôn/chơi lại không trừ lượt. Thi trượt trừ một lượt; sự kiện lặp không trừ thêm.
- Lần trượt cuối mở đúng bài ôn bổ sung của môn hiện tại; bài luyện đã đạt từ trước không tự nạp lượt.
- Một lượt ôn bổ sung mới đạt nạp đúng 5 lượt một lần; giữ mọi môn đã đạt và khóa môn tương lai.
- Tiếp tục sau đóng game giữ checkpoint, điểm, lượt thi và trạng thái bổ sung; đóng tại màn kết quả không hoàn lại lượt đã mất.
- Chuyển đổi save cũ được kiểm tra với mọi tổ hợp môn đã đạt, nhất là tổ hợp khác thứ tự và enum `[0, 7, 6]`.
- Chơi mới, save chỉ có cài đặt, tệp hỏng và lỗi ghi/tải cảnh có hành vi rõ ràng, không ghi tiến trình hai lần.

### Gameplay

- Sprint: chuỗi 12 đúng, reset chuỗi khi sai, 100 m/20 giây luyện và 100 m/14 giây thi; kiểm tra đúng mốc giới hạn và thứ hạng không khóa đạt. Mô phỏng xác nhận người giữ nhịp đúng có thể đạt bài thi với cơ chế thể lực mới.
- Volleyball: ba Receive hợp lệ; chỉ điểm từ đúng chuỗi mới tính bài luyện; thi cần năm điểm trước đối thủ trong 120 giây, gồm hết giờ khi đang dẫn và điểm thứ năm đúng mốc giới hạn.
- Soccer: Normal cố định ở mọi bài, thủ môn tắt ở bài đầu và bật ở bài luyện/bài thi; lượt luyện cấp lại đến khi đủ bàn; bài thi đúng năm cú sút và ngưỡng ba bàn. Kiểm tra preview và đường bóng thật dùng cùng mô phỏng.
- Tutorial, pause, retry và kết quả một lần hoạt động trong mọi cấu hình thử thách.

### Trải nghiệm và thiết bị

- Chơi thử với người chưa biết game để đo thời lượng, số lần trượt từng bài, hiểu mục tiêu và lý do thất bại. Dùng kết quả để điều chỉnh cân bằng.
- Chụp và xem các trạng thái lộ trình ban đầu/mở môn/thi bổ sung/tổng kết, tutorial và HUD ba môn; kiểm tra chữ tiếng Việt và khả năng đọc trạng thái.
- Kiểm tra thao tác chạm, safe area, âm thanh, vào nền/khôi phục và hiệu năng trên Android thật; ghi riêng kết quả Editor, tự động, emulator và thiết bị.
- Chạy các kiểm thử hiện có phù hợp và các kiểm thử mới cho quy tắc hành trình. Lỗi âm thanh Sprint trong bộ PlayMode đã lưu cần được xác minh, báo cáo riêng và xử lý theo nguyên nhân trước khi kết luận game hoàn thiện.

## 11. Phạm vi triển khai từ bản thiết kế

Triển khai tiến trình học phần và dữ liệu thử thách làm nền; hoàn thiện một chương Sprint xuyên suốt để kiểm chứng luồng học → thi → trượt → bổ sung → mở môn; tiếp tục các bài bóng chuyền, Soccer, hội thoại và tổng kết. Dùng các sân và bộ hình nhân vật hiện có cho hành trình này.

Bản kế hoạch triển khai cụ thể được lập sau khi người dùng duyệt đặc tả. Kế hoạch cần chia thành các phần có thể kiểm chứng, chỉ rõ thay đổi từng môn và bàn giao riêng các kết quả kiểm thử, QA hình ảnh, build và thiết bị.
