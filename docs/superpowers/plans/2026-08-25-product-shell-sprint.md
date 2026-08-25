# Product Shell, Save, and Sprint Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce the first complete player route: New Game/Continue -> Map -> Sprint -> Result or Punishment -> Map, with atomic persistence and Android touch.

**Architecture:** `AppRoot` creates the persistent session, catalog, save, settings, audio, and router services. The router is initialized instead of creating its own session, while uGUI shell scenes call router APIs and Sprint remains a thin adapter over `SprintRules`.

**Tech Stack:** Unity 6000.3.22f1, C#, uGUI, Input System 1.20.0, JsonUtility, NUnit EditMode/PlayMode.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Android minimum API 26, target API 35, ARM64, landscape only.
- `GameSession` is the sole progression authority; `BossUnlocked` remains derived.
- Save only settled route state; closing during gameplay returns to Map without a penalty.
- Touch and keyboard invoke the same semantic controller methods.
- Keep the current 121 EditMode and 38 PlayMode tests green.

---

### Task 1: Versioned session snapshots

**Files:**
- Create: `Assets/_Project/Scripts/Persistence/SaveEnvelope.cs`
- Create: `Assets/_Project/Scripts/Persistence/KMA.Gameplay.Persistence.asmdef`
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Progression/SubjectRecord.cs`
- Test: `Assets/Tests/EditMode/Persistence/GameSessionSnapshotTests.cs`
- Test: `Assets/Tests/EditMode/Persistence/KMA.Gameplay.Persistence.EditMode.Tests.asmdef`

**Interfaces:**
- Produces: `GameSessionSnapshot GameSession.ExportSnapshot()`.
- Produces: `static GameSession GameSession.Restore(GameSessionSnapshot snapshot)`.
- Produces: `void GameSession.MarkBossCompleted()` and `bool GameSession.CompletedBoss`.
- Produces: `SaveEnvelope.FromSession(GameSession, SettingsSnapshot)` and `ToSessionSnapshot()`.

- [ ] **Step 1: Write failing snapshot round-trip tests**

```csharp
[Test]
public void ExportRestore_PreservesSettledProgression()
{
    var session = new GameSession();
    session.StartSubject(SubjectId.Sprint);
    session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8.2f, Rank.A));
    session.MarkBossCompleted();

    var restored = GameSession.Restore(session.ExportSnapshot());

    Assert.That(restored.Lives, Is.EqualTo(5));
    Assert.That(restored.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(8.2f));
    Assert.That(restored.CompletedBoss, Is.True);
    Assert.That(restored.PendingPunishmentSubject, Is.Null);
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter GameSessionSnapshotTests -testResults /tmp/kma-snapshot-red.xml -logFile /tmp/kma-snapshot-red.log`

Expected: compilation fails because `GameSessionSnapshot`, `ExportSnapshot`, and `Restore` do not exist.

- [ ] **Step 3: Implement immutable domain snapshots and serializable save DTOs**

```csharp
public sealed class GameSessionSnapshot
{
    public GameSessionSnapshot(int lives, IReadOnlyList<SubjectRecordSnapshot> subjects, bool completedBoss)
    { Lives = lives; Subjects = subjects; CompletedBoss = completedBoss; }
    public int Lives { get; }
    public IReadOnlyList<SubjectRecordSnapshot> Subjects { get; }
    public bool CompletedBoss { get; }
}

public readonly struct SubjectRecordSnapshot
{
    public SubjectRecordSnapshot(SubjectId id, bool passed, float bestScore, Rank bestRank, int failedVisits)
    { Id = id; Passed = passed; BestScore = bestScore; BestRank = bestRank; FailedVisits = failedVisits; }
    public SubjectId Id { get; }
    public bool Passed { get; }
    public float BestScore { get; }
    public Rank BestRank { get; }
    public int FailedVisits { get; }
}

[Serializable]
public sealed class SaveEnvelope
{
    public int schemaVersion = 1;
    public string savedAtUtc;
    public int lives;
    public bool completedBoss;
    public List<SubjectSaveRecord> subjects = new List<SubjectSaveRecord>();
    public SettingsSnapshot settings = new SettingsSnapshot();
}

[Serializable]
public sealed class SubjectSaveRecord
{
    public SubjectId id;
    public bool passed;
    public float bestScore;
    public Rank bestRank;
    public int failedVisits;
}

