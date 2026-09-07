# S1–S9 Stabilization Gate

Evidence for [`docs/superpowers/plans/2026-09-02-s1-s9-stabilization.md`](../superpowers/plans/2026-09-02-s1-s9-stabilization.md), Task 9.

S1–S9 is a **checkpoint, not a release**. S10–S16 are outside this plan.

- Commit under test: the tip of this branch, whose docs are added by `test: verify S1-S9 stabilization gate`
- Editor: Unity `6000.3.23f1`, Windows 11 Pro 10.0.26200, batch mode, `-nographics`
- Working tree at gate time: no tracked modifications other than the font churn recorded below

## 1. Clean verification, run twice

Both suites were run twice from a clean tracked status.

| Run | Suite | Result | Evidence |
| --- | --- | --- | --- |
| 1 | EditMode | 253 total, 253 passed, 0 failed | `t9-gate-em-1.xml` / `.log` |
| 1 | PlayMode | 193 total, 193 passed, 0 failed | `t9-gate-pm-1.xml` / `.log` |
| 2 | EditMode | 253 total, 253 passed, 0 failed | `t9-gate-em-2.xml` / `.log` |
| 2 | PlayMode | 193 total, 193 passed, 0 failed | `t9-gate-pm-2.xml` / `.log` |

Evidence paths are relative to `.superpowers/sdd/2026-09-02-s1-s9-stabilization/`, which is untracked working evidence rather than committed history.

Commands (Windows form of the plan's verification commands; the plan's `rtk` wrapper does not exist in this environment):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode `
  -testResults '<evidence>\t9-gate-em-1.xml' -logFile '<evidence>\t9-gate-em-1.log'

& 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode `
  -testResults '<evidence>\t9-gate-pm-1.xml' -logFile '<evidence>\t9-gate-pm-1.log'
```

Static checks:

- `git diff --check` — clean, exit 0, no output.
- `git status --short` after each run — see `t9-gate-status-1.txt` and `t9-gate-status-2.txt`.
- Duplicate asmdefs — none. 30 `.asmdef` files, all names unique.
- No scene, prefab or `ProjectSettings` churn from test runs.

### Recorded churn

Unity test runs regenerate `Assets/_Project/Fonts/Nunito-Bold.asset` — the dynamic TMP atlas caching the glyphs the new Volleyball HUD labels render. It is reverted rather than committed, per the plan's constraint on incidental font churn. The plan names `Baloo2-ExtraBold.asset` as the churning asset; that asset no longer churns, and `Nunito-Bold.asset` does instead.

### Trailing whitespace in scene YAML

`git diff --check` passes on the working tree. It does **not** pass on a diff range that contains a Unity-written scene: Unity's serialiser emits a trailing space after empty YAML keys (`m_Name: `, `m_text: `, `value: `). This is pre-existing and repo-wide — at `8f7b6f1`, `MG_Volleyball.unity` already had 19 such lines, `MG_Sprint.unity` 55 and `Menu.unity` 34. Hand-stripping would diverge from every other scene and Unity would reintroduce it on the next save, so the artifact is recorded rather than "fixed".

## 2. End-to-end route verification

**Not performed as an interactive Editor session.** This environment is headless (`-batchmode -nographics`); no human drove Bootstrap → Menu → Map → subject → Result in the Editor, and no screenshots were taken. Each documented leg is instead covered by automated PlayMode tests in the green runs above. Those checks are listed as automated coverage, not as an interactive pass.

| Documented leg | Automated coverage |
| --- | --- |
| Bootstrap → Menu with restored save | `GameManagerStartupTests.PreparedSave_IsRestoredBeforeMenuAndSettingsAreApplied`, `.PreparedSave_RebuildsRouterTransitionerWithRestoredSession` |
| Continue / New Game gating | `S5NewGameTests.Continue_WithoutAnExistingSave_IsDisabledAndRequestsNoRoute`, `.Continue_WithAnExistingUncompletedDefaultSave_IsEnabled`, `.Continue_WithACompletedSave_IsDisabled`, `.Continue_WithRestoredProgress_IsEnabled`, `.NewGame_RequiresExplicitConfirmation` |
| Map → subject → Result → Continue | `FullGameplayFlowTests.FullFlow_UsesAttemptsLivesNormalizedResultsAndBossUnlock`, `CoreLoopTests.ResultPanel_ContinueEmitsActionOnlyOnce` |
| Map → Volleyball → Result Continue → Map, record persisted | `VolleyballCampaignTests.VolleyballPass_PreviewsMapThenContinuesAndPersistsTheRecord` |
| First failure → Punishment → retry, attempt kept | `VolleyballCampaignTests.VolleyballFirstFailure_RoutesToPunishmentAndKeepsTheAttemptActive`, `PunishmentRouteTests.KeyboardInput_CompletesLivePunishmentAndRoutesSprintRetry` |
| Kill / relaunch at active attempt 1 | `S5NewGameTests.Continue_DuringAttemptOne_RequestsTheSubjectOnly` |
| Kill / relaunch at Punishment | `S5NewGameTests.Continue_AwaitingPunishment_RequestsPunishmentOnly`, `FullGameplayFlowTests.Continue_AfterFirstFailure_ResumesPunishmentForTheSameSubjectAcrossRelaunch` |
| Kill / relaunch at attempt 2 | `S5NewGameTests.Continue_DuringAttemptTwo_RequestsTheRetryOnly`, `FullGameplayFlowTests.CompletingProductionPunishment_PersistsTheRetryAttemptAcrossRelaunch` |
| Save durability across relaunch | `S4BootstrapPersistenceGateTests.Bootstrap_PersistsLivesAndSprintBestRankAcrossRelaunch`, `.Bootstrap_ResetPreservesPreferencesAndClearsCampaignAcrossRelaunch`, `.Bootstrap_TutorialCompletionPersistsInJsonWithoutPlayerPrefs` |

The Volleyball campaign gate asserts persistence against `GameSession.ToSaveData()`, the in-memory projection. Disk round-trips through `SaveSystem` are covered by the `S4BootstrapPersistenceGateTests` rows above rather than by the Volleyball gate itself.

## 3. Android build

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'D:\project\the-chat-KMA' `
  -executeMethod KMA.EditorTools.BuildScript.BuildAndroid `
  -buildOutput 'Builds/Android/kma-s1-s9.apk' -androidArchitecture arm64 `
  -logFile '<evidence>\t9-gate-android.log' -quit
