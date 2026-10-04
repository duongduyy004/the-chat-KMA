# KMA Student Journey Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Xây dựng hành trình tân sinh viên KMA học và thi chín thử thách, theo thứ tự Sprint → Bóng chuyền → Soccer, với năm lượt thi và đợt thi bổ sung.

**Architecture:** Danh mục thử thách và quy tắc hành trình nằm trong assembly Progression; GameSession sở hữu trạng thái và điểm học phần. SceneRouter truyền ngữ cảnh tới controller qua interface, ghi kết quả cùng bản lưu trước khi điều hướng; ba scene thể thao chạy các cấu hình học/luyện/thi. Map trình bày lộ trình, hội thoại và tổng kết.

**Tech Stack:** Unity 6000.3.23f1, C#, ScriptableObject, Input System 1.20.0, uGUI/TextMeshPro, Unity Test Framework 1.6.0, Android ARM64 IL2CPP.

**Spec:** ../specs/2026-10-04-kma-student-journey-design.md — đã được người dùng duyệt trong hội thoại.

## Global Constraints

- Thứ tự học tường minh [Sprint, Volleyball, Football]; các SubjectId theo thứ tự này là [0, 7, 6].
- Mỗi môn có ba bài theo thứ tự Learn → Practice → Exam; chỉ thi đạt mở phần học môn tiếp theo.
- Sprint Learn: 12 lần đúng liên tiếp; sai đặt chuỗi về 0. Practice: 100 m trong 20 giây. Exam: 100 m trong 14 giây, ba đối thủ; thứ hạng không khóa đạt.
- Volleyball Learn: ba Receive hợp lệ, không cần liên tiếp. Practice: hai điểm từ Receive chạm 1 → Receive chạm 2 → Smash chạm 3 trong cùng quyền kiểm soát bóng. Exam: năm điểm trước đối thủ trong 120 giây; dẫn điểm khi hết giờ chưa đủ để đạt.
- Soccer Learn: ba bàn, thủ môn tắt. Practice: hai bàn, Easy. Hai bài này không giới hạn số cú thử. Exam: đủ năm cú sút, ít nhất ba bàn, Normal.
- Có năm lượt thi chung toàn học phần. Luyện/ôn/chơi lại không mất lượt. Thi trượt mất đúng một lượt; qua môn không tự nạp lượt.
- Hết lượt yêu cầu một lượt luyện mới của bài Practice hiện tại, sau đó đặt lượt về 5 một lần; giữ các môn đã đạt.
- Tăng save version 6 → 7; settingsOnly không bật Tiếp tục. Nhập tiến trình cũ theo tiền tố môn đạt liên tục; giữ thành tích tốt nhất, cài đặt và lịch sử thất bại.
- UI tiếng Việt dùng “Bóng đá”; định danh Football và scene MG_Football giữ ý nghĩa dữ liệu hiện tại. Hội thoại hài hước đời sinh viên, giảng viên nghiêm nhưng công bằng.
- Kết quả được ghi nhận và lưu trước khi điều hướng. Rời lượt chưa có kết quả bắt đầu lại cùng thử thách; đóng tại kết quả không hoàn lại lượt.
- Preview Soccer và đường bóng thật dùng chung mô phỏng; preview chỉ hiện khi giữ nút. Input giữ qua pause/rời scene phải được hủy.
- Sprite dùng bộ Toon Characters và ba sân hiện có; typography dùng VietTypography/VietText.Fix và theme chung. Gameplay variation tiếp tục dựa trên mẫu được thiết kế.
- Không thêm package cho tính năng này. Bao gồm .meta cho file/asset mới; chỉ stage các file thuộc nhiệm vụ đang commit.

## Review Focus

1. Đóng ứng dụng ngay tại màn kết quả thi trượt: đọc lại save phải giữ lượt đã trừ và checkpoint sau kết quả — Task 4.
2. Kết quả ôn bổ sung bị gửi lặp sau khi khôi phục save: chỉ một lượt mới đúng bài được nạp 5 lượt — Tasks 2–3.
3. Save cũ đạt Football nhưng chưa đạt Sprint/Volleyball: thành tích được giữ, môn tương lai vẫn khóa theo thứ tự [0, 7, 6] — Task 3.
4. Điểm thứ năm bóng chuyền ở mốc 120 giây hoặc frame vượt mốc: xử lý tới deadline, đạt tại deadline, không ghi điểm sau deadline — Task 7.
5. Đang giữ SHOOT rồi pause/thoát/chuyển cảnh: hủy charge/preview và không phát cú sút sau khi quay lại — Task 8.

## File Structure and Dependency Order

Các file Journey trong Progression dùng namespace KMA.Gameplay để phù hợp GameSession hiện tại. Core tham chiếu interface từ Progression; Core không tham chiếu trực tiếp assembly của từng môn. Sprint và Volleyball bổ sung reference tới Progression; Football đã có reference này.

| Nhóm | File mới chính | File hiện có được tích hợp |
|---|---|---|
| Danh mục/hợp đồng | Progression/Journey/{ChallengeDefinition,ChallengeCatalog,ChallengeAttempt,ChallengeContracts}.cs | Assets/Editor/StudentJourneyContentBuilder.cs, asset thử thách và Resources |
| Quy tắc học phần | Progression/Journey/{JourneyProgress,JourneyStateData}.cs | Progression/GameSession.cs, SubjectRecord.cs |
| Lưu dữ liệu | Progression/Journey/JourneySaveMigration.cs | Progression/SaveData.cs, Core/SaveSystem.cs |
| Ghi nhận/điều hướng | Core/{JourneySaveCoordinator,SceneRouter.Journey}.cs | SceneRouter.cs, GameManager.cs, MinigameBase.cs, ResultPanel.cs |
| Lộ trình/kết quả | UI/{JourneyLessonList,JourneyCourseSummary}.cs | MapScreen.cs, MapNodeView.cs, MapPresentationBuilder.cs, ResultPanel.cs, shell |
| Sprint | Gameplay/Sprint/{SprintBalanceConfig,SprintChallengeRules}.cs | SprintRules.cs, SprintController.cs, SprintHud.cs, SprintFestivalPresentation.cs |
| Volleyball | Gameplay/Volleyball/{VolleyballMatchOptions,VolleyballChallengeRules}.cs | VolleyballMatch.cs, VolleyballController.cs, VolleyballHud.cs |
| Soccer | Gameplay/Football/FootballMatchOptions.cs | FootballRules.cs, FootballController.cs, FootballHud.cs, FootballPresentation.cs |
| Hội thoại | Progression/Journey/JourneyDialogueLibrary.cs, UI/JourneyDialoguePresenter.cs | GameManager.cs, shell, ContentBuilder |
| Kiểm chứng | Các fixture ghi rõ trong từng task; docs/qa/kma-student-journey.md | Test/scene authoring/build tools hiện có |

Các đường dẫn script rút gọn trong bảng trên bắt đầu tại Assets/_Project/Scripts; đường dẫn Assets/Editor, Assets/Tests và docs dùng trực tiếp từ root. Asset danh mục: Assets/_Project/Resources/Journey/ChallengeCatalog.asset. Chín asset thử thách: Assets/_Project/ScriptableObjects/Journey/<challenge_id>.asset, dùng các ID chính xác ở spec §3.

