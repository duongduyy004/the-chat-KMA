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

## Integration checks — in progress

- First full integrated PlayMode run: 159/163 passed; `/tmp/kma-report-play.xml`. Four failures identified: startup graph, stale splash test instance, old bounce assertion, and sliced-renderer double scale. These are tracked for correction, not counted as a successful final gate.
- Focused splash/loading before integration: 12/12 passed; `/tmp/kma-task5-green4-play.xml`.
- Initial runner regression: 0/2, correctly detecting missing Animator and placeholder texture; `/tmp/kma-runner-red.xml`.
- First emulator visual timeline showed Unity's native splash, then black loading frames, then Home; the authored splash was skipped because its minimum interval elapsed during scene activation before Android rendered it. The presenter now starts the 1.5-second visible hold on load completion; rebuild/recheck pending.

## Final gate

To be completed after integration: focused tests, full EditMode/PlayMode, final APK hash/metadata, screenshots, cold-start flow, touch, safe area, orientation, FPS, and physical device model/API/ABI.
