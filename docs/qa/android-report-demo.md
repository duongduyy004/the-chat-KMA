# Android Report Demo QA

## Baseline — 2026-09-05

- Source baseline: `f796c52e9f130c473d0c587d2db5e3f4a4b5bade` on `master`; implementation branch `codex/android-report-demo`.
- Preserved pre-existing user state: modified `.gitignore`, untracked `.worktrees/`.
- Unity: `6000.3.23f1`.
- EditMode: `253/253` passed. XML `/tmp/kma-demo-baseline-edit.xml`; log `/tmp/kma-demo-baseline-edit.log`.
- PlayMode: `149/149` passed. XML `/tmp/kma-demo-baseline-play.xml`; log `/tmp/kma-demo-baseline-play.log`.
- Baseline APK: `Builds/Android/kma-report-baseline.apk`, 35,220,704 bytes, SHA-256 `c0653969d043a39be72128b79d998383e584eda50b059f8a7cdf4016b9691c24`.
- Baseline APK manifest: package `com.kma.thechat`, label `Thể Chất KMA`, minimum API 25, target API 35, `arm64-v8a`, Unity Player launch activity.

## Early Android check

The connected target is `127.0.0.1:6555`: Genymotion model `Phone` / device `vbox86p`, API 35, ABI `x86_64,x86`, reported physical size `570x1230`. Installing the baseline ARM64 APK failed with `INSTALL_FAILED_NO_MATCHING_ABIS`. On 2026-09-06 the user explicitly changed the demo target to this running emulator. `DemoEmulatorBuild.Build` produces a separate x86_64 APK and restores the project's ARM64 setting afterward. Emulator checks below do not certify a physical phone.

## Art direction and provenance

- Style: flat school-athletics cartoon, bold dark outlines, neo-brutalist palette from `UITheme`.
- Generated specifically for this project with OpenAI image generation: `GameLogo.png`, `AppIcon.png`, `HomeIllustration.png`, and Sprint `Sky.png`, `Campus.png`, `Track.png`. No third-party image source is incorporated in these files.
- Runner frames are selected unchanged from Kenney Toon Characters (`character_malePerson_idle`, `run0`, `run1`, `run2`, `hit`). The downloaded archive's `License.txt` states Creative Commons Zero (CC0).
- The first generated title logo was rejected during visual QA because its Vietnamese lettering was incorrect. The final logo contains no text; the exact title is rendered separately by TextMeshPro.
- Source rasters are preserved at their generated sizes: logo/icon 1254×1254, home 1928×816, sky 1931×814, campus/track 1983×793. These differ from the plan's requested source dimensions. Unity imports the layers at PPU 100, max size 2048, full-rect sprites; the authoring script scales every tile to 25.6 world units wide. Coverage is a world-layout target, not a claim that the source PNGs contain 2560×1080 pixels.
- Adjacent parallax tiles alternate horizontal reflection, sharing the same edge pixels at each join. Scroll rates remain the existing 0.15/0.35/0.65 distance multipliers. The track extends below the controls to cover the viewport edge.
- Runner source canvases are 96×128 with bottom-center pivots. Run uses poses 0–1–2–1; Burst plays faster. Scale curves affect only Visual, preserving lane roots and progress positions. A PLAYER label identifies the player without tinting skin.

## Integration checks

- First full integrated PlayMode run: 159/163 passed; `/tmp/kma-report-play.xml`. Four failures identified: startup graph, stale splash test instance, old bounce assertion, and sliced-renderer double scale. These are tracked for correction, not counted as a successful final gate.
- Focused splash/loading before integration: 12/12 passed; `/tmp/kma-task5-green4-play.xml`.
- Initial runner regression: 0/2, correctly detecting missing Animator and placeholder texture; `/tmp/kma-runner-red.xml`.
- First emulator visual timeline showed Unity's native splash, then black loading frames, then Home; the authored splash was skipped because its minimum interval elapsed during scene activation before Android rendered it. Starting the hold on load completion did not fix it — see the splash presentation section below.

## Splash presentation — resolved 2026-09-07

Rebuilding and rechecking on the emulator showed the authored splash was still never presented: sampled cold-start frames were solid black from about 6.2 s to 13.5 s, then Home. A control run proved this was not a capture artifact — repeated `screencap` of Home and of the Menu→Map transition returned real frames every time.

