# PLAN — "VƯỢT THỂ KMA" (Unity 6.x, C#, Android, offline)

> Nguồn tham chiếu:
> - UI/UX: `/home/duydt/project/TheChatKMA` (React + Vite + Tailwind v4 prototype, Figma Make)
> - GDD: `/home/duydt/project/TheChatKMA/kma-pe.md` (20 slide)
> - Thư mục triển khai Unity: `/home/duydt/project/the-chat-KMA` (hiện trống)

---

## 0. Chốt phạm vi & điểm lệch so với GDD

Nguyên tắc: **mỗi môn = 1 minigame riêng (1:1)**. Tổng **7 minigame + 1 Boss**:

| # | Môn / Minigame | Scene | Thao tác chính | Ghi chú vs GDD |
|---|---|---|---|---|
| 1 | Chạy ngắn 100m | `MG_Sprint` | Tap luân phiên L-R + bonus theo nhịp; **3 nhân vật AI chạy cùng** | GDD slide 8 **chưa có đối thủ AI** → bổ sung |
| 2 | Chạy bền 1500m | `MG_Endurance` | **Tap đều theo nhịp** (rhythm) + chướng ngại vật (swipe up/down) | GDD slide 9 dùng **hold/nhả nhịp** → đổi sang rhythm-tap |
| 3 | Bóng chuyền | `MG_Volleyball` | Vuốt theo **3 động tác**: đỡ / nâng / đập qua lưới; combo | **Giữ nguyên** GDD slide 10 |
| 4 | Bóng rổ | `MG_Basketball` | Vuốt chuyền cho đồng đội → alley-oop → tap kết thúc khi bóng gần apex | **Môn thêm mới**, GDD chưa có → bổ sung slide |
| 5 | Bóng bàn | `MG_PingPong` | Tap đúng timing, bóng nhanh dần mỗi lượt | Giữ nguyên GDD slide 11 |
| 6 | Cầu lông | `MG_Badminton` | **Nhấn giữ nạp lực + nhả đúng lúc** → đập (cao) / phông (thấp) | **Môn thêm mới**, GDD chưa có |
| 7 | Bóng đá (sút phạt) | `MG_Football` | Vuốt chọn hướng + lực (+ xoáy), thủ môn AI | Giữ nguyên GDD slide 11 |
| + | Boss (giảng viên) | `MG_Boss` | Ghép 3 cơ chế phạt, dồn dập 30–40s | Giữ nguyên GDD slide 14 |

**`kma-pe.md` KHÔNG sửa** (đã chốt). Hệ quả cần biết:
- **PLAN.md này là nguồn sự thật duy nhất cho code**; `kma-pe.md` giữ vai trò tài liệu báo cáo/slide như hiện tại.
- Tồn tại 3 điểm lệch giữa slide và sản phẩm: (a) slide 9 ghi chạy bền = hold/nhả nhịp, code làm rhythm-tap; (b) slide 10/11 chưa có Bóng rổ và Cầu lông, sản phẩm có; (c) slide 11/19/20 ghi "5 minigame", sản phẩm có **7**.
- → Chuẩn bị **1 slide phụ lục hoặc câu trả lời khi bảo vệ**: "bản build đã mở rộng thêm Bóng rổ + Cầu lông và đổi cơ chế chạy bền sau playtest" — biến điểm lệch thành điểm cộng (có iterate theo playtest), thay vì bị hỏi vặn.

**Bản đồ (`LevelSelect`)** — giữ nguyên toàn bộ 9 node cũ, **thêm** node Bóng bàn:

| Node | Trạng thái |
|---|---|
| SPRINT, ENDURANCE, VOLLEYBALL, BASKETBALL, **TABLE_TENNIS** (mới), **BADMINTON**, FOOTBALL | Playable — 7 minigame |
| PUSHUP (Hít đất), AEROBICS (Nhịp điệu), SWIMMING (Bơi lội) | `Locked / Coming soon` — giữ art, 0 code |
| FINAL_BOSS | Mở sau khi xong 7 môn |

Tổng **10 node môn + boss** trên bản đồ. Cầu lông và Bóng bàn là 2 node riêng biệt (không gộp, không đổi tên), **cả hai đều chơi được** nhưng cơ chế tách hẳn nhau (xem M5 vs M6).

Hệ thống xuyên suốt (giữ nguyên GDD slide 7 & 12): **5 tim dùng chung**, **tối đa 2 lượt/môn**, thất bại lượt 1 → mini-challenge phạt → lượt 2; thất bại lượt 2 → −1 tim, 0 điểm môn đó; 0 tim → Game Over toàn bộ.

---

## 1. Tech stack

| Hạng mục | Chọn | Lý do |
|---|---|---|
| Engine | **Unity 6.3 LTS** — pin **1 patch duy nhất** (`6.3.x`) cho cả nhóm, ghi vào `ProjectSettings/ProjectVersion.txt` + README | LTS = vá lỗi dài hạn. Khác patch giữa các máy → vỡ scene/prefab. Không dùng Beta/Alpha. |
| Template | **Universal 2D** (URP 2D Renderer) | Sprite sorting/lighting 2D tốt, tắt hết post-process cho perf mobile |
| Ngôn ngữ | C#, .NET Standard 2.1 | |
| Input | **Input System** (package `com.unity.inputsystem`) + `EnhancedTouch` | Multi-touch cho 2 nút tap L/R, swipe, hold; 1 file `.inputactions` duy nhất |
| UI | **uGUI (Canvas)** + TextMeshPro | Style neo-brutalist = 9-slice sprite + shadow layer, uGUI làm nhanh và dễ animate hơn UI Toolkit runtime |
| Physics | Physics2D (chỉ dùng cho bóng rổ / bóng bàn / bóng đá) | Chạy ngắn & chạy bền: kinematic, tự tính, không cần physics |
| Anim | Sprite-sheet frame animation (Animator) | Rẻ hơn 2D Animation (bone) cho scope 2 tháng |
| Audio | AudioMixer 2 group `Music` / `SFX`; timing nhịp bằng `AudioSettings.dspTime` | Tránh lệch nhịp do latency Android |
| Save | JSON tại `Application.persistentDataPath` (atomic write) + PlayerPrefs cho settings | Offline hoàn toàn, không network |
| Loading | Scene per minigame, `LoadSceneMode.Additive` | Không dùng Addressables (overkill với 5 minigame) |
| VCS | Git + GitHub, `.gitignore` Unity chuẩn, **Force Text serialization** + `.gitattributes` merge=unityyamlmerge | Nhiều người sửa scene/prefab |
| Định dạng màn hình | **Landscape 1920×1080** (khoá landscape, cho phép cả Left + Right) | Khớp khung `16/9` của `.game-container` trên desktop prototype → port UI 1:1; 2 nút tap 2 góc dưới = **2 ngón cái**, thoải mái hơn portrait; side-scroll (chạy ngắn/bền) và sân bóng rộng đẹp hơn hẳn |

