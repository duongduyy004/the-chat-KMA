# S10 Basketball — Device Gate

Evidence for `.superpowers/sdd/2026-09-07-s10-basketball-vertical-slice`, Task 7.

**S10 makes the Basketball minigame playable. It proves Basketball only.** S1–S9 (see
[`docs/qa/s1-s9-stabilization-gate.md`](s1-s9-stabilization-gate.md)) is a separate, earlier
checkpoint. S11–S16 (PingPong, Badminton, Football, Boss/Punishment polish, ending/credits,
art/audio/release) are untouched by this gate and are not claimed here. This document does not
claim the game-wide Definition of Done.

- Commit under test: `36dc85d` ("fix: correct three citation errors in the S10 gate document"),
  branch `master`. (Originally captured against `4558ad9`; head advanced by three more
  documentation-only commits before this gate's own commit landed. The delta between `4558ad9` and
  `36dc85d` is documentation-only — no source, scene or test file differs — so every measurement,
  count and verdict in this document is unaffected and was not re-run for this correction.)
- Editor: Unity `6000.3.23f1`, Windows 11 Pro 10.0.26200, batch mode, `-nographics`
- A second session was concurrently active in this same working copy during this gate (see
  "Concurrent session" below); its files were left untouched throughout

## 1. Clean verification, run twice

Both full suites were run twice from a clean tracked status.

| Run | Suite | Result | Evidence |
| --- | --- | --- | --- |
| 1 | EditMode | 258 total, 258 passed, 0 failed | `t7-gate-edit-1.xml` / `.log` |
| 1 | PlayMode | 241 total, 241 passed, 0 failed | `t7-gate-play-1.xml` / `.log` |
| 2 | EditMode | 258 total, 258 passed, 0 failed | `t7-gate-edit-2.xml` / `.log` |
| 2 | PlayMode | 241 total, 241 passed, 0 failed | `t7-gate-play-2.xml` / `.log` |

Counts read from each XML's root `<test-run>` attributes (`total`/`passed`/`failed`), not counted
by eye. Evidence paths are relative to
`.superpowers/sdd/2026-09-07-s10-basketball-vertical-slice/`, git-ignored working evidence.

These are the counts actually observed in this environment on this commit, not the plan's
originally-written figures, which are stale (the branch was rebased onto an advanced
`origin/master` and a concurrent session has been adding tests throughout this gate).

Commands:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$evidence = 'D:\project\the-chat-KMA\.superpowers\sdd\2026-09-07-s10-basketball-vertical-slice'

& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform EditMode `
  -testResults "$evidence\t7-gate-edit-1.xml" -logFile "$evidence\t7-gate-edit-1.log"
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' -runTests -testPlatform PlayMode `
  -testResults "$evidence\t7-gate-play-1.xml" -logFile "$evidence\t7-gate-play-1.log"
