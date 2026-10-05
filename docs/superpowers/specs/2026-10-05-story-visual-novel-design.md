# Cốt truyện Visual Novel — Thiết kế

Ngày: 2026-10-05 · Trạng thái: chờ duyệt

## 1. Mục tiêu

Viết lại 12 đoạn hội thoại của hành trình tân sinh viên theo giọng **meme/teen code đậm**, đặt tên và tính cách cho dàn nhân vật, và thay thẻ thoại hiện tại bằng giao diện **visual novel** có hiệu ứng chữ chạy, tư thế nhân vật, sticker cảm xúc và **emoji sprite** trong câu thoại.

**Thành công khi:**
- Toàn bộ 12 node (`opening` … `course_complete`) dùng kịch bản ở §5, hiển thị emoji thật trên Android.
- Màn thoại có bố cục visual novel như §4; người chơi chạm để hiện hết câu, rồi chạm tiếp để sang câu sau.
- Luồng hiện có (thứ tự node trong `ShowJourney`, lưu mốc "đã xem", nút BỎ QUA, lỗi lưu) giữ nguyên hành vi.

**Ngoài phạm vi:** lịch sử thoại (backlog), lồng tiếng/âm thanh riêng cho thoại, lựa chọn rẽ nhánh, thêm nhân vật mới, đổi thứ tự hay điều kiện kích hoạt node.

## 2. Dàn nhân vật

| id | Tên hiển thị | Sprite | Vai | Tính cách |
|---|---|---|---|---|
| `tan_thu` | Tân Thủ | MalePerson | Người chơi, tân sinh viên | Hơi "gà", tự an ủi, thích flex khi thắng. Luôn đứng **bên phải**. |
| `mai_toang` | Mai Toang | FemalePerson | Bạn cùng lớp | Bạn thân lầy, hay hoảng "ét o ét", luôn cổ vũ. |
| `anh_khoa_tren` | Anh Khoá Trên | MaleAdventurer | Sinh viên khóa trên | Hay chém gió, dọa tân binh rồi tự cười. |
| `co_the_chat` | Cô Thể Chất | FemaleAdventurer | Giảng viên | Nghiêm nhưng lầy ngầm, nói ngắn mà thâm. |

Mỗi nhân vật có ảnh cho 5 tư thế lấy từ sprite sẵn có: `Idle` (`_idle`), `Cheer` (`_cheer0`), `Hurt` (`_hit`, mặt hoảng nhìn thẳng; `_hurt` là ảnh quay lưng nên không dùng), `Jump` (`_jump`), `Duck` (`_duck`). Màu tag tên: Tân Thủ `#FFC928` (gold), Mai Toang `#FF8FB1`, Anh Khoá Trên `#7FD1FF`, Cô Thể Chất `#B9F27C`; chữ trên tag luôn là navy `#0B2A4A`.

## 3. Dữ liệu và cấu trúc code

### 3.1 Dữ liệu (`Assets/_Project/Scripts/Progression/Journey/`)

- `enum DialoguePose { Idle, Cheer, Hurt, Jump, Duck }`.
- `JourneyCharacter` (Serializable, mới): `id:string`, `displayName:string`, `tagColor:Color`, `isPlayer:bool`, `poses:List<JourneyPoseSprite>` (`pose:DialoguePose`, `sprite:Sprite`). API: `Sprite GetPose(DialoguePose pose)` trả sprite của tư thế, nếu thiếu thì trả `Idle`, nếu thiếu nữa thì `null`.
- `JourneyDialogueLine` thay đổi: `characterId:string`, `pose:DialoguePose`, `text:string` (có mã emoji `:name:`), `sticker:string` (rỗng = không có). Bỏ `speakerRole` và `portrait`.
- `JourneyDialogueLibrary`:
  - Thêm `cast:List<JourneyCharacter>` và `JourneyCharacter GetCharacter(string id)` (ném `KeyNotFoundException` nếu không có).
  - `Validate(out string error)` giữ các kiểm tra cũ (id node duy nhất, 2–4 câu, câu có text) và thêm: cast không rỗng, id nhân vật duy nhất, đúng một nhân vật `isPlayer`; mỗi `characterId` tồn tại; nhân vật có sprite cho `pose` được dùng; mọi mã emoji trong text thuộc `DialogueEmoji.KnownNames`.

### 3.2 Emoji

