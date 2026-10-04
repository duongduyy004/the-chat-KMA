# Student Journey QA

QA was run against Unity `6000.3.23f1` on Linux on 2026-10-04, from the Task 9 checkpoint `c86dbdc` plus the Task 10 test and map fallback changes. Visual captures use the Unity Editor Play Mode screenshot service. Each capture completed with a matching request ID and `status: ok`.

## Automated checks

The focused progression/recovery set passed `14/14` PlayMode tests (`Builds/TestResults/tmp/kma-task10-focused-green.xml.xml`). The full suites contain the journey and recovery checks, including `ScenePresentationContractTests.EveryExistingSceneHasS2CameraAndCanvas`.

| Suite | Passed | Failed | Skipped | Result |
|---|---:|---:|---:|---|
| EditMode | 602/607 | 2 | 3 | Failed on the same two pre-existing baseline cases |
| PlayMode | 223/227 | 4 | 0 | Failed on the pre-existing baseline cases below |

EditMode XML: `Builds/TestResults/tmp/kma-task10-edit-full.xml.xml`. The failures are `ProjectConfiguratorApplyRepairsProductNameDrift` (FMOD cannot initialize an output device) and `CurrentProjectCoverageHasNoMissingCharactersOrLegacyComponents` (coverage 8, expected at least 9). Its only skipped leaves are the three named `ChallengeSequenceTests` ignores in the plan.

PlayMode XML: `Builds/TestResults/tmp/kma-task10-play-full.xml.xml`. The remaining failures are `AudioGameplayTests.AllPlayableScenesHaveOneListenerAndProduceAudioSamples` (MG_Sprint peak `1.27413768E-08`, below `1E-05`), `FestivalUiExperienceTests.SplashShowsReadableProgressAndLoadingHint` (texture is not CPU-readable), and two `SplashLoadingFlowTests` whose presenter lookup returns null. The initial full run had eight additional failures from stale one-visit subject fixtures; those were updated for the nine-challenge journey, and the focused progression fixtures now pass.

## Visual captures

Captured and inspected at the Editor viewport's available `1088x503` resolution. These are static Editor images; they do not establish touch interaction, Android safe-area, or device rendering.

| Capture | Observation |
|---|---|
| `Builds/Screenshots/student-journey/map-start.png` | Course cards and locked states are clear. The lower lesson panel's objective text is small at this viewport size. |
| `Builds/Screenshots/student-journey/sprint-learn.png` | Alternating-tap instruction and both touch controls are visible. |
| `Builds/Screenshots/student-journey/volleyball-learn.png` | Court and joystick/action controls render. Opened directly, this capture has no journey lesson overlay. |
| `Builds/Screenshots/student-journey/soccer-learn.png` | Aim, shot, five-attempt HUD, and tutorial text render clearly. |

Only the available Editor viewport aspect was captured. The requested 16:9, 16:10, 18:9, and 4:3 comparison remains outstanding; the current Editor viewport is fixed at 1088x503. No interaction was simulated by these captures.

## Balance and Android handoff

The three-person novice playtest was not run in this environment. Session duration, attempts per lesson, misunderstanding points, and failure reasons therefore have no player evidence yet.

An Android 17 API 37 AVD (`sdk_gphone16k_x86_64`, 1080x2400 at 420 dpi, 16 KB page-size system image) was used for an x86_64 smoke run. The pre-contrast-fix build launched, New Game and dialogue flow reached the journey map, Sprint Learn accepted alternating left/right touches and completed with score 12, and Continue restored progress after force-stop and relaunch. Unity/Android logcat had no exception, fatal, or crash entries. This was a virtual-device smoke run; it does not establish physical handset behavior, performance, audio quality, or background-resume behavior.

That emulator screenshot also exposed low contrast on lesson labels rendered over white cards: the original `TextPrimary` foreground measured 1.05:1. `JourneyLessonLabelsMeetContrastOnTheirButtonSurface` now asserts a minimum 4.5:1, and the focused EditMode test passed after switching the label to `MutedForeground`. The updated x86_64 APK installed and launched, but ADB disconnected during the follow-up capture, so the corrected map was not visually confirmed on the emulator.

Both current APKs include the contrast fix. `tools/build-apk.sh --arm64 --output-dir Builds/Android --name journey` completed with zero errors and 20 warnings; `Builds/Android/journey-arm64.apk` is 59,800,646 bytes (58 MiB), SHA-256 `0acfa79daf0af4337fa8e9a29f8316d6dde97f08caae1124d6ab656b314011c3`. `tools/build-apk.sh --x86_64 --output-dir Builds/Android --name journey` completed with zero errors and 29 warnings; `Builds/Android/journey-x86_64.apk` is 61,084,901 bytes, SHA-256 `77738f564672232c00594b7bc57ec2d95f3bcb6c3fc59cd96742180a37436b90`. The archives contain their matching `lib/arm64-v8a` and `lib/x86_64` `libil2cpp.so` and `libunity.so` libraries. No physical-device validation is claimed.

The end-to-end journey test exercises the persisted challenge ordering, subject unlocks, exam records, and five-failure supplementary recovery through `GameSession`. Per-subject PlayMode tests separately exercise Sprint input, Volleyball match rules, and Soccer controller outcomes. The complete nine-challenge progression test uses deterministic challenge results; it does not yet drive real input through all three controller scenes in one uninterrupted run.