```

Static checks:

- `git status --short`, captured before run 1 (`t7-status-1.txt`), between run 1 and run 2
  (`t7-status-2.txt`), and after run 2 (`t7-status-3.txt`) — all three are byte-identical. The
  test runs did not dirty the tracked tree, and the concurrent session did not change any file
  during this gate's window either.
- The working tree carried the concurrent session's own uncommitted changes throughout
  (`Assets/Tests/EditMode/Config/ProjectLayoutTests.cs`, `Assets/_Project/Fonts/Nunito-Bold.asset`,
  `README.md` modified; `.claude/`, `Assets/Editor/AndroidBuildMatrix.cs(.meta)`, `tools/`
  untracked). These were left untouched by this gate and are included in the counts above because
  Unity tests the working tree, not a clean commit — the identical status snapshots confirm they
  did not change between or because of these runs.

### Concurrent session

A second Claude session was working in this same directory throughout this gate. Its footprint
(`ProjectLayoutTests.cs`, `Nunito-Bold.asset`, `README.md`, `Assets/Editor/AndroidBuildMatrix.cs`,
`tools/`) was left alone entirely — not read for correctness, not staged, not reverted. Its
`Builds/Android/` output (`kma-x86_64.apk`, `build-apk.log`, dated 09:45 the same morning) predates
this gate's own build and was not used, inspected, or depended on; this gate produced its own APK
under a different filename (`kma-s10.apk`, section 3).

## 2. Scene contract — `MG_Basketball`

Authored reproducibly by `Assets/Editor/BasketballSceneConfigurator.cs` (`KMA/S10/Author
Basketball Scene`), which destroys and rebuilds every object it owns by name on each run. Objects
and their owning components:

| Object | Component(s) | Owns |
| --- | --- | --- |
| `BasketballCourt`, `BasketballHoop`, `BasketballBackboard`, `BasketballPlayer`, `BasketballFinisher`, `BasketballDefender` | `SpriteRenderer` (tinted built-in `UISprite`) | Presentation only — no colliders |
| `BasketballPlayerHand` | `Transform` only, at `(-3.5, 1.2, 0)` | The launch origin `BasketballController.LaunchOrigin` reads |
| `BallPresentation` (from `BallPresentation.prefab`) | `Rigidbody2D` (Kinematic while held), `BallRig`, `TrajectoryPreview`, `BallShadow`, a `SpriteRenderer` added on the scene instance | Ball physics (`BallRig`/`Ballistics`, S8's shared kit) |
| `FullScreenGameplayInput` | `Canvas` (ScreenSpaceOverlay, order `-1`), `CanvasScaler`, `GraphicRaycaster`, full-screen transparent `Image` (raycast target), `GameplayInputRouter`, `ScreenTapArea` | The one shared input surface and router for this scene |
| `BasketballController` | `BasketballController`, `BoxCollider2D` (`isTrigger`, size `8×4`, world-origin-relative) — the finisher's world-space clamp | Gameplay rules, difficulty, flight prediction; the only physics collider the controller scene owns |
| `BasketballHudCanvas` | `Canvas` (order `5`), `SafeAreaFitter`, TMP labels, apex ring/zone `Image`s, charge track/fill/band `Image`s, `BasketballHud` | Basketball-specific HUD |

The scene also rebinds the shared S2 kit: `MinigameHUD.minigameSource` and
`PhaseOverlay.minigameSource` are pointed at `BasketballController`, and `MinigameHUD.theme` at the
shared `UITheme` asset — these are shared-presentation objects the configurator does not own but
must wire up correctly (`BasketballSceneTests.BasketballScene_ShowsGenericAndBasketballHudLabels`,
`.BasketballScene_TeachesHoldAimFinishThroughTheSharedTutorialOverlay`).

**Physics ownership:** `BallRig` (attached to `BallPresentation`) owns the ball's
`Rigidbody2D`/flight integration via `Ballistics.AdvanceVelocity`, driven by
`FlightProfile_Basketball` (`gravityScale: 1`, `linearDrag: 0.02`, `groundY: 0`,
`bounceDamping: 0.8`). `BasketballController` owns no ball physics itself; its only collider
(`finisherBounds`) is a trigger used purely as a world-space clamp for the finisher actor's
transform, never for contact resolution.

**Input ownership:** `BasketballController` installs its own three detectors
(`TapMashInputDetector`, `HoldInputDetector`, `SwipeInputDetector`) onto the one shared
`GameplayInputRouter` via the router's additive `SetTapMashDetector`/`SetHoldDetector`/
`SetSwipeDetector` setters, so it cannot silently drop a detector another owner installed on the
same router (there is none in this scene, but the setters are shared code with Volleyball).

Verified by `Assets/Tests/PlayMode/Gameplay/Ball/Basketball/BasketballSceneTests.cs` (10 tests, all
in the green PlayMode runs above): single controller/ball/HUD, rim inside the authored apex band,
one router/one surface with the screen-centre raycast owned by the gameplay surface, the pause
button's own screen rect raycasting to a `Selectable` above that same surface, every `Image` of
`type == Filled` in the HUD carrying a non-null sprite (a `Filled` `Image` with no sprite silently
draws as a plain static rectangle, ignoring `fillAmount`/`fillMethod` entirely), the S8 kit referenced not duplicated, HUD labels
backed by a real font asset, the finisher bounds able to reach an in-band shot's landing point,
every actor visibly rendered, and the 3-step tutorial content.

## 3. Input route

```
ScreenTapArea (IPointerDownHandler/IPointerUpHandler/IDragHandler on the full-screen surface)
  → GameplayInputRouter.FeedPointerDown/Move/Up
    → TapMashInputDetector / HoldInputDetector / SwipeInputDetector
      → router dispatches OnTap / OnHoldEnd / OnSwipe
        → BasketballController.OnRouterTap / .OnRouterHoldEnd / .OnRouterSwipe
          → BasketballRules.TryPass → TryLaunchAlleyOop → TapFinish
