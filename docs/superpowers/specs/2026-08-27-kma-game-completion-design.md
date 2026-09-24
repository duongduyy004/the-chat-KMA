# Design — Hoàn thiện "Vượt Thể KMA" thành game chơi được

Ngày: 2026-08-27
Trạng thái: đã duyệt qua brainstorming, sẵn sàng chuyển sang implementation plan.

> Tài liệu này **thay thế** `docs/IMPLEMENTATION-PLAN.md` (bản nháp trước khi brainstorm, đã xoá).
> Nguồn sự thật cho gameplay vẫn là `PLAN.md`. Tài liệu này ghi các **quyết định thiết kế** và **cách cắt công việc**; nơi nào lệch `PLAN.md` đều nói rõ.

---

## 1. Điểm khởi đầu

Repo hiện là **thư viện logic + test suite**, không phải game.

Đã có:
- 158 test (121 EditMode + 37 PlayMode) — theo README, chưa verify lại vì editor chưa cài.
- `GameSession` (5 tim, 2 lượt, route hình phạt), `SceneRouter`, `MinigameLifecycle`, `ScoreUtil`, `BallRig`.

Chưa có (đã kiểm bằng grep toàn `Assets/`):
- 0 Canvas (`!u!223`), 0 SpriteRenderer (`!u!212`), 0 prefab, 0 file `.png/.wav/.ttf/.anim/.controller/.mat`.
- `Map.unity` chỉ chứa 1 GameObject `SceneRouter` → không đường nào gọi `StartSubject()`.
- 0 hit `PlayerPrefs|persistentDataPath|JsonUtility|File.` → không có persistence.
- `SwipeDetector` không tồn tại; 4 "detector" ở `Progression/PunishmentController.cs:20-40` là class rỗng.
- `TrajectoryPreview`, `BallShadow` (PLAN §2.3b) không tồn tại.

Toolchain (đã xác minh lại — bản trước của tài liệu này ghi sai là "editor chưa cài"):
- Unity **6000.3.23f1 đã cài** tại `/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity`, kèm module `android`, `android-sdk-ndk-tools`, `android-open-jdk-17.0.18+8`. File `.tar.xz` trong `~/.config/unityhub/downloads/` là rác sau khi cài.
- Project vẫn pin `6000.3.22f1` → cần repin.
- CLI `unity` (`~/.local/bin/unity`) chạy headless được `install`, `install-modules`, `test`, `build`, `open`, `editors -i`. Không có lệnh package manager → thao tác package bằng cách sửa `Packages/manifest.json`.
- Version package khớp editor này (lấy từ `Editor/Data/Resources/PackageManager/Editor/manifest.json`): URP `17.3.0`, `com.unity.ugui` `2.0.0`, `com.unity.2d.sprite` `1.0.0`.
- Guid 6 scene trong `EditorBuildSettings.asset` **khớp** `.meta` thật (đã đối chiếu) — build settings viết tay không bị lệch guid.

---

## 2. Nguyên tắc xuyên suốt

**Không sửa rules engine đã có test.** Mọi thứ mới bọc bên ngoài qua interface / adapter / method additive. 158 test là tài sản cần bảo toàn, và là lưới an toàn duy nhất khi refactor.

Suy ra 3 quy tắc thao tác:
1. Thêm method/event mới thì được; đổi chữ ký hoặc hành vi method đã test thì không.
2. Khi PLAN.md và code đã cài đặt xung đột, **ưu tiên code đã test**, ghi điểm lệch vào mục 6.
3. Mỗi section phải để lại thứ chạy được và verify được, không để lại "sẽ nối sau".

Và một nguyên tắc về **đích**: mốc S5 ("chơi được trọn loop") là checkpoint giữa đường, **không** phải định nghĩa hoàn thành. Định nghĩa hoàn thành nằm ở §10 — chưa tick hết §10 thì chưa xong.

---

## 3. Nguồn lực & cách làm

- **1 người + AI, không deadline cứng.** 16 section chạy tuần tự. S1–S15 = 39 ngày-người; S16 ≥8 → tổng ~47 ngày-người ≈ 47 ngày thực. Cấu trúc phụ thuộc dùng để chọn thứ tự, không tăng tốc.
- **Không có người làm art** → S16 là section rủi ro nhất, cần brainstorm riêng khi tới.
- **Cách viết spec: lai (Approach 3).** S1–S5 spec đầy đủ (tài liệu này). S6–S16 giữ ở mức brief, nâng lên spec đầy đủ ngay trước khi implement.
  Lý do: S1–S5 là nơi quyết định sai đội giá 10 section sau. S6–S13 dựa trên rules API đã cố định và có test, brief đủ định hướng. Spec hết 15 upfront sẽ drift nặng vì S9–S13 phụ thuộc ball kit chưa tồn tại.

---

## 4. Cắt công việc — 16 section

Đường găng: `S1 → S4 → S5 → S8 → (môn bóng bất kỳ) → S15 → S16`.
S16 là section cuối vì nó cần toàn bộ nội dung đã xong.
S6, S7, S8, S14 mở khoá ngay sau S5. S9–S13 mở khoá ngay sau S8.

Mỗi section minigame có cùng shape:
> input mapping → gọi rules API sẵn có → HUD binding → dựng scene → thay placeholder → PlayMode test

---

## 5. Spec đầy đủ — S1 đến S5

### S1 — Toolchain & config (1d)