Task 1 → 2 → 3 → 4 → 5 tạo nền tảng và UI; Task 6 kiểm chứng chương Sprint xuyên suốt; Task 7–8 bổ sung hai môn còn lại; Task 9 hoàn thiện câu chuyện; Task 10–11 kiểm chứng toàn hành trình. Các task cuối sử dụng hợp đồng đã định nghĩa, không tự đổi chữ ký của task trước.

## Execution Preparation

- [ ] Đọc cả spec và plan, kiểm tra AGENTS.md/RTK.md áp dụng, xác nhận nhánh và git status.
- [ ] Dùng superpowers:using-git-worktrees tại thời điểm thực thi để chọn workspace cách ly; ghi rõ projectPath của mọi lệnh Unity và QA.
- [ ] Kiểm tra Editor/lock trước chạy batch; phối hợp với phiên Editor đang mở hoặc dùng project cách ly. Xác nhận Unity 6000.3.23f1 và package từ checkout.
- [ ] Ghi baseline đầy đủ EditMode/PlayMode cùng XML; xác nhận lại lỗi audio Sprint đã được ghi trong QA cũ bằng môi trường hiện tại.
- [ ] Trước từng commit, xem git diff --cached --name-status và kiểm tra staged diff. Thay đổi atlas font phát sinh ngoài nhiệm vụ không thuộc commit tính năng.

Các lệnh dưới chạy tại root workspace thực thi, đều có tiền tố rtk. Focused tests dùng tools/run-unity-tests.sh PLATFORM FILTER NAME; expected total > 0, failed = 0, result = Passed trong XML. Full suite có ba Ignore Punishment được kiểm tra riêng ở Task 10.

---

### Task 1: Danh mục chín thử thách và hợp đồng giữa các assembly

**Files:**

- Create: Assets/_Project/Scripts/Progression/Journey/ChallengeDefinition.cs
- Create: Assets/_Project/Scripts/Progression/Journey/ChallengeCatalog.cs
- Create: Assets/_Project/Scripts/Progression/Journey/ChallengeAttempt.cs
- Create: Assets/_Project/Scripts/Progression/Journey/ChallengeContracts.cs
- Create: Assets/Editor/StudentJourneyContentBuilder.cs
- Create: Assets/_Project/Resources/Journey/ChallengeCatalog.asset và chín asset ở thư mục ScriptableObjects/Journey.
- Test: Assets/Tests/EditMode/Progression/JourneyCatalogTests.cs
- Modify: Assets/_Project/Scripts/Gameplay/Sprint/KMA.Gameplay.Sprint.asmdef; Assets/_Project/Scripts/Gameplay/Volleyball/KMA.Gameplay.Volleyball.asmdef.
- Modify: Assets/Tests/EditMode/Gameplay/Running/KMA.Gameplay.Running.EditMode.Tests.asmdef; Assets/Tests/PlayMode/Gameplay/Running/KMA.Gameplay.Running.PlayMode.Tests.asmdef.
- Modify: Assets/Tests/EditMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.EditMode.Tests.asmdef; Assets/Tests/PlayMode/Gameplay/Volleyball/KMA.Gameplay.Volleyball.PlayMode.Tests.asmdef.
- Modify: Assets/Tests/EditMode/Gameplay/Ball/KMA.Gameplay.Ball.EditMode.Tests.asmdef.

**Interfaces:**

- ChallengeKind = Learn, Practice, Exam; ChallengeAttemptMode = Journey, Supplementary, Review, FreePlay; ChallengeDifficulty = Easy, Normal, Hard.
- ChallengeDefinition : ScriptableObject có serialized fields Id:string, Subject:SubjectId, Kind:ChallengeKind, Distance:float, TimeLimit:float, TargetCount:int, KeeperEnabled:bool, Difficulty:ChallengeDifficulty, TimingHelp:bool, Objective:string. TimeLimit = 0 là bài không giới hạn thời gian.
- ChallengeCatalog.LoadDefault():ChallengeCatalog; Ordered:IReadOnlyList<ChallengeDefinition>; Get(string id):ChallengeDefinition; Validate(out string error):bool. Get từ chối ID không tồn tại.
- ChallengeAttemptContext(string attemptId, string challengeId, ChallengeAttemptMode mode, ChallengeDifficulty difficulty), các property cùng tên PascalCase.
- ChallengeMetrics(float distance=0, float elapsed=0, int completedTargets=0, int kicks=0, float stamina=0, int placement=0), các property PascalCase.
- ChallengeAttemptResult(ChallengeAttemptContext context, bool pass, ChallengeMetrics metrics, MinigameResult examResult=null), property Context, Pass, Metrics, ExamResult.
- JourneyCommitOutcome: readonly struct, property Accepted:bool, NextChallengeId:string, AttemptsRemaining:int, AwaitingSupplementary:bool, CourseComplete:bool.
- IChallengeController có Subject:SubjectId, ConfigureChallenge(ChallengeDefinition definition, ChallengeAttemptContext context):void và event Action<ChallengeAttemptResult> ChallengeCompleted.
- IChallengeResultPanel có event Action<JourneyResultAction> JourneyActionRequested và ShowChallenge(context:ChallengeAttemptContext, result:ChallengeAttemptResult, outcome:JourneyCommitOutcome?, saveError:string):void; JourneyResultAction = Continue, Retry, Practice, RetrySave.

- [ ] **Step 1: Viết test dữ liệu và trường hợp catalog sai**

    Assert.That(catalog.Ordered.Select(x => x.Id).Distinct().Count(), Is.EqualTo(9));
    Assert.That(catalog.Get("sprint_practice").TimeLimit, Is.EqualTo(20f));
    Assert.That(catalog.Get("sprint_exam").Distance, Is.EqualTo(100f));
    Assert.That(catalog.Get("soccer_exam").TargetCount, Is.EqualTo(3));

Thêm assertion cho đủ chín bộ giá trị §3, thứ tự enum [0,7,6], ID trùng/mất, giá trị NaN/âm, và giới hạn Practice Sprint phải dài hơn Exam. Fixture namespace KMA.Tests.Gameplay.Progression.

- [ ] **Step 2: Chạy red** — rtk proxy tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression.JourneyCatalogTests journey-catalog-red. Expected: thiếu type/catalog hoặc assertion giá trị chưa đạt.
- [ ] **Step 3: Tạo hợp đồng và catalog** — triển khai các chữ ký trên, asset validation và StudentJourneyContentBuilder.BuildChallenges():void. Dùng AssetDatabase để tạo/reuse asset, giữ GUID khi cập nhật. Map loại/giá trị từ spec, dùng 0 cho bài untimed và bộ đếm mục tiêu 12/3/2/3/2. Hợp đồng chứa dữ liệu; assembly Progression tiếp tục chỉ tham chiếu Common. Thêm Progression vào reference của Sprint/Volleyball và năm asmdef test liệt kê trên; hai asmdef Running/Volleyball PlayMode thêm Core để kiểm tra routing. Không tạo vòng Core → gameplay.
- [ ] **Step 4: Chạy green** — cùng filter, tên journey-catalog-green; expected XML Passed. Kiểm tra Resources.Load tìm được catalog trong Player qua smoke test ở Task 10.
- [ ] **Step 5: Commit** — stage riêng các file/task và .meta, kiểm tra staged paths, commit “feat: define student journey challenges and contracts”.

### Task 2: Quy tắc tiến trình, năm lượt thi và thi bổ sung

**Files:**