**Bước 0 bắt buộc (máy hiện tại chưa có Unity):** cài Unity Hub → Unity 6 LTS + module **Android Build Support** (OpenJDK + Android SDK & NDK). Máy đã có `~/Android/Sdk`, có thể trỏ External Tools vào SDK này hoặc dùng SDK do Unity quản lý (khuyên dùng SDK của Unity để đồng bộ cả nhóm).

---

## 2. Kiến trúc

### 2.1 Scene
```
Bootstrap (persistent)  → GameManager, GameSession, AudioManager, SaveSystem,
                          SceneRouter, UIRoot (Canvas overlay), LoadingScreen
Menu                    → MainMenu, LevelSelect, Result (3 screen trong 1 scene)
MG_Sprint / MG_Endurance / MG_Volleyball / MG_Basketball
MG_PingPong / MG_Badminton / MG_Football / MG_Boss
                        → load Additive, unload khi xong
```

### 2.2 Map React → Unity (giữ đúng luồng `App.tsx`)
| Prototype | Unity |
|---|---|
| `Screen` state machine | `SceneRouter` + `ScreenStack` (enum `MAIN_MENU / LEVEL_SELECT / GAMEPLAY / RESULT`) |
| `lives` useState | `GameSession.Lives` (5, không hồi giữa các môn) |
| `LevelId` union type | `SubjectId` enum + `SubjectConfig` ScriptableObject |
| `finishGame(success, score, rank)` | `MinigameResult { bool Pass; float Score; Rank Rank; Dictionary<string,float> Stats }` — `Score` luôn nằm trong `0..10` |
| `LEVEL_INFO` record | asset `SubjectConfig` (name, icon, goal, color, scene, timeLimit, passThreshold) |
| `getScoreRank` | `ScoreUtil.ToRank(float)` — S≥9, A≥8, B≥7, C≥6, D≥5, F<5 |
| `INSTRUCTOR_QUOTES_*` | `InstructorQuoteSet` ScriptableObject (chill / urgent) |
| `FloatingText` | `FloatingTextPool` (object pool, không Instantiate/Destroy runtime) |

### 2.3 Lớp input dùng chung (chìa khoá tiết kiệm thời gian)
Tất cả minigame + hình phạt + boss dùng lại đúng 5 detector:

| Detector | API | Dùng ở |
|---|---|---|
| `TapMashDetector` | `TapsPerSecond`, `OnTap` | Chạy ngắn, phạt "Bật cóc" |
| `AlternateTapDetector` | `OnValidTap(side)`, `OnWrongSide` | Chạy ngắn, phạt "Chạy quanh sân" |
| `RhythmBeatDetector` | `OnJudge(Perfect/Good/Miss, deltaMs)`, dùng `dspTime` | Chạy bền, phạt "Chống đẩy", Boss |
| `HoldDetector` | `OnHoldStart/OnHoldEnd(duration)`, `ChargeRatio 0..1` | **Cầu lông (nạp lực)**, Chạy bền (hít thở), phạt "Chống đẩy" |
| `SwipeDetector` | `OnSwipe(dir, length, duration, curvature)` | Bóng chuyền, Bóng rổ, Bóng đá, chướng ngại vật chạy bền |
| `TimingWindow` | `Evaluate(value) → 0..1 accuracy` | Bóng chuyền, Bóng rổ (apex), Bóng bàn |

### 2.3b `BallRig` — component bóng dùng chung (chìa khoá gộp 4 môn bóng)
Một prefab + script duy nhất phục vụ **cả 5 môn có vật thể bay**: Bóng chuyền, Bóng rổ, Bóng bàn, Cầu lông, Bóng đá.
```csharp
class BallRig : MonoBehaviour {
  Rigidbody2D rb;                       // gravity, Continuous collision
  float ApexHeight { get; }             // đỉnh quỹ đạo dự đoán
  bool IsNearApex(float vThreshold);    // |velocity.y| < threshold
  Vector2 PredictLandingPoint();        // cho AI đón bóng + đổ bóng (shadow)
  void Launch(Vector2 dir, float force, float curvature); // xoáy = lực Magnus liên tục
  void AttachTo(Transform hand);        // kinematic khi đang giữ bóng
  event Action<Collider2D> OnHit;
  TrajectoryPreview preview;            // đường dashed khi ngón còn kéo
  BallShadow shadow;                    // đọc độ cao
  FlightProfile profile;                // ScriptableObject: drag, bounciness, gravityScale
}
```
`FlightProfile` (ScriptableObject) tách đặc tính bay ra khỏi code — **Cầu lông** chỉ là 1 profile khác: `linearDrag` rất cao + `bounciness = 0` + không nảy đất → quả cầu vọt nhanh rồi **rơi dốc đứng**, đúng cảm giác thật, không cần script riêng.

