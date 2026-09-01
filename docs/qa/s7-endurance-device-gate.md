# S7 Endurance device gate

Date: 2026-09-01

## Automated evidence

- Unity EditMode: 258 passed, 0 failed.
- Endurance PlayMode controller contract: 8 passed, 0 failed.
- Endurance presentation/input-path gate: 1 passed, 0 failed.
- Router regression check: 1 passed, 0 failed.
- Android APK build: succeeded, `Builds/Android/kma-s7.apk` (31.6 MB), 0 build errors and 3 compiler/tool warnings.
- Full PlayMode: 109 passed, 2 failed; both failures are pre-existing S6/Sprint coverage (`SprintScene_AuthorsThreeCosmeticRivalsAndThreeLayerParallax`) and the generic router test was fixed and re-run independently.

## Device/build status

No Android device was available in this environment. FPS, frame-time, draw-call, audio-latency, pause-drift, and physical vertical-swipe observations therefore remain unverified. The scene contract does verify one shared `ScreenTapArea`, authored `EnduranceMetronome.wav`, tutorial flow, and the four Endurance presentation components.