- Create: Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs
- Create: Assets/_Project/Scripts/Progression/Journey/JourneyStateData.cs
- Modify: Assets/_Project/Scripts/Progression/GameSession.cs; Assets/_Project/Scripts/Progression/SubjectRecord.cs.
- Test: Assets/Tests/EditMode/Progression/JourneyProgressTests.cs; Assets/Tests/EditMode/Progression/JourneyTestData.cs.
- Modify tests: Assets/Tests/EditMode/Progression/GameSessionTests.cs; Assets/Tests/EditMode/Progression/SceneRouterSessionTests.cs — cập nhật setup khi hành vi mở môn đổi.

**Interfaces:**

- Consumes: catalog/context/result/outcome của Task 1.
- JourneyProgress(ChallengeCatalog catalog); CheckpointChallengeId:string; AttemptsRemaining:int; AwaitingSupplementary:bool; SupplementaryRounds:int; CourseComplete:bool; ActiveAttempt:ChallengeAttemptContext.
- IsChallengeUnlocked(string id):bool; IsSubjectUnlocked(SubjectId subject):bool; IsChallengeComplete(string id):bool.
- TryBegin(string id, ChallengeAttemptMode mode, ChallengeDifficulty difficulty, out ChallengeAttemptContext context):bool; Apply(ChallengeAttemptResult result):JourneyCommitOutcome; AbandonAttempt():void.
- ToData():JourneyStateData; Restore(JourneyStateData data, int attemptsRemaining):void. Restore phục hồi bản chụp hợp lệ, giữ nguyên ActiveAttempt; chuẩn hóa save từ đĩa thuộc Task 3. Số lượt dùng SaveData.lives.
- JourneyStateData: [Serializable] class với public fields completedChallengeIds:List<string>, activeAttempt:JourneyAttemptData, lastCommittedAttemptId:string, lastCommittedResult:JourneyResultData, awaitingSupplementaryChallengeId:string, supplementaryRounds:int, seenDialogueIds:List<string>.
- JourneyAttemptData: [Serializable] class với fields attemptId:string, challengeId:string, mode:ChallengeAttemptMode, difficulty:ChallengeDifficulty; static FromContext(ChallengeAttemptContext):JourneyAttemptData; ToContext():ChallengeAttemptContext.
- JourneyResultData: [Serializable] class với fields context:JourneyAttemptData, pass:bool, distance/elapsed/stamina:float, completedTargets/kicks/placement:int, examResult:MinigameResult; static FromResult(ChallengeAttemptResult):JourneyResultData; ToResult():ChallengeAttemptResult. DTO và converter đặt trong JourneyStateData.cs, sao chép dữ liệu để bản chụp không dùng chung list/result có thể đổi.
- GameSession.Journey:JourneyProgress; TryStartChallenge(string id, ChallengeAttemptMode mode, ChallengeDifficulty difficulty, out ChallengeAttemptContext context):bool; SubmitChallengeResult(ChallengeAttemptResult result, bool notify=true):JourneyCommitOutcome; AbandonActiveChallenge():void; NotifyJourneyChanged():void. Lives đọc từ Journey.AttemptsRemaining. notify=false dành cho coordinator, chỉ phát sự kiện sau khi lưu thành công.

- [ ] **Step 1: Viết test cho khóa môn, khóa bài và budget**

    var session = new GameSession();
    Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
    JourneyTestData.CompleteThrough(session, "sprint_practice");
    Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.False);
    JourneyTestData.CompleteThrough(session, "sprint_exam");
    Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Volleyball), Is.True);

JourneyTestData.CompleteThrough(GameSession session, string challengeId):void chỉ dùng trong assembly test này, tạo kết quả miền hợp lệ để unit test tiến trình. Bổ sung test: năm lần thi trượt → 0; Practice không mất lượt; Review/FreePlay không mở môn; sai mode/ID/attempt bị từ chối; cùng receipt lần hai không đổi state. Thi bổ sung phải chờ lượt mới đúng Practice, trả 5 một lần; khôi phục rồi gửi receipt cũ không trả thêm. Hoàn tất chín bài kết thúc học phần; bài thi chơi lại Normal mới cải thiện bảng điểm Soccer. SubjectRecord.FromData có Passed=false nhưng BestScore=8 phải giữ điểm 8 khi Accept kết quả đạt 6; dữ liệu chuyển đổi có thể chưa đạt chương nhưng đã có thành tích.

- [ ] **Step 2: Chạy red** — rtk proxy tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression.JourneyProgressTests journey-progress-red.
- [ ] **Step 3: Triển khai miền** — constructor GameSession nhận catalog optional, default dùng LoadDefault. Checkpoint là bài chưa hoàn thành đầu tiên trong danh mục, hoặc Practice bắt buộc khi chờ bổ sung. Apply xác nhận active AttemptId/ChallengeId/Mode/Difficulty; chỉ bài Exam trong Journey tiêu tốn lượt khi trượt, chỉ Exam đạt mở môn. Budget reset một lần cho Supplementary mới. Giữ thành tích tốt nhất qua ôn/chơi lại, khóa FreePlay cho tới CourseComplete. StartSubject/SubmitResult hiện có phải đi qua eligibility và đúng loại bài; cập nhật các test cũ theo quyết định mới, giữ kiểm tra giao dịch và sự kiện một lần.
- [ ] **Step 4: Chạy green** — filter JourneyProgressTests, tên journey-progress-green; thêm chạy GameSessionTests và SceneRouterSessionTests từng fixture để kiểm tra adapter.
- [ ] **Step 5: Commit** — “feat: enforce sequential course progress and supplementary exams”.

### Task 3: Save version 7, chuyển đổi và checkpoint khôi phục

**Files:**

- Create: Assets/_Project/Scripts/Progression/Journey/JourneySaveMigration.cs
- Modify: Assets/_Project/Scripts/Progression/SaveData.cs; Assets/_Project/Scripts/Progression/GameSession.cs; Assets/_Project/Scripts/Progression/Journey/JourneyProgress.cs.
- Modify: Assets/_Project/Scripts/Core/SaveSystem.cs.
- Test: Assets/Tests/EditMode/Progression/JourneySaveTests.cs.
- Modify tests: Assets/Tests/EditMode/Progression/SaveDataTests.cs; Assets/Tests/EditMode/Progression/SaveSystemTests.cs; Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs.

**Interfaces:**

- Consumes: JourneyStateData/ToData/Restore của Task 2.
- SaveData.CurrentVersion = 7; journey:JourneyStateData, lives vẫn là budget.
- JourneySaveMigration.MigrateLegacy(SaveData source, ChallengeCatalog catalog):SaveData; Normalize(SaveData data, ChallengeCatalog catalog):SaveData.
- GameSession.ToSaveData()/Restore(SaveData) round-trip cả Journey và SubjectRecord.

- [ ] **Step 1: Viết round-trip và migration tests**

    var restored = new GameSession();
    restored.Restore(saved);
    Assert.That(restored.Lives, Is.EqualTo(0));
    Assert.That(restored.Journey.AwaitingSupplementary, Is.True);
    Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_practice"));

Thêm ma trận tám tổ hợp môn đạt ở save v6, xác nhận chỉ tiền tố liên tục được nhập; Football ngoài tiền tố giữ BestScore/BestRank nhưng còn khóa. Test SubjectId 0/7/6, settingsOnly, all-passed, active attempt chưa kết quả, result đã ghi, unknown challenge ID, completion không liên tục, budget ngoài 0–5, thiếu DTO, và supplementary receipt gửi lặp sau round-trip. Test migration v0–v5 hiện có tiếp tục đi qua các bước cũ rồi tới v7.

