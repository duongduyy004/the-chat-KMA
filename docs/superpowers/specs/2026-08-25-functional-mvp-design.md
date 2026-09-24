# The Chat KMA Functional MVP Design

**Date:** 2026-08-25  
**Status:** Approved in chat  
**Product source of truth:** `PLAN.md`  
**Target:** Functional Android MVP

## 1. Purpose

The implementation will use free placeholder assets with verified redistribution terms when suitable assets are available. Missing or unsuitable art must fall back to simple Unity shapes and uGUI so asset discovery never blocks a playable checkpoint.

## 2. Product Boundary

### In scope

- Distinct input and objective mechanics for every subject exactly as described in `PLAN.md`.
- Five shared lives, two attempts per subject visit, punishment between attempts, normalized `0..10` scores, and ranks.
- Touch input for Android and keyboard input for Editor testing.
- Versioned atomic JSON save/load, New Game, Continue, and reset confirmation.
- Simple readable uGUI, gameplay cues, placeholder visuals, and basic audio feedback.
- Android debug APK for ARM64, minimum API 26, target API 35, landscape only.
- A verified 30 FPS floor during representative gameplay on a mid-range Android device or emulator profile.

### Out of scope

- Google Play release, signing for production, store listing, monetization, analytics, cloud sync, multiplayer, or network services.
- Production-quality custom art, cinematic animation, voice acting, localization beyond the Vietnamese MVP copy, or advanced accessibility features.
- Saving and resuming the physical state of a minigame in progress.
- New sports, alternate modes, optional spin extensions, or unrelated refactoring.

## 3. Current Baseline

The production gap is at the adapter and product-shell layers: five ball sports have no controller or scene, Map cannot start a subject, the presentation is a debug IMGUI overlay, mobile input is incomplete, save/load does not exist, and no Android player build has been verified.

Existing rule behavior remains authoritative unless it conflicts with `PLAN.md`. New presentation code must adapt to the rule models rather than duplicate their scoring or objective logic.

## 4. Runtime Architecture

### 4.1 Scene flow

The first enabled build scene is `Bootstrap`. It creates a single persistent `AppRoot` and then loads `Menu`.

Scenes load with `LoadSceneMode.Single`. Only `AppRoot` survives scene changes. This preserves the current routing approach and avoids the lifecycle complexity of additive gameplay scenes.

### 4.2 Persistent services

`AppRoot` owns exactly one instance of each service:

- `SceneRouter`: resolves routes, loads scenes, binds controller completion once, and rejects duplicate transitions.
- `SaveSystem`: validates, loads, writes, backs up, and migrates save snapshots.
- `SubjectCatalog`: contains the display name, scene, icon/fallback color, tutorial copy, and configuration reference for the three retained subjects.
- `SettingsService`: stores master/music/SFX volume and other MVP settings.
- `AudioService`: applies the persisted volume settings and provides shared UI/gameplay SFX playback.

The runtime must reject duplicate `AppRoot` instances. Gameplay scenes must not contain their own persistent router.

### 4.3 Minigame boundary

Each minigame is divided into three layers:

1. **Rule model:** deterministic state, objective, scoring, authored pattern resolution, and result construction.
2. **Controller:** advances the rule with time and semantic input, manages the shared lifecycle, and emits one `Completed(MinigameResult)` event.
3. **Presentation/input adapter:** renders snapshots, cues, HUD, touch controls, keyboard bindings, and placeholder animation.

The five ball sports reuse `BallRig`, `TimingWindow`, flight profiles, cue conventions, and input detectors. They do not share a generic rules controller. Each sport retains its own state machine and primary objective.

### 4.4 Input architecture

One `KmaInputActions.inputactions` asset contains action maps for navigation and each gameplay family. Scene adapters enable only the active map.

Shared detectors expose semantic events for tap, alternating tap, hold/release, swipe vector, vertical swipe, and timing evaluation. Controllers consume semantic events and do not inspect screen coordinates or device types directly.

Touch is the production input. Keyboard bindings mirror the same semantic actions for Editor use and automated PlayMode tests. Every required gameplay action must be reachable without a physical keyboard.

## 5. Product Shell and UI

The IMGUI `GameplayPresentation` is removed from production routes. The MVP uses uGUI with a `1920x1080` landscape reference resolution and safe-area anchoring.

Required UI surfaces are:

- Menu: New Game, Continue, Settings, and Quit where supported.
- Shared gameplay HUD: hearts, timer/progress, tutorial, countdown, cue/status, pause, and sport-specific metric slots.
- Result: pass/fail, normalized score, rank, attempt/life consequence, and the next route.
- Punishment: active mechanic, target, progress, and control cue.
- GameOver: summary, New Game, and Menu.
- Victory: completion summary and Menu.
- Error: configuration or scene-load message plus a safe route back to Menu.

UI may use simple panels, shapes, icons, and text. It must remain readable and operable; visual polish is not an acceptance dependency.

## 6. Subject Vertical Slices

Implementation proceeds in dependency-aware checkpoints. Each checkpoint must be playable from Map, routed through Result, saved, and covered by scene-level tests before the next begins.

