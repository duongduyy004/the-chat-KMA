# Minigame bật cóc khi trượt bài và hồi lượt thi theo thời gian

**Ngày:** 2026-10-05

**Trạng thái:** Thiết kế đã được người dùng duyệt từng phần trong hội thoại; đặc tả chờ người dùng duyệt.

**Liên quan:** thay thế mục 5 (“Lượt thi, thất bại và đợt thi bổ sung”) của `2026-10-04-kma-student-journey-design.md`. Không động tới phần Punishment đã ngừng dùng trong `2026-09-14-remove-punishment-loss-route-design.md`.

## 1. Mục tiêu và các quyết định đã thống nhất

Trượt một bài luyện hoặc bài thi trong hành trình không còn trừ lượt thi ngay. Người chơi bắt buộc chơi minigame **bật cóc**; lần trượt đầu tiên của một bài, về đích kịp 60 giây sẽ giữ lượt thi. Lượt thi không còn được nạp lại bằng đợt thi bổ sung mà hồi dần theo thời gian thực.

Các quyết định của người dùng:

- Bật cóc: một kim chạy qua lại trên thanh lực; chạm để dừng. Càng gần giữa nhảy càng xa, giảm dần về hai biên; quá sát biên thì **ngã**, mất thời gian đứng dậy rồi bật tiếp (không thua ngay).
- Giới hạn **60 giây** để về đích.
- Áp dụng cho **bài luyện và bài thi**. Bài làm quen không tốn lượt thi và không có bật cóc.
- Lần trượt **thứ nhất** của một bài: bật cóc kịp giờ → không trừ lượt; không kịp → trừ 1 lượt.
- Lần trượt **thứ hai trở đi** của bài đó: vẫn phải bật cóc, **trừ 1 lượt bất kể kết quả** bật cóc.
- Sau bật cóc luôn phải chơi lại đúng bài vừa trượt.
- `volleyball_practice` và `soccer_practice` có thêm điều kiện trượt: bóng chuyền giới hạn **120 giây**, bóng đá giới hạn **6 cú sút**.
- **Bỏ đợt thi bổ sung.** Lượt thi hồi **1 lượt mỗi 5 phút**, tối đa 5.
- Khi 0 lượt: khóa bài luyện và bài thi đến khi hồi được lượt; bài làm quen và ôn tập môn đã đạt vẫn chơi được.
- Đồng hồ hồi lượt hiển thị **trên thanh tim ở header Map** (ví dụ `4:32`), không hiện trong panel của game.
- Nhãn header giữ nguyên **“Lượt thi: X/5”**; mọi câu chữ mới dùng từ “lượt thi”.
- Kiến trúc: scene riêng `MG_FrogJump` kế thừa `MinigameBase` (hướng 1 trong ba hướng đã cân nhắc).

## 2. Gameplay bật cóc

### 2.1. Màn chơi

Khung nhìn ngang. Nhân vật dùng bộ sprite đồng phục thể dục hiện có, ngồi xổm ở vạch xuất phát; đích cách **40 m**. HUD hiển thị đồng hồ 60 giây đếm ngược và quãng đường còn lại. Thanh lực nằm ngang phía dưới, có một kim.

### 2.2. Thanh lực và cú nhảy

- Kim chạy qua lại liên tục (ping-pong); một lượt từ biên này sang biên kia mất **1,2 giây**.
- Chạm bất kỳ đâu trên màn hình: kim dừng và nhân vật thực hiện cú nhảy.
- Gọi `d = |vị trí kim − 0,5| × 2`, với `d ∈ [0, 1]` (0 là tâm, 1 là biên).
  - `d ≤ 0,8`: nhảy. Cự ly `= lerp(3,0 m, 1,0 m, d / 0,8)` — giảm tuyến tính và liên tục từ tâm ra mép vùng an toàn.
  - `d > 0,8` (10% mỗi đầu thanh): **ngã**. Không tiến quãng đường; nằm dậy **2,5 giây**.