Không có quyết định kiến trúc; là checklist. Một điểm lệch PLAN đã chốt.

1. ~~Cài editor~~ — **đã xong**: `6000.3.23f1` + `android` + `android-sdk-ndk-tools` + OpenJDK đã cài. Chỉ cần xác nhận bằng `unity editors -i`.
2. **Repin** `ProjectSettings/ProjectVersion.txt` 22f1 → 23f1; sửa README. (Tránh tải lại 2GB. Cả nhóm pin 1 patch — PLAN §11 mục 5.)
3. Packages:
   - Thêm `com.unity.render-pipelines.universal` (**URP 2D Renderer**), `com.unity.2d.sprite`.
   - Xác nhận `com.unity.ugui` có mặt — TextMeshPro nằm trong package này ở Unity 6. Thiếu thì thêm.
   - **Xoá** `com.unity.multiplayer.center`.
4. **Quyết định: dùng URP 2D, tắt light + post-process ngay từ đầu.**
   Cân nhắc: PLAN §6 đã chốt không realtime light, không post-process — tức gần như bỏ hết lý do dùng URP, và Built-in sẽ nhẹ hơn trên máy low-end. Vẫn chọn URP vì project có ràng buộc báo cáo/bảo vệ và PLAN §0 đã phải chuẩn bị slide phụ lục cho 3 điểm lệch; thêm điểm lệch thứ 4 ở tầng engine là chỗ dễ bị hỏi vặn nhất mà lợi ích kỹ thuật nhỏ.
   Việc cần làm: tạo `URP-2D.asset` + `Renderer2D` asset, set Graphics/Quality Settings, mỗi Camera có `UniversalAdditionalCameraData`.
5. `ProjectSettings.asset` theo PLAN §6 + §11:
   ```
   productName             → Thể Chất KMA          (đang: the-chat-KMA)
   applicationIdentifier   → com.kma.thechat        (đang: rỗng)
   allowedAutorotateToPortrait / UpsideDown → 0     (đang: 1)
   allowedAutorotateToLandscapeLeft / Right → 1
   AndroidMinSdkVersion    → 24                     (đang: 25)
   AndroidTargetSdkVersion → mới nhất Unity hỗ trợ  (đang: 0)
   scriptingBackend        → IL2CPP                 (đang: rỗng = Mono)
   targetArchitectures     → ARM64 (+ARMv7 nếu cần)
   apiCompatibilityLevel   → .NET Standard 2.1
   managedStrippingLevel   → Medium
   graphicsAPIs (Android)  → Vulkan, OpenGLES3
   audio DSP buffer        → Best latency
   ```
6. Tạo cây thư mục PLAN §2.6 còn thiếu: `Art/{Characters,Environments,UI,FX}`, `Audio/{Music,SFX}`, `Fonts/`, `Prefabs/{UI,Gameplay}`, `Settings/{URP,Input,AudioMixer}`, `ScriptableObjects/{Subjects,Difficulty,Quotes}`.
7. **Normalize scene YAML.** 6 scene hiện viết tay, thiếu `RenderSettings`/`LightmapSettings`/`NavMeshSettings`. Mở từng scene trong Editor, save → commit riêng `chore: normalize scenes via Editor` → chạy lại 158 test. Kỳ vọng diff rất to; đó là bình thường, không phải lỗi.

**Gate S1**: batchmode compile không lỗi + 121 EditMode + 37 PlayMode pass + APK "hello" chạy trên máy Android thật.

---

### S2 — Presentation foundation (4d)

Mục tiêu: gameplay **thấy được**, không chạm rules engine.

#### Thành phần
```
Scripts/UI/        UITheme(SO) BrutalButton SafeAreaFitter ScreenBase
                   HeartBar FloatingTextPool MinigameHUD PhaseOverlay ResultPanel
Prefabs/UI/        HUD_Minigame  ResultPanel  PhaseOverlay  Btn_Brutal
Prefabs/Gameplay/  GameCamera (ortho, size cố định theo chiều cao, + URP camera data)
Fonts/             Baloo2-ExtraBold + Nunito-Bold, TMP asset charset VN
```

#### Sửa additive vào code đã có
- `MinigameLifecycle`: thêm `event Action<MinigamePhase> PhaseChanged`, phát trong `Tick` khi Phase đổi.
- `MinigameBase.Awake()` hiện hardcode `new MinigameLifecycle(2f, 3f)` → thay bằng `[SerializeField] float tutorialSeconds = 2f, countdownSeconds = 3f`. Default giữ nguyên nên test không đổi.

#### Quyết định S2-1 — hợp đồng dữ liệu HUD: **pull qua ViewModel**

`MinigameBase` thêm:
```csharp
protected virtual MinigameHudState BuildHudState();   // default: rỗng
```
`MinigameHudState` là struct: `timeRemaining`, `primary01`, `primaryLabel`, `secondary01`, `secondaryLabel`, `statusText`.
`MinigameHUD` đọc struct trong `Update` của **chính nó**.

Lý do: controller không biết HUD tồn tại → 0 null-check, và test hiện tại (dựng controller không có HUD) chạy y nguyên. `virtual` trả default rỗng → mỗi môn override trong section của nó.

Phương án loại: push model (`hud.SetTimer(x)`) buộc mọi controller null-check vì test không có HUD, và cưỡng chế controller biết về UI. Interface N-property thì phình dần theo môn.