```

`ScreenTapArea.OnPointerDown` claims the pointer only if no `Selectable` owns the raycast hit and
the hit is inside its configured `gameplayArea`; `CanOwn`/`IsInsideGameplayArea` gate every event
before it reaches the router. `BasketballController.OnRouterTap` branches on
`Rules.State == AlleyOopFlight`: a tap during flight is a finish attempt (`SubmitFinishTap`),
otherwise it starts a charge (`BeginCharge`). `OnRouterHoldEnd` reads the charge ratio from the
production `HoldInputDetector`. `OnRouterSwipe` requires an active charge and a real swipe length
(`minimumSwipeLengthPixels = 0.5px`) or cancels the charge; otherwise it calls `SubmitPass`, which
computes `PassVectorForCharge` and calls `BasketballRules.TryPass`.

**Keyboard fallback**, unlike the S9 Volleyball scene: the scene binds
`inputActions = KMA.inputactions` with `gameplayActionMapName = "Basketball"`. The shared
`KMA.inputactions` asset declares six action maps —
`Sprint, Endurance, Boss, Punishment, UI, Basketball` — and the `Basketball` map exposes
`Tap, Hold, Left, Right` (`InputAssetContractTests.SharedInputAssetDeclaresTheS3MapsPlusBasketball`,
`.SharedInputAssetDeclaresRequiredActionsAndMeaningfulBindings`). `GameplayInputRouter` feeds
keyboard `Tap`/`Hold` straight into the same `tapMashDetector`/`holdDetector`, and `Left`/`Right`
into `FeedKeyboardSwipe`, so a desktop tester without a touchscreen can still charge, aim and
finish through the identical detector chain, verified by
`BasketballSceneTests.BasketballScene_RoutesGameplayThroughOneSharedInputRouterAndSurface`
(`router.InputActions` non-null) and by
`BasketballCampaignTests.BasketballPass_PreviewsMapThenContinuesAndPersistsTheRecord`, which calls
the private helper `ScoreOneBasketThroughTheRouter` five times to drive whole baskets through
`FeedPointerDownForTest`/`FeedPointerMoveForTest`/`FeedPointerUpForTest` on the real router.

## 4. Difficulty axes

Five authored steps (`BasketballController.difficultySteps`, index = `Baskets` clamped to the
table), each changing exactly one axis from the previous step
(`AuthoredDifficultyTable_ChangesExactlyOneAxisPerStep`, PlayMode —
`BasketballControllerTests.cs:265`):

| Step (baskets scored) | `finishCueLeadSeconds` | `chargeAngleSpanDegrees` | Axis changed from previous step |
| --- | --- | --- | --- |
| 0 | 0.60 | 35 | — (initial) |
| 1 | 0.45 | 35 | Timing (cue lead shortens) |
| 2 | 0.45 | 45 | Aim (charge span widens) |
| 3 | 0.30 | 45 | Timing (cue lead shortens) |
| 4 | 0.30 | 55 | Aim (charge span widens) |

`finishCueLeadSeconds` is the timing axis: how long before apex `FinishCueVisible` turns true.
`chargeAngleSpanDegrees` is the aiming axis: it widens the charge range around
`passAngleCentreDegrees = 47.5°`, shrinking the fraction of the charge slider (`TargetChargeMin`–
`TargetChargeMax`) that lands the lob's apex inside the authored band — the band itself
(`AlleyOopPattern.AuthoredDefault`'s `ApexMin`/`ApexMax` = 2.8/3.2) never moves, so aiming and
timing stay independent per the plan's design. `alleyOopLeadSeconds` (0.35s, the delay between
pass and launch) and `playerHand`'s authored y (1.2, `BasketballScene_PutsTheRimInsideTheAuthoredApexBand`)
are constants across all five steps, not part of the per-step table.

## 5. Ruling — `AlleyOopPattern.AuthoredDefault` velocity threshold

**Before → after:** `velocityThreshold` changed from `0.1` to `1.5` (`AlleyOopPattern.cs`, S10
Task 2). `ApexMin`/`ApexMax` (2.8/3.2) and `LaunchForce` (8) were not touched — only the timing
window, not the aiming band.

**Measured, not merely asserted.** At the authored physics (`gravity = -9.81`, fixed step `0.02s`,
`linearDrag = 0.02`, matching `FlightProfile_Basketball`), gravity changes `velocityY` by roughly
`0.196` per fixed step. Re-deriving the window length independently (PowerShell, this gate, not
the C# test) with a representative apex-crossing velocity:

| `velocityThreshold` | Band width (`2×threshold`) | Fixed steps the window stays open |
| --- | --- | --- |
| `0.1` (before) | `0.2` | **1** step (`0.02s`) |
| `1.5` (after) | `3.0` | **16** steps (`0.32s`) |

A one-step window is not reliably hittable by a human tap; the old threshold made
`FinishJudge.Perfect` practically unreachable in the build, exactly as the source comment in
`AlleyOopPattern.cs` records. The committed test
`AlleyOopWindowTests.AuthoredApexWindow_StaysOpenForAtLeastTwelveFixedSteps` asserts `>= 12` steps
and is green in both EditMode runs above; this gate's independent 16-step recomputation is
consistent with that assertion and gives the concrete number the test itself only bounds from
below. `AuthoredApexWindow_KeepsTheAuthoredHeightBandAndLaunchContract` and
`PerfectTiming_OnAMisaimedLob_IsNotPerfect` pin the height band and confirm timing and aiming
remain separate axes after the change.

## 6. Balance

**First-attempt pass rate: unavailable — not measured.** Per this task's Correction 3, measuring
"pass rate across 10 play-throughs" (spec §16's 40–60% target) requires playing the game on a real
device, and no physical Android device was available during this gate (section 8). No balance
number in `difficultySteps`, `BasketballPlayerHand.y`, or `alleyOopLeadSeconds` was tuned, blind or
otherwise — Correction 3 is explicit that nothing should be tuned without that measurement, and
none of the four permitted knobs were touched.

**Known, unmeasured balance defect — carried forward, not fixed.** At difficulty step 0 the
authored cue lead is `finishCueLeadSeconds = 0.60s`. The ball's actual ascent time from the
authored hand height, launched at the step-0 charge that centres the authored angle span
(`passAngleCentreDegrees = 47.5°`, `LaunchForce = 8`), is:

```
vy0 = 8 · sin(47.5°) ≈ 5.90
t   = vy0 / 9.81      ≈ 0.601 s
```

So the apex cue (`FinishCueVisible`) is up for essentially the entire rise (`0.60s` lead against a
`0.601s` flight) and conveys almost no advance warning at that step — the player has effectively no
time between "cue appears" and "ball reaches apex." This was found and diagnosed during Task 4's
review and deliberately deferred to this task's balance pass, because tuning it correctly requires
device measurement this gate could not obtain. It is recorded here, unresolved, with the exact
numbers above.

**Second, unmeasured balance defect — the charge band reads as uniform but is not.** The HUD draws
`BasketballChargeTargetBand` as a single flat-opacity strip across its whole authored width, but a
charge released at the band's low edge (`TargetChargeMin`) does not get the same Perfect-timing
window as a charge released at its high edge (`TargetChargeMax`), because the two charges apex at
different heights inside the (fixed-width) authored apex band and therefore satisfy the height gate
for very different durations. Measured by simulating the authored integrator (not by device, and
not by the committed C# test suite):

- A charge at `TargetChargeMin` apexes exactly at `ApexMin = 2.8` — the ball only satisfies the
  height gate for two to four physics steps.
- A charge at `TargetChargeMax` apexes at `~3.196` and stays inside the height gate for the entire
  `0.30s` velocity-threshold window `AlleyOopWindowTests` measures.

| Difficulty step | Band (min–max) | Width | Perfect window at band mid | at low edge | at high edge |
| --- | --- | --- | --- | --- | --- |
| 0 (35°) | 0.450 – 0.655 | 0.205 | 0.300 s | 0.060 s | 0.300 s |
| 2 (45°) | 0.460 – 0.620 | 0.160 | 0.300 s | 0.040 s | 0.300 s |
| 4 (55°) | 0.470 – 0.600 | 0.130 | 0.300 s | 0.080 s | 0.320 s |

A player releasing in the bottom fifth of the glowing band is given a 40–80 ms Perfect window
against the mid-band's (and high-edge's) ~300 ms — a window narrow enough to read as the game
cheating them, not as their own timing error. This is a genuine asymmetry in the authored pattern,
not a HUD rendering bug (Fix 1 in this fix wave corrected the HUD rendering itself; this finding is
about what the corrected HUD would now faithfully show). It was found by simulation during the
whole-branch review, is **not fixed here** — Correction 3 forbids tuning any of the four balance
knobs (`difficultySteps`, `BasketballPlayerHand.y`, `alleyOopLeadSeconds`) without a real-device
pass-rate measurement this gate still cannot obtain — and is recorded here, unresolved, alongside
the step-0 cue-lead finding above. Candidate fixes for whoever picks this up next: clamp the
advertised band inward so the whole visible strip carries a usable window, or grade the band's
opacity by its local window length so the player can see the low edge is a worse bet. Neither was
applied.

**The 40–60% first-attempt pass-rate target is S16-owned, not an S10 gap.** Spec §6 assigns
first-attempt pass-rate tuning to S16 (the polish/release pass); the "unavailable — not measured"
row for it in section 8's device checklist below reflects that this gate could not obtain a device
to measure it, not that S10 owes a fix here. Do not read that open row as an S10 failure.

## 7. Known gaps

Found during S10's reviews, deliberately deferred, listed here rather than silently dropped:

- **Step 0's apex cue conveys almost no warning** — section 6 above. Unmeasured on device.
- **The pause button was unreachable during play — fixed in this document's own fix wave, in
  Basketball only.** `FullScreenGameplayInput`'s `ScreenSpaceOverlay` canvas outranked the
  `ScreenSpaceCamera` S2 HUD canvas in `RaycastAll` regardless of sorting order (`GraphicRaycaster.
  sortOrderPriority` returns `canvas.sortingOrder` for Overlay and `int.MinValue` for every other
  render mode), so its full-screen image sat above the pause button and
  `ScreenTapArea.OnPointerDown` consumed the event first. `BasketballSceneConfigurator` now authors
  `FullScreenGameplayInput` as `ScreenSpaceCamera` on the same camera as the HUD, with a lower
  `sortingOrder`, restoring ordinary sorting-order comparison; covered by
  `BasketballSceneTests.BasketballScene_PauseButtonIsReachableAboveTheGameplayInputSurface`. This
  defect was inherited verbatim from the reviewed `MG_Volleyball` scene and **still affects
  `MG_Volleyball`** — that scene was deliberately left untouched by this fix wave (out of scope)
  and remains a separate, open follow-up for whoever owns the shared input surface.
- **`CreateHudState` allocates on every read** (a `ToString()` plus a fresh `MinigameResult`), and
  the shared `MinigameHUD` polls it every frame — per-frame GC on a mobile target. Identical in
  `VolleyballController`, so it must be fixed in both places at once or not at all.
- **The art is tinted built-in primitives, not authored art.** S16 owns real art; the scene is
  deliberately placeholder (`BasketballSceneConfigurator.LoadPlaceholderSprite`, the same built-in
  `UISprite` guid as `MG_Volleyball`).
- **`BasketballHud.targetBaskets` duplicates the controller's private field**, so changing the
  objective would silently desync the HUD text (`ScoreText` reads the HUD's own copy, not the
  controller's).
- **`AssertVisible` proves a sprite is assigned and enabled, not that an object is inside the
  camera frustum**, and `BasketballScene_RoutesGameplayThroughOneSharedInputRouterAndSurface`'s
  raycast-ownership test samples only screen centre — neither is a substitute for an on-device
  visual check.
- **The README reconciliation is outstanding.** `README.md` is dirty with the concurrent session's
  uncommitted work at gate time; per this task's Correction 1, it was left untouched rather than
  risk committing someone else's changes under this gate's message. The verification counts,
  S10 status line, and playable-scene grouping (Sprint, Endurance, Volleyball, Basketball playable;
  PingPong, Badminton, Football rules-only) it should reflect are exactly the counts and lists in
  this document.

## 8. Physical-device gate — UNAVAILABLE

No physical Android device was connected at any point during this gate. The only attached target
was an emulator — the same kind of target, and the same manufacturer string, as the S1–S9 gate:

```
adb devices -l
List of devices attached
192.168.56.104:5555   device product:vbox86p model:Phone device:vbox86p transport_id:1