### 6.1 Product shell plus Sprint

Create Bootstrap, Menu, Map, Result, catalog, save/load, and the shared HUD. Retrofit Sprint with Android touch zones, visible runners/progress, wind cue, result routing, punishment/retry, and persistence.

### 6.6 Badminton

Provide hold-to-charge and release input, height-based smash/drive/lift selection, shuttle-specific flight profile, authored rallies, and five-point win logic.

### 6.7 Football

Provide swipe direction/force/curvature, trajectory preview, five authored goalkeeper patterns, telegraphed counterplay, five kicks, and the three-goal pass objective.

### 6.9 Android hardening

Finalize player settings, application identifier, placeholder icon, landscape lock, safe area, pause/back behavior, build validation, debug APK generation, install/launch smoke tests, and performance measurement.

## 7. Save and Restore

The save file contains a versioned DTO rather than Unity object references:

Writes use a temporary file in the same directory, flush it, and replace the primary file while retaining the last valid backup. If the primary save is corrupt, the runtime tries the backup. If neither is valid, it preserves the corrupt input with a `.corrupt` suffix, creates a new session, and informs the player.

Transient minigame physics and active punishment progress are not saved. Closing during active gameplay resumes at Map using the most recent settled snapshot and does not consume a life or attempt.

## 8. Error Handling

- Missing subject/catalog/scene/input configuration fails the validation suite and blocks the build.
- In Editor and tests, configuration errors throw with the exact subject, scene, or reference name.
- In an APK, an unexpected scene-load failure preserves the in-memory session and opens the Error UI surface with a route back to Menu.
- A save-write failure keeps the current in-memory session, displays a non-blocking warning, and retries at the next save boundary.
- Missing optional art uses a colored shape and text fallback. Missing required gameplay configuration is never silently replaced.
- Duplicate completion and route requests are ignored after the first accepted transition.
- New Game and reset require confirmation before replacing an existing save.

## 9. Third-Party Asset Policy

Asset discovery is scoped to each vertical slice and cannot block its functional acceptance. Prefer CC0, CC-BY, or another license that explicitly permits use and redistribution in the project. License terms are verified before import.

Imported content is stored under `Assets/ThirdParty/<pack-name>/`. `THIRD_PARTY_ASSETS.md` records the source URL, author, license, access date, local paths, modifications, and attribution text. Assets without a verifiable license are not imported.

If no suitable licensed asset is found, the slice ships with Unity shapes, generated sprites, and text labels. A later art replacement must not require changes to rule or controller behavior.

## 10. Verification Strategy

### EditMode

- Preserve deterministic rule, score, lifecycle, and progression coverage.
- Add save serialization, validation, backup recovery, and schema migration tests.
- Add catalog and build-scene validation tests.
- Keep adverse events authored and verify that bonus mechanics cannot bypass the primary objective.

### PlayMode

Every subject scene test must:

- Load the enabled production scene.
- Find exactly one subject controller.
- Confirm required input actions and references.
- Advance tutorial/countdown into Play.
- Send representative Input System events.
- Complete or fail through the real controller.
- Observe exactly one completion event and a normalized result.
- Verify routing through Result/Punishment/Map as appropriate.

### Android

An Editor build method and repository script create a debug APK reproducibly. Final verification includes:

- Clean Android build for ARM64, minimum API 26, target API 35.
- `adb install` and launch.
- Landscape and safe-area checks.
- Touch smoke test for every distinct gesture family.
- Pause/background/resume and Android Back handling.
- Logcat review for unhandled exceptions and missing references.
- Representative 30 FPS measurement on the reference performance class: an ARM64 Android device with Snapdragon 730-equivalent performance or better, at least 4 GB RAM, and a 1080p-class display. The exact model and OS version are recorded in the test report; if only an emulator is available, the result is labeled provisional until repeated on physical hardware.

## 11. Definition of Done

The Functional MVP is complete only when all of the following are true:

- A clean clone opens in Unity `6000.3.22f1` without manual reference repair.
- Menu supports New Game and Continue, and Map starts the retained subject set.
- Each subject preserves its distinct `PLAN.md` mechanic and primary objective.
- Touch completes all required actions; keyboard remains available for Editor/test use.
- Save data survives process termination and restores the last settled progression snapshot.
- All EditMode and PlayMode tests pass with zero failures.
- Build validation reports no missing scene, catalog entry, input action, or required reference.
- The debug APK installs and launches on Android 8.0+ ARM64, stays landscape, and has no unhandled exceptions during a complete run.
- Representative gameplay maintains at least 30 FPS on the defined reference performance class, with the measured device and OS recorded.
- Every imported third-party asset has recorded provenance and a compatible license.

## 12. Planning Decomposition

This design is implemented through separate, sequential implementation plans so each plan fits a reviewable unit:

1. Product shell, save/load, and Sprint vertical slice.
6. Badminton vertical slice.
7. Football vertical slice.
9. Android build, device QA, and final full-run verification.

Each plan begins from the verified previous checkpoint and ends with focused tests plus the complete accumulated suite. No plan may claim completion solely because rule-model tests pass.