→ 5 môn chỉ khác **luật + AI + input mapping + FlightProfile**, không viết lại vật lý/preview/shadow.

### 2.4 MinigameBase (state machine chung)
```
Intro → Tutorial (2–3s, icon thao tác, tự ẩn) → Countdown (3-2-1) → Play
      → Resolve (Pass / Fail) → báo về GameSession
```
`MinigameBase` lo: pause, quit, HUD (tim + timer + thanh chỉ số), tutorial overlay, countdown, timeout, gọi `Finish(MinigameResult)`. Mỗi minigame chỉ implement `OnPlayTick(dt)` + logic riêng → **giảm ~40% code trùng**.

### 2.5 Contract pass + điểm chung cho 7 môn

Mỗi môn có đúng **1 `PrimaryObjective`**. `Pass` chỉ được xác định bằng việc hoàn thành mục tiêu chính; combo, style hoặc thành tích phụ **không được tạo đường tắt để pass**.

- **Fail** → `Score = 0`, `Rank = F`.
- **Pass** → điểm chuẩn hóa `0..10`, làm tròn 1 chữ số thập phân:
  - `6.0 điểm` — hoàn thành `PrimaryObjective`.
  - `0..2.0 điểm` — độ chính xác/timing của input chính.
  - `0..1.0 điểm` — hiệu suất riêng của môn (thời gian, stamina, số lượt lỗi...).
  - `0..1.0 điểm` — mastery bonus (combo, perfect, clean sheet...).
- Rank dùng chung: `S ≥ 9`, `A ≥ 8`, `B ≥ 7`, `C ≥ 6`, `D ≥ 5`, `F < 5`.
- `SubjectConfig.passThreshold` mô tả ngưỡng của **mục tiêu gameplay** (thời gian, số điểm trận, số bàn...), không phải điểm tổng `0..10`.

Nguyên tắc công bằng dùng chung: mọi sự kiện gây bất lợi phải có **cue nhìn/nghe trước khi xảy ra + thao tác đối phó rõ ràng**. Không dùng RNG trực tiếp làm nhân vật khựng, AI bỏ bóng, hoặc đổi quỹ đạo giữa đường bay mà người chơi không thể dự đoán.

### 2.6 Cây thư mục
```
Assets/_Project/
  Art/{Characters,Environments,UI,FX}
  Audio/{Music,SFX}
  Fonts/                     # TMP asset Baloo2 + Nunito (charset Tiếng Việt)
  Prefabs/{UI,Gameplay}
  ScriptableObjects/{Subjects,Rhythm,Difficulty,Quotes}
  Scenes/
  Settings/{URP,Input,AudioMixer}
  Scripts/
    Core/       GameManager GameSession SceneRouter SaveSystem EventBus AudioManager Pool
    Input/      TapMashDetector AlternateTapDetector RhythmBeatDetector HoldDetector SwipeDetector TimingWindow
    UI/         BrutalButton SafeAreaFitter ScreenBase MainMenuScreen LevelSelectScreen ResultScreen HeartBar FloatingTextPool
    Minigames/Common/  MinigameBase MinigameResult MinigameHUD RivalRunnerAI ParallaxLayer
    Minigames/{Sprint,Endurance,Volleyball,Basketball,PingPong,Badminton,Football}/
    Minigames/Common/Ball/  BallRig TrajectoryPreview BallShadow
    Punishment/ ChallengeSequence PunishmentController
    Boss/       BossPhaseController
Assets/Tests/{EditMode,PlayMode}
```

---

## 3. Port design system (neo-brutalist) sang Unity

Trích từ `src/index.css`:

- **Palette** → `UITheme.asset` (ScriptableObject):
  `primary #FF595E`, `accent #FFCA3A`, `background #1982C4`, `success #8ACB88`, `warning #FFCA3A`, `card #FFFFFF`, `muted #E2E8F0`, `muted-fg #475569`, viền/shadow `#000000`.
- **Font**: `Baloo 2` (ExtraBold 800) cho display/nút; `Nunito` (Bold/Black) cho body. Sinh **TMP Font Asset với charset Tiếng Việt đầy đủ** (dump toàn bộ ký tự từ file text VN + range Latin Extended Additional `1EA0-1EF9` + `0110/0111` + `01A0-01B0`). Bật fallback dynamic để không thiếu glyph.
- **Nút** (`.btn-*`): 9-slice sprite radius 16, viền đen 4px. Shadow = **child Image đen** offset `(+4, −4)` đặt phía sau.
  `BrutalButton.cs`: pointer-down → foreground dịch `(+4,−4)`, shadow offset về `0` (khớp `:active`), phát SFX; pointer-up/exit → reset. Tween 0.1s (khớp `transition: transform .1s`).
- **Card** (`.brutal-card`): 9-slice radius 24, viền 4, shadow offset `(+6, −6)`.
- **Text effect**: `.text-shadow` → TMP **Underlay** (offset x 0.04 / y −0.04, màu đen, softness 0); `.text-stroke-dark` → TMP **Outline** đen 0.2 + Underlay.
- **Safe area**: `SafeAreaFitter.cs` đọc `Screen.safeArea` → set anchor RectTransform (notch/punch-hole), tương đương `env(safe-area-inset-*)`.
- **Canvas Scaler**: Scale With Screen Size, reference **`1920×1080`**, **Match Width Or Height = `1.0` (theo chiều cao)**.
  → UI scale theo chiều cao; máy rộng hơn 16:9 (19.5:9, 20:9, 21:9) **không co UI**, chỉ hở thêm ở 2 bên → neo UI vào góc/cạnh, không neo vào giữa.
- Sprite 9-slice tự vẽ trong Figma/Aseprite, xuất PNG @3x, `Sprite (2D and UI)`, border set trong Sprite Editor.