getprop ro.product.manufacturer  -> Genymobile
getprop ro.product.model         -> Phone
getprop ro.product.cpu.abi       -> x86_64
getprop ro.build.version.release -> 15
getprop ro.build.version.sdk     -> 35
```

The shipping APK is ARM64-only (section 9's build, `native-code: 'arm64-v8a'` only, no other
`lib/` ABI directory), so it cannot run on that x86_64 target. Install was not attempted a second
time given the identical, already-documented S1–S9 failure mode for the same class of target.

Every item below is therefore **unavailable — not verified, not passed**:

| Device check | Status |
| --- | --- |
| Menu → Map → `Bóng rổ` node → `MG_Basketball` on device | Unavailable |
| Tutorial 3-step next/back/skip on device | Unavailable |
| Charge ring + glowing charge band visible on device | Unavailable |
| Release-in-band → lob → apex ring closes, apex zone glows before apex | Unavailable |
| Tap timing → `EARLY`/`PERFECT`/`LATE` on device | Unavailable |
| `BASKETS`/`ATTEMPTS`/`COMBO` HUD update on device | Unavailable |
| 5 baskets → Result panel → Continue → Map with rank/stars | Unavailable |
| Kill/relaunch → record persists | Unavailable |
| Pause Resume/Restart/Exit on device | Unavailable |
| First-attempt pass rate across 10 play-throughs (spec §16 target 40–60%) | Unavailable |
| 16:9 and notched-display layout — no HUD clipping, no black screen | Unavailable |
| Sustained FPS (Profiler, on-device) | Unavailable |
| Draw-call count (Profiler, on-device) | Unavailable |
| Vietnamese glyph rendering on device | Unavailable |

**This gate's device row is incomplete**, exactly as S1–S9's was. It cannot be closed without a
physical ARM64 Android device (or an ARM64-capable emulator image).

## 9. Android build

Read from `Assets/Editor/BuildScript.cs` first: the real entry point is
`KMA.EditorTools.BuildScript.BuildAndroid`, taking `-buildOutput` (default
`Builds/Android/kma.apk`) and `-androidArchitecture` (must be `arm64` or omitted; the script sets
`PlayerSettings.Android.targetArchitectures = ARM64` unconditionally). This matches the brief's
assumed method name and path exactly.

```powershell
& $unity -batchmode -nographics -projectPath 'D:\project\the-chat-KMA' `
  -executeMethod KMA.EditorTools.BuildScript.BuildAndroid `
  -buildOutput 'Builds/Android/kma-s10.apk' -androidArchitecture arm64 `
  -logFile "$evidence\t7-android-build.log" -quit
```

