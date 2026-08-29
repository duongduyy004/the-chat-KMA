# S2 Task 6 Verification Report

Date: 2026-08-30

## Verified

- `SprintSceneShowsTutorialCountdownHudAndInputResponse` passed in PlayMode.
- The gate loaded the real `MG_Sprint` scene, observed the Sprint tutorial, waited through the serialized 2-second tutorial and 3-second countdown, observed Play, and confirmed the expected-side tap advanced the controller and refreshed `MinigameHUD.LastState`.
- Focused presentation suites passed: 14 EditMode tests (`/tmp/s2-final-edit.xml`) and 7 PlayMode tests (`/tmp/s2-final-play-3.xml`).
- Full project suites passed: 135 EditMode tests (`/tmp/s2-full-edit-after-buildscript.xml`) and 45 PlayMode tests (`/tmp/s2-full-play.xml`).
- Scene presentation contracts passed for all six existing scenes.
- Final production APK build succeeded: `Builds/Android/kma.apk`, SHA-256 `76ecd68a46bf052db1b11f4a6e5a7b74fc77153ad68d9c9101c59ae0880b7a5f`, containing `lib/arm64-v8a` only.
- Emulator: serial `127.0.0.1:6555`, model `vbox86p`/`Phone`, API 35, ABIs `x86_64,x86`. The production ARM64 APK correctly rejected with `INSTALL_FAILED_NO_MATCHING_ABIS`.
- A separate x86_64 verification APK (`038185a3e7fa7d416d611e94afc1738fb70024d19511602fff19103c69764f6d`) installed and launched as `com.kma.thechat/com.unity3d.player.UnityPlayerGameActivity`; screenshot: `docs/qa/s2-emulator-screenshot.png`.

## Implementation notes

- `PhaseOverlay` now supplies the authored Sprint tutorial steps when bound to a Sprint controller.
- `MinigameHUD.LastState` exposes the most recently pulled view-model state for deterministic presentation verification.
- The existing PhaseFlow test clears the Sprint tutorial preference so test order and prior local runs cannot hide the tutorial.
- `activeInputHandler: 2` keeps existing UGUI StandaloneInputModule scene bindings compatible while Input System actions remain enabled.

## Limits

The emulator verification APK is x86_64-only and is not the production artifact. No physical-device verification is claimed. The build log reports three Unity build errors alongside a successful `BuildResult.Succeeded`; these are logged Unity/editor diagnostics, not BuildPipeline failures.