### 3.1 Chiến lược layout Landscape (đa tỉ lệ)
Điện thoại Android thực tế rộng hơn 16:9 rất nhiều (19.5:9 → 21:9). Quy tắc:
- **Safe zone gameplay = 16:9 giữa màn hình.** Mọi thứ *bắt buộc thấy* (thanh stamina, tim, timer, bóng, nhân vật, khung thành, lưới) nằm trong vùng này.
- Phần rộng dư 2 bên **chỉ dùng cho nền/parallax** → art nền vẽ tối thiểu **2560×1080** (21:9), không để hở màu trơn.
- Camera orthographic: `orthographicSize` **cố định theo chiều cao** → máy rộng thấy nhiều nền hơn, không bao giờ thấy ít gameplay hơn.
- **Safe area landscape nằm ở 2 cạnh trái/phải** (notch/tai thỏ + thanh gesture) → `SafeAreaFitter` phải neo cả `left/right`, không chỉ `top/bottom` như portrait.
- **Vị trí ngón tay**: nút tap L/R ở **2 góc dưới**, đường kính ≥ 140px @1080p; vùng giữa dưới để trống (tránh thanh gesture Android); nút Pause góc trên **phải** (xa ngón cái đang chơi, tránh bấm nhầm).
- Vuốt (bóng chuyền / rổ / đá): nhận swipe trên **toàn bộ** vùng gameplay, không ép vuốt trong khung nhỏ.

---

## 4. Thiết kế chi tiết 7 minigame

### M1 — Chạy ngắn 100m (`MG_Sprint`)
- **Input**: 2 nút lớn L/R hai bên (GDD slide 8). Tap **luân phiên đúng** → xung lực đầy `+18` (speed cap `120`); tap trùng bên → chỉ `40%` xung lực (chặn mash 1 ngón).
- **Bonus nhịp**: nếu khoảng tap nằm trong `[0.9x, 1.1x]` của nhịp trung bình trượt → multiplier `×1.25` + combo meter (thoả yêu cầu "hoặc theo 1 nhịp điệu nào đó").
- **Vật lý**: `speed -= 15 * dt` (giảm dần); `distance += speed * dt * k`.
- **Stamina**: `speed > 20` → `stamina -= speed * dt * 0.25`; ngược lại `+6/s`. Stamina 0 → fail.
- **Nhân vật chạy cùng (mới)**: `RivalRunnerAI` × 3, lane 1/3/4 (player lane 2).
  - Mỗi AI dùng một `RivalPaceProfile` cố định theo difficulty (khởi động nhanh / giữ sức / nước rút); profile của cuộc đua được hiện bằng icon trước countdown để người chơi đọc đối thủ.
  - Không rubber-band âm thầm. AI chạy đúng pace profile đã chọn; mọi pha tăng tốc đều có anim/cue trước khi tốc độ thay đổi.
  - AI "nước rút" ở mốc 70% quãng đường; có anim `run/burst/stumble/celebrate/fail`.
  - HUD hiện thứ hạng hiện tại `1st/2nd/3rd/4th`.
- **Thử thách có counterplay**:
  - Gió ngang xuất hiện tại checkpoint cố định, có cờ + âm thanh báo trước `0.8s`; trong 2 nhịp tiếp theo vùng timing của bên ngược gió hẹp hơn, nhưng chuỗi L/R không bị phá.
  - Sự kiện tuột dây giày ngẫu nhiên bị loại bỏ. Thay bằng vạch đường trơn có cue rõ; tap đúng nhịp khi đi qua để không mất tốc độ.
- **PrimaryObjective / Pass**: hoàn thành 100m trong `timeLimit` (≈14s). Thứ hạng không tạo đường tắt để pass.
- **Điểm `0..10`**: `6` hoàn thành; `0..2` theo thời gian; `0..1` stamina còn lại; `0..1` cadence combo + thứ hạng.
- **Trình bày**: side-scroll, parallax 3 lớp (trời / khán đài / đường chạy), player khoá ở x = 35% màn hình.

### M2 — Chạy bền 1500m (`MG_Endurance`)
- **Input chính**: tap **đều theo nhịp** metronome. BPM `100 → 140` tăng dần theo vòng. Mỗi thời điểm chỉ có **một mode input đang active**; HUD đổi màu và phát cue khi chuyển mode.
- **Judge** (`RhythmBeatDetector`, mốc theo `dspTime`): `Perfect ±80ms`, `Good ±160ms`, còn lại `Miss`.
  - Perfect → không tụt stamina, +combo; Good → tụt nhẹ; Miss → tụt mạnh + nhân vật loạng choạng.
- **Pha thở**: mỗi 8 nhịp có "beat xanh" → `RhythmBeatDetector` tạm ngừng đòi tap; người chơi **giữ** (`HoldDetector`) trọn 1 nhịp để hồi stamina. Nhả xong mới quay lại pha tap ở beat kế tiếp.
- **Pha chướng ngại vật**: vũng nước / cọc / sinh viên khác chỉ xuất hiện tại beat được đánh dấu trước trong `LapPattern`; rhythm judge tạm ngừng đúng 1 beat để nhận `swipe up` = nhảy hoặc `swipe down` = trượt. Swipe đúng không bị tính Miss; bỏ qua → `stamina −15` + khựng.
- `LapPattern` là pattern authored, không random; người chơi luôn thấy icon chướng ngại tối thiểu 2 beat trước khi cần swipe.
- **Ức chế**: 10s cuối stamina tụt nhanh **+20%**; BPM tăng mỗi vòng.
- **Vòng**: 3–4 vòng, side-scroll + đếm vòng + mini-map oval góc trên (rẻ hơn làm view top-down riêng).
- **PrimaryObjective / Pass**: hoàn thành đủ số vòng trước khi stamina = 0 và trong `timeLimit`.
- **Điểm `0..10`**: `6` hoàn thành; `0..2` tỷ lệ Perfect/Good; `0..1` stamina còn lại; `0..1` longest combo + obstacle clean.

