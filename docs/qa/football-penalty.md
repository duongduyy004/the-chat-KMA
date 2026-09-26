# Football Penalty Shootout QA

> Historical AIM-based implementation. Current goal-facing implementation and fresh evidence: [football-goal-flight.md](football-goal-flight.md).

Date: 2026-09-26
Unity: 6000.3.23f1
Target: Android, landscape, ARM64/IL2CPP

## Automated tests

| Gate | Result | Evidence |
| --- | --- | --- |
| Football rules and solver | Pass | `football-task-1-final` and later Football filters |
| Football input, controller, and saved scene | Pass, 18/18 final Football filter | `Builds/TestResults/football-play-visual-final.xml` |
| Progression and save compatibility | Pass in full suites | `football-edit-task6-final.xml`, `football-play-task7-full.xml` |
| Full EditMode suite | Pass, 461 passed, 0 failed, 3 ignored | `Builds/TestResults/football-edit-task6-final.xml` |
| Full PlayMode suite | Pass, 191 passed, 0 failed | `Builds/TestResults/football-play-task7-full.xml` |

## Visual and interaction checks

| Gate | Result | Evidence |
| --- | --- | --- |
| Start scene captured and reviewed | Pass with host-size limitation | `Builds/Screenshots/football/start-final.png` (1101x503 host Game view, not 1920x1080) |
| Goal, player, ball, AIM, and labeled score/remaining count visible | Pass after visual fixes | Reviewed `start-final.png` |
| Gameplay-state crosshair and keeper reaction | Covered by component tests; no screenshot | `FootballPresentationTests` |
| Full match input, pause/resume, retry, difficulty on device | Not verified | Device unavailable |
| Android safe area and touch behavior | Not verified | Device unavailable |

## Android build and device

| Gate | Result | Evidence |
| --- | --- | --- |
| ARM64 IL2CPP APK build | Pass, 43,462,359 bytes | `Builds/Android/kma-penalty-arm64.apk`, SHA-256 `c2c8add1defeb1d5b840f77fcc2eaac9911d6cd2620c5149bdb149545207145e` |
| APK contains ARM64 IL2CPP and Unity libraries | Pass | `unzip -l` lists `lib/arm64-v8a/libil2cpp.so` and `libunity.so` |
| Install and offline launch | Not verified | `adb devices -l` returned no devices |

## Limitations

Unity emitted an Android-build warning that Active Input Handling is set to Both; the ARM64 build still succeeded. The host GUI Editor exited with a native shutdown fault after the screenshot was captured; the screenshot request completed with status `ok`, and batch test/build runs succeeded afterward. The host Game view was 1099x504, so a 16:9 device screenshot and live Android interaction remain unverified. No Android device was attached. Do not infer device or interaction success from unit tests or a static screenshot.