- Cú nhảy kéo dài **0,6 giây**. Trong lúc nhảy hoặc đang đứng dậy, kim ẩn và chạm bị bỏ qua. Khi sẵn sàng lại, kim bắt đầu chạy từ một biên, nên không thể bấm dồn.
- Quãng đường được chặn ở 40 m; cú nhảy chạm hoặc vượt đích kết thúc ván ngay khi cú nhảy hoàn tất.
- Thanh có dải màu: xanh đậm ở tâm, nhạt dần ra ngoài, hai đầu đỏ cho vùng ngã. Mỗi cú hiện chữ nổi (“Đẹp! 2,8 m”, “Ngã!”) qua `FloatingTextPool`. Có âm thanh và rung khi nhảy/ngã. Hoạt ảnh bật dùng co giãn (squash & stretch) vì bộ sprite chưa có tư thế bật cóc riêng.

### 2.3. Kết quả và độ khó

- Về đích trước khi hết 60 giây: **thắng**. Hết giờ: **thua**. Nếu cú nhảy cuối hoàn tất đúng mốc 60 giây và chạm đích, xử lý đích trước khi kiểm tra hết giờ.
- Ước lượng: trung bình khoảng 2 m mỗi cú → khoảng 20 cú × khoảng 1,4 giây (0,6 nhảy + 0,8 canh) ≈ 28 giây. Người chơi còn dư cho khoảng 8–10 lần ngã. Chơi ẩu, ngã liên tục hoặc toàn nhảy ngắn sẽ không kịp. Đây là ước lượng xuất phát, cần chơi thử.
- Mọi tham số (40 m, 1,2 s, 3,0/1,0 m, ngưỡng 0,8, 0,6 s, 2,5 s, 60 s) nằm trong ScriptableObject `FrogJumpBalanceConfig`.

### 2.4. Tutorial, tạm dừng, thoát

- Dùng thẻ tutorial và đếm ngược 3-2-1 dùng chung (`UsesSharedTutorial`, `UsesSharedCountdown`). Nội dung: “Chạm khi kim ở giữa để bật xa. Sát mép là ngã!”
- Panel tạm dừng chỉ có “Tiếp tục”; không có lối thoát vì bật cóc là bắt buộc.
- Đóng ứng dụng giữa ván: lần mở sau tính là **thua bật cóc** (xem mục 5).

## 3. Luật lượt thi khi trượt

### 3.1. Phạm vi

Chỉ áp dụng cho `ChallengeKind.Practice` và `ChallengeKind.Exam` chạy ở `ChallengeAttemptMode.Journey`. Không áp dụng cho bài làm quen, chế độ `Review` và `FreePlay`.

Chế độ `Supplementary` bị bỏ khỏi luồng. Thành viên enum `ChallengeAttemptMode.Supplementary` được **giữ lại** (đánh dấu ngừng dùng) vì giá trị enum được lưu dạng số trong bản lưu; `TryBegin` luôn từ chối chế độ này.

### 3.2. Điều kiện bắt đầu

Bắt đầu bài luyện hoặc bài thi ở chế độ Journey cần **ít nhất 1 lượt thi** (sau khi tính bù hồi lượt, mục 4). Khi 0 lượt, `JourneyProgress.TryBegin` và router từ chối trước khi sửa trạng thái; thẻ bài trên Map hiển thị khóa với nhãn “Hết lượt thi”.

### 3.3. Khi trượt bài X

Khi ghi nhận kết quả trượt của X, session tăng `failCounts[X]`:

| `failCounts[X]` sau khi tăng | Khi ghi kết quả | Khi bật cóc kết thúc |
|---|---|---|
| 1 | Không trừ lượt. Tạo `pendingFrogJump` với `savesLife = true` | Thắng: giữ lượt. Thua: −1 lượt |
| ≥ 2 | **−1 lượt ngay** khi ghi kết quả. Tạo `pendingFrogJump` với `savesLife = false` | Không đổi lượt |

Trừ ngay ở lần thứ hai trở đi để đóng ứng dụng không tránh được hình phạt. Số lượt luôn chặn trong `[0, 5]`.

### 3.4. Đạt bài X