Result: `[KMA] Build Succeeded, 832984423 bytes, 0 errors, 20 warnings` (`t7-android-build.log`,
line 5916). That byte count is the uncompressed player payload, not the APK.

| Property | Observed |
| --- | --- |
| APK | `Builds/Android/kma-s10.apk` |
| Size | 42,794,793 bytes (40.8 MiB) |
| SHA-256 | `57741bcdc122050998545025256b7e5ae47679231e481e7d59fa05de96a5fedc` |
| Package | `com.kma.thechat` |
| Label | `Thể Chất KMA` (Vietnamese glyphs present) |
| Version | `versionCode=1`, `versionName=1.0` |
| `minSdkVersion` | 25 |
| `targetSdkVersion` | 35 (`compileSdkVersion` 35) |
| Native code | `arm64-v8a` only — `unzip -l` shows only `lib/arm64-v8a/`, no other ABI |
| Orientation | `screenOrientation=11` (`sensorLandscape`) |
| Launcher | `com.unity3d.player.UnityPlayerGameActivity` |
| Permissions | `android.permission.INTERNET`, `com.kma.thechat.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` |

Inspected with `aapt2 dump badging` and `aapt2 dump xmltree --file AndroidManifest.xml` from
build-tools `36.0.0`. This build is separate from, and does not depend on, the concurrent session's
own `Builds/Android/kma-x86_64.apk` / `build-apk.log` (see "Concurrent session" above).