[Serializable]
public sealed class SettingsSnapshot
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
}
```

`GameSession.Restore` must require exactly one valid record for every `SubjectId`, clamp lives to `0..5`, rebuild only settled records, and never restore an active attempt or punishment.

- [ ] **Step 4: Run snapshot tests and the progression suite**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "GameSessionSnapshotTests|GameSessionTests" -testResults /tmp/kma-snapshot-green.xml -logFile /tmp/kma-snapshot-green.log`

Expected: all selected tests pass with zero failures.

- [ ] **Step 5: Commit the snapshot contract**

```bash
rtk git add Assets/_Project/Scripts/Persistence Assets/_Project/Scripts/Progression Assets/Tests/EditMode/Persistence
rtk git commit -m "feat: add versioned session snapshots"
```

### Task 2: Atomic SaveSystem and recovery

**Files:**
- Create: `Assets/_Project/Scripts/Persistence/SaveSystem.cs`
- Test: `Assets/Tests/EditMode/Persistence/SaveSystemTests.cs`

**Interfaces:**
- Consumes: `SaveEnvelope` from Task 1.
- Produces: `SaveSystem(string directory, string fileName = "save.json")`.
- Produces: `void Save(SaveEnvelope value)`, `bool TryLoad(out SaveEnvelope value, out SaveLoadStatus status)`, and `void Delete()`.
- Produces read-only `PrimaryPath`, `BackupPath`, and `TempPath` for diagnostics/tests.
- `SaveLoadStatus` values are `PrimaryLoaded`, `BackupRecovered`, `FreshStart`, and `NoSave`.

- [ ] **Step 1: Write failing primary/backup/corruption tests**

```csharp
[Test]
public void CorruptPrimary_LoadsLastValidBackup()
{
    var saves = new SaveSystem(tempDirectory);
    saves.Save(EnvelopeWithLives(5));
    saves.Save(EnvelopeWithLives(4));
    File.WriteAllText(saves.PrimaryPath, "not-json");

    Assert.That(saves.TryLoad(out var value, out var status), Is.True);
    Assert.That(status, Is.EqualTo(SaveLoadStatus.BackupRecovered));
    Assert.That(value.lives, Is.EqualTo(5));
}
```

- [ ] **Step 2: Run SaveSystem tests and verify RED**

Run the EditMode filter `SaveSystemTests`; expected compilation failure because `SaveSystem` is absent.

- [ ] **Step 3: Implement same-directory temp, backup, and recovery**

```csharp
public void Save(SaveEnvelope value)
{
    Validate(value);
    Directory.CreateDirectory(directory);
    File.WriteAllText(TempPath, JsonUtility.ToJson(value, true));
    using (var stream = new FileStream(TempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        stream.Flush(true);
    if (File.Exists(PrimaryPath)) File.Replace(TempPath, PrimaryPath, BackupPath);
    else File.Move(TempPath, PrimaryPath);
}
```

`TryLoad` validates schema version `1`, seven unique subject IDs, finite scores, rank consistency, and `0..5` lives. Preserve unrecoverable primary content as `save.json.corrupt` before returning `FreshStart`.

- [ ] **Step 4: Run all persistence tests**

Run the EditMode filter `KMA.Tests.Persistence`; expected zero failures and no remaining `.tmp` file.

- [ ] **Step 5: Commit SaveSystem**

```bash
rtk git add Assets/_Project/Scripts/Persistence/SaveSystem.cs Assets/Tests/EditMode/Persistence/SaveSystemTests.cs
rtk git commit -m "feat: persist progression atomically"
```

### Task 3: SubjectCatalog and persistent AppRoot