`failCounts[X]` đặt về 0. Mở khóa bài tiếp theo như hiện tại. Bộ đếm tính riêng cho từng bài.

### 3.5. Sau bật cóc

`pendingFrogJump` được xóa. Còn lượt: router tự bắt đầu một lượt chơi mới của X ở chế độ Journey. Hết lượt: về Map, X hiển thị khóa “Hết lượt thi” và header chạy đồng hồ hồi lượt.

Khi `pendingFrogJump` tồn tại, mọi thao tác bắt đầu thử thách khác bị từ chối; chỉ `StartFrogJump` hợp lệ.

### 3.6. ResultPanel khi trượt

Các nút “Thi lại” và “Về luyện tập” hiện có của kết quả trượt trong hành trình được thay bằng **một nút “Bật cóc”** (`JourneyResultAction.FrogJump`, thêm vào cuối enum). Dòng hậu quả:

- Lần 1: “Về đích trong 60 s để giữ lượt thi”.
- Lần ≥ 2: “−1 lượt thi. Bật cóc xong mới được thi lại”.

Kết quả trượt của bài làm quen, `Review` và `FreePlay` giữ nguyên các nút hiện có.

### 3.7. Một lần duy nhất

`pendingFrogJump.id` là một GUID mới, gắn với `attemptId` của lượt trượt. Kết quả bật cóc chỉ được áp dụng nếu khớp `id` đang chờ và chưa từng được áp dụng (lưu `lastAppliedFrogJumpId`), theo đúng khuôn `lastCommittedAttemptId` hiện có. Nút bấm lặp, sự kiện `Completed` lặp hoặc tải scene thất bại không được trừ hai lần lượt.

## 4. Hồi lượt thi theo thời gian

### 4.1. Quy tắc

- Tối đa 5, hồi 1 lượt mỗi **5 phút**. Đồng hồ chỉ chạy khi lượt dưới 5.
- Khi lượt giảm từ 5 xuống dưới 5: `nextLifeAtUtc = now + 5 phút`. Trừ thêm lượt trong khi đồng hồ đang chạy không đặt lại mốc.
- Tính bù: trong khi lượt dưới 5 và `now ≥ nextLifeAtUtc`, +1 lượt và `nextLifeAtUtc += 5 phút`. Đủ 5 lượt thì xóa mốc.
- Chỉnh giờ lùi: nếu `now < nextLifeAtUtc − 5 phút`, đặt `nextLifeAtUtc = now + 5 phút` (không tặng lượt, không kẹt chờ vô hạn). Chỉnh giờ tiến được chấp nhận vì trò chơi chạy offline.

### 4.2. Thành phần và điểm gọi

Lớp C# thuần `LifeRegen` chứa quy tắc trên, nhận `DateTime utcNow` làm tham số để kiểm thử. `GameSession` có `IClock` (mặc định `DateTime.UtcNow`, thay được trong test) và gọi tính bù:

1. Khi khôi phục bản lưu / mở ứng dụng.
2. Mỗi giây khi đang ở Map.
3. Trước khi bắt đầu bài luyện hoặc bài thi.
4. Trước khi trừ lượt.

Mỗi lần tính bù có thay đổi số lượt thì lưu ngay.

### 4.3. Header Map

- Nhãn giữ nguyên `Lượt thi: X/5`.
- Ngay trên `HeartBar` có nhãn đếm ngược `m:ss` (ví dụ `4:32`), cập nhật mỗi giây; ẩn khi đủ 5 lượt.
- Khi 0 lượt, nhãn đếm ngược dùng màu cảnh báo của theme.
- Khi một lượt được hồi trên Map, `HeartBar`, nhãn lượt và trạng thái khóa của thẻ bài cập nhật ngay, không cần tải lại scene.

## 5. Điều hướng, khôi phục và bản lưu

### 5.1. Luồng