### M3 — Bóng chuyền (`MG_Volleyball`) — GDD slide 10
- **Setup**: sân nhìn ngang, lưới giữa, bên mình = player + 1 đồng đội AI, bên kia = đối thủ AI. Bóng dùng `BallRig`.
- **Cơ chế 3 lần chạm** (đúng luật bóng chuyền, và tạo khác biệt rõ với Bóng rổ): **hướng vuốt quyết định động tác**, không phải lực.

  | Vuốt | Động tác | Tác dụng |
  |---|---|---|
  | Xuống / chéo xuống | **Đỡ** (dig) | Cứu bóng thấp, dựng bóng cao lên |
  | Lên | **Nâng** (set) | Đưa bóng vào tầm đập cho lượt kế |
  | Ngang / chéo xuống về phía lưới | **Đập** (spike) | Dứt điểm qua lưới, ăn điểm |

- **Điều kiện hợp lệ**: bóng phải nằm trong `reachZone` của nhân vật đang active **và** đúng động tác cho pha bóng đó (bóng thấp → phải đỡ; bóng đã nâng cao & gần apex → mới đập được). `TimingWindow` cho `accuracy 0..1` → độ chính xác điểm rơi bên sân đối thủ.
- **Chỉ số**: **combo** số lần đỡ/xử lý thành công liên tiếp (GDD slide 10), HUD hiện `TOUCH 1/2/3` để dạy luật trong 2s.
- **Thử thách có counterplay**:
  - Sau rally thứ 3, đối thủ mở khóa cú spin/fake. Animation tay + trail màu báo loại quỹ đạo trước khi bóng qua lưới; quỹ đạo không đổi vô cớ giữa đường bay.
  - Bỏ sự kiện đồng đội AI ngẫu nhiên đứng im. Player và đồng đội auto-position theo `PredictLandingPoint()`; thử thách của người chơi chỉ là chọn động tác + timing.
  - Đối thủ AI đập trả với tốc độ tăng theo phase đã báo trên HUD.
- **PrimaryObjective / Pass**: đạt `targetScore` (mặc định 5 rally point) trước đối thủ và trước khi hết giờ. `targetCombo` chỉ là mastery bonus.
- **Điểm `0..10`**: `6` thắng trận; `0..2` accuracy của dig/set/spike; `0..1` số rally thua; `0..1` longest combo + perfect spike.

---

### M4 — Bóng rổ (`MG_Basketball`) — chuyền → alley-oop → kết thúc ở apex
- **Setup**: player + đồng đội AI + 1 rổ. Bóng dùng `Rigidbody2D` với `collisionDetection = Continuous`; nhân vật auto-position để input chỉ tập trung vào chuyền và timing.
- **Khác biệt với M3**: M3 chọn *động tác bằng hướng vuốt*; M4 tạo *đường chuyền bằng hướng + lực*, sau đó chốt pha bóng bằng *timing apex*.
- **Vòng lặp một possession**:
  1. Player đang giữ bóng (`BallRig.AttachTo(hand)`), không có apex ở trạng thái này.
  2. Người chơi **vuốt chuyền**; `SwipeDetector.dir` quyết định hướng và `length` quyết định lực. Không dùng `curvature` cho đường chuyền.
  3. Đồng đội AI bắt bóng rồi tung một đường alley-oop đã authored về vùng trước rổ. Loại đường tung (thấp / chuẩn / cao) được báo bằng animation và màu trail, không có sai số RNG âm thầm.
  4. Player auto-run/cắt vào rổ. Khi bóng đang bay gần apex, người chơi tap để kết thúc pha bóng; timing quyết định layup / dunk / miss.
  5. Hết pha, bóng reset về player và possession mới bắt đầu; độ khó tăng bằng cửa sổ timing hẹp dần hoặc loại đường alley-oop khó hơn, không tăng cả hai cùng lúc.
- **Apex window**: hợp lệ khi `ball.y ∈ [hMin, hMax]` và `|velocity.y| < vApexThreshold`.
  - `TimingWindow.Evaluate()` → `accuracy 0..1`; Perfect = `SWISH/DUNK!`, cộng mastery bonus nhưng không nhân đôi rally point.
  - HUD dùng vòng tròn thu quanh bóng + vùng apex phát sáng; cue xuất hiện ngay từ possession đầu.
  - Tap quá sớm → layup yếu/có thể trượt; tap quá muộn → bóng qua tầm với. Kết quả luôn giải thích bằng nhãn `EARLY / PERFECT / LATE`.
- **PrimaryObjective / Pass**: ghi đủ `targetBaskets` (mặc định 5) trong 30s. Combo không tạo đường tắt để pass.
- **Điểm `0..10`**: `6` hoàn thành; `0..2` apex accuracy; `0..1` số possession cần dùng; `0..1` consecutive baskets + perfect finish.

### M5 — Bóng bàn (`MG_PingPong`)
- **Input**: tap đúng lúc bóng vào `hitZone` (dùng lại `TimingWindow` + `BallRig`). Tốc độ bóng `+8%` mỗi lượt đánh qua lại (GDD slide 11).
- Tuỳ chọn mở rộng nếu còn thời gian: swipe up/down → topspin / lob (chỉ set flag `spin`, không đổi kiến trúc).
- **AI** đối thủ dùng `ReturnPattern` authored theo difficulty; không dùng `missChance` RNG. AI trả hỏng khi cú đánh của player có placement/accuracy vượt ngưỡng phòng thủ của pattern hiện tại. Bàn nhìn từ góc trên (2.5D), bóng có bóng đổ để đọc độ cao.
- Tốc độ tăng có `maxBallSpeed`; sau khi đạt cap, độ khó chỉ tăng bằng placement pattern để rally không trở thành bất khả thi theo cấp số nhân.
- **PrimaryObjective / Pass**: thắng trận chạm 5 điểm. Rally dài chỉ tăng mastery bonus.
- **Điểm `0..10`**: `6` thắng trận; `0..2` timing accuracy; `0..1` chênh lệch điểm; `0..1` longest rally + perfect return.