HUD riêng của từng môn (beat ring, apex ring, mini-map, TOUCH 1/2/3) là **component riêng đặt trong scene môn đó**, không nhồi vào shell chung. 7 môn có HUD rất khác nhau; chung chỉ có timer, tiến độ mục tiêu, tim, pause, phase overlay.

#### Quyết định S2-2 — HUD đặt trong từng scene

`SceneRouter.LoadGameplayScene()` đã cài đặt bằng `LoadSceneMode.**Single**`, trong khi PLAN §1 nói Additive và PLAN §2.1 đặt `UIRoot` canvas trong Bootstrap persistent. Đây là **điểm lệch PLAN đã tồn tại trong code**, có test bao quanh.

Chọn: HUD prefab đặt trong từng scene, khớp `Single` mode đang chạy. Canvas persistent chỉ giữ loading screen + toast.
Phương án loại: đổi router sang Additive + UIRoot persistent — đúng tài liệu hơn nhưng phải sửa `SceneRouter` + `SessionRouteTransitioner` + test, ngay ở section nền tảng, khi chưa có gì chạy để verify.

#### Phần còn lại
- `UITheme.asset` — palette PLAN §3: primary `#FF595E`, accent `#FFCA3A`, background `#1982C4`, success `#8ACB88`, card `#FFFFFF`, muted `#E2E8F0`, muted-fg `#475569`, viền/shadow `#000000`.
- `BrutalButton`: pointer-down dịch `(+4,−4)`, shadow offset về `0`, tween `0.1s`, phát SFX. Card: 9-slice radius 24, viền 4, shadow `(+6,−6)`.
- TMP: `.text-shadow` → Underlay (offset x `0.04` / y `−0.04`, đen, softness 0); `.text-stroke-dark` → Outline đen `0.2` + Underlay.
- **Font VN làm ngay ở S2**, không để cuối — PLAN §9 rủi ro #1. Sinh TMP Font Asset với charset đầy đủ: dump toàn bộ ký tự text VN của game + range `1EA0–1EF9` + `0110/0111` + `01A0–01B0` + Latin cơ bản. Bật dynamic fallback.
- Canvas Scaler: Scale With Screen Size, reference `1920×1080`, **Match Width Or Height = 1.0** (scale theo chiều cao; máy rộng hơn 16:9 hở thêm 2 bên, không co UI → neo UI vào góc/cạnh).
- `SafeAreaFitter` neo **cả left/right** — landscape notch + thanh gesture nằm ở 2 cạnh (PLAN §3.1).
- `GameCamera.prefab` thêm vào **cả 6 scene hiện có**; orthographic size cố định theo chiều cao. (5 scene stub tạo ở S5 lấy prefab này.)

- **Vỏ tutorial** (`TutorialOverlay`): PLAN §2.4 chỉ cho 2–3s icon tự ẩn. Với 7 môn cơ chế khác hẳn nhau (nạp-lực-nhả của cầu lông không dạy nổi trong 2s), vỏ phải hỗ trợ **nhiều bước có thể bấm qua** + tuỳ chọn "bỏ qua" ghi vào save theo từng môn (`tutorialSeen[subject]`). Nội dung từng bước do section môn đó soạn.

**Gate S2**: mở `MG_Sprint`, Play → tutorial icon → countdown 3-2-1 → HUD timer + stamina chạy; bấm Left/Right thấy giá trị đổi.

---

### S3 — Shared input layer (3d)

#### Chốt trước: đặt tên
Detector thật đặt **hậu tố `Input`** trong asmdef mới `KMA.Input`. 4 stub cùng tên trong `KMA.Gameplay` giữ nguyên — `ChallengeSequenceTests.cs:88-98` assert đúng type của chúng. Stub tiếp tục giữ vai trò "mô tả mechanic đang active"; detector thật đẩy dữ liệu vào qua `PunishmentController.ReportDetectorProgress()`.

#### Quyết định S3-1 — detector là plain C# class, thời gian truyền vào

```
TapMashInputDetector.FeedTap(double t)                        → TapsPerSecond, OnTap
RhythmBeatInputDetector.FeedTap(double inputDsp, double beatDsp) → OnJudge(TimingJudge, deltaMs)
HoldInputDetector.FeedDown(double t) / FeedUp(double t)        → ChargeRatio 0..1, OnHoldStart/OnHoldEnd(duration)
AlternateTapInputDetector.FeedTap(Side, double t)              → OnValidTap(Side), OnWrongSide
SwipeInputDetector.FeedSample(Vector2, double t) / FeedEnd()   → OnSwipe(dir, length, duration, curvature)
```

Detector **không tự đọc clock**. Một MonoBehaviour duy nhất `GameplayInputRouter` đọc Input System + EnhancedTouch rồi feed vào detector.
`ScreenTapArea` là vùng UI định nghĩa nơi tap gameplay được tính — **điểm vào duy nhất**, chặn double-fire với EventSystem (PLAN §9).

Lý do: PLAN §10 yêu cầu EditMode test cho detector (biên `±80/±160ms`, đúng/sai bên, `ChargeRatio`) — EditMode không có scene, không có frame loop. Plain class + time injection cho test deterministic. Cũng khớp pattern đã có trong repo: rules = plain class, controller = MonoBehaviour.

`curvature` tính bằng độ lệch của chuỗi điểm mẫu so với đường thẳng đầu-cuối (dùng cho lực Magnus bóng đá, PLAN §M7).
`rhythmOffsetMs` cộng ở **tầng router**, không nhúng trong detector → màn calibrate ở S5 chỉ sửa 1 chỗ.

