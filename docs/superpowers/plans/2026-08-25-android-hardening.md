# Android Hardening and Release Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce and verify the Functional MVP debug APK for Android 8.0+ ARM64 with landscape, safe-area handling, lifecycle controls, clean logs, and recorded performance evidence.

**Architecture:** Editor validation blocks malformed builds before `BuildPipeline.BuildPlayer`; a repository script pins the Unity executable and output path. Device checks use `adb`, with physical-device performance required for a non-provisional 30 FPS claim.

**Tech Stack:** Unity 6000.3.22f1 Android Build Support, C#, UnityEditor.BuildPipeline, bash/RTK, adb, NUnit.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Global Constraints

- Requires the complete production campaign checkpoint.
- Android minimum API 26, target API 35, ARM64 only, landscape left/right.
- Application ID is `vn.edu.kma.vuotthekma`; debug APK output is `Builds/Android/the-chat-kma-debug.apk`.
- Zero unhandled exceptions or missing required references in install/launch/full-run logs.

---

### Task 1: Build configuration validator

**Files:**
- Create: `Assets/Editor/MvpBuildValidator.cs`
- Create: `Assets/Tests/EditMode/Build/MvpBuildValidatorTests.cs`
- Modify: `ProjectSettings/ProjectSettings.asset`
- Modify: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Produces `static IReadOnlyList<string> MvpBuildValidator.Validate()` and `static void ValidateOrThrow()`.

- [ ] **Step 1: Write failing validation tests for scene order, seven catalog entries, input asset, AppRoot, duplicate routers, API values, orientation, and required references**

```csharp
[Test]
public void ProductionConfiguration_HasNoValidationErrors()
{
    Assert.That(MvpBuildValidator.Validate(), Is.Empty);
}
```

- [ ] **Step 2: Run validator tests; verify missing validator fails compilation**

- [ ] **Step 3: Implement explicit validation messages and pin PlayerSettings**

```csharp
public static void ValidateOrThrow()
{
    var errors = Validate();
    if (errors.Count > 0)
        throw new BuildFailedException(string.Join("\n", errors));
}
```

Set company `KMA`, product `Vượt Thể KMA`, identifier `vn.edu.kma.vuotthekma`, min SDK 26, target SDK 35, ARM64, Auto Rotation with portrait directions disabled and both landscape directions enabled. Enable every production scene in the exact route order.

- [ ] **Step 4: Run validator and all EditMode tests**

- [ ] **Step 5: Commit validation/settings**

```bash
rtk git add Assets/Editor/MvpBuildValidator.cs Assets/Tests/EditMode/Build ProjectSettings
rtk git commit -m "build: validate Android MVP configuration"
```

### Task 2: Reproducible debug APK build

**Files:**
- Create: `Assets/Editor/MvpAndroidBuild.cs`
- Create: `scripts/build-android-debug.sh`
- Modify: `.gitignore`
- Modify: `README.md`

**Interfaces:**
- Produces `MvpAndroidBuild.BuildDebug()` for `-executeMethod`.
- Produces `scripts/build-android-debug.sh [unity-path]` with default pinned local path and overridable `KMA_UNITY_EDITOR`.

- [ ] **Step 1: Add a failing EditMode test asserting the build method resolves enabled production scenes and exact APK path**

```csharp
[Test]
public void DebugBuild_UsesExactOutputAndAllEnabledScenes()
{
    Assert.That(MvpAndroidBuild.OutputPath, Is.EqualTo("Builds/Android/the-chat-kma-debug.apk"));
    Assert.That(MvpAndroidBuild.EnabledScenes(), Is.EqualTo(EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path)));
}
```
- [ ] **Step 2: Run it; verify missing build type fails**
- [ ] **Step 3: Implement validation plus BuildPipeline invocation and shell wrapper**

```csharp
public static void BuildDebug()
{
    MvpBuildValidator.ValidateOrThrow();
    Directory.CreateDirectory("Builds/Android");
    var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
        "Builds/Android/the-chat-kma-debug.apk", BuildTarget.Android, BuildOptions.Development);
    if (report.summary.result != BuildResult.Succeeded)
        throw new BuildFailedException(report.summary.ToString());
}
```