### M6 — Cầu lông (`MG_Badminton`) — nạp lực + nhả đúng lúc
- **Trục input riêng (không trùng môn nào)**: **nhấn giữ để nạp lực → nhả**. `HoldDetector.ChargeRatio 0..1` (vòng tròn nạp quanh nhân vật), **thời điểm nhả** quyết định loại cú đánh:

  | Nhả khi cầu ở | Cú đánh | Hiệu ứng |
  |---|---|---|
  | Cao (trên vai) | **Đập** (smash) | Cầu lao nhanh & dốc, khó đỡ, nhưng cửa sổ timing hẹp |
  | Ngang tầm | **Đánh thẳng** (drive) | An toàn, tốc độ trung bình |
  | Thấp (dưới hông) | **Phông** (lift) | Cầu bay cao, đổi nhịp, mua thời gian |

  → **Khác Bóng bàn rõ ràng**: Bóng bàn = *tap 1 nhịp, chỉ có timing*; Cầu lông = *giữ–nhả, có 2 trục (lực nạp × độ cao lúc nhả)*. Người chơi cảm nhận được ngay là 2 môn khác nhau.
- **Vật lý**: `BallRig` + `FlightProfile_Shuttle` (drag cao, không nảy) → cầu **rơi dốc**, buộc người chơi đọc điểm rơi thay vì phản xạ thuần.
- **Nạp lực quá tay**: giữ vượt `1.0` → **quá lực**, cầu bay ra ngoài sân → mất điểm (yếu tố "ức chế có chủ đích", khớp goal text prototype *"Bay cao không bằng ngã đau"*).
- **Đối thủ AI**: xen **drop shot** sát lưới (buộc chạy lên gấp) và **lob** cuối sân (buộc lùi) theo `RallyPattern` asset; difficulty cao dùng pattern authored có tần suất drop shot lớn hơn, không chọn ngẫu nhiên từng cú.
- **Thử thách có counterplay**: ở rally dài (≥5 lượt), quạt trần có thể bật theo `RallyPattern`; icon hướng gió + audio cue xuất hiện trước cú trả của đối thủ. `TrajectoryPreview/BallShadow` cập nhật điểm rơi mới ngay khi gió bắt đầu, không đổi điểm rơi bí mật.
- **PrimaryObjective / Pass**: thắng trận chạm 5 điểm. Rally dài chỉ tăng mastery bonus.
- **Điểm `0..10`**: `6` thắng trận; `0..2` release timing + charge accuracy; `0..1` chênh lệch điểm; `0..1` shot variety + longest rally.

---

### M7 — Bóng đá, sút phạt (`MG_Football`)
- **Input**: vuốt = hướng + lực + xoáy (dùng lại `SwipeDetector`, map `curvature` → lực Magnus).
- **Thủ môn AI**: bay theo pattern (`GKPatternSet` asset) + reaction delay `0.15s`, vùng bay phủ 1 trong 6 ô khung thành. Mỗi phase chọn trước một modifier: tăng reaction của thủ môn **hoặc** thu hẹp vùng mục tiêu hợp lệ.
- **Trình bày**: góc nhìn chính diện, `TrajectoryPreview` dashed khi ngón còn đang kéo (giúp học thao tác nhanh).
- Mỗi phase chỉ tăng **một** trục độ khó: hoặc reaction của thủ môn, hoặc vùng mục tiêu hợp lệ; không đồng thời tăng AI và thu hẹp khung thành.
- **PrimaryObjective / Pass**: trong 5 quả sút, ghi ≥3 bàn.
- **Điểm `0..10`**: `6` hoàn thành; `0..2` placement accuracy; `0..1` số bàn vượt mốc 3; `0..1` shot variety + perfect corner.

### Hình phạt + Boss (tái dùng 100%)
- `ChallengeSequence` (ScriptableObject) = list `(mechanic, duration, target)`:
  - **Bật cóc** → `TapMashDetector`, thanh tiến trình theo tốc độ tap.
  - **Chống đẩy** → `RhythmBeatDetector` + `HoldDetector` (rhythm-game).
  - **Chạy quanh sân** → `AlternateTapDetector` (rút gọn từ M1).
- **Boss**: cùng `ChallengeSequence` nhưng nối 3 mechanic liên tiếp, không nghỉ, 30–40s, BPM/target cao nhất → **gần như không tốn code mới** (đúng GDD slide 14).

---

## 5. Dữ liệu & lưu game (offline)
```csharp
[Serializable] class SaveData {
  int version;               // migration
  int lives;                 // 0..5
  SubjectRecord[] subjects;  // id, passed, bestScore (0..10), rank, stars, attempts
  bool bossUnlocked;
  Settings settings;         // musicVol, sfxVol, vibration, rhythmOffsetMs
}
```
- Ghi file `save.json` tại `persistentDataPath`, **atomic**: ghi `save.tmp` → `File.Replace`. Chống mất dữ liệu khi app bị kill.
- Ghi khi: kết thúc môn, mất tim, đổi settings, `OnApplicationPause(true)`.
- `rhythmOffsetMs`: có màn **calibrate nhịp** trong Settings (bắt buộc cho M2 trên Android).

---

