# Football fullscreen backdrop and aim arrow — 2026-09-26

## Changes

- `FootballCameraFit` fits the field backdrop to the camera viewport, including wide Android landscape screens. Goal and character transforms retain their original scale.
- `FootballPresentation` shows a gold arrow immediately in Aiming and Charging. Its direction comes from the projected launch velocity used by `FootballShotSolver`; it disappears on release and presentation disable.
- The saved scene and its configurator bind the field renderer to the camera fitter.

## Automated evidence

- Missing-arrow regression reproduced before implementation: `Builds/TestResults/football-fullscreen-red.xml`.
- Wide-screen coverage regression reproduced independently: `Builds/TestResults/football-coverage-red.xml` (field edge -9.6 versus viewport edge approximately -11.65).
- Full PlayMode suite: `Builds/TestResults/football-fullscreen-full-play.xml`, **191 passed, 0 failed**. Coverage includes 1230×570, 16:9 and 4:3, unchanged actor scales, left/center/right aiming, charge, release, next shot and disable.
- Scene regeneration: `Builds/TestResults/football-fullscreen-scene-test.xml`, **1 passed, 0 failed**.
- The first coverage test used `WaitForEndOfFrame`, unsupported by Unity batch testing; replaced it with normal frame yields before validating the coverage failure.

## Android verification

- Built and installed `Builds/Android/kma-football-fullscreen-x86_64.apk` on the connected Genymotion emulator, Android viewport 1230×570. Build log: `Builds/Android/football-fullscreen-build.log`.
- APK SHA-256: `d4fedb6406846c5bad2da7c340e88f419b12d4b1100fa8a0b46cc3728800562d`.
- Navigated from Continue through the Football card, started a match, dragged the direction slider left/right/near center, then held and released SHOOT using Android input events.
- Inspected actual device PNGs in `Builds/Screenshots/football-fullscreen/`: `start-android.png`, `aim-right-android.png`, `aim-left-android.png`, `aim-center-android.png`, `released-android.png`.
- The field covers both screen edges, the gold arrow is visible immediately at the ball and follows the slider direction, and the release frame shows the arrow hidden. No physical Android device was tested.