- [ ] **Step 2: Chạy red** — rtk proxy tools/run-unity-tests.sh EditMode KMA.Tests.Gameplay.Progression.JourneySaveTests journey-save-red.
- [ ] **Step 3: Triển khai migration** — nhập ba bài hoàn thành cho từng môn trong tiền tố; đồng bộ Passed và bảo lưu best score/rank của môn ngoài tiền tố. GameSession.Restore(SaveData) dùng Normalize cho dữ liệu từ đĩa, loại active attempt chưa kết quả và suy ra checkpoint cùng bài để bắt đầu lượt mới; receipt đã ghi không chạy Apply lại. JourneyProgress.Restore phục hồi DTO đã hợp lệ và tiếp tục giữ identity khi dùng cho rollback ở Task 4. Lượt 0 ở môn chưa đạt suy ra chờ bổ sung. HasLoadedValidSave/settingsOnly giữ ranh giới hành trình và cài đặt. Lưu các DTO bằng public serialized fields tương thích JsonUtility.
- [ ] **Step 4: Chạy green** — JourneySaveTests, sau đó từng fixture SaveSystemTests và GameSessionPersistenceTests. Expected: các ma trận và byte round-trip hợp lệ; không bật Continue từ settingsOnly.
- [ ] **Step 5: Commit** — “feat: persist journey checkpoints and migrate existing saves”.

### Task 4: Bind controller và lưu kết quả trước khi điều hướng

**Files:**

- Create: Assets/_Project/Scripts/Core/JourneySaveCoordinator.cs
- Create: Assets/_Project/Scripts/Core/SceneRouter.Journey.cs
- Modify: Assets/_Project/Scripts/Core/SceneRouter.cs; Assets/_Project/Scripts/Core/GameManager.cs.
- Modify: Assets/_Project/Scripts/Progression/GameSession.cs — bổ sung RestoreSnapshot và dùng notify sau persist.
- Modify: Assets/_Project/Scripts/Gameplay/Common/MinigameBase.cs.
- Modify: Assets/_Project/Scripts/UI/ResultPanel.cs.
- Modify: Assets/_Project/Scripts/Shell/GameplayPauseFlowController.cs.
- Test: Assets/Tests/EditMode/Progression/JourneyCommitTests.cs.
- Test: Assets/Tests/PlayMode/Progression/JourneyRoutingTests.cs.
- Modify tests: Assets/Tests/PlayMode/Progression/FootballResultRoutingTests.cs — chuyển expected timing ghi kết quả sang lúc Completed.

**Interfaces:**

- Consumes: GameSession/result/outcome Task 2, save Task 3, IChallengeController/IChallengeResultPanel Task 1.
- delegate bool JourneyPersistHandler(out string error).
- JourneySaveCoordinator(GameSession session, JourneyPersistHandler persist); TryCommit(ChallengeAttemptResult result, out JourneyCommitOutcome outcome, out string error):bool.
- GameSession.RestoreSnapshot(SaveData snapshot):void phục hồi dữ liệu trong bộ nhớ không chuẩn hóa như save từ đĩa, giữ ActiveAttempt và không phát SessionChanged. Coordinator dùng SubmitChallengeResult(result, notify:false), rồi NotifyJourneyChanged sau persist.
- GameManager.TryPersistSession(out string error):bool; SceneRouter.TryStartChallenge(string id, ChallengeAttemptMode mode=Journey, ChallengeDifficulty difficulty=Normal):bool; RetryActiveChallenge():bool; PracticeCurrentSubject():bool.
- MinigameBase.TryBeginResolve(bool passed):bool là protected hook dùng chung lifecycle/audio; Finish(MinigameResult) hiện có gọi hook này.

- [ ] **Step 1: Viết test transaction và route**

    Assert.That(coordinator.TryCommit(failedExam, out var outcome, out _), Is.True);
    Assert.That(saved.lives, Is.EqualTo(4));
    Assert.That(coordinator.TryCommit(failedExam, out _, out _), Is.False);
    Assert.That(session.Lives, Is.EqualTo(4));

Thêm test: Save IOException → RestoreSnapshot giữ nguyên ActiveAttempt.AttemptId, không phát SessionChanged, giữ payload để RetrySave; save lại thành công chỉ mất một lượt và phát sự kiện một lần sau persist. Scene load thất bại sau commit → giữ kết quả đã lưu. Reload ngay tại màn kết quả → 4 lượt và đúng checkpoint. Router từ chối future lesson trước khi sửa state. Restart/exit lúc chưa có kết quả không mất lượt. Dùng fake IChallengeController/panel/saver trong tests để kiểm tra route và giao dịch.

- [ ] **Step 2: Chạy red** — fixture KMA.Tests.Gameplay.Progression.JourneyCommitTests (EditMode) và JourneyRoutingTests (PlayMode), tên journey-commit-red / journey-routing-red.
- [ ] **Step 3: Tích hợp** — tách phần Journey của SceneRouter bằng partial class, dùng interface để tránh vòng assembly. OnSceneLoaded configure controller khi còn Tutorial, bind một event kết quả; controller có Journey context dùng ChallengeCompleted thay cho đường legacy Completed. Coordinator snapshot → Apply → TryPersistSession → thông báo UI; khi lưu lỗi rollback và giữ màn RetrySave. Kết quả đã lưu không rollback vì route tiếp theo lỗi. SessionChanged trở thành thông báo view; các mutator bắt đầu/reset/rời bài tự gọi persistence rõ ràng để tránh save lặp.

TryPersistSession gộp settings/tutorialSeen hiện có, ghi settingsOnly=false cho thay đổi hành trình và chỉ bật HasSavedCampaign sau lưu thành công. Save cài đặt giữ policy settingsOnly hiện có. ResultPanel triển khai ShowChallenge tối thiểu: luyện hiển thị metric, thi hiển thị điểm, nút mới phát JourneyActionRequested; nút điều hướng không Apply lại kết quả. Pause/restart/rời bài giữ ngữ cảnh thử thách; hủy input khi rời.

- [ ] **Step 4: Chạy green** — hai fixture mới và FootballResultRoutingTests/PauseFlowTests hiện có. Expected: ghi kết quả đúng một lần, lỗi lưu/route có retry, reload không hoàn lượt.
- [ ] **Step 5: Commit** — “feat: commit challenge results before journey navigation”.

### Task 5: Map thành lộ trình học phần và UI kết quả

**Files:**

- Create: Assets/_Project/Scripts/UI/JourneyLessonList.cs
- Create: Assets/_Project/Scripts/UI/JourneyCourseSummary.cs
- Modify: Assets/_Project/Scripts/UI/MapScreen.cs; Assets/_Project/Scripts/UI/MapNodeView.cs; Assets/_Project/Scripts/UI/MapPresentationBuilder.cs; Assets/_Project/Scripts/UI/ResultPanel.cs.
- Modify: Assets/_Project/Scripts/Shell/S5ShellSceneController.cs.
- Modify: Assets/Editor/ShellSceneAuthoring.cs; Assets/_Project/Scenes/Map.unity.
- Test: Assets/Tests/PlayMode/Presentation/JourneyMapTests.cs.
- Modify tests: Assets/Tests/PlayMode/Presentation/FestivalUiExperienceTests.cs; Assets/Tests/PlayMode/Progression/S5NewGameTests.cs; Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs — phân biệt số môn/record và trạng thái môn được học.

**Interfaces:**