## 6. Cấu hình build Android
| Setting | Giá trị |
|---|---|
| Package name | `com.kma.thechat` — app display name **"Thể Chất KMA"** |
| Orientation | **Landscape Left + Landscape Right** (bật cả 2, tắt Portrait & Portrait Upside Down) |
| Min API | 24 (Android 7.0) |
| Target API | bản mới nhất Unity hỗ trợ (Play yêu cầu 34/35+) |
| Scripting backend | **IL2CPP** (bắt buộc cho ARM64) |
| Target arch | ARM64 (+ ARMv7 nếu cần máy cũ) |
| Api Compatibility | .NET Standard 2.1 |
| Managed Stripping | Medium (test kỹ reflection/JSON) |
| Texture | ASTC (6×6 cho art, 4×4 cho UI sắc nét) |
| Graphics API | Vulkan + OpenGLES3 fallback |
| Audio | DSP Buffer = **Best latency** (rhythm) |
| Frame rate | `Application.targetFrameRate = 60`, vSync off |
| Output | APK cho demo/báo cáo, AAB nếu lên Play |

**Ngân sách hiệu năng/máy tầm thấp**: < 200 draw call, 1 Sprite Atlas / scene, không realtime light, không post-process, `Object Pool` cho mọi FX & floating text, không `GetComponent` trong `Update`.

---

## 7. Asset pipeline
- Sprite: atlas 2048, `Pixels Per Unit = 100`, filter Bilinear, compression ASTC.
- Nhân vật: sprite-sheet 8–12 fps. State: `idle / run / burst / stumble / celebrate / fail`. Giảng viên: `idle / angry / whistle / nod`.
**Chiến lược đã chốt: ƯU TIÊN ASSET CÓ SẴN.** Thứ tự xử lý mỗi hạng mục art:
1. **Tìm asset free trước** — Kenney.nl (ưu tiên: license CC0, style flat/outline khớp neo-brutalist sẵn), itch.io, OpenGameArt, Google Fonts.
2. **Chỉnh màu asset về `UITheme`** (hue shift / recolor) để 7 môn trông cùng 1 game, không bị chắp vá — đây là công việc *bắt buộc*, không bỏ qua.
3. **Chỉ tự vẽ 3 thứ** (không có asset free nào khớp được):
   - **Giảng viên** — điểm nhấn cảm xúc mạnh nhất, xuất hiện xuyên suốt (GDD slide 14).
   - **Nhân vật chính** (nếu không tìm được sprite-sheet có đủ state `idle/run/stumble/celebrate/fail`).
   - **UI 9-slice + icon môn** — style neo-brutalist rất đặc thù, tự vẽ nhanh hơn tìm.
- **Kiểm license trước khi dùng**, không sau: CC0 / CC-BY / OFL = OK; **tránh** NC nếu có ý định lên Play Store. Ghi ngay vào `Assets/_Project/CREDITS.md` mỗi lần thêm asset (bắt buộc cho báo cáo — bị hỏi là chắc chắn).
- **Rủi ro của asset-first**: 7 môn lấy từ nhiều nguồn → lệch style. Chặn bằng bước (2) + chốt **1 người duyệt art cuối** trước khi merge.
- Audio: SFX `.wav` → Vorbis q70, `Decompress On Load`; nhạc → `Streaming`.

---

## 8. Roadmap 8 tuần (khớp GDD slide 19, chi tiết hoá)

| Tuần | Deliverable | Gate nghiệm thu |
|---|---|---|
| **0** (2–3 ngày) | Cài Unity **6.3 LTS** + Android module; tạo project Universal 2D; git init + `.gitignore`/`.gitattributes`; build APK "hello" lên máy thật | APK chạy được trên máy Android thật |
| **1** | `Core`: GameManager, GameSession (5 tim, 2 lượt), SceneRouter, SaveSystem, AudioManager, Pool. `Input`: 5 detector + `TimingWindow` + EditMode test | Test detector pass; save/load OK |
| **2** | Port design system: UITheme, 9-slice sprite, `BrutalButton`, `SafeAreaFitter`, TMP font VN. Dựng **MainMenu + LevelSelect + Result** giống prototype | So sánh side-by-side với prototype web, sai lệch thị giác nhỏ |
| **3** | `MinigameBase` + **M1 Chạy ngắn** đầy đủ (AI đối thủ + gió + tuột dây) | Chơi được, cân bằng thô, 60fps máy mid |
| **3.5** | **`BallRig`** + `TrajectoryPreview` + `BallShadow` (làm song song tuần 3, người khác) | Bóng bay/đổ bóng/preview OK trong scene test |
| **4** | **M2 Chạy bền** (rhythm + pha thở + obstacle theo beat + calibrate offset) | Tap/hold/swipe không active chồng nhau; swipe đúng không bị tính Miss |
| **5** | **M3 Bóng chuyền** + **M4 Bóng rổ alley-oop** (cùng dùng `BallRig` + `SwipeDetector`) | Bóng chuyền kiểm tra chọn động tác; bóng rổ kiểm tra lực chuyền + apex timing |
| **6** | **M5 Bóng bàn** + **M6 Cầu lông** (chung rig "vợt + lưới + rally", khác `FlightProfile` & input) | Người chơi mô tả được 2 môn khác nhau; cầu rơi dốc đúng cảm giác |
| **7** | **M7 Bóng đá** + hệ thống **phạt** + **Boss** | Chạy trọn core loop: map → môn → phạt → lượt 2 → mất tim → game over → boss |
| **8** | Playtest nội bộ (≥8 người ngoài nhóm) + cân bằng + tối ưu + build APK + tài liệu demo | Pass lượt 1 mỗi môn ≈ 40–60%; APK < 100MB; cold start < 4s máy tầm thấp |
| **2 → 7** | **Luồng art/anim/audio chạy SONG SONG** (người khác, không block code): nhân vật, giảng viên, 7 bối cảnh, SFX, nhạc | Mỗi tuần giao art của minigame **tuần sau** → code không chờ art |

**Cảnh báo tiến độ (quan trọng)**: 7 minigame trong 8 tuần → art/audio **không còn tuần riêng** như bản 5 môn. Bắt buộc:
1. **Tách luồng art song song từ tuần 2** (hàng cuối bảng) — nếu nhóm không có người làm art riêng, phải giảm còn 5–6 môn hoặc giãn lịch ~2 tuần.
2. Code chạy bằng **placeholder art** (hình khối màu) và chỉ swap sprite ở cuối — `SubjectConfig` + prefab đã tách data khỏi art nên swap không sửa code.
3. **Playtest cuốn**: chơi thử cuối mỗi tuần từ tuần 3, không dồn về tuần 8.