```

Result: `[KMA] Build Succeeded, 832106774 bytes, 0 errors, 22 warnings` (`t9-gate-android.log`). The byte count in that line is the uncompressed player payload, not the APK.

| Property | Observed |
| --- | --- |
| APK | `Builds/Android/kma-s1-s9.apk` |
| Size | 42,766,621 bytes (40.8 MiB) |
| SHA-256 | `5714454acd2def09aba2dca4be54f273c2150567c1f299ce5e13cfde6f4381a3` |
| Package | `com.kma.thechat` |
| Label | `Thể Chất KMA` (Vietnamese glyphs present in the manifest) |
| `minSdkVersion` | 25 |
| `targetSdkVersion` | 35 |
| Native code | `arm64-v8a` only — `lib/arm64-v8a/` contains `libil2cpp.so`, `libunity.so`, `libgame.so`, `libmain.so`, `libc++_shared.so`, `libswappywrapper.so`, `lib_burst_generated.so`; no other ABI directory exists |
| Orientation | `screenOrientation=11` (`sensorLandscape`) |
| Launcher | `com.unity3d.player.UnityPlayerGameActivity` |
| Scripting backend | IL2CPP (`PlayerSettings.GetScriptingBackend(Android)`, asserted by `ProjectSettingsTests.AndroidRuntimeUsesIl2CppAndArm64Only`) |

Inspected with `aapt2 dump badging` and `aapt2 dump xmltree --file AndroidManifest.xml` from build-tools 36.0.0.

### minSdkVersion discrepancy

The plan says "minimum API 23" in both its global constraints and its own Task 1 step. The accepted Task 1 implementation says 25, and so do `ProjectSettings.asset`, the passing `ProjectSettingsTests.AndroidIdentityAndSdkLevelsMatchContract`, the README and the earlier demo QA. The APK is minSdk 25. Task 9 modifies source only when test-discovered, so this is recorded as a plan-versus-implementation discrepancy rather than changed here.

## 4. Physical-device gate — UNAVAILABLE

No physical Android device was connected at any point during this gate. The only attached target was an emulator:

```
adb devices
List of devices attached
192.168.56.101:5555   device