- Consumes: Journey eligibility/checkpoint/outcome và router của Tasks 2–4.
- MapNodeView.ConfigureJourneyState(SubjectId subject, string title, bool unlocked, bool passed, SubjectRecord record):void.
- JourneyLessonList.Bind(GameSession session, Action<string,ChallengeAttemptMode> onSelected):void.
- JourneyCourseSummary.Show(GameSession session):void; ScoreRows:IReadOnlyList<JourneyScoreRow>; SupplementaryRounds:int. JourneyScoreRow: readonly struct với Subject:SubjectId, Score:float, Rank:Rank; định nghĩa cùng file summary. MapScreen.RefreshJourney(GameSession session):void.

- [ ] **Step 1: Viết test trạng thái lộ trình**

    Assert.That(sprintButton.interactable, Is.True);
    Assert.That(volleyballButton.interactable, Is.False);
    Assert.That(soccerButton.interactable, Is.False);
    Assert.That(budgetLabel.text, Is.EqualTo("Lượt thi: 5/5"));

Kiểm tra sau Practice Sprint vẫn khóa Volleyball, sau Exam mở đúng bài Learn; các bài chưa mở khóa. Kiểm tra zero budget có nút ôn đúng Practice; Review không đổi checkpoint; practice result không hiển thị Rank F/điểm học phần. Test menu Continue sau reload settingsOnly/campaign và saved Map có layout lộ trình.

- [ ] **Step 2: Chạy red** — rtk proxy tools/run-unity-tests.sh PlayMode KMA.Tests.Presentation.JourneyMapTests journey-map-red.
- [ ] **Step 3: Dựng UI** — giữ ba môn nhưng thể hiện đường đi theo thứ tự; list ba bài trong môn đã mở, objective từ catalog, nút tiếp tục tới checkpoint. MapNodeView nhận availability từ session, không sửa shared SubjectConfig.unlocked ở runtime. ResultPanel có nút Retry/Practice/Continue/RetrySave theo outcome; supplementary có thông báo và chỉ dẫn ôn. Dùng theme/typography chung, metric luyện và điểm/hạng thi đúng vai trò. Shell gọi TryStartChallenge; Continue đọc state đã khôi phục.

Chạy KMA.EditorTools.ShellSceneAuthoring.Apply() sau khi builder thay đổi và review diff scene; kiểm tra persistent layout ngoài Play Mode. Tích hợp CourseSummary ở Task 9.

- [ ] **Step 4: Chạy green** — JourneyMapTests và các fixture startup/menu bị ảnh hưởng. Expected: UI và route cùng khóa; tiếp tục không bị chuyển sang môn cũ do ôn tập.
- [ ] **Step 5: Commit** — “feat: present the sequential course and challenge results”.

### Task 6: Chương Sprint, 100 m luyện/thi và thể lực có tác động

**Files:**

- Create: Assets/_Project/Scripts/Gameplay/Sprint/SprintBalanceConfig.cs
- Create: Assets/_Project/Scripts/Gameplay/Sprint/SprintChallengeRules.cs
- Create: Assets/_Project/Resources/Journey/SprintBalance.asset
- Modify: Assets/_Project/Scripts/Gameplay/Sprint/SprintRules.cs; Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs; Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs; Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs.
- Modify: Assets/Editor/StudentJourneyContentBuilder.cs.
- Test: Assets/Tests/EditMode/Gameplay/Running/SprintChallengeTests.cs.
- Test: Assets/Tests/PlayMode/Gameplay/Running/SprintChallengeRuntimeTests.cs.
- Modify: Assets/_Project/ScriptableObjects/Subjects/Sprint.asset; Assets/Tests/EditMode/Gameplay/Running/SprintRulesTests.cs; Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs khi assertion liên quan mục tiêu/cấu hình thay đổi.

**Interfaces:**

- Consumes: IChallengeController/context/result và ChallengeDefinition Task 1.
- SprintBalanceParameters: readonly struct với float properties InitialStamina, MaxStamina, CorrectImpulse, WrongImpulseFactor, SpeedCap, CorrectTapCost, WrongTapCost, BurstRateThreshold, BurstExtraCost, ActiveDrainSpeedThreshold, ActiveDrainPerSpeed, RestRegenPerSecond, FatigueThreshold, FatigueImpulseFactor, FatigueSpeedCap, DragPerSecond, DistanceScale. Constructor nhận đủ các giá trị cùng tên camelCase. static Default:SprintBalanceParameters trả các giá trị theo thứ tự [100,100,18,0.4,120,0.25,1.5,6,0.75,20,0.02,6,30,0.75,90,15,0.08]; asset có serialized fields tương ứng. Định nghĩa struct cùng file SprintBalanceConfig.cs; ToRuntime truyền đủ giá trị, không dùng default struct bằng 0.
- SprintBalanceConfig.ToRuntime():SprintBalanceParameters; static LoadDefault():SprintBalanceConfig.
- SprintRules thêm optional SprintBalanceParameters? balance vào cuối constructor; cung cấp CorrectStreak:int, TimeLimit:float. Tuning Journey áp dụng khi truyền balance; constructor hiện có vẫn hợp lệ.
- SprintChallengeRules(ChallengeDefinition definition, SprintBalanceParameters balance, RivalPaceProfile[] rivals); Race:SprintRules; IsComplete:bool; Tap(Side side):void; Tick(float dt):void; BuildResult(ChallengeAttemptContext context):ChallengeAttemptResult.
- SprintController triển khai IChallengeController; controller/HUD dùng definition.Distance/TimeLimit/TargetCount theo loại.

- [ ] **Step 1: Viết test ba bài và cân bằng**

    Assert.That(practiceDefinition.Distance, Is.EqualTo(100f));
    Assert.That(practiceDefinition.TimeLimit, Is.EqualTo(20f));
    Assert.That(examDefinition.TimeLimit, Is.EqualTo(14f));
    Assert.That(regularTapsResult.Pass, Is.True);
    Assert.That(regularTapsResult.Metrics.Elapsed, Is.LessThanOrEqualTo(14f));

Pin 12 liên tiếp/reset khi sai; mốc 100 m đúng 20/14 giây; đứng hạng 4 vẫn đạt nếu kịp giờ. Simulate input luân phiên 2/4/6 Hz với bước 1/240 giây; 8–10 Hz cạn thể lực và chậm hơn 6 Hz. Kiểm tra hao đúng 0,25/sai 1,5, dồn nhanh thêm 0,75, hồi 6 khi speed ≤20, ngưỡng 30, cap mệt 90/impulse 75%, giá trị hữu hạn/chặn giới hạn. Runtime test dùng tap bridge và đủ lifecycle, kiểm tra Completed một lần và mở Volleyball sau thi.

- [ ] **Step 2: Chạy red** — SprintChallengeTests (EditMode), SprintChallengeRuntimeTests (PlayMode), namespace KMA.Tests.Gameplay.Running.
- [ ] **Step 3: Triển khai** — balance từ spec và asset; Race ghi thời gian giữa taps để tính hao. Learn chấm chuỗi, Practice/Exam chấm 100 m cùng deadline, chỉ Exam tạo MinigameResult. HUD/finish line/time đọc definition; bài Learn hiển thị chuỗi mục tiêu, bài thi có ba đối thủ. Cập nhật goalText Sprint thành “Hoàn thành 100 m trong 14 giây.” Thứ hạng/độ chính xác/thể lực đóng góp điểm, không thêm điều kiện thứ hạng để qua môn.