## 10. Gate verdict

| Requirement | Status |
| --- | --- |
| Full EditMode green, twice | Pass — 258/258, twice |
| Full PlayMode green, twice | Pass — 241/241, twice |
| Worktree clean/identical across both runs | Pass — `t7-status-1/2/3.txt` identical |
| Android ARM64 build and inspection | Pass — `kma-s10.apk`, arm64-v8a only |
| Scene contract, input route, difficulty axes documented | Pass — sections 2–4 |
| `AlleyOopPattern.AuthoredDefault` velocity-threshold ruling, measured before/after | Pass — section 5 |
| Balance tuned or honestly recorded as unmeasured | Recorded unmeasured — section 6; no blind tuning performed |
| Physical-device and performance gate | Unavailable — no ARM64 device connected, section 8 |
| README verification numbers and status lines updated | Skipped — `README.md` dirty with a concurrent session's uncommitted work; left untouched per this task's Correction 1 |

**S10 makes Basketball playable and is green and reproducible on this machine as an automated
checkpoint.** It is not a device-verified release: the physical-device gate remains entirely open,
the balance pass rate is unmeasured, and the known gaps in section 7 (including one already-diagnosed
timing defect) are recorded rather than closed. This document proves Basketball only — it makes no
claim about PingPong, Badminton, Football, the Boss/Punishment loop, the ending, or the game-wide
Definition of Done.