#### Quyết định S3-2 — S3 chỉ tạo `.inputactions`, không rewire

→ S3 thành **additive thuần**, không chạm file nào có test. Tồn tại 3 file input song song đến hết S7; chấp nhận được vì 1 người làm.

**Gate S3**: EditMode test 5 detector pass — biên `±80ms`/`±160ms` của rhythm, đúng/sai bên của alternate tap, `ChargeRatio` clamp `0..1`, swipe dir/length/duration/curvature.

---

### S4 — Core systems (3d)

#### Quyết định S4-1 — inject `GameSession`, không đổi chủ sở hữu

Chọn:
- `SceneRouter.Awake()` giữ nguyên hành vi tạo session default → test không đổi.
- Thêm `SceneRouter.LoadSession(GameSession)` — thay session + dựng lại `transitioner`.
- `GameManager` ở Bootstrap: đọc save → dựng `GameSession` → `EnsurePersistentInstance().LoadSession(...)`. Thứ tự `Awake` không thành vấn đề vì Bootstrap load `Menu` **sau** khi inject xong.
- Restore additive: `GameSession.Restore(SaveData)` + `ToSaveData()`; `SubjectRecord.FromData(...)` static factory + DTO riêng `SubjectRecordData { SubjectId id; bool passed; float bestScore; Rank bestRank; int failedVisits; }`. **Không** thêm `[SerializeField]` vào `SubjectRecord` (file có test).

#### Quyết định S4-2 — tách mối quan tâm giữa `SceneRouter` và `SubjectConfig`

- `SceneRouter.subjectScenes` = nguồn sự thật **duy nhất** cho routing (đã serialize trong `Map.unity`, có test qua `TryGetSceneName`).
- `SubjectConfig` SO **không có** field `sceneName`. Chỉ giữ dữ liệu trình bày: `displayName`, `icon`, `color`, `goalText`, `timeLimit`, `passThreshold`, `unlocked/comingSoon`.

→ 0 churn trên router đã test; mỗi loại dữ liệu có đúng 1 chủ; không có 2 nguồn sự thật về tên scene.

Phương án loại: `SubjectConfig` giữ `sceneName`, router đọc từ registry SO — gọn hơn về concept nhưng phải sửa `TryGetSceneName` + `DefaultSubjectScenes()` + test.

#### Quyết định S4-3 — Settings nằm trong `save.json`, không dùng PlayerPrefs

PLAN §1 ghi "PlayerPrefs cho settings" nhưng PLAN §5 lại đặt `Settings` **bên trong** `SaveData` — tài liệu tự mâu thuẫn. Chọn theo §5: 1 file, atomic cùng nhau, 1 chỗ để migrate. `rhythmOffsetMs` là settings nhưng ảnh hưởng gameplay → càng nên đi cùng save.

#### Quyết định S4-4 — `AudioManager` không sở hữu beat clock

`AudioManager` chỉ quản AudioMixer 2 group `Music`/`SFX` + phát SFX + volume.
Không rút clock ra thành global — đó là viết lại code đang chạy để lấy sự đối xứng trên giấy.

#### Phần còn lại
- Ghi `save.json` tại `Application.persistentDataPath`, **atomic**: `save.tmp` → `File.Replace`.
- Ghi khi: kết thúc môn, mất tim, đổi settings, `OnApplicationPause(true)`.
- Migration theo `version`.
- `HapticsService`: `Settings.vibration` có field trong PLAN §5 nhưng không nơi nào cài đặt. Đặt cạnh `AudioManager` — API `Light()`/`Medium()`/`Success()`/`Fail()`, no-op khi tắt hoặc khi thiết bị không hỗ trợ. Android dùng `Handheld.Vibrate` hoặc AndroidJavaObject `VibrationEffect` cho biên độ khác nhau.
- `Pool<T>` cho FX + floating text (PLAN §6: không Instantiate/Destroy runtime).
- `GameManager`: `Application.targetFrameRate = 60`, vSync off.
- `SubjectConfig` × 10: 7 playable + 3 locked (Hít đất, Nhịp điệu, Bơi lội).
- `InstructorQuoteSet` SO: bộ chill + bộ urgent.
- `RivalPaceProfile` hiện là plain class (`Sprint/RivalPaceProfile.cs`) → thêm SO wrapper để author trong Editor.
- `Bootstrap.unity` thành scene index 0.
- **Định nghĩa `stars`** (PLAN §2.2/§5 nhắc tới nhưng chưa định nghĩa, và `SubjectRecord` hiện không có field này):
  `stars` **suy ra từ `BestRank`**, không lưu trong save — `S/A → 3`, `B/C → 2`, `D → 1`, `F → 0`. Hàm thuần `ScoreUtil.ToStars(Rank)`.
  Lệch PLAN §5 (liệt kê `stars` là field lưu trong `SaveData`): lưu giá trị suy ra được sẽ tạo cơ hội drift khi đổi ngưỡng rank.

**Gate S4**: EditMode save round-trip + migration pass; chơi 1 môn → kill app → mở lại → tim + record đúng.

---

### S5 — Shell & core loop (5d) — checkpoint, **không phải đích**

#### Quyết định S5-1 — `Map.unity` là scene LevelSelect