**Files:**
- Create: `Assets/_Project/Scripts/Core/AppRoot.cs`
- Create: `Assets/_Project/Scripts/Core/SubjectCatalog.cs`
- Create: `Assets/_Project/Scripts/Core/SettingsService.cs`
- Create: `Assets/_Project/Scripts/Core/AudioService.cs`
- Create: `Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Test: `Assets/Tests/EditMode/Progression/SubjectCatalogTests.cs`
- Test: `Assets/Tests/PlayMode/Progression/AppRootTests.cs`

**Interfaces:**
- Produces: `SubjectDefinition SubjectCatalog.Get(SubjectId id)` and `void Validate()`.
- Produces: `static AppRoot AppRoot.Instance`, `GameSession Session`, `SaveSystem Saves`, and `SceneRouter Router`.
- Changes: `SceneRouter.Initialize(GameSession session, SubjectCatalog catalog, SaveSystem saves)` replaces session construction in `Awake`.

- [ ] **Step 1: Write failing catalog and singleton tests**

```csharp
[Test]
public void Catalog_ContainsSevenUniqueLoadableSubjects()
{
    var catalog = AssetDatabase.LoadAssetAtPath<SubjectCatalog>(
        "Assets/_Project/ScriptableObjects/Subjects/SubjectCatalog.asset");
    Assert.DoesNotThrow(catalog.Validate);
    foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
        Assert.That(catalog.Get(id).SceneName, Is.Not.Empty);
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run filters `SubjectCatalogTests|AppRootTests`; expected failure because the catalog and root do not exist.

- [ ] **Step 3: Implement catalog validation and root initialization**

```csharp
[Serializable]
public sealed class SubjectDefinition
{
    public SubjectId Id;
    public string DisplayName;
    public string SceneName;
    public Color FallbackColor;
    public string Tutorial;
}

public void Initialize(GameSession loadedSession, SubjectCatalog subjectCatalog, SaveSystem saveSystem)
{
    session = loadedSession ?? throw new ArgumentNullException(nameof(loadedSession));
    catalog = subjectCatalog ?? throw new ArgumentNullException(nameof(subjectCatalog));
    saves = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
    catalog.Validate();
}
```

Generate the asset with all seven exact scene names: `MG_Sprint`, `MG_Endurance`, `MG_Volleyball`, `MG_Basketball`, `MG_PingPong`, `MG_Badminton`, and `MG_Football`.

- [ ] **Step 4: Run catalog/root tests and all progression tests**

Expected: singleton survives a scene load, duplicate root destroys itself, and progression tests remain green.

- [ ] **Step 5: Commit the persistent service boundary**

```bash
rtk git add Assets/_Project/Scripts/Core Assets/_Project/ScriptableObjects/Subjects Assets/Tests
rtk git commit -m "feat: add persistent gameplay services"
```

### Task 4: Production scene routes and settled saves

**Files:**
- Modify: `Assets/_Project/Scripts/Progression/GameSession.cs`
- Modify: `Assets/_Project/Scripts/Core/SceneRouter.cs`
- Create: `Assets/_Project/Scripts/Core/RouteOutcome.cs`
- Test: `Assets/Tests/PlayMode/Progression/ProductionRouteTests.cs`

**Interfaces:**
- Produces routes `Menu`, `Result`, `Victory`, and `Error` in addition to current routes.
- Produces: `RouteOutcome SceneRouter.LastOutcome` with `Subject`, `Result`, `NextRoute`, and `LifeDelta`.
- Produces: `void SceneRouter.NewGame()`, `bool SceneRouter.ContinueGame()`, and `void SceneRouter.GoToMap()`.

- [ ] **Step 1: Write failing route/save-boundary tests**

```csharp
[UnityTest]
public IEnumerator PassingSprint_SavesThenLoadsResultScene()
{
    var root = TestAppRoot.Create();
    root.Router.StartSubject(SubjectId.Sprint);
    yield return WaitForScene("MG_Sprint");
    root.Router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(true, 7f, Rank.B));
    yield return WaitForScene("Result");
    Assert.That(root.Saves.TryLoad(out var save, out _), Is.True);
    Assert.That(save.subjects.Single(x => x.id == SubjectId.Sprint).passed, Is.True);
}
```

- [ ] **Step 2: Run `ProductionRouteTests` and verify RED**

Expected: `Result` route and `RouteOutcome` are undefined.

- [ ] **Step 3: Implement route outcomes and save-before-load ordering**

```csharp
public bool SubmitSubjectResult(SubjectId subject, MinigameResult result)
{
    var next = session.SubmitResult(subject, result);
    LastOutcome = RouteOutcome.ForSubject(subject, result, next);
    SaveSettledSession();
    return Route(next == SessionRoute.Punishment ? SessionRoute.Punishment : SessionRoute.Result, subject);
}
```

Result decides its next button from `LastOutcome.NextRoute`; it must never resubmit the result. Punishment completion saves the settled retry state before loading the subject again.

- [ ] **Step 4: Run production route and existing full-flow tests**

Expected: production tests use real scenes; isolated domain-flow tests still pass after updating expected `Result` transitions.

- [ ] **Step 5: Commit routing**

```bash
rtk git add Assets/_Project/Scripts/Core Assets/_Project/Scripts/Progression Assets/Tests/PlayMode/Progression
rtk git commit -m "feat: route settled gameplay outcomes"
```

### Task 5: Bootstrap, Menu, Map, Result, and shared UI

**Files:**
- Create: `Assets/_Project/Scenes/Bootstrap.unity`
- Create: `Assets/_Project/Scenes/Menu.unity`
- Modify: `Assets/_Project/Scenes/Map.unity`
- Create: `Assets/_Project/Scenes/Result.unity`
- Create: `Assets/_Project/Scenes/Error.unity`
- Create: `Assets/_Project/Scripts/UI/MenuScreen.cs`
- Create: `Assets/_Project/Scripts/UI/MapScreen.cs`
- Create: `Assets/_Project/Scripts/UI/ResultScreen.cs`
- Create: `Assets/_Project/Scripts/UI/ErrorScreen.cs`
- Create: `Assets/_Project/Scripts/UI/HeartBar.cs`
- Create: `Assets/_Project/Scripts/UI/SafeAreaFitter.cs`
- Create: `Assets/Editor/MvpShellSceneFactory.cs`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/PlayMode/UI/ProductShellTests.cs`

**Interfaces:**
- `MenuScreen.NewGame()`, `ContinueGame()`, and `ConfirmReset(bool confirmed)` call router APIs.
- `MapScreen.SelectSubject(SubjectId id)` and `SelectBoss()` call router APIs.
- `ResultScreen.Continue()` follows `SceneRouter.LastOutcome.NextRoute`.

- [ ] **Step 1: Write failing shell navigation tests**

```csharp
[UnityTest]
public IEnumerator NewGame_ShowsSevenSubjectButtonsAndLockedBoss()
{
    yield return SceneManager.LoadSceneAsync("Menu");
    Object.FindFirstObjectByType<MenuScreen>().NewGame();
    yield return WaitForScene("Map");
    var map = Object.FindFirstObjectByType<MapScreen>();
    Assert.That(map.SubjectButtonCount, Is.EqualTo(7));
    Assert.That(map.BossInteractable, Is.False);
}
```

- [ ] **Step 2: Run `ProductShellTests` and verify RED**

Expected: scenes and screen components are absent.

- [ ] **Step 3: Implement screens and generate exact scene hierarchies**

Each scene factory output must contain `Main Camera`, `EventSystem`, `Canvas` with `CanvasScaler` at `1920x1080`, `SafeArea`, and exactly one screen component. Map creates seven buttons from `SubjectCatalog`; no hard-coded two-subject list is allowed.

```csharp
public void SelectSubject(SubjectId id)
{
    if (router.IsTransitioning) return;
    router.StartSubject(id);
}
```

Set build order to Bootstrap, Menu, Map, Result, Error, Punishment, seven subject scenes, Boss, GameOver, Victory; references to not-yet-created later scenes may be disabled until their checkpoint creates them.

- [ ] **Step 4: Run shell tests and visually inspect rendered test screenshots**

Run `ProductShellTests`; render Bootstrap/Menu/Map/Result at 1920x1080 and 2400x1080. Expected: all required controls are inside the safe area and readable.

- [ ] **Step 5: Commit the product shell**

```bash
rtk git add Assets/_Project/Scenes Assets/_Project/Scripts/UI Assets/Editor/MvpShellSceneFactory.cs ProjectSettings/EditorBuildSettings.asset Assets/Tests/PlayMode/UI
rtk git commit -m "feat: add playable product shell"
```

### Task 6: Sprint touch and MVP presentation

**Files:**
- Create: `Assets/_Project/Scripts/Input/KmaInputActions.inputactions`
- Create: `Assets/_Project/Scripts/Input/SemanticTapButton.cs`
- Create: `Assets/_Project/Scripts/Gameplay/Sprint/SprintPresentationAdapter.cs`
- Modify: `Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs`
- Modify: `Assets/_Project/Scenes/MG_Sprint.unity`
- Remove from production: `Assets/_Project/Scripts/Core/GameplayPresentation.cs`
- Test: `Assets/Tests/PlayMode/Gameplay/Running/SprintProductionSceneTests.cs`

**Interfaces:**
- `SemanticTapButton.Pressed` emits once per pointer-down.
- `SprintPresentationAdapter.Bind(SprintController controller)` updates distance, stamina, wind cue, rank, and runner transforms.
- Existing `SprintController.OnLeftTap()` and `OnRightTap()` remain the semantic input boundary.

- [ ] **Step 1: Write failing touch/route/presentation tests**

```csharp
[UnityTest]
public IEnumerator TouchButtons_DriveSprintAndCompletionRoutesOnce()
{
    var router = TestAppRoot.Create().Router;
    router.StartSubject(SubjectId.Sprint);
    yield return WaitForScene("MG_Sprint");
    var controller = Object.FindFirstObjectByType<SprintController>();
    var buttons = Object.FindObjectsByType<SemanticTapButton>(FindObjectsSortMode.None);
    Assert.That(buttons, Has.Length.EqualTo(2));
    Assert.That(controller.InputActionsReady, Is.True);
    Assert.That(Object.FindFirstObjectByType<SprintPresentationAdapter>(), Is.Not.Null);
}
```

- [ ] **Step 2: Run `SprintProductionSceneTests` and verify RED**

Expected: touch components and adapter are missing.

- [ ] **Step 3: Add shared Input Actions and wire Sprint scene**

`KmaInputActions` must bind SprintLeft to keyboard left arrow plus the left UI button callback, and SprintRight to keyboard right arrow plus the right callback. The scene must contain visible player/rival shapes, progress track, stamina bar, rank label, wind cue, tutorial/countdown, pause, and two bottom-corner touch zones.

```csharp
public void Bind(SprintController value)
{
    controller = value ?? throw new ArgumentNullException(nameof(value));
    Apply(controller.Snapshot);
}
```

Delete the runtime bootstrap attribute from `GameplayPresentation`; no production scene may create the opaque IMGUI overlay.

- [ ] **Step 4: Run Sprint production, existing Running, and route tests**

Expected: keyboard tests remain green, touch buttons are present, and a completed Sprint reaches Result exactly once.

- [ ] **Step 5: Commit Sprint MVP presentation**

```bash
rtk git add Assets/_Project/Scripts/Input Assets/_Project/Scripts/Gameplay/Sprint Assets/_Project/Scripts/Core/GameplayPresentation.cs Assets/_Project/Scenes/MG_Sprint.unity Assets/Tests
rtk git commit -m "feat: complete Sprint MVP route"
```

### Task 7: Product-shell full gate and documentation

**Files:**
- Modify: `Assets/Tests/PlayMode/Progression/FullGameplayFlowTests.cs`
- Create: `Assets/Tests/PlayMode/Progression/SprintSavedFlowTests.cs`
- Create: `Assets/Tests/PlayMode/Helpers/TestAppRoot.cs`
- Create: `Assets/Tests/PlayMode/Helpers/ProductFlow.cs`
- Create: `Assets/Tests/PlayMode/Helpers/PointerFixture.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes all prior tasks; produces the verified checkpoint for plan 2.

- [ ] **Step 1: Add a failing close/reload flow test**

```csharp
[UnityTest]
public IEnumerator PassedSprint_ReloadedAppRoot_ContinuesFromMap()
{
    yield return ProductFlow.PassSprintWithScore(7.5f);
    Object.DestroyImmediate(AppRoot.Instance.gameObject);
    var restored = TestAppRoot.CreateFromDisk();
    Assert.That(restored.Session.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(7.5f));
    Assert.That(restored.Session.PendingPunishmentSubject, Is.Null);
}
```

- [ ] **Step 2: Run the saved-flow test and verify RED before its fixture helpers exist**

Expected: test compilation fails on `ProductFlow` or the disk-backed test root.

- [ ] **Step 3: Implement only the test fixtures and any missing route-boundary fix**

```csharp
public static IEnumerator PassSprintWithScore(float score)
{
    yield return StartSubject(SubjectId.Sprint);
    var sprint = Object.FindFirstObjectByType<SprintController>();
    sprint.CompleteWithNormalizedScoreForTest(score);
    yield return WaitForScene("Result");
}
```

`TestAppRoot.CreateFromDisk()` uses a per-test temporary directory and the production `SaveSystem`. `PointerFixture` sends Input System pointer events to uGUI surfaces. Do not inject results into `GameSession`; drive real controller completion. Update README to state exactly one production subject route is complete at this checkpoint and document New Game/Continue behavior.

- [ ] **Step 4: Run the full Unity suites**

Run EditMode and PlayMode without `-quit`, writing `/tmp/kma-shell-edit.xml` and `/tmp/kma-shell-play.xml`. Expected: zero failed, skipped, or inconclusive tests; inspect both XML root totals.

- [ ] **Step 5: Commit checkpoint evidence**

```bash
rtk git add Assets/Tests README.md
rtk git commit -m "test: verify saved Sprint product flow"
```