Controller ConfigureChallenge thay rules trước Play, giữ input bridge và lifecycle hiện có; gọi TryBeginResolve và phát typed result trong Journey, đường standalone vẫn có kết quả tương thích. Tự kiểm chứng luồng Sprint Learn → Practice → Exam → mở Volleyball, gồm năm lần trượt/ôn bổ sung.

- [ ] **Step 4: Chạy green** — hai fixture mới, sau đó SprintRulesTests/SprintRuntimeInputTests/SprintControllerTests. Expected: đủ ba profile, mốc thời gian đúng và budget chỉ thay đổi khi thi trượt.
- [ ] **Step 5: Commit** — “feat: add Sprint lessons and stamina based acceleration”.

### Task 7: Bài đỡ, chuỗi chuyền–đập và thi bóng chuyền đúng deadline

**Files:**

- Create: Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatchOptions.cs
- Create: Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballChallengeRules.cs
- Modify: Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.cs; Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballMatch.Opponent.cs; Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballController.cs; Assets/_Project/Scripts/Gameplay/Volleyball/VolleyballHud.cs.
- Modify: Assets/Editor/VolleyballSceneConfigurator.cs nếu cần bind trợ giúp.
- Test: Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballChallengeTests.cs.
- Test: Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballChallengeRuntimeTests.cs.
- Modify tests: Assets/Tests/EditMode/Gameplay/Volleyball/VolleyballMatchTests.cs; Assets/Tests/EditMode/Gameplay/Volleyball/ActionResolverTests.cs; Assets/Tests/PlayMode/Gameplay/Volleyball/VolleyballControllerTests.cs.

**Interfaces:**

- Consumes: Task 1 definition/context/result; IChallengeController.
- VolleyballMatchOptions(int pointsToWin=5, float timeLimit=120f, bool opponentAlwaysServes=false); giá trị 0 tắt điều kiện điểm/giờ trong drill.
- VolleyballMatch thêm optional options vào cuối constructor, ResetRally(CourtSide server):void và event Action<CourtSide,ActionDecision,int> TouchRegistered. int là số chạm trước hành động; reset rally không tạo kết quả thi.
- VolleyballChallengeRules(ChallengeDefinition definition); Match:VolleyballMatch; CompletedTargets:int; IsComplete:bool; SetMove(Vector2 move):void; PressAction():ActionDecision; Tick(float dt):void; BuildResult(context):ChallengeAttemptResult.

- [ ] **Step 1: Viết test đỡ/chuỗi và điểm tại deadline**

    Assert.That(learnAfterThreeReceives.IsComplete, Is.True);
    Assert.That(practiceAfterFreeBallPoint.CompletedTargets, Is.EqualTo(0));
    Assert.That(examLeadingTwoOneAtTimeout.Pass, Is.False);
    Assert.That(fifthPointAt120Seconds.Pass, Is.True);

Drill lỗi lặp được cấp bóng mới; nhận không cần liên tiếp. Practice chỉ cộng điểm khi cùng possession có Receive với touchesBefore=0, Receive với touchesBefore=1, Smash với touchesBefore=2 rồi ghi điểm. Reset tracker khi rally/possession đổi, lỗi hoặc đối thủ chạm. Test 119,999/120/120,001 giây và frame dt lớn vượt deadline; điểm sau 120 không được ghi. Runtime chứng minh typed result, pause và số mục tiêu HUD.

- [ ] **Step 2: Chạy red** — VolleyballChallengeTests (EditMode), VolleyballChallengeRuntimeTests (PlayMode), namespace KMA.Tests.Gameplay.Volleyball.
- [ ] **Step 3: Triển khai** — options cho drill không giới hạn điểm/giờ, cấp lại bóng từ đối thủ; dùng BallFlight/ActionResolver và các pha chạm hiện có. Learn reset rally sau Receive hợp lệ; Practice đối thủ dễ tạo cơ hội, theo dõi đúng sequence. Exam dùng mẫu OpponentPlan hiện có và clamp Tick tới thời gian còn lại, xử lý điểm tại deadline trước timeout. BuildResult Exam chỉ pass khi người chơi đạt 5 trước đối thủ trong 120 giây.

Controller triển khai interface, HUD drill dùng bộ đếm/điểm rơi/timing help; thi giảm trợ giúp. Joystick/nút ĐÁNH và vật lý sân tiếp tục do bridge/rules hiện có sở hữu. Asset/scene bind mới được author và kiểm tra GUID.

- [ ] **Step 4: Chạy green** — hai fixture mới và VolleyballMatchTests/ActionResolverTests/VolleyballInputBridgeTests. Expected: bài tập có thể hoàn thành bằng input hợp lệ, không có điểm sau deadline, Exam mở Soccer.
- [ ] **Step 5: Commit** — “feat: add volleyball training and timed course assessment”.

### Task 8: Soccer drill không giới hạn và bài thi năm cú sút

**Files:**

- Create: Assets/_Project/Scripts/Gameplay/Football/FootballMatchOptions.cs
- Modify: Assets/_Project/Scripts/Gameplay/Football/FootballRules.cs; Assets/_Project/Scripts/Gameplay/Football/FootballController.cs; Assets/_Project/Scripts/Gameplay/Football/FootballHud.cs; Assets/_Project/Scripts/Gameplay/Football/FootballPresentation.cs.
- Modify: Assets/Editor/FootballSceneConfigurator.cs nếu có field bind mới.
- Test: Assets/Tests/EditMode/Gameplay/Ball/FootballChallengeTests.cs.
- Test: Assets/Tests/PlayMode/Gameplay/Football/FootballChallengeRuntimeTests.cs.
- Modify tests: Assets/Tests/EditMode/Gameplay/Ball/FootballRulesTests.cs; Assets/Tests/PlayMode/Gameplay/Football/FootballControllerTests.cs; Assets/Tests/PlayMode/Gameplay/Football/FootballInputTests.cs.

**Interfaces:**

- Consumes: definition/context/result Task 1, IChallengeController và FootballTuning/FlightSimulation hiện có.
- FootballMatchOptions(int? maxKicks, int requiredGoals, bool keeperEnabled); Exam=(5,3,true), Learn=(null,3,false), Practice=(null,2,true).
- FootballRules thêm optional options vào cuối constructor; BuildChallengeResult(context):ChallengeAttemptResult. Constructor hiện có mặc định bài thi năm cú sút.
- FootballController.ConfigureChallenge(definition, context):void; BeginMatch giữ chữ ký hiện có, độ khó Journey Exam lấy từ definition Normal; FreePlay lấy context.Difficulty.

- [ ] **Step 1: Viết test unlimited drill, tuning và hủy hold**

    Assert.That(learnRules.Kicks, Is.GreaterThan(5));
    Assert.That(learnRules.State, Is.Not.EqualTo(FootballState.MatchResult));
    Assert.That(fiveKicksThreeGoalsResult.Pass, Is.True);
    Assert.That(fiveKicksTwoGoalsResult.Pass, Is.False);

Learn chưa đạt ba bàn tiếp tục sau cú thứ 5, thủ môn không can thiệp; Practice cần hai bàn/Normal; Exam đợi đủ năm cú kể cả đã ghi ba sớm. Kiểm tra Journey/Review/FreePlay luôn chạy Normal, bộ chọn độ khó không xuất hiện. Runtime tests hold → pause/exit/disable → resume: preview tắt, charge hủy, không phát shot muộn; result một lần, Retry không trừ lượt lần hai.