- `Map.unity` = LevelSelect. `Menu.unity` = MainMenu + Settings + Calibrate (dùng `ScreenStack` nhẹ).
- Lý do: `SceneRouter.mapScene = "Map"` đã serialize trong `Map.unity` và `SessionRoute.Map` là đích sau mỗi môn → giữ tên scene là 0 churn.
- PLAN §2.1 gộp MainMenu + LevelSelect + Result vào 1 scene, nhưng Result đã tách thành overlay (S5-2) nên cách gộp của PLAN tan.
- Phương án loại: gộp LevelSelect vào `Menu.unity`, đổi `mapScene = "Menu"`, xoá `Map.unity` — phải sửa `EditorBuildSettings` + field serialize + kiểm lại test routing (`TryGetSceneName` gọi `Application.CanStreamedLevelBeLoaded`).

#### Quyết định S5-2 — Result overlay + `GameSession.PreviewRoute`

Vấn đề do quyết định S2 sinh ra: overlay hiện **trước** khi `Completed` phát, mà `GameSession` chỉ trừ tim / chọn route **trong** `SubmitResult`. Lúc overlay đang hiện, session còn chưa biết kết cục → overlay không nói được "−1 tim" hay "→ hình phạt".

Chọn: thêm `GameSession.PreviewRoute(subject, result)` **thuần, không mutate**.
Cách làm: refactor `SubmitResult` rút phần quyết định route ra một private helper thuần; `PreviewRoute` gọi **đúng helper đó** → không nhân đôi logic nên không drift. Hành vi `SubmitResult` không đổi → test hiện tại giữ nguyên.

Luồng: `Finish(result)` → hiện `ResultPanel` (score `0..10`, rank, stars, quote giảng viên, + hậu quả từ `PreviewRoute`) → người chơi bấm tiếp → **mới** `Completed?.Invoke(result)`.
Giữ nguyên hợp đồng "`Completed` phát đúng 1 lần" → `SceneRouter` và `SessionRouteTransitioner` không đổi.

Phương án loại: overlay trung tính (mất phản hồi nhân-quả đúng lúc cần nhất); hoặc cho `Completed` phát trước rồi router hoãn load scene (phải sửa router + transitioner).

#### Quyết định S5-3 — 5 scene stub có `PlaceholderMinigameController`

`SceneRouter.OnSceneLoaded` tìm `MinigameBase` để bind, nhưng đặt `awaitingSubjectScene = false` **bất kể** có tìm thấy hay không. Scene stub rỗng ⇒ không bind ⇒ session có `active` subject mà không đường submit ⇒ **soft-lock**.

Mỗi stub chứa: Camera + HUD prefab + `PlaceholderMinigameController : MinigameBase` với 2 nút debug Pass / Fail.

#### Phần còn lại
- `Bootstrap.unity` index 0 → `GameManager` → load `Menu`.
- `Menu.unity`: MainMenu (Play / Settings / Quit) + Settings (music/sfx volume, vibration) + **màn calibrate nhịp** ghi `rhythmOffsetMs` vào save. PLAN §5 ghi calibrate là bắt buộc cho M2 trên Android.
- `Punishment.unity`: sprite giảng viên, cue mechanic hiện tại, progress bar theo `sequence.CurrentProgress`, tap zone → `PunishmentSceneController.SubmitTap()` / `SubmitRhythmHold(float)` / `SubmitAlternateTap(bool)` (`Core/PunishmentSceneController.cs:72-87`), nguồn dữ liệu là detector thật từ S3.
- `GameOver.unity`: tổng kết + Retry / về MainMenu.
- **3 node locked** (Hít đất, Nhịp điệu, Bơi lội): `SubjectConfig.comingSoon` → node hiện mờ + nhãn "Coming soon", không nhận bấm. PLAN §0: giữ art, 0 code gameplay. Lưu ý: 3 node này **không** nằm trong `SubjectId` enum (enum chỉ có 7) nên chúng là dữ liệu Map thuần, không tạo record.
- **New Game vs Continue**: MainMenu hiện `Continue` khi có save; `New Game` luôn hiện, có xác nhận trước khi ghi đè.
- **Reset save**: Retry từ GameOver = ghi save mới (5 tim, records rỗng, giữ `settings` + `tutorialSeen`). Settings và tutorial đã xem không mất khi chơi lại — đó là dữ liệu người chơi, không phải tiến trình.
- **Credits screen** (vỏ ở S5, nội dung ở S16): đọc từ `CREDITS.md` hoặc SO tương ứng, scroll được. PLAN §7 bắt buộc ghi license — game hoàn chỉnh phải hiện được.
- Đăng ký **7** scene vào `ProjectSettings/EditorBuildSettings.asset`; nâng `SceneRouter.DefaultSubjectScenes()` (`Core/SceneRouter.cs:311`) từ 2 lên 7 entry; cập nhật field `subjectScenes` đã serialize trong `Map.unity`.

**Gate S5** — chạy trên máy thật, không cần Editor. Đây là **checkpoint giữa đường**, không phải định nghĩa hoàn thành (xem §10):
1. MainMenu → Map → chọn môn → fail lượt 1 → Punishment → lượt 2 → fail → −1 tim → Map → lặp tới 0 tim → GameOver.
3. Kill app giữa chừng → mở lại → tiến trình đúng.
4. Pause giữa môn → Resume, nhịp không lệch; Restart, Thoát về Map đều đúng.
5. New Game ghi đè có xác nhận; Continue vào đúng chỗ đang dở.

---

## 6. Brief — S6 đến S16