```
Bài luyện/thi trượt
  → commit: failCounts[X]++, trừ lượt nếu ≥ 2, tạo pendingFrogJump, LƯU
  → ResultPanel [Bật cóc]
  → MG_FrogJump: tutorial → 3-2-1 → chơi → kết quả
  → ApplyFrogJump: trừ lượt nếu savesLife && thua, xóa pending, LƯU
  → còn lượt ? bắt đầu lại X : Map
```

Lưu hoàn tất trước khi chuyển cảnh, giữ cơ chế báo lỗi lưu / “Thử lưu lại” hiện có của `JourneySaveCoordinator`.

### 5.2. Khôi phục

Khi khôi phục bản lưu có `pendingFrogJump`: tính là **thua bật cóc** (áp luật mục 3.3 theo `savesLife`), xóa trạng thái chờ, lưu. Sau đó còn lượt thì Tiếp tục đưa vào X; hết lượt thì về Map. Một ván bật cóc chưa có kết quả chỉ mang một nghĩa — thua — nên không thể đóng ứng dụng để chơi lại ván bật cóc nhằm giữ lượt.

### 5.3. Bản lưu v8

`SaveData.CurrentVersion` tăng từ 7 lên 8. Bổ sung:

- `JourneyStateData.failCounts`: danh sách cặp `{ challengeId, count }` (JsonUtility không lưu Dictionary).
- `JourneyStateData.pendingFrogJump`: `{ id, failedAttemptId, failedChallengeId, savesLife }` hoặc null.
- `JourneyStateData.lastAppliedFrogJumpId`.
- `SaveData.nextLifeAtUtcTicks` (`long`, 0 = không chạy).

Tiếp tục dùng trường `lives`.

Chuyển đổi bản lưu v7:

- `failCounts` rỗng, không có `pendingFrogJump`.
- Nếu lượt dưới 5: bắt đầu mốc hồi lượt từ thời điểm mở ứng dụng.
- `awaitingSupplementaryChallengeId` bị xóa; checkpoint trở lại bài thi của môn đang học theo chuỗi thử thách đã hoàn thành. Lượt thi tự hồi như bình thường.
- Lượt chơi đang chạy ở chế độ `Supplementary` bị bỏ (như một lượt chưa có kết quả).
- `supplementaryRounds` vẫn đọc và ghi để giữ dữ liệu, nhưng dòng “Lượt thi bổ sung” bị gỡ khỏi `JourneyCourseSummary`.

## 6. Thành phần mới và thay đổi