- [ ] **Step 2: Chạy red** — FootballChallengeTests (EditMode, namespace KMA.Tests.Gameplay.Football), FootballChallengeRuntimeTests (PlayMode cùng namespace).
- [ ] **Step 3: Triển khai** — bỏ phụ thuộc constant MaxKicks ở drill, terminal theo requiredGoals; Exam vẫn 5 kicks và goals ≥3. Chuyển keeperEnabled vào cả prediction và FootballFlightSimulation thật. HUD drill hiển thị mục tiêu/bàn đạt, Exam giữ năm chấm kết quả; bỏ picker và cố định Normal trong Journey/Review/FreePlay theo điều chỉnh của người dùng ngày 2026-10-04. Trial metrics giữ số cú thực hiện, practice không tạo điểm học phần. Dùng tuning asset hiện có, mapping difficulty bằng switch tường minh.

Controller đi qua TryBeginResolve và typed result khi có context, hủy charge/bridge khi pause/rời scene. Author scene nếu serialized wiring đổi, kiểm tra input references và preview release synchronous.

- [ ] **Step 4: Chạy green** — hai fixture mới, FootballRulesTests/FootballShotSolverTests/FootballInputTests/FootballPresentationTests. Expected: preview và cú sút thật đồng nhất; bài Exam cuối mở tổng kết.
- [ ] **Step 5: Commit** — “feat: add Soccer drills and the final five kick exam”.

### Task 9: Hội thoại theo mốc, tổng kết và chơi lại

**Files:**

- Create: Assets/_Project/Scripts/Progression/Journey/JourneyDialogueLibrary.cs
- Create: Assets/_Project/Scripts/UI/JourneyDialoguePresenter.cs
- Create: Assets/_Project/Resources/Journey/JourneyDialogues.asset
- Modify: Assets/_Project/Scripts/UI/JourneyCourseSummary.cs; Assets/_Project/Scripts/Core/GameManager.cs; Assets/_Project/Scripts/Shell/S5ShellSceneController.cs.
- Modify: Assets/Editor/StudentJourneyContentBuilder.cs; Assets/_Project/Scenes/Map.unity nếu layout authored đổi.
- Test: Assets/Tests/PlayMode/Presentation/JourneyNarrativeTests.cs.
- Test: Assets/Tests/EditMode/Progression/JourneyDialogueDataTests.cs.

**Interfaces:**

- JourneyDialogueLibrary.LoadDefault():JourneyDialogueLibrary; Get(string nodeId):JourneyDialogueNode.
- JourneyDialogueNode có Id:string và Lines:IReadOnlyList<JourneyDialogueLine>; line có SpeakerRole:string, Text:string, Portrait:Sprite.
- Node IDs: opening, sprint_intro, sprint_exam, sprint_pass, volleyball_intro, volleyball_exam, volleyball_pass, soccer_intro, soccer_exam, soccer_pass, supplementary, course_complete. supplementary là thoại lặp theo đợt; ID xem lưu thêm chỉ số đợt để không lặp trong cùng đợt.
- JourneyDialoguePresenter.Show(string nodeId, Action onClosed):void.
- GameManager.TryMarkJourneyDialogueSeen(string seenKey, out string error):bool; JourneyProgress.IsDialogueSeen(string key):bool; MarkDialogueSeen(string key):void.

- [ ] **Step 1: Viết test hội thoại và ending**

    Assert.That(library.Get("opening").Lines.Count, Is.InRange(2, 4));
    Assert.That(session.Journey.IsDialogueSeen("opening"), Is.False);
    Assert.That(summary.ScoreRows.Count, Is.EqualTo(3));
    Assert.That(summary.SupplementaryRounds, Is.EqualTo(session.Journey.SupplementaryRounds));

Test mở/skip/đóng lưu seenKey; đóng ứng dụng giữa hội thoại chưa xác nhận có thể xem lại; lỗi lưu không bỏ mốc. Hội thoại chặn input/timer trước Play và nút Start; có thể skip. Mốc pass/intro chỉ chạy khi mở chương lần đầu; supplementary theo từng đợt. Hoàn thành ba Exam hiện bảng điểm và thoại kết; reload giữ complete, chơi lại không tiêu budget/đổi checkpoint.

- [ ] **Step 2: Chạy red** — JourneyDialogueDataTests (EditMode, namespace KMA.Tests.Gameplay.Progression), JourneyNarrativeTests (PlayMode, namespace KMA.Tests.Presentation).
- [ ] **Step 3: Viết nội dung và nối các mốc** — 2–4 lượt nói ngắn mỗi node, dùng các câu mẫu spec §6, các vai trò và chân dung Toon hiện có. StudentJourneyContentBuilder.BuildDialogues():void tạo/reuse library; BuildAll():void chạy BuildChallenges, tạo/reuse SprintBalance và BuildDialogues, sau đó ShellSceneAuthoring.Apply.

Seen được ghi khi đóng/skip thành công; queue theo opening → intro môn → trước Exam → pass môn → intro kế tiếp → ending. Supplementary đi qua phiên thoại có seenKey theo đợt. Presenter dùng unscaled time, VietTypography/VietText.Fix và tutorial/start gate của controller. CourseSummary trình bày ba điểm/hạng, số đợt bổ sung, lời kết; nút chơi lại chọn bài và mode Review/FreePlay theo eligibility.

- [ ] **Step 4: Chạy green** — hai fixture mới, VietnameseFontTests/FestivalUiExperienceTests/S5NewGameTests phù hợp. Expected: đúng mốc, glyph đọc được, timer/input bị giữ tới khi thoại đóng.
- [ ] **Step 5: Commit** — “feat: tell the freshman story and celebrate course completion”.

### Task 10: Kiểm chứng toàn hành trình và sửa các hồi quy được phát hiện

**Files:**

- Create: Assets/Tests/PlayMode/Progression/StudentJourneyFlowTests.cs
- Create: Assets/Tests/PlayMode/Progression/JourneyGameplayDriver.cs
- Modify: Assets/Tests/PlayMode/Progression/KMA.Gameplay.Progression.PlayMode.Tests.asmdef — thêm KMA.Gameplay.Volleyball để helper chạy đủ ba môn.
- Modify: Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs; Assets/Tests/PlayMode/Progression/CoreLoopTests.cs; Assets/Tests/PlayMode/Progression/GameplayPresentationTests.cs; Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs và các fixture có giả định map mở sẵn.
- Modify: runtime/scene/test liên quan chỉ khi có lỗi được tái hiện.
- Create: docs/qa/kma-student-journey.md.

**Interfaces:**

- Consumes: toàn bộ API các task trước.
- JourneyGameplayDriver.CompleteActiveChallenge(SceneRouter router):IEnumerator là helper test; dùng input/rules thật của từng môn và đọc kết quả do controller tạo.
- Test namespace KMA.Tests.Gameplay.Progression.StudentJourneyFlowTests.

- [ ] **Step 1: Viết test toàn luồng**

    Assert.That(router.Session.Journey.CourseComplete, Is.True);
    Assert.That(router.Session.Journey.IsChallengeComplete("soccer_exam"), Is.True);
    Assert.That(router.Session.Records.Values.Count(x => x.Passed), Is.EqualTo(3));
    Assert.That(reloaded.Journey.CourseComplete, Is.True);

Đi Bootstrap → New Game → đủ chín bài → tổng kết, kiểm tra khóa môn giữa các chặng. Test một vòng năm lần trượt tại Volleyball, ôn đúng Practice, thi lại đạt, giữ Sprint. Các result trong full flow phải do rules/controller thật phát ra; helper tiến trình giả chỉ dành cho unit/route test. Thêm smoke Resources catalog/dialogue/balance trong Player test, reload trước/sau receipt, pause/restart, skip thoại và settingsOnly.