Shell command: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -buildTarget Android -executeMethod KMA.Editor.MvpAndroidBuild.BuildDebug -logFile /tmp/kma-android-build.log -quit`.

- [ ] **Step 4: Run the actual build and verify exit `0`, APK exists, and log contains `Build completed with a result of 'Succeeded'`**

- [ ] **Step 5: Commit build tooling, not the APK**

```bash
rtk git add Assets/Editor/MvpAndroidBuild.cs scripts/build-android-debug.sh .gitignore README.md
rtk git commit -m "build: add reproducible Android debug APK"
```

### Task 3: Safe area, pause, background, and Android Back

**Files:**
- Modify: `Assets/_Project/Scripts/UI/SafeAreaFitter.cs`
- Create: `Assets/_Project/Scripts/Core/AppLifecycleController.cs`
- Modify: `Assets/_Project/Scripts/UI/MinigameHud.cs`
- Test: `Assets/Tests/PlayMode/UI/AndroidLifecycleTests.cs`

**Interfaces:**
- `AppLifecycleController.SetPaused(bool)`, `OnApplicationPause(bool)`, and `HandleBack()`.

- [ ] **Step 1: Write failing tests for simulated landscape safe areas, pause/resume, background save, Back-to-pause, and Back-to-Menu**

```csharp
[UnityTest]
public IEnumerator BackInGameplay_TogglesPauseWithoutChangingRoute()
{
    yield return ProductFlow.StartSubject(SubjectId.Sprint);
    var lifecycle = Object.FindFirstObjectByType<AppLifecycleController>();
    lifecycle.HandleBack();
    Assert.That(lifecycle.IsPaused, Is.True);
    Assert.That(AppRoot.Instance.Router.CurrentRoute, Is.EqualTo(SessionRoute.Subject));
}
```
- [ ] **Step 2: Run tests; verify lifecycle type/behavior fails**
- [ ] **Step 3: Implement normalized safe-area anchors and route-aware Back behavior**

```csharp
public void HandleBack()
{
    if (router.IsGameplayScene) SetPaused(!IsPaused);
    else if (router.CurrentRoute != SessionRoute.Menu) router.GoToMenu();
}
```

On background, save the last settled snapshot and pause simulation/audio. Resume without advancing rule time by the background duration.

- [ ] **Step 4: Run lifecycle, UI, persistence, and full PlayMode suites**
- [ ] **Step 5: Commit lifecycle hardening**

```bash
rtk git add Assets/_Project/Scripts/Core/AppLifecycleController.cs Assets/_Project/Scripts/UI Assets/Tests/PlayMode/UI/AndroidLifecycleTests.cs
rtk git commit -m "feat: harden Android lifecycle handling"
```

### Task 4: Install, launch, gesture, and logcat smoke

**Files:**
- Create: `scripts/smoke-android-debug.sh`
- Create: `docs/qa/android-smoke-report.md`

- [ ] **Step 1: Write the smoke script with strict device/build preconditions**

```bash
rtk proxy adb get-state
rtk proxy adb install -r Builds/Android/the-chat-kma-debug.apk
rtk proxy adb logcat -c
rtk proxy adb shell am start -n vn.edu.kma.vuotthekma/com.unity3d.player.UnityPlayerGameActivity
rtk proxy adb shell pidof vn.edu.kma.vuotthekma
```

- [ ] **Step 2: Run without a device or APK first; verify it exits non-zero with the exact missing precondition**
- [ ] **Step 3: Add scripted taps/swipes for Menu, Map, and each distinct gesture family plus log filters for `AndroidRuntime`, `Unity`, `MissingReferenceException`, and `NullReferenceException`**
- [ ] **Step 4: Run against the connected device/emulator; record serial/model/API, APK hash, every smoke result, and attach log path in the report**
- [ ] **Step 5: Commit script and evidence report**

```bash
rtk git add scripts/smoke-android-debug.sh docs/qa/android-smoke-report.md
rtk git commit -m "test: add Android install and input smoke"
```

### Task 5: Performance and final acceptance gate

**Files:**
- Create: `Assets/_Project/Scripts/Diagnostics/FrameRateSampler.cs`
- Create: `Assets/Tests/EditMode/Diagnostics/FrameRateSamplerTests.cs`
- Create: `Assets/Tests/PlayMode/Diagnostics/FrameRateSamplerPlayModeTests.cs`
- Create: `docs/qa/android-performance-report.md`
- Modify: `README.md`

**Interfaces:**
- Produces `FrameRateSample { AverageFps, P95FrameMs, MinimumFps, SampleSeconds }` over a 60-second unscaled-time window.

- [ ] **Step 1: Write failing EditMode tests for deterministic sample aggregation and a PlayMode test that emits one completed sample**

```csharp
[Test]
public void SixtyThirtyFpsFrames_ReportExpectedAverageAndMinimum()
{
    var sampler = new FrameRateSamplerCore();
    sampler.AddFrameMilliseconds(16.6667f);
    sampler.AddFrameMilliseconds(33.3333f);
    var result = sampler.Complete();
    Assert.That(result.MinimumFps, Is.EqualTo(30f).Within(.1f));
}
```
- [ ] **Step 2: Run tests; verify missing sampler fails**
- [ ] **Step 3: Implement sampler without per-frame allocation and expose a development-build overlay/export**

```csharp
public FrameRateSample Complete()
{
    var ordered = frameMilliseconds.OrderBy(x => x).ToArray();
    return new FrameRateSample(1000f / frameMilliseconds.Average(), ordered[Mathf.FloorToInt(.95f * (ordered.Length - 1))],
        1000f / frameMilliseconds.Max(), elapsed);
}
```

- [ ] **Step 4: Run full Unity suites, rebuild APK, repeat smoke, and record 60-second samples for Sprint, Endurance, one ball sport, and Boss**

Expected: physical Snapdragon 730-equivalent/4 GB/1080p-class device averages at least 30 FPS with no unhandled exception. Emulator-only evidence is marked provisional and cannot satisfy the physical-device clause.

- [ ] **Step 5: Commit final reports and README status**

```bash
rtk git add Assets/_Project/Scripts/Diagnostics Assets/Tests docs/qa/android-performance-report.md README.md
rtk git commit -m "test: verify Android MVP acceptance"
```