Template: `input → rules API sẵn có → HUD → scene → test → gate`. Nâng lên spec đầy đủ ngay trước khi implement.

Điểm chung khiến brief đủ dùng: 5 rules engine môn bóng đã hoàn chỉnh và có test, API cố định. S9–S13 là việc nối dây, không phải thiết kế luật.

Mỗi section môn (S6–S13) ngoài các gạch đầu dòng riêng còn phải giao **nội dung tutorial** cho môn đó, dùng vỏ `TutorialOverlay` từ S2: bao nhiêu bước, mỗi bước dạy gì, hình/animation minh hoạ thao tác. Đây là phần PLAN §2.4 gộp thành "2–3s icon" — không đủ cho cơ chế phức tạp.

### S6 — `MG_Sprint` (3d)
- Input: `AlternateTapInputDetector`; nút L/R 2 góc dưới, đường kính ≥140px @1080p, giữa dưới để trống (thanh gesture Android), Pause góc trên **phải**. Rewire `SprintController.inputActions` → `KMA.inputactions` map `Sprint`.
- API sẵn: `ExpectedSide`, `Snapshot`, `WindCueVisible`, `WindWindowActive`, `WindChallengeCountered/Failed/Expired`, `LastResult`, `Phase`.
- HUD: `BuildHudState()` → timer + stamina + distance. Extras: rank `1st–4th`, cadence combo, cờ gió.
- Scene: parallax 3 lớp (nền ≥`2560×1080` cho 21:9), player khoá `x=35%`, `RivalRunnerAI` ×3 lane 1/3/4 (player lane 2) dùng SO wrapper của `RivalPaceProfile`; anim `idle/run/burst/stumble/celebrate/fail`; AI nước rút mốc 70%.
- Test: wind cue hiện trước `0.8s`; tap trùng bên = 40% xung lực.
- Gate: 60fps máy mid (Profiler trên **máy thật**), pass lượt 1 ≈40–60%.

### S8 — Ball presentation kit (2d)
- Viết `TrajectoryPreview` (đường dashed khi ngón còn kéo) + `BallShadow` (đọc độ cao) — PLAN §2.3b, chưa tồn tại.
- 5 `FlightProfile` asset. `FlightProfile_Shuttle`: `linearDrag` rất cao + `bounciness = 0` → cầu vọt nhanh rồi rơi dốc (PLAN §M6).
- `BallRig` đã đủ: `Launch(dir,force,curvature)`, `AttachTo`, `IsNearApex(threshold)`, `PredictLandingPoint()`, `Bounce`, `Snapshot`, `Collided`, `Ballistics.PredictGround`.
- Test: preview khớp `Ballistics.PredictGround`.

### S12 — `MG_Badminton` (2d)
- `HoldInputDetector.ChargeRatio` + độ cao lúc nhả → `rules.TryExchange(charge, height, authoredWindCue)` → `BadmintonShot{Lift,Drive,Smash,Overcharge}`.
- Charge vượt `1.0` = quá lực, cầu ra ngoài, mất điểm.
- HUD: vòng nạp lực quanh nhân vật, cue quạt trần từ `LastWindCue`, `LastExchangeTiming`.
- `TrajectoryPreview`/`BallShadow` cập nhật điểm rơi mới ngay khi gió bật — không đổi điểm rơi bí mật.

### S13 — `MG_Football` (2d)
- Swipe → `new FootballShot(placement, force, spin, kind)` → `rules.ResolveAuthoredShot(shot)`.
- HUD: `TrajectoryPreview` dashed khi ngón còn kéo, 5 quả sút / goals, GK anim theo `LastKeeperPattern` từ `GKPatternSet`.
- Mỗi phase chỉ tăng một trục: reaction thủ môn **hoặc** thu hẹp vùng mục tiêu.

### S15 — Kết thúc game & màn meta (2d)

#### Nội dung (victory tĩnh, không cutscene)
- Bảng tổng kết 7 môn: rank + sao từng môn (`ScoreUtil.ToStars(BestRank)`), điểm trung bình, số tim còn lại, tổng thời gian nếu có.
- Quote giảng viên từ `InstructorQuoteSet`. Nút về Menu.

#### Post-game state
- **Chưa quyết**: chơi lại sau khi hoàn thành thì tim có tiêu không. Nâng lên spec đầy đủ khi tới S15.

#### Credits screen
Nội dung điền vào vỏ dựng ở S5, đọc từ `CREDITS.md` / SO tương ứng.

### S16 — Art / audio / release (8d+)
⚠️ **Section duy nhất còn rủi ro chưa giải. Cần brainstorm riêng khi tới, không quyết bây giờ.**
PLAN §7 chốt "tự vẽ giảng viên + nhân vật chính + UI 9-slice" nhưng nguồn lực là 1 người không phải artist. Ba hướng sẽ phải cân khi tới: thuê/nhờ người vẽ, chấp nhận thuần asset CC0 recolor (bỏ tự vẽ), hoặc sinh art bằng công cụ khác.