| Thành phần | Trách nhiệm |
|---|---|
| `FrogJumpRules` (mới, C# thuần) | Kim ping-pong, tính cự ly/ngã, thời gian nhảy và đứng dậy, quãng đường, giới hạn 60 s, kết quả |
| `FrogJumpBalanceConfig` (mới, ScriptableObject) | Toàn bộ tham số mục 2 |
| `FrogJumpController : MinigameBase` (mới) | Chuyển chạm (`ScreenTapArea`) thành lệnh dừng kim, cập nhật HUD và hoạt ảnh, phát `Completed` đúng một lần |
| `MG_FrogJump.unity` + configurator Editor (mới) | Dựng scene theo khuôn configurator của Sprint/Volleyball; thêm vào Build Settings |
| `LifeRegen` (mới, C# thuần) và `IClock` | Quy tắc mục 4 |
| `JourneyProgress` (sửa) | `failCounts`, `pendingFrogJump`, `TryBeginFrogJump` / `ApplyFrogJump`, chặn bắt đầu khi 0 lượt; gỡ nhánh Supplementary |
| `GameSession` (sửa) | Đồng hồ hồi lượt, tính bù tại các điểm mục 4.2, ghi `nextLifeAtUtcTicks` |
| `SceneRouter.Journey` (sửa) | `StartFrogJump()`, nhận kết quả bật cóc, tự bắt đầu lại X hoặc về Map; gỡ `PracticeCurrentSubject` |
| `SessionRoute` (sửa) | Thêm `FrogJump` vào **cuối** enum để giữ giá trị đã lưu |
| `ResultPanel` (sửa) | Nút “Bật cóc” và dòng hậu quả mục 3.6 |
| `MapPresentationBuilder` / `MapScreen` / `JourneyLessonList` (sửa) | Đồng hồ trên thanh tim, khóa “Hết lượt thi”, gỡ thông báo đợt thi bổ sung |
| `JourneyDialoguePresenter` / thư viện hội thoại (sửa) | Gỡ mốc hội thoại “bắt đầu đợt thi bổ sung” |
| `ChallengeDefinition` (sửa) | Thêm `attemptLimit` (số cú thử tối đa, 0 = không giới hạn) |
| `VolleyballChallengeRules` (sửa) | `volleyball_practice` dùng `timeLimit = 120`: hết giờ chưa đủ 2 điểm combo → trượt |
| Luật thử thách bóng đá (sửa) | `soccer_practice` dùng `attemptLimit = 6`: hết 6 cú chưa đủ 2 bàn → trượt; đạt ngay khi đủ 2 bàn, không kết thúc sớm khi đã không thể đạt |
| `StudentJourneyContentBuilder` / asset thử thách (sửa) | Cập nhật hai bài luyện và câu mục tiêu: “Ghi 2 điểm chuyền–đập trong 120 giây”, “Ghi 2 bàn trong 6 cú sút” |

Code Punishment cũ (`PunishmentController`, `PunishmentSceneController`, `Punishment.unity`) giữ nguyên, không tái dùng.

## 7. Kiểm thử và nghiệm thu

### 7.1. EditMode

- `FrogJumpRules`: kim ở tâm cho 3,0 m; `d = 0,8` cho 1,0 m; `d` giữa hai mức nội suy tuyến tính; `d > 0,8` ngã, không tiến và chặn chạm 2,5 s; chạm bị bỏ qua khi đang nhảy; về đích trước 60 s thắng; hết 60 s thua; cú nhảy cuối chạm đích đúng mốc 60 s thắng; quãng đường chặn ở 40 m.
- `LifeRegen`: đặt mốc khi giảm từ 5; không đặt lại mốc khi trừ thêm; hồi đúng mốc; hồi bù nhiều lượt sau thời gian dài; đủ 5 thì xóa mốc; giờ lùi đặt lại mốc.
- `JourneyProgress`: lần trượt 1 + thắng bật cóc giữ lượt; lần 1 + thua trừ 1; lần ≥ 2 trừ ngay và bật cóc không đổi lượt; đạt bài đặt bộ đếm về 0; bộ đếm tách theo bài; bài làm quen trượt không tạo `pendingFrogJump`; 0 lượt chặn bài luyện/thi nhưng không chặn bài làm quen và Review; khi có `pendingFrogJump` chặn mọi `TryBegin`; kết quả bật cóc áp dụng đúng một lần; khôi phục khi đang chờ tính là thua.
- Chuyển đổi bản lưu v7 → v8, gồm trường hợp đang chờ thi bổ sung và lượt chơi `Supplementary` đang chạy.
- `volleyball_practice` trượt khi hết 120 s; `soccer_practice` trượt sau 6 cú chưa đủ 2 bàn và đạt khi đủ 2 bàn trong 6 cú.
- Cập nhật các test hiện có phụ thuộc đợt thi bổ sung.

### 7.2. QA hình ảnh

Dùng skill `testing-unity-ui-with-screenshots` chụp: `MG_FrogJump` lúc tutorial, lúc đang chơi (kim, thanh màu, đồng hồ, quãng đường), lúc ngã; ResultPanel trượt lần 1 và lần ≥ 2; header Map có đồng hồ hồi lượt và khi 0 lượt với thẻ bài khóa.

### 7.3. Nghiệm thu

- Trượt bài luyện hoặc bài thi trong hành trình luôn dẫn tới bật cóc, rồi quay lại đúng bài đó nếu còn lượt.
- Số lượt thay đổi đúng bảng mục 3.3 trong mọi trường hợp, kể cả khi đóng ứng dụng giữa chừng.
- Lượt hồi 1 mỗi 5 phút theo giờ thực, kể cả khi ứng dụng đóng; đồng hồ hiện đúng trên thanh tim.
- Không còn đường nào vào đợt thi bổ sung.
