# Student Journey QA

QA was run against Unity `6000.3.23f1` on Linux on 2026-10-04, from the Task 9 checkpoint `c86dbdc` plus the Task 10 test and map fallback changes in this working tree. Visual captures use the Unity Editor Play Mode screenshot service. Each capture completed with a matching request ID and `status: ok`.

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

`tools/build-apk.sh --arm64 --output-dir Builds/Android --name journey` succeeded with zero errors and 54 warnings. The APK is `Builds/Android/journey-arm64.apk`, 59,800,642 bytes (58 MiB), SHA-256 `98910c4ed86dcd46da13140569e5e751d6be06171ac23ba5876d67293299a456`. The archive contains `lib/arm64-v8a/libil2cpp.so` and `libunity.so`, with no `armeabi-v7a` library. `adb devices` could not start its daemon (`Operation not permitted`), so no emulator or physical-device installation, gameplay, performance, audio, touch, or background/resume validation is claimed.

The end-to-end journey test exercises the persisted challenge ordering, subject unlocks, exam records, and five-failure supplementary recovery through `GameSession`. Per-subject PlayMode tests separately exercise Sprint input, Volleyball match rules, and Soccer controller outcomes. The complete nine-challenge progression test uses deterministic challenge results; it does not yet drive real input through all three controller scenes in one uninterrupted run.