Phần đã rõ:
- Asset-first: Kenney.nl (CC0, flat/outline khớp neo-brutalist) → itch.io → OpenGameArt. **Recolor về `UITheme`** là bắt buộc, không bỏ qua. Ghi `Assets/_Project/CREDITS.md` mỗi lần thêm asset (PLAN §7: bắt buộc cho báo cáo). Kiểm license **trước** khi dùng; tránh NC nếu định lên Play.
- Audio: AudioMixer `Music`/`SFX`; SFX `.wav` → Vorbis q70 Decompress On Load; nhạc → Streaming. Bộ SFX tối thiểu: tap, perfect, good, miss, whistle, crowd, fail, pass, button.
- Perf: 1 Sprite Atlas 2048/scene; ASTC 6×6 art, 4×4 UI; <200 draw call; không realtime light; không post-process; pool mọi FX + floating text; **không `GetComponent` trong `Update`**.
- Ma trận thiết bị: 1 low-end (2GB RAM, GLES3) + 1 mid (Vulkan) + 1 máy có notch.
- **App icon + splash**: Android adaptive icon (foreground/background layer) mọi mật độ; **tắt hoặc thay Unity splash mặc định** — splash Unity nguyên bản trong bản demo/báo cáo là điểm trừ rõ. Thêm splash riêng theo `UITheme`.
- Build: APK cho demo/báo cáo, AAB nếu lên Play. APK <100MB, cold start <4s máy tầm thấp.
- Playtest ≥8 người ngoài nhóm; bảng cân bằng (`primaryObjective/timeLimit/targetScore/BPM/timingWindow/weights`) tune bằng data, không bằng cảm giác.
- **Tiêu chí số của balance pass** (thay cho "playtest rồi tune" mơ hồ): mỗi môn tỉ lệ pass lượt 1 nằm trong `40–60%`; không môn nào có tỉ lệ pass `<25%` hoặc `>80%`; phân bố rank không dồn quá `50%` vào một bậc; ≥6/8 người playtest phân biệt được **Bóng chuyền vs Bóng rổ** và **Bóng bàn vs Cầu lông** khi bị hỏi thẳng (PLAN §9 hai rủi ro "hai môn giống nhau").

---

## 7. Điểm lệch so với PLAN.md

PLAN §0 đã ghi 3 điểm lệch với `kma-pe.md`. Tài liệu này thêm các điểm lệch **giữa PLAN.md và sản phẩm**:

| # | PLAN.md nói | Thực tế / quyết định | Lý do |
|---|---|---|---|
| 1 | §1: scene minigame load `Additive` | Code đã cài `LoadSceneMode.Single` trong `SceneRouter.LoadGameplayScene()`; **giữ Single** | Đã có test bao quanh; đổi ở section nền tảng là rủi ro cao lợi ích thấp |
| 2 | §2.1: `UIRoot` canvas persistent trong Bootstrap | HUD prefab trong từng scene; persistent canvas chỉ giữ loading screen + toast | Hệ quả của #1 |
| 3 | §2.1: MainMenu + LevelSelect + Result trong 1 scene `Menu` | `Menu.unity` = MainMenu + Settings + Calibrate; `Map.unity` = LevelSelect; Result = overlay trong scene minigame | Giữ `mapScene = "Map"` đã serialize → 0 churn trên router đã test |
| 4 | §1: PlayerPrefs cho settings | Settings nằm trong `save.json` | PLAN tự mâu thuẫn: §5 đặt `Settings` bên trong `SaveData`. Chọn §5 |
| 5 | §2.3: 5 detector là "component" | Detector là plain C# class + time injection; 1 MonoBehaviour `GameplayInputRouter` feed vào | PLAN §10 yêu cầu EditMode test cho detector, EditMode không có frame loop |
| 6 | §11: pin `6000.3.22f1` | Repin `6000.3.23f1` | Bản 23f1 đã tải sẵn 2.1GB; tránh tải lại |
| 7 | §1: Unity 6.3 LTS "Universal 2D" | Dùng URP 2D nhưng **tắt light + post-process** ngay từ đầu | PLAN §6 vốn đã cấm light/post-process. Giữ URP để không thêm điểm lệch tầng engine khi bảo vệ |
| 8 | §5: `stars` là field lưu trong `SaveData` | `stars` suy ra từ `BestRank` qua `ScoreUtil.ToStars`, không lưu | Lưu giá trị suy ra được → drift khi đổi ngưỡng rank |
| 10 | §2.4: tutorial = 2–3s icon tự ẩn | `TutorialOverlay` nhiều bước bấm qua được, `tutorialSeen` theo môn | 2s không dạy nổi cơ chế giữ-nhả 2 trục của cầu lông hay apex timing của bóng rổ |

Điểm lệch #7 gần như là no-op, #8 là chi tiết lưu trữ; #1, #2, #3, #5, #9, #10 là lệch thật và nên có mặt trong slide phụ lục cùng 3 điểm của PLAN §0.

---

## 8. Rủi ro theo dõi