Temporary `Debug.Log` instrumentation in `SplashScreenPresenter`, read back over `adb logcat`, gave the cause:

```text
Awake                  t=2.17  frame=0
OnLoadStarted          t=2.62  frame=0
OnLoadCompleted        t=3.13  frame=1
nativeSplashFinished   t=3.13  frame=1
holdElapsed            t=9.32  frame=2
Hide                   t=9.32  frame=2
```

The whole Bootstrap→Menu startup renders **two frames**. The splash is active with `alpha=1` throughout, but scene activation stalls the player for about 6.2 s inside a single frame, so the wall-clock hold expires before Android ever presents the intro. A time-only hold cannot work here.

`FinishAfterMinimumIntro` now also requires a minimum number of *presented* frames (`minimumIntroFrames`, default 30) alongside `minimumIntroSeconds`. PlayMode test `Splash_StaysVisibleUntilItHasBeenPresentedForMinimumFrames` covers the frame requirement with a zero-second hold. The instrumentation was removed before the verified build.

After the fix, the sampled cold start shows the authored splash — logo, `THỂ CHẤT KMA`, subtitle, illustration, `SẴN SÀNG!` and the loading bar — at 13.28 s and 13.72 s, with Home at 14.20 s: [emulator-splash.png](images/emulator-splash.png), [emulator-home.png](images/emulator-home.png).

Cold start to Home is about 14 s on this x86_64 emulator under software rendering. That figure is an emulator characteristic and is not a phone measurement.

## Verified gate — 2026-09-07

Run against working tree at `c71e426` plus the fixes described here. Unity `6000.3.23f1`.

- EditMode: `253/253` passed.
- PlayMode: `168/168` passed.
- Emulator APK: `Builds/Android/kma-report-emulator.apk`, 44,015,952 bytes, SHA-256 `0b838e9500c1a8b3e3e468b540c856b7641ee2f4a5480fb17430edf7a225fb56`.
- APK manifest: package `com.kma.thechat`, label `Thể Chất KMA`, minimum API 25, target API 35, `native-code: x86_64`, launcher `com.unity3d.player.UnityPlayerGameActivity`.
- Emulator: Genymotion `Phone` / `vbox86p`, Android 15 (API 35), ABI `x86_64,x86`, landscape 1230×570. Installed with `adb install -r`; no signature mismatch and no save reset was needed.
- Observed on the emulator: cold start reaches the authored splash and then Home; Vietnamese labels `CHƠI`, `TIẾP TỤC`, `CHƠI MỚI`, `CÀI ĐẶT`, `THOÁT` render correctly; `CHƠI` routes to Map and the Sprint tutorial overlay appears.

Three test failures found and fixed during this gate:

- `HomePlayButton_RoutesThroughTheRealRuntimeBinding` loaded `Menu` standalone, so `SceneRouter.Instance` was null and `S5ShellSceneController.OpenMap` silently no-opped until the timeout. The test now enters Menu through the Bootstrap route it claims to exercise.
- `Assets/Resources/TMP Settings.asset` carried an empty `assetVersion` while the ugui package expects `"2"`, so TMP scheduled its package-importer window one second later. In `-nographics` that logged `No graphic device is available to initialize the view.` and failed whichever test happened to be running. Setting `assetVersion: 2` removed the race.
- `Bootstrap_ResetPreservesPreferencesAndClearsCampaignAcrossRelaunch` was a cascade of the TMP failure and passes once that race is gone.

`DemoEmulatorBuild.Build` restored `PlayerSettings.Android.targetArchitectures` in memory but only called `AssetDatabase.SaveAssets()`, which does not write `ProjectSettings.asset`. A batch build therefore quit leaving `AndroidTargetArchitectures: 8` (X86_64) as the project default, and that value was committed at `c71e426`. The build now also calls `File/Save Project`; the setting is back to ARM64 and stays ARM64 across an emulator build.

## Still not done

- No physical Android phone has been connected. Every device result above is from the Genymotion emulator and does not certify a phone. Task 6's real-device gate — model/API/ABI, FPS measurement, safe area, both landscape orientations, 10-minute stability, launcher icon — remains unmet.
- No ARM64 APK has been built or hashed in this session.
- No demo video has been recorded.
- Sprint gameplay, parallax and Result were not re-verified on device in this session.
