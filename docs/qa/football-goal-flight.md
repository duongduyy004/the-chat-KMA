# Football goal-facing flight QA — 2026-09-26

Approved reference: `docs/proposals/football-goal-view.html` (preview visible only while holding SHOOT).

## Implementation

- `FootballFlightSimulation`: fixed 1/240 second ballistic integration, 22 degree launch, 6–26 m/s initial speed, gravity 9.81, goal/ground/keeper/frame contacts, bounce and roll. No random shot drift.
- `FootballRules`: manual direction, cosine charge, preview only in Charging, deterministic flight, five kicks and unchanged campaign scoring.
- Input/controller: slider, single pointer ownership, pause/focus/disable cancellation, symmetric listener binding on re-enable. Saved BackButton calls a persistent handler through result ownership.
- Frontal scene uses original vector layers from the approved preview; separate net ripple, circle slider handle and sliced panels. Reproducible art exporter and provenance live with the new assets.

## Evidence collected

- Initial direct-charge regression failed against old rules, then passed after implementation: `Builds/TestResults/football-flight-red.xml` and `football-flight-edit.xml` (32/32).
- Controller re-enable regression failed before fix, then passed in Football PlayMode: `football-flight-lifecycle-red.xml`, `football-flight-play-visual.xml` (16/16).
- Full EditMode: `football-flight-full-edit-final.xml`: 470 total, 467 passed, 3 ignored, 0 failed. Ignored cases are the existing retired Punishment sequence tests. Runner root status is `Skipped:Ignored`, so the shell wrapper exits 1 despite no failed cases.
- Final scene regeneration check: `football-flight-scene-final.xml`: 1/1, including saved Back handler and absence of the old AIM control.
- Full GUI PlayMode: `football-flight-full-play-final.xml`: 188/189. Remaining failure: `SprintRuntimeInputTests.SprintScene_ScreenTapAreaIsOnlyTouchEntryPointAndRoutesOneImpulse`, expected RightTap raycast but got null. No Sprint implementation was changed.
- Football scene interaction captures: `Builds/Screenshots/football-flight/idle.png`, `charging.png`, `flight.png`, `outcome.png`. These are actual PlayMode frames from the saved scene, captured using Unity ScreenCapture after pointer events. Reviewed for goal-facing layout and charge-only trajectory. The first capture attempt with additive scene loading was invalid because Menu overlaid Football; final capture uses isolated Single loading.

- Immediate release visibility regression: `football-flight-immediate-hide-red.xml` failed before the synchronous hide fix; final Football PlayMode `football-flight-play-final.xml` passed 16/16 afterward.
- Project screenshot workflow `tools/qa-screenshot.sh` captured `Builds/Screenshots/football-flight/start.png`; the actual PNG was inspected along with idle, charge, flight and goal frames.

## Final batch validation

- Full PlayMode in a fresh batch Editor: `Builds/TestResults/football-flight-full-play-batch-final.xml`: **189 passed, 0 failed, 0 skipped**. The earlier GUI Sprint raycast failure did not recur.
- Android ARM64 build succeeded: `Builds/Android/kma-football-flight-arm64.apk` (43,393,341 bytes). ZIP contents contain `lib/arm64-v8a/libil2cpp.so` and only the ARM64 native ABI.
  SHA-256: `3fed92e3e2163fa16f35bc456fe71979ca3e1e24a4e9758aaf09ad5bb87ff4c3`. Build log: `Builds/Android/football-flight-build.log`.
- Screenshots do not validate Android safe areas, GPU behavior or physical touch input; no physical Android device was tested.