| Rủi ro | Chặn ở đâu |
|---|---|
| TMP thiếu glyph tiếng Việt (ô vuông) | **S2** — sinh font asset từ charset dump ngay, không để cuối (PLAN §9 rủi ro #1) |
| Lệch nhịp rhythm trên Android (audio latency) | `dspTime` + DSP Best latency (S1) + màn calibrate (S5) + `rhythmOffsetMs` cộng ở tầng router (S3) |
| Tap bắn 2 lần (Input System + UI raycast) | **S3** — gameplay tap chỉ qua `ScreenTapArea`; UI qua EventSystem; không đọc `Input.touches` rải rác |
| Normalize scene YAML làm vỡ gì đó | **S1** — commit riêng, chạy lại 158 test ngay sau |
| Scene stub gây soft-lock progression | **S5** — `PlaceholderMinigameController` (quyết định S5-3) |
| `PreviewRoute` drift khỏi `SubmitResult` | **S5** — cả hai gọi cùng một private helper thuần, không nhân đôi logic |
| Không có người làm art | **S16** — brainstorm riêng; code chạy placeholder hình khối màu tới đó, swap sprite cuối (data đã tách khỏi art qua `SubjectConfig` + prefab) |
| Máy tầm thấp tụt fps | Ngân sách draw call + pooling từ S2; đo bằng Profiler trên **máy thật** từ S6 |
| Nhầm S5 là đích, dừng ở game half-done | **§10 Definition of Done** — checklist tick hết mới xong; nhãn S5 ghi rõ "checkpoint, không phải đích" |
| Chưa quyết: chơi lại post-game có tiêu tim không | **S15** — nâng lên spec đầy đủ khi tới, không quyết bây giờ |

---

## 9. Test bổ sung

| Loại | Nội dung mới | Section |
|---|---|---|
| EditMode | 5 detector: biên `±80/±160ms`, đúng/sai bên, `ChargeRatio` clamp, swipe metric | S3 |
| EditMode | `SaveSystem` round-trip + migration version; `GameSession.Restore/ToSaveData`; `PreviewRoute` khớp `SubmitResult` | S4, S5 |
| PlayMode | 5 controller môn bóng qua `InputTestFixture` | S9–S13 |
| PlayMode | Pause: `Resume` không lệch nhịp `dspTime`; `Restart`/`Thoát` route đúng | S5 |
| EditMode | `ScoreUtil.ToStars` biên rank; reset save giữ `settings` + `tutorialSeen` | S4, S5 |
| Contract | giữ nguyên: chỉ `PrimaryObjective` đặt `Pass = true`; mọi sự kiện bất lợi có cue trước cửa sổ phản ứng | mọi section môn |

Chạy sau **mỗi** section:
```bash
KMA_UNITY_EDITOR=/path/to/Unity
"$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testResults /tmp/kma-editmode.xml -logFile /tmp/kma-editmode.log
"$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode \
  -testResults /tmp/kma-playmode.xml -logFile /tmp/kma-playmode.log
```

---

## 10. Definition of Done — "hoàn chỉnh" nghĩa là gì

Mục tiêu **không** phải "chơi được trọn loop" (đó là gate S5). Game hoàn chỉnh khi **toàn bộ** danh sách dưới đây đúng, kiểm trên máy Android thật, không qua Editor.

### Nội dung
- [ ] 7 môn chơi được thật, không còn `PlaceholderMinigameController` nào trong build.
- [ ] Hình phạt chơi được với cả 3 mechanic (TapMash / RhythmHold / AlternateTap).
- [ ] 3 node `Coming soon` hiện đúng trạng thái, không bấm được, không crash.
- [ ] Mỗi môn có tutorial riêng, bấm qua được, `tutorialSeen` ghi nhớ.

### Vòng lặp & dữ liệu
- [ ] Loop đầy đủ: Menu → Map → môn → (fail lượt 1 → phạt → lượt 2) → pass/mất tim → Map → 0 tim → GameOver.
- [ ] Save/load đúng qua kill app ở mọi điểm; `save.tmp` → `File.Replace` atomic; migration `version` chạy được.
- [ ] New Game / Continue / Reset save đúng; reset giữ `settings` + `tutorialSeen`.
- [ ] Pause ở mọi môn: Resume không lệch nhịp, Restart, Thoát về Map.

### Trình bày & nghe
- [ ] Không màn hình đen, không GameObject rỗng thay cho UI, không placeholder art trong build cuối.
- [ ] Text tiếng Việt hiện đủ dấu, không ô vuông, ở mọi screen.
- [ ] Có nhạc + SFX cho: tap, perfect/good/miss, whistle, pass, fail, button. AudioMixer 2 group hoạt động, volume trong Settings có tác dụng.
- [ ] Haptics theo `Settings.vibration`, tắt được.
- [ ] Layout đúng ở 16:9, 19.5:9, 21:9 và máy có notch — không thứ gì bắt buộc thấy bị cắt.
- [ ] Credits screen hiện được, liệt kê đủ license asset đã dùng.
- [ ] App icon riêng; splash Unity mặc định đã tắt/thay.

### Chất lượng
- [ ] Toàn bộ test pass: 158 test cũ + test mới của S3/S4/S5/S9–S13/S15.
- [ ] 60fps trên máy mid; không tụt dưới 30fps trên low-end (2GB RAM, GLES3). Đo bằng Profiler **trên máy thật**.
- [ ] < 200 draw call mỗi scene.
- [ ] Balance đạt tiêu chí số ở S16: pass lượt 1 mỗi môn `40–60%`, không môn nào `<25%` hay `>80%`, ≥6/8 người playtest phân biệt được 2 cặp môn dễ lẫn.
- [ ] Playtest ≥8 người ngoài nhóm, không còn bug chặn tiến trình.

### Giao hàng
- [ ] APK < 100MB, cold start < 4s trên máy tầm thấp.
- [ ] `CREDITS.md` đầy đủ, license đã kiểm trước khi dùng.
- [ ] README cập nhật: version pin, cách build, cách chạy test.
- [ ] Danh sách điểm lệch ở §7 đã chuyển thành slide phụ lục để bảo vệ (cộng 3 điểm lệch của PLAN §0).

Chưa tick hết thì chưa hoàn chỉnh, dù đã chơi được từ S5.
