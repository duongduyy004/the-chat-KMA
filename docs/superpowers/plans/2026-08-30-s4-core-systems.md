# S4 Core Systems Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add durable campaign persistence, session restoration, settings, audio/haptics/pooling services, authored subject data, and a Bootstrap entry scene without changing the tested gameplay contracts.

**Architecture:** `SaveData` is a plain serializable DTO owned by the progression layer; `SaveSystem` owns JSON migration and atomic file I/O; `GameManager` owns startup orchestration and injects a restored `GameSession` into the existing persistent `SceneRouter`. `SceneRouter` continues to create a default session in `Awake`, route with `LoadSceneMode.Single`, and expose its existing APIs. `SubjectConfig` owns presentation/game-balance metadata only; scene names remain exclusively in `SceneRouter`.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, UnityEngine JSON/AudioMixer, Unity Test Framework EditMode, Android IL2CPP ARM64.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` (§S4 and §9–§10)

## Global Constraints

- Không sửa rules engine đã có test; chỉ thêm method/event, adapter hoặc component presentation additive.
- Giữ nguyên chữ ký và hành vi đã test của `SceneRouter`, `GameSession`, `MinigameBase`, các rules và `BossSequenceAsset`; khi cần mở rộng, thêm API mới rồi kiểm regression.
- Không dùng `PlayerPrefs` cho save/settings; save chính là `Application.persistentDataPath/save.json`, ghi atomic qua `save.tmp` và `File.Replace`.
- `SceneRouter.subjectScenes` là nguồn sự thật duy nhất cho routing; `SubjectConfig` không có field `sceneName`.
- `AudioManager` không sở hữu beat clock; `EnduranceController` giữ `dspTime` và `rhythmOffsetMs` behavior hiện có.
- `stars` không được lưu; luôn suy ra bằng `ScoreUtil.ToStars(Rank)`: `S/A → 3`, `B/C → 2`, `D → 1`, `F → 0`.
- Không commit hoặc xoá các thay đổi không thuộc S4; preflight phải ghi nhận worktree đang dirty trước mỗi task.
- Mỗi task có commit riêng sau khi test pass; chạy lại full EditMode và PlayMode sau mỗi task khi Unity editor khả dụng.

## File Structure

| Area | Files/responsibility |
|---|---|
| Persistence DTO | `Assets/_Project/Scripts/Progression/SaveData.cs` — versioned `SaveData`, `SubjectRecordData`, `Settings` |
| Session adapters | `Assets/_Project/Scripts/Progression/{GameSession,SubjectRecord}.cs`, `Assets/_Project/Scripts/Gameplay/Common/ScoreUtil.cs` — restore/export and derived stars |
| File I/O | `Assets/_Project/Scripts/Core/SaveSystem.cs` — path, atomic write, migration, invalid-file recovery |
| Startup/routing | `Assets/_Project/Scripts/Core/{GameManager,SceneRouter}.cs`, `Assets/_Project/Scenes/Bootstrap.unity` |
| Services | `Assets/_Project/Scripts/Core/{AudioManager,HapticsService,Pool}.cs` |
| Authoring data | `Assets/_Project/Scripts/ScriptableObjects/{SubjectConfig,InstructorQuoteSet,RivalPaceProfileAsset}.cs`, `Assets/_Project/ScriptableObjects/Subjects/*.asset` |
| Tests | `Assets/Tests/EditMode/Progression/{SaveDataTests,GameSessionPersistenceTests,ResetSaveTests,SaveSystemTests,ScoreStarsTests,ServiceContractTests,SceneRouterSessionTests}.cs`, `Assets/Tests/PlayMode/Core/{GameManagerStartupTests,S4BootstrapPersistenceGateTests}.cs` |

---

### Task 1: Lock the S4 data contracts and derived stars

**Files:**
- Create: `Assets/_Project/Scripts/Progression/SaveData.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Common/ScoreUtil.cs`
- Create: `Assets/Tests/EditMode/Progression/ScoreStarsTests.cs`
- Create: `Assets/Tests/EditMode/Progression/SaveDataTests.cs`
- Modify: `Assets/Tests/EditMode/Progression/KMA.Gameplay.Progression.EditMode.Tests.asmdef` — add `KMA.Gameplay.Core` so the shared S4 EditMode assembly can test `SaveSystem` and `SceneRouter`.

**Interfaces:** `SaveData` exposes serializable fields/properties `version`, `lives`, `subjects`, `bossUnlocked`, `gameCompleted`, `tutorialSeen`, and `settings`. `SubjectRecordData` exposes `id`, `passed`, `bestScore`, `bestRank`, and `failedVisits`. `Settings` exposes `musicVol`, `sfxVol`, `vibration`, and `rhythmOffsetMs`. `ScoreUtil.ToStars(Rank rank)` returns an `int` and is pure.

- [ ] **Step 1: Snapshot the dirty worktree and confirm the existing contracts.**

  Run:

  ```bash
  rtk git status --short --branch
  rtk rg -n "class GameSession|class SubjectRecord|enum SubjectId|enum Rank|class ScoreUtil" Assets/_Project/Scripts
  ```

  Expected: only S4-owned files are selected later; existing modified S1/S2/S3 files remain unstaged.

- [ ] **Step 2: Write failing tests for stars and DTO JSON shape.**

  Add tests with these assertions:

  ```csharp
  [TestCase(Rank.F, 0)] [TestCase(Rank.D, 1)] [TestCase(Rank.C, 2)]
  [TestCase(Rank.B, 2)] [TestCase(Rank.A, 3)] [TestCase(Rank.S, 3)]
  public void ToStars_UsesRankBands(Rank rank, int expected) =>
      Assert.That(ScoreUtil.ToStars(rank), Is.EqualTo(expected));

  [Test]
  public void SaveData_ContainsSevenRecordsAndSettings()
  {
      var data = SaveData.CreateDefault();
      Assert.That(data.subjects, Has.Length.EqualTo(7));
      Assert.That(data.settings, Is.Not.Null);
      Assert.That(data.tutorialSeen, Has.Length.EqualTo(7));
  }
  ```

- [ ] **Step 3: Run the focused tests and verify they fail for missing APIs.**

  Run:

  ```bash
  rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s4-task1.xml -logFile /tmp/kma-s4-task1.log -quit -testFilter "ScoreStarsTests|SaveDataTests"
  ```

  Expected: FAIL because `ToStars`, `SaveData`, and its default factory are not implemented yet.

- [ ] **Step 4: Implement the minimal DTOs and star conversion.**

  Use `[Serializable]` classes with public data fields so `JsonUtility` can serialize them. `SaveData.CreateDefault()` must create exactly one record for every `SubjectId` enum value, seven `false` tutorial flags, default lives `5`, `version` equal to the current schema constant, and default settings. `ScoreUtil.ToStars` must use only the `Rank` enum and never read persisted data.

- [ ] **Step 5: Run the focused tests and compile regression tests.**

  Run the same focused command, then:

  ```bash
  rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-editmode.xml -logFile /tmp/kma-editmode.log -quit
  ```

  Expected: focused tests and all existing EditMode tests PASS.

- [ ] **Step 6: Commit only Task 1 files.**

  ```bash
  rtk git diff --check
  rtk git add Assets/_Project/Scripts/Progression/SaveData.cs Assets/_Project/Scripts/Gameplay/Common/ScoreUtil.cs Assets/Tests/EditMode/Core/ScoreStarsTests.cs Assets/Tests/EditMode/Progression/SaveDataTests.cs
  rtk git commit -m "feat: define S4 persistence data contracts"
  ```

### Task 2: Add GameSession/SubjectRecord export and restore

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Progression/SubjectRecord.cs`
- Create: `Assets/Tests/EditMode/Progression/GameSessionPersistenceTests.cs`

**Interfaces:** Add `GameSession.ToSaveData()`, `GameSession.Restore(SaveData)`, and `SubjectRecord.FromData(SubjectRecordData)`. Existing constructor, private setters, `StartSubject`, `SubmitResult`, `CompletePunishment`, `BossUnlocked`, and all existing route semantics remain unchanged.

- [ ] **Step 1: Write failing round-trip tests.**

  Cover a passing record, failed visits, lives, tutorial flags, settings, and a seven-record restore:

  ```csharp
  [Test]
  public void ToSaveDataAndRestore_PreserveCampaignState()
  {
      var original = new GameSession();
      original.StartSubject(SubjectId.Sprint);
      original.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
      var data = original.ToSaveData();
      data.lives = 3;
      data.settings.rhythmOffsetMs = -42f;
      data.tutorialSeen[0] = true;

      var restored = new GameSession();
      restored.Restore(data);

      Assert.That(restored.Lives, Is.EqualTo(3));
      Assert.That(restored.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
      Assert.That(restored.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(8f));
  }
  ```

- [ ] **Step 2: Run the test to verify it fails.**

  Run the focused EditMode filter `GameSessionPersistenceTests`; expected failure is missing `ToSaveData`, `Restore`, or `FromData`.

- [ ] **Step 3: Implement immutable-safe export and restore.**

  `ToSaveData()` must copy every record into DTOs and never expose `BestResult` as a mutable reference. `Restore()` must reject null input, clamp lives to `0..5`, rebuild all seven records from DTOs, tolerate missing records by using defaults, and restore only data owned by the session. `SubjectRecord.FromData` must rebuild `Passed`, `BestScore`, `BestRank`, and `FailedVisits` through a controlled factory; do not add Unity serialization attributes or public setters to `SubjectRecord`.

- [ ] **Step 4: Run focused and legacy progression tests.**

  ```bash
  rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s4-task2.xml -logFile /tmp/kma-s4-task2.log -quit -testFilter "GameSessionPersistenceTests|GameSessionTests|ChallengeSequenceTests"
  ```

  Expected: all focused and legacy progression tests PASS, including boss unlock and punishment routes.

- [ ] **Step 5: Run both suites and commit.**

  Run the full EditMode and PlayMode commands from the parent plan, then commit only the Task 2 files with `feat: add session persistence adapters`.

### Task 3: Implement versioned atomic SaveSystem

**Files:**
- Create: `Assets/_Project/Scripts/Core/SaveSystem.cs`
- Create: `Assets/Tests/EditMode/Progression/SaveSystemTests.cs`

**Interfaces:** `SaveSystem` provides `string SavePath`, `SaveData Load()`, `void Save(SaveData data)`, `void DeleteSave()`, and `SaveData Migrate(SaveData data)`. It uses `Application.persistentDataPath/save.json`, stages at `save.tmp`, and writes the replacement atomically.

- [ ] **Step 1: Write failing tests with an injected temporary path.**

  Add an internal constructor accepting a path provider so tests never touch a real user save. Test these exact cases: round-trip, current-version no-op migration, older-version default filling, missing file, malformed JSON, and no leftover `.tmp` after a successful save.

- [ ] **Step 2: Run focused tests and verify failure.**

  Run the `SaveSystemTests` filter; expected failure is missing `SaveSystem` and its path-provider seam.

- [ ] **Step 3: Implement JSON and migration.**

  Serialize `SaveData` with `JsonUtility.ToJson(data, true)`. `Load()` returns `SaveData.CreateDefault()` for a missing, empty, malformed, or structurally invalid file; valid older versions pass through explicit migration code that fills newly introduced fields and sets the current version. Never silently accept a future version as current. Do not use `PlayerPrefs`.

- [ ] **Step 4: Implement atomic replacement and reset semantics.**

  Ensure the parent directory exists, write and flush `save.tmp`, then replace `save.json` with `File.Replace` when the destination exists. For a first save, create the destination through the platform-safe equivalent while preserving the same staged-file guarantee. `DeleteSave()` removes only the validated `save.json` and `save.tmp` paths used by this instance. `Reset` behavior is represented by a new default save carrying forward only `settings` and `tutorialSeen`.

- [ ] **Step 5: Test migration, reset, and file postconditions.**

  Assert that reset restores five lives and empty records while preserving settings/tutorial flags; assert that a successful save leaves `save.json` readable and no `save.tmp`. Run full EditMode and PlayMode, then commit `feat: add versioned atomic save system`.

### Task 4: Inject restored sessions through GameManager and SceneRouter

**Files:**
- Create: `Assets/_Project/Scripts/Core/GameManager.cs`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Create: `Assets/Tests/EditMode/Progression/SceneRouterSessionTests.cs`
- Create: `Assets/Tests/PlayMode/Core/GameManagerStartupTests.cs`

**Interfaces:** Add `SceneRouter.LoadSession(GameSession session)`; it replaces the session and rebuilds the existing `SessionRouteTransitioner`. `GameManager` is a persistent `MonoBehaviour` with `SaveSystem`, restored `GameSession`, and save hooks; it must load the save before loading `Menu`.

- [ ] **Step 1: Write failing injection and startup tests.**

  Verify `LoadSession` changes `router.Session` without changing route configuration, a fresh manager loads default data, and a manager with a prepared save restores lives/records before `Menu` is loaded. Include a test that `OnApplicationPause(true)` invokes a save exactly once.

- [ ] **Step 2: Run focused tests and verify failure.**

  Expected failure is the absent `LoadSession`/`GameManager` APIs.

- [ ] **Step 3: Implement additive `LoadSession`.**

  Keep `SceneRouter.Awake()` exactly responsible for default `new GameSession()` and its existing `DontDestroyOnLoad`/scene-loaded subscription. `LoadSession` must reject null, unbind any scene handlers if needed, assign the supplied session, and construct a new `SessionRouteTransitioner` with the same sink. Do not change `DefaultSubjectScenes`, `TryGetSceneName`, `LoadSceneMode.Single`, or `BossSceneSessionHandoff`.

- [ ] **Step 4: Implement GameManager startup and save hooks.**

  On the persistent Bootstrap object, load/migrate the DTO, construct a `GameSession`, restore it, call `SceneRouter.EnsurePersistentInstance().LoadSession(session)`, apply settings to services, then load `Menu` exactly once. Subscribe to session result/life-loss/settings-change seams only through additive APIs; save after subject completion, life loss, settings change, and `OnApplicationPause(true)`. Apply `Application.targetFrameRate = 60` and `QualitySettings.vSyncCount = 0`.

- [ ] **Step 5: Run startup and regression tests.**

  Expected: a restored session is visible before Menu, existing router tests still pass, and no duplicate persistent router/manager is created across scene loads. Run both Unity suites and commit `feat: restore campaign session at bootstrap`.

### Task 5: Add audio, haptics, pooling, and authored ScriptableObjects

**Files:**
- Create: `Assets/_Project/Scripts/Core/AudioManager.cs`
- Create: `Assets/_Project/Scripts/Core/HapticsService.cs`
- Create: `Assets/_Project/Scripts/Core/Pool.cs`
- Create: `Assets/_Project/Scripts/ScriptableObjects/{SubjectConfig,InstructorQuoteSet,RivalPaceProfileAsset}.cs`
- Create: `Assets/_Project/ScriptableObjects/Subjects/*.asset` for 7 playable and 3 locked subjects
- Create/modify through Unity: `Assets/_Project/Settings/Audio/KMA-AudioMixer.mixer`
- Create: `Assets/Tests/EditMode/Progression/ServiceContractTests.cs`

**Interfaces:** `AudioManager.SetMusicVolume(float)`, `SetSfxVolume(float)`, and `PlaySfx(AudioClip)` control only Music/SFX mixer groups. `HapticsService.Light()`, `Medium()`, `Success()`, and `Fail()` no-op when vibration is disabled or unsupported. `Pool<T>` exposes `Get()` and `Release(T)` with reuse and no runtime instantiate/destroy churn. `SubjectConfig` contains display name, icon, color, goal text, time limit, pass threshold, and unlocked/comingSoon; it contains no scene name.

- [ ] **Step 1: Write failing service and asset contract tests.**

  Assert volume clamping, haptics disabled behavior, pool reuse, exactly 10 subject assets, seven enum-backed records plus three `comingSoon` assets, and absence of a `sceneName` field on `SubjectConfig`.

- [ ] **Step 2: Implement services with safe platform fallbacks.**

  `AudioManager` resolves serialized mixer groups and clamps linear volume before converting to mixer dB; missing clips/groups must fail safely. `HapticsService` uses platform-supported vibration APIs and a no-op fallback; it must never throw in Editor. `Pool<T>` prewarms serialized capacity and returns released instances to an inactive queue.

- [ ] **Step 3: Implement ScriptableObject authoring types and assets.**

  Create the 7 playable configs for the existing `SubjectId` values and 3 locked configs named Hít đất, Nhịp điệu, and Bơi lội with `comingSoon = true`. Keep all scene routing in `SceneRouter.subjectScenes`. Add chill and urgent quote arrays and a `RivalPaceProfileAsset` wrapper whose `ToRuntime()` returns the existing plain `RivalPaceProfile`.

- [ ] **Step 4: Run asset/service tests and inspect generated assets.**

  Run the focused EditMode tests plus full suites. Verify the mixer has exactly Music and SFX groups, assets are non-null after import, and no S4 service uses `PlayerPrefs` or owns endurance/Boss beat timing. Commit `feat: add core audio haptics and authored data`.

### Task 6: Create Bootstrap and prove the S4 gate

**Files:**
- Create: `Assets/_Project/Scenes/Bootstrap.unity` and `.meta`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Modify through Unity: Bootstrap references to `GameManager`, `SceneRouter`, `AudioManager`, `HapticsService`, and `GameCamera`/startup dependencies
- Create: `Assets/Tests/PlayMode/Core/S4BootstrapPersistenceGateTests.cs`

**Interfaces:** Bootstrap is build index 0, initializes services and restores the campaign, then enters `Menu`. A kill/relaunch preserves lives and the best record; reset preserves settings/tutorial flags while clearing campaign progress.

- [ ] **Step 1: Write the PlayMode gate before scene wiring.**

  The test must start Bootstrap, wait for `SceneManager.GetActiveScene().name == "Menu"`, complete one Sprint result through the existing router/session API, save, destroy/reload the Bootstrap scene, and assert the restored lives and Sprint rank. A second test asserts reset behavior.

- [ ] **Step 2: Run the gate to verify the scene is missing or incomplete.**

  Expected failure identifies missing Bootstrap/build-index wiring, not an unverified success.

- [ ] **Step 3: Create and wire Bootstrap as scene index 0.**

  Add one persistent `GameManager` root and the service roots needed by the startup contract. Ensure Bootstrap does not duplicate a scene-local `SceneRouter`; use `SceneRouter.EnsurePersistentInstance()` and let GameManager inject before loading Menu. Register Bootstrap, Menu, Map, existing gameplay scenes, Punishment, and GameOver in `EditorBuildSettings.asset` through Unity.

- [ ] **Step 4: Run the S4 gate and all verification commands.**

  Run full EditMode and PlayMode XML tests, then an Android development build/install smoke test if the Unity Android module is available. Expected gate: round-trip/migration pass, one subject survives kill/relaunch with lives and record intact, reset preserves settings/tutorial flags, and no legacy tests regress.

- [ ] **Step 5: Review scope and commit the final S4 integration.**

  Run:

  ```bash
  rtk git diff --check
  rtk git status --short
  rtk git diff --stat
  ```

  Stage only the Bootstrap/build-index, S4 source, assets, and S4 tests; leave unrelated dirty S1/S2/S3 files untouched. Commit `feat: complete S4 core systems`.

## Verification Commands

```bash
KMA_UNITY_EDITOR=/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity
rtk "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-editmode.xml -logFile /tmp/kma-editmode.log -quit
rtk "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-playmode.xml -logFile /tmp/kma-playmode.log -quit
```

When Unity cannot start because of the known sandbox loopback limitation, report the exact limitation and do not claim the S4 gate passed; use static `rtk rg`, `rtk git diff --check`, and available compiler/test alternatives only as partial verification.

## Plan Self-Review

- **Spec coverage:** S4-1 is covered by Tasks 2 and 4; S4-2 by Task 5 and the unchanged router; S4-3 by Tasks 1 and 3; S4-4 by Task 5; Bootstrap, target frame rate, migration, reset retention, and the S4 gate are covered by Tasks 4–6.
- **Placeholder scan:** no step delegates unspecified validation or says “implement later”; each task names files, interfaces, tests, commands, and expected outcomes.
- **Type consistency:** DTO names and fields match `GameSession.Restore`, `SubjectRecord.FromData`, `SceneRouter.LoadSession`, and `ScoreUtil.ToStars`; `RivalPaceProfileAsset.ToRuntime()` returns the existing `RivalPaceProfile` type.
- **Dirty worktree safety:** every task preserves existing uncommitted S1/S2/S3 paths and stages only its own files.

## Focused EditMode follow-up evidence (2026-08-31)

The EditMode route test was made independent of build-scene availability. `SceneRouterSessionTests.LoadSession_ReplacesSessionWithoutChangingRouteConfiguration` now uses reflection to inspect the private `SessionRouteTransitioner` before and after `LoadSession`, asserts that a new transitioner was created, and asserts that its private session is the restored `GameSession`. It no longer calls `SceneRouter.Route`, which requires `Application.CanStreamedLevelBeLoaded` in EditMode and starts scene loading.

Exact command:

```bash
rtk proxy timeout 180s /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath /home/duongduy/data/project/the-chat-KMA -runTests -testPlatform editmode -testFilter 'SaveSystemTests|SceneRouterSessionTests' -testResults /tmp/s4-focused-editmode.xml
```

Evidence: command exit code `0`; `/tmp/s4-focused-editmode.xml` has `testcasecount="19" result="Passed" total="19" passed="19" failed="0"`, with `SaveSystemTests` `17/17` passed and `SceneRouterSessionTests` `2/2` passed. No `-quit` flag was used. `rtk git diff --check` also passed.