- [ ] **Step 2: Chạy red** — rtk proxy tools/run-unity-tests.sh PlayMode KMA.Tests.Gameplay.Progression.StudentJourneyFlowTests journey-flow-red.
- [ ] **Step 3: Hoàn thiện tích hợp theo lỗi thực tế** — cập nhật setup/expectation test cũ khi sản phẩm chuyển thành khóa môn và chín bài; giữ các assertion lifecycle/input/transaction còn đúng. Nếu audio Sprint vẫn lỗi, dùng systematic-debugging, tái hiện với baseline/môi trường audio thực, sửa nguyên nhân đã xác định ở AudioManager/GameAudioLibrary/scene hoặc cấu hình môi trường chạy. Giữ ngưỡng non-silent và kiểm tra listener của AudioGameplayTests.
- [ ] **Step 4: Chạy focused green rồi toàn bộ EditMode và PlayMode**

    rtk proxy tools/run-unity-tests.sh PlayMode KMA.Tests.Gameplay.Progression.StudentJourneyFlowTests journey-flow-green
    rtk proxy tools/run-unity-tests.sh EditMode "" journey-all-edit
    rtk proxy tools/run-unity-tests.sh PlayMode "" journey-all-play

Đọc root XML và leaf test-case, không cộng result của test-suite. Gate: total >0, failed=0 cho cả hai suite; mọi leaf skipped chỉ được thuộc ba Ignore ChallengeSequenceTests: NonFiniteProgress_CannotAdvanceOrCompletePunishment, Controller_ActivatesAuthoredCueAndCounterplayAdapter, Controller_RequestsRetryOnceWithoutChangingLivesOrMutatingTheSession. Wrapper có thể trả 1 khi root Skipped:Ignored dù failed=0; trong trường hợp này xác nhận chính xác các leaf skipped rồi ghi counts, không báo suite thất bại vì Ignore hoặc báo suite đạt nếu có failure.

Chạy ScenePresentationContractTests trong full suite, không dùng pass của test chạy riêng để thay thế full result. Ghi môi trường, commit, XML và các failure còn lại vào QA; chưa đóng nhiệm vụ nếu required gate còn lỗi.

- [ ] **Step 5: Commit** — “test: verify the complete student journey and recovery flows”, gồm các sửa hồi quy có bằng chứng và QA tự động.

### Task 11: QA hình ảnh, cân bằng và Android

**Files:**

- Modify: docs/qa/kma-student-journey.md; README.md.
- Modify: balance/copy/layout/scene cụ thể theo vấn đề quan sát được.
- Artifacts: Builds/Screenshots/student-journey/, Builds/TestResults/journey-*.xml, Builds/Android/journey-*.apk.

**Interfaces:**

- Consumes: BuildAll của Task 9, scene authoring/test/build tools hiện có.
- KMA.EditorTools.StudentJourneyContentBuilder.BuildAll():void là entry point author nội dung đã xác định.

- [ ] **Step 1: Author và kiểm tra asset/scene** — gọi BuildAll bằng Editor menu hoặc executeMethod; review GUID/reference diff của scene và asset. Rerun fixture tương ứng sau author. Ghi checkpoint commit thực tế dùng để QA.
- [ ] **Step 2: Capture và xem PNG** — áp dụng .claude/skills/testing-unity-ui-with-screenshots/SKILL.md; dùng một Editor GUI có render surface, match done.json/request ID. Xem lộ trình lúc đầu/mở môn/chờ bổ sung/đã hoàn tất, từng HUD Learn/Practice/Exam, kết quả thi trượt và tổng kết. Kiểm tra 16:9, 16:10, 18:9, 4:3; chứng cứ capture tĩnh và thao tác được ghi riêng.
- [ ] **Step 3: Chơi thử cân bằng** — ít nhất ba người chưa biết game; ghi thời lượng, số lần thử mỗi bài, lúc không hiểu mục tiêu và lý do trượt. Dùng mốc 20–30 phút như mục tiêu, thay đổi tuning dựa trên dữ liệu và kiểm tra lại các bài liên quan. Giữ Practice Sprint dài thời gian hơn Exam; thay đổi mốc mục tiêu đã chốt phải được ghi rõ để người dùng review.
- [ ] **Step 4: Build Android** — đóng Editor giữ project lock trước batch, chạy:

    rtk proxy tools/build-apk.sh --arm64 --output-dir Builds/Android --name journey

Expected: Unity Build Success, APK mới journey-arm64.apk, ghi hash/kích thước/ABI. Chỉ build thêm x86_64 nếu cần QA emulator, dùng --x86_64 với cùng tên. Recheck project architecture sau build.

- [ ] **Step 5: QA trên điện thoại ARM64 thật** — ghi model/API/aspect/safe area và bản APK; chơi nhập học → Sprint → Volleyball → Soccer → tổng kết, một vòng hết lượt/bổ sung, đóng/mở tại kết quả, pause/charge cancel, âm thanh, touch và vào nền/khôi phục. Đo frame time/FPS bằng công cụ có sẵn và ghi số liệu, không suy ra từ build hoặc ảnh Editor. Thiếu thiết bị/người thử thì ghi phần còn thiếu và trạng thái bàn giao, không ghi đạt.
- [ ] **Step 6: Kiểm chứng cuối và commit** — chạy lại tests phù hợp những sửa từ QA; nếu runtime/model đổi thì chạy full suites. Kiểm tra git diff --check và staged scope. Commit “docs: record student journey visual balance and Android QA”, kèm các sửa cụ thể đã kiểm chứng. Báo riêng automated tests, hình ảnh, chơi thử, build, emulator và điện thoại.

## Plan Self-Review and Handoff

| Yêu cầu spec | Nhiệm vụ chịu trách nhiệm |
|---|---|
| Cốt truyện/hành trình ngắn, mở đầu và kết | 9, 11 |
| Chín thử thách, thứ tự và khóa môn | 1, 2, 4, 5, 6–8 |
| Chiều sâu và điều kiện từng môn | 6, 7, 8 |
| Năm lượt, thi bổ sung mới, giữ môn đạt | 2, 3, 4, 5, 10 |
| Lưu trước điều hướng, save v7/legacy/settingsOnly | 3, 4, 9, 10 |
| Lộ trình, metric luyện, điểm thi, chơi lại | 5, 8, 9 |
| Input/lifecycle/âm thanh/asset/GUID | 4, 6–8, 10, 11 |
| QA người mới, hình ảnh, build và thiết bị | 11 |

Đã tự rà coverage spec, chữ ký API, thứ tự phụ thuộc và tỷ lệ nội dung. Bổ sung DTO/converter, rollback giữ nguyên attempt, tham chiếu test assembly và các property bảng điểm còn thiếu. Cả năm Review Focus có test ở task sở hữu. Giá trị cấu hình ban đầu lấy từ spec; kết quả runtime sẽ được kiểm chứng khi thực thi.

Người dùng duyệt kế hoạch và chọn cách thực thi trước khi bắt đầu. Đề xuất **Native** cho kế hoạch này: các task nối tiếp qua cùng hợp đồng tiến trình/lưu/route, nên một người triển khai xuyên suốt giảm việc lặp lại ngữ cảnh; có review toàn nhánh sau cùng theo skill thực thi. Có thể chọn **Subagent-driven** để có reviewer riêng ở từng task, với chi phí ngữ cảnh cao hơn.