- 15 PNG Twemoji 72×72 tại `Assets/_Project/Art/Emoji/`, tên file = mã: `sob` 😭, `skull` 💀, `sunglasses` 😎, `fire` 🔥, `scream` 😱, `runner` 🏃, `dash` 💨, `soccer` ⚽, `volleyball` 🏐, `eyes` 👀, `clown` 🤡, `salute` 🫡, `100` 💯, `tada` 🎉, `muscle` 💪.
- `TMP_SpriteAsset` tại `Assets/_Project/Resources/Journey/JourneyEmoji.asset`, do builder tạo; mỗi glyph đặt tên theo mã.
- `static class DialogueEmoji` (Progression, thuần C#):
  - `IReadOnlyCollection<string> KnownNames`.
  - `string Expand(string text)`: đổi `:name:` có trong `KnownNames` thành `<sprite name="name">`; mã không biết giữ nguyên chữ.
  - `IEnumerable<string> FindCodes(string text)`: dùng cho `Validate`.
- Credit "Twemoji © Twitter, Inc. and other contributors, CC-BY 4.0" ghi trong `Assets/_Project/CREDITS.md` (README đã trỏ tới file này).

### 3.3 UI (`Assets/_Project/Scripts/UI/`)

- `JourneyDialoguePresenter` giữ API công khai: `Configure(library, saveSeen, sharedBackground)`, `Show(nodeId, onClosed)`, `ShowJourney(session, manager, sharedBackground)`, `IsShowing`, `CurrentLineIndex`. Bỏ tham số `femaleInstructorPortrait` và nhánh `"Giảng viên"` hard-code; `S5ShellSceneController` bỏ field `instructorPortrait` (và tham chiếu trong `Map.unity`). Text component gán `spriteAsset = JourneyEmoji`.
- Thêm `void Advance()` công khai (cũng là handler chạm) và `bool IsLineFullyShown`.
- `DialogueTypewriter` (mới, plain C# class): `Begin(int totalCharacters)`, `Tick(float deltaTime)`, `Complete()`, `VisibleCharacters`, `IsDone`, tốc độ `charactersPerSecond` (mặc định 45). Presenter gán `maxVisibleCharacters` từ `VisibleCharacters` trong `Update`.
- `StudentJourneyContentBuilder.BuildDialogues()` ghi cast, 12 node ở §5 và tạo/cập nhật `JourneyEmoji.asset`; `BuildAll` giữ nguyên.

## 4. Giao diện và hành vi

Canvas tham chiếu 1920×1080 ngang. Mockup: `.superpowers/brainstorm/764-1791173143/content/vn-detail.html`.

- **Nền:** ảnh menu dùng chung (như hiện tại) + lớp navy 36%.
- **Nhân vật:** hai slot, cao khoảng 68% màn hình, đáy ở 24% chiều cao. Slot phải luôn là nhân vật `isPlayer` (lật ngang để nhìn vào giữa). Slot trái là người nói gần nhất không phải người chơi; khi người đó đổi, nhân vật cũ trượt ra trái và nhân vật mới trượt vào (0,25 giây). Đầu mỗi node, slot trái hiện sẵn (tư thế Idle) người không phải người chơi nói đầu tiên trong node; node không có ai như vậy thì slot trái ẩn.
- **Người nói / người nghe:** người nói màu gốc, nảy lên 6% một nhịp (0,3 giây); người nghe tối (nhân màu 0,4), lún xuống 4%.
- **Tư thế:** sprite của người nói đổi theo `pose`; người nghe giữ tư thế gần nhất.
- **Khung thoại:** đáy màn hình, cách lề 3,5%, cao 29%, nền navy 93%, viền gold 3 px, bo góc. Tag tên dạng viên thuốc nghiêng ±3°, màu theo nhân vật, nằm cùng phía với người nói.
- **Chữ chạy:** 45 ký tự/giây; emoji tính là một ký tự. Chạm bất kỳ đâu trên overlay (trừ BỎ QUA) gọi `Advance()`: chưa hiện hết thì hiện hết; hiện hết rồi thì sang câu tiếp; ở câu cuối thì đóng node (lưu "đã xem").
- **Gợi ý:** góc phải dưới khung hiện "chạm để hiện hết" khi đang chạy chữ, "TIẾP »" khi xong, "ĐÓNG »" ở câu cuối.
- **Chấm tiến độ:** góc trái dưới khung thoại, mỗi câu một chấm, chấm đã qua tô gold.
- **Sticker:** nếu `sticker` không rỗng, hiện thẻ trắng viền navy, chữ đỏ `#E2553D`, phía trên người nói, phóng 0→1,15→1 trong 0,25 giây, nghiêng ngẫu nhiên ±8°; ẩn khi sang câu không có sticker.
- **BỎ QUA:** viên thuốc góc trên phải, GameObject vẫn tên `"BỎ QUA"`; đóng ngay node hiện tại như hiện nay.
- **Lỗi lưu:** dòng chữ `GoldLight` trong khung thoại, nội dung như hiện tại; overlay vẫn mở để thử lại.

## 5. Kịch bản

Dạng: `Nhân vật (tư thế) [sticker]: text`. Mã emoji giữ nguyên trong dữ liệu.

**opening**
1. Anh Khoá Trên (Cheer): Chào tân binh :eyes: Sân trường dài bao nhiêu hả? Đợi buổi thể chất đầu tiên là biết liền :skull:
2. Mai Toang (Hurt) [ÉT O ÉT!]: Ổng dọa tụi mình kìa :sob: Mới nhập học mà đã thấy mùi toang rồi đó.
3. Tân Thủ (Idle): Bình tĩnh. Qua môn trước, flex tính sau :sunglasses:
4. Cô Thể Chất (Idle): Lộ trình: chạy nước rút, rồi bóng chuyền, rồi bóng đá. Qua môn trước mới mở khóa môn sau nha các em :salute:

**sprint_intro**
1. Cô Thể Chất (Idle): Khởi động bằng nhịp chân. Trái, phải, trái, phải. Đều như nhịp tim crush lúc nhắn "seen" :eyes:
2. Mai Toang (Hurt) [TOANG?!]: Ét o ét :sob: 12 nhịp liền mạch, sai một phát là làm lại từ đầu đó bà con ơi!
3. Tân Thủ (Idle): Chân trái, chân phải thôi mà. Chạy như chưa từng được chạy :runner::dash:

**sprint_exam**
1. Cô Thể Chất (Idle) [14 GIÂY]: Bài thi: 100 mét trong 14 giây. Giữ sức, đừng bung hết từ vạch xuất phát nha :fire:
2. Tân Thủ (Jump): Tập rồi, giờ thi thôi. Đường đua ơi, chờ anh :100:

**sprint_pass**
1. Cô Thể Chất (Cheer) [ĐẠT!]: Đạt! Nhịp chân ổn áp rồi đó. Cô công nhận em hơi bị đỉnh nóc :fire:
2. Mai Toang (Cheer): Sân trường vẫn dài, nhưng giờ mình chạy hết nổi rồi :sob::tada:

**volleyball_intro**
1. Mai Toang (Cheer): Bóng chuyền nè! Đỡ bóng đúng tầm trước, chuyền đẹp tính sau :volleyball:
2. Cô Thể Chất (Idle): Đỡ, chuyền, đập, đủ ba chạm. Bóng rơi xuống sân mình là mất điểm, không có chuyện "chưa sẵn sàng" đâu :eyes:
3. Tân Thủ (Duck) [CỨU!]: Ba đường bóng liền :scream: Thôi được, tay em đây, cứ phát bóng đi!

**volleyball_exam**
1. Cô Thể Chất (Idle) [120 GIÂY]: Thi đấu: ghi đủ 5 điểm trước đối thủ, trong 120 giây :fire:
2. Mai Toang (Jump): Tui đỡ thật đẹp, ông lo cú đập nha. Đừng để tui phải ét o ét :sob:
3. Tân Thủ (Cheer): Combo đỡ, chuyền, đập, nhận về 5 điểm :100:

**volleyball_pass**
1. Cô Thể Chất (Cheer) [ĐẠT!]: Đạt! Phối hợp mượt như wifi thư viện lúc 6 giờ sáng :volleyball:
2. Tân Thủ (Jump): Cảm ơn đồng đội :salute: Hai môn rồi, môn cuối đâu, ra đây!

**soccer_intro**
1. Mai Toang (Idle): Còn môn cuối thôi. Bình tĩnh, quả bóng không có deadline đâu :soccer:
2. Anh Khoá Trên (Cheer) [HỒI ĐÓ…]: Hồi anh thi, thủ môn cao hai mét, sân dốc lên trời :skull: Em giờ sướng chán.
3. Cô Thể Chất (Idle): Đừng nghe ổng chém :clown: Tập hướng sút và lực sút trước. Lúc thi em có đúng 5 cú.

**soccer_exam**
1. Cô Thể Chất (Idle) [5 CÚ]: Thi: 5 cú sút, vào ít nhất 3 bàn là qua. Thủ môn hôm nay không nương tay đâu :eyes:
2. Tân Thủ (Idle): Nhắm chắc, lực vừa đủ, sút là vào. Chân này đã được khai quang :fire:

**soccer_pass**
1. Cô Thể Chất (Cheer) [ĐẠT!]: Đạt học phần! Ba môn, ba lần qua. Cô hơi bị tự hào đó :tada:
2. Mai Toang (Cheer): QUA RỒI :sob::sob: Từ "toang" lên "đỉnh nóc" trong một học kỳ!

**supplementary**
1. Cô Thể Chất (Idle): Chưa qua thì ôn lại bài luyện rồi thi tiếp. Phần đã đạt vẫn được giữ nguyên, không mất gì đâu :salute:
2. Mai Toang (Hurt) [HỒI SINH!]: Toang nhẹ thôi, chưa toang hẳn :clown: Luyện đúng chỗ còn vướng rồi quẩy lại nha!

**course_complete**
1. Cô Thể Chất (Cheer) [HOÀN THÀNH!]: Chúc mừng! Học phần Thể chất chính thức hoàn tất :tada:
2. Tân Thủ (Jump): Từ tân binh run run thành tuyển thủ cấp trường. Flex được rồi đúng không? :sunglasses:
3. Anh Khoá Trên (Cheer): Được! Nhưng năm sau nhớ dọa khóa dưới y như anh nha :skull:
4. Mai Toang (Cheer): Hội qua môn Thể chất, điểm danh :100::tada: :muscle:

## 6. Xử lý lỗi

| Tình huống | Khi build | Khi chạy |
|---|---|---|
| Mã emoji không biết | `Validate` báo lỗi, builder ném | Giữ nguyên `:xxx:` dạng chữ |
| Thiếu sprite tư thế | `Validate` báo lỗi | Dùng `Idle`; thiếu nữa thì ẩn ảnh |
| `characterId` không có trong cast | `Validate` báo lỗi | `GetCharacter` ném `KeyNotFoundException` |
| Thiếu `JourneyEmoji.asset` | Smoke test Player thất bại | Emoji thành ô trống, chữ vẫn đọc được |
| Lỗi lưu "đã xem" | — | Giữ hành vi hiện tại |

## 7. Kiểm thử

**EditMode**
- `DialogueEmojiTests`: đổi đúng một mã, nhiều mã liền nhau (`:sob::sob:`), giữ nguyên mã lạ, text không có mã thì không đổi; `FindCodes` trả đúng danh sách.
- `DialogueTypewriterTests`: tăng theo thời gian, không vượt tổng, `Complete()` → `IsDone`.
- `JourneyDialogueDataTests` (cập nhật): đủ 12 node, 2–4 câu; mọi `characterId` có trong cast; có sprite cho mọi tư thế được dùng; mọi mã emoji có glyph trong `JourneyEmoji.asset`; đúng một nhân vật `isPlayer`.
- `JourneyDialogueLibrary.Validate`: báo lỗi với id nhân vật lạ, tư thế thiếu sprite, mã emoji lạ.
- `StudentJourneyContentBuilderTests` (cập nhật nếu kiểm tra nội dung cũ).

**PlayMode (`JourneyNarrativeTests`)**
- Giữ test BỎ QUA lưu `opening` và gọi callback.
- Chạm khi chữ đang chạy: `IsLineFullyShown` thành true, `CurrentLineIndex` không đổi; chạm lần hai: `CurrentLineIndex` tăng.
- Slot phải luôn hiển thị nhân vật `isPlayer`; người nói sáng, người nghe tối.
- Câu cuối + chạm: node đóng, mốc "đã xem" được lưu.

**Smoke (Player)** — `JourneyEmoji.asset` load được từ Resources và có đủ 15 glyph.

**QA thị giác** — chụp `opening` và `course_complete` bằng skill `testing-unity-ui-with-screenshots`, lưu vào `docs/qa/images/`, cập nhật `docs/qa/kma-student-journey.md`.