getprop ro.product.manufacturer  -> Genymobile
getprop ro.product.model         -> Phone
getprop ro.product.cpu.abi       -> x86_64
getprop ro.build.version.release -> 15
getprop ro.build.version.sdk     -> 35
```

The shipping APK is ARM64-only, so it cannot run on that x86_64 target. The install was attempted and did not succeed:

```
adb -s 192.168.56.101:5555 install -r D:\project\the-chat-KMA\Builds\Android\kma-s1-s9.apk
Performing Streamed Install
adb.exe: failed to install ...: cmd: Can't find service: package
```

Every item below is therefore **unavailable — not verified, not passed**:

| Device check | Status |
| --- | --- |
| Touch ownership on device | Unavailable |
| Vietnamese glyph rendering on device | Unavailable |
| Safe-area handling on a notched display | Unavailable |
| Audio and haptics settings | Unavailable |
| Pause / resume / restart / exit | Unavailable |
| Volleyball three-touch scoring on device | Unavailable |
| Counter cue readability on device | Unavailable |
| Save and Continue after process kill on device | Unavailable |
| Sustained FPS | Unavailable |
| Draw-call count | Unavailable |

**The plan's device gate is incomplete.** It cannot be closed without a physical ARM64 device (or an ARM64-capable emulator image).

## 5. Known gaps in the Volleyball slice

Found by code review during Task 8, verified, and deliberately not closed here. Each is a gameplay-design or shared-ownership item beyond this stabilization plan.

- **The authored net and court colliders are inert.** The ball has no `Collider2D`, so flight resolves analytically against the `FlightProfile` ground plane. A spike scores on crossing `netX` regardless of the net tape height, and a ball landing outside the 10-unit court still scores. Net-fault and out-of-bounds rules were implemented and reverted: with the authored net tape at 2.5 no spike can clear the net, and with a 5-unit half-court the authored spike lands out — the pair made the minigame unwinnable. Closing this requires re-tuning the Dig/Set/Spike trajectories and the serve together.
- **`FlightProfile_Volleyball.groundY` is 0 while the drawn floor sits at y = -1.2**, so flights resolve 1.2 units above the visible floor. `bounceDamping: 0.75` in that profile is unreachable while the ball has no collider.
- **Timing is only modelled at the apex.** `CalculateTimingAccuracy` derives accuracy from swipe *duration*, not from when the ball was struck, so a fast flick is always accurate. The apex window is the only real timing constraint. The plan's "timing input" wording is not otherwise represented.
- **Counterplay is mechanical but not animated.** A flagged return now launches a distinct spin trajectory after its cue, and the trajectory is fixed at launch, so the S9 cue-then-differ contract holds. The spec's hand animation and coloured trail are not implemented — the cue is a text label.
- **The court, net and actors are Unity built-in primitive sprites, tinted.** Enough for a visible checkpoint scene; the Definition of Done bars placeholder art from the final build, so authored art is still owed.
- **The shared `PhaseOverlay` backdrop dims gameplay.** It is a 35 %-black full-screen image parented outside the four phase roots, so it stays visible during Play in every minigame scene. Its `raycastTarget` was turned off here so it can no longer swallow gestures, but phase-gating its visibility is shared-presentation work left for its owner.
- **Volleyball has no keyboard fallback.** The scene router assigns no `InputActionAsset`, so the subject is gesture-only; a desktop tester needs a mouse drag.
- **Shared-code items left to their owners:** `EnduranceInputBridge` still uses the destructive `SetDetectors(null, …)` install that `GameplayInputRouter.SetSwipeDetector` now replaces for Volleyball; every `HUD_Minigame` graphic is a raycast target on a canvas above the gameplay surface, which shrinks the swipeable area in Sprint and Endurance as well; `minimumSwipeLengthPixels` is an absolute device-pixel count with no DPI scaling.
- **The deadline drops the final partial frame.** `ResolveDeadline` returns before the landing check, so `Rules.Elapsed` stops just short of `timeLimit` and the HUD timer never reads exactly 0. The verdict is unaffected: `Pass` requires a qualifying score, and any qualifying score already finishes the attempt on the frame it is scored.
- **Canvas roots serialise `m_LocalScale: {0,0,0}`** with collapsed anchors. Unity drives a root canvas rect and re-serialises these values however they are assigned, so `VolleyballSceneTests` asserts the runtime rect a touch is hit-tested against instead of the serialised values.

## 6. Spec deltas

Differences between the spec and what shipped, recorded rather than silently resolved.

- **HUD touch label.** The spec's S9 section asks for `TOUCH 1/2/3`; the plan's Task 8 interface asks for `TOUCH n/3`. The build renders `TOUCH n/3`, following the plan.
- **`minSdkVersion`.** The plan's constraints say API 23; the accepted Task 1 contract, its passing test, `ProjectSettings.asset` and the shipped APK all say 25. See section 3.
- **Auto-positioning.** The spec says the player and teammate auto-position via `PredictLandingPoint()`. They do, but only during a live flight: while the ball is attached its predicted landing *is* its anchor's position, so the previous unconditional assist walked both the actor and the ball off the court about a unit per frame.
- **Definition of Done.** The spec's Definition of Done covers work this plan does not schedule — audio and mixer groups, haptics, multi-aspect and notch layout verification, credits screen, app icon and splash, balance targets, and playtesting with at least eight people. Those belong to S10–S16 and are neither claimed nor verified here. Of the quality items, only "all tests pass" is met (253 EditMode, 193 PlayMode); the on-device FPS and draw-call items are unavailable per section 4.

## 7. Gate verdict

| Plan requirement | Status |
| --- | --- |
| Full EditMode green | Pass — 253/253, twice |
| Full PlayMode green | Pass — 193/193, twice |
| Worktree clean after repeated tests | Pass, with the recorded `Nunito-Bold.asset` atlas churn |
| `git diff --check` | Pass on the working tree; Unity scene YAML trailing whitespace recorded |
| No duplicate asmdefs | Pass |
| Android ARM64 build and inspection | Pass |
| Interactive Editor smoke | Not performed — headless environment; automated coverage listed instead |
| Physical-device and performance gate | Unavailable — no ARM64 device connected |

S1–S9 is green and reproducible on this machine as an automated checkpoint. The plan is **not fully complete**: its device gate remains open, and the Volleyball gaps in section 5 are recorded rather than closed.