Ưu tiên **cắt** nếu trễ (theo thứ tự): xoáy bóng đá → topspin bóng bàn → gió/quạt cầu lông → mini-map chạy bền → đối thủ AI bóng chuyền (đổi thành "tường trả bóng" scripted) → số AI chạy ngắn (3 → 1) → `RallyPattern` cầu lông rút còn 1 pattern. **Không cắt môn nào** — vì đã chốt 1 môn = 1 minigame.

---

## 9. Rủi ro & cách chặn trước
| Rủi ro | Chặn |
|---|---|
| TMP thiếu glyph Tiếng Việt (ô vuông) | Sinh font asset từ **charset dump toàn bộ text VN** ngay tuần 2, không để cuối |
| Lệch nhịp rhythm trên Android (audio latency) | Timing theo `AudioSettings.dspTime`, DSP Best latency, thêm màn **calibrate offset** cho người chơi |
| Input System + UI raycast bắn 2 lần 1 tap | Gameplay tap chỉ qua `ScreenTapArea` (1 component), UI qua EventSystem; không đọc `Input.touches` rải rác |
| AI / sự kiện bị cảm giác "gian lận" | Không rubber-band hoặc RNG gây bất lợi âm thầm; dùng authored pattern/profile, cue trước và counterplay rõ |
| Scene/prefab merge conflict | Force Text serialization + `unityyamlmerge` + chia scene theo minigame, 1 người/1 minigame |
| Máy tầm thấp tụt fps | Ngân sách draw call, pooling, đo bằng Profiler trên **máy thật** từ tuần 3 (không đo trong Editor) |
| Scope phình (10 môn) | Chốt **7 môn + boss**; 3 môn còn lại (Hít đất, Nhịp điệu, Bơi lội) là node `Locked` |
| **7 minigame / 8 tuần → hết chỗ cho art & polish** | `BallRig` + `FlightProfile` dùng chung 5 môn; **asset-first** (mục 7) cắt phần lớn công vẽ; luồng art song song từ tuần 2; code chạy placeholder art; playtest cuốn từ tuần 3; cut list (không cắt môn) đã định trước |
| Asset free từ nhiều nguồn → game trông chắp vá | Bắt buộc recolor về `UITheme`; 1 người duyệt art cuối; ưu tiên **1 bộ asset chính** (Kenney) rồi mới bù từ nguồn khác |
| Bóng chuyền vs Bóng rổ bị cảm giác giống nhau | Cố ý tách trục input: **bóng chuyền = hướng vuốt (3 động tác)**, **bóng rổ = lực vuốt + timing apex**. Playtest xác nhận người chơi mô tả được khác biệt |
| **Bóng bàn vs Cầu lông** bị cảm giác giống nhau (cùng "vợt + lưới") | Tách trục input: **bóng bàn = tap, chỉ timing**; **cầu lông = giữ–nhả, 2 trục (lực nạp × độ cao lúc nhả)**. Thêm `FlightProfile` khác hẳn (cầu rơi dốc vs bóng nảy bàn). Playtest hỏi thẳng: "2 môn này khác nhau chỗ nào?" — không trả lời được = phải sửa |

---

## 10. QA
- **EditMode test**: `AlternateTapDetector` (đúng/sai bên), `RhythmBeatDetector` (biên ±80/±160ms), `TimingWindow.Evaluate`, `ScoreUtil.ToRank` (biên 5/6/7/8/9 trên thang 10), clamp/làm tròn score `0..10`, `SaveSystem` round-trip + migration version.
- **PlayMode test**: chạy trọn state machine `MinigameBase`; giả lập tap/swipe qua `InputTestFixture`.
- **Contract test từng môn**: chỉ `PrimaryObjective` mới đặt `Pass = true`; combo/mastery không thể pass thay mục tiêu chính; mọi event bất lợi phải phát cue trước cửa sổ phản ứng tối thiểu đã cấu hình.
- **Ma trận thiết bị**: 1 máy low-end (2GB RAM, GLES3) + 1 mid (Vulkan) + 1 máy có notch (kiểm safe area).
- **Bảng cân bằng**: Google Sheet `primaryObjective / timeLimit / targetScore / BPM / timingWindow / AI pattern / score weights` mỗi môn → tune bằng data playtest, không tune bằng cảm giác.

---

## 11. Quyết định đã chốt

| # | Hạng mục | Chốt |
|---|---|---|
| 1 | Hướng màn hình | **Landscape 1920×1080**, bật Left + Right, tắt portrait. Safe zone gameplay = 16:9 giữa, nền vẽ tới 21:9 (xem 3.1) |
| 2 | Tên & package | Display name **"Thể Chất KMA"**, package `com.kma.thechat` |
| 3 | `kma-pe.md` | **Không sửa.** PLAN.md là nguồn sự thật cho code; chuẩn bị slide phụ lục cho 3 điểm lệch (mục 0) |
| 4 | Art | **Ưu tiên asset có sẵn** (Kenney/itch.io/OpenGameArt), recolor về `UITheme`; chỉ tự vẽ giảng viên + nhân vật chính + UI 9-slice (mục 7) |
| 5 | Engine | **Unity 6.3 LTS**, pin 1 patch `6.3.x` cho cả nhóm |

**Còn 2 thứ nhỏ cần xác nhận khi bắt đầu tuần 0:**
- Patch chính xác của 6.3 LTS (lấy bản LTS mới nhất trong Unity Hub tại thời điểm cài) → ghi vào README để cả nhóm khớp.
- Tên nhóm + danh sách thành viên hiển thị ở MainMenu (GDD slide 1).
