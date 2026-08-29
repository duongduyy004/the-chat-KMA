# Task 4 Report — Phase, tutorial, and result presentation

## Delivered scope

- Added serializable `TutorialStep` data with title, instruction, optional icon,
  and optional animation key.
- Added `TutorialOverlay.Show(...)`, test-store configuration, multi-step
  Next/Back navigation, Skip/Close completion, button-state refresh, per-subject
  seen persistence, and non-blocking behavior for empty or already-seen subjects.
- Added `PhaseOverlay.Bind(MinigameBase)`, event-driven Tutorial → Countdown →
  Play → Resolve presentation, a visible local 3-2-1 countdown, and guaranteed
  `OnDisable` unsubscription.
- Added passive `ResultPanel.Show(MinigameResult, previewRoute)` rendering and an
  `ActionRequested` event. It contains no `GameSession` mutation, scene routing,
  or runtime object discovery.
- Added `PhaseOverlay.prefab`, `ResultPanel.prefab`, and two TMP material presets.
  All prefab text uses the existing Baloo2/Nunito assets and their repaired
  `VietnameseFallback` chain. The presets serialize the required black Underlay
  (`x=.04`, `y=-.04`, softness `0`) and heading Outline (`0.2`).

## Required assembly bridge

Task 2 made `KMA.Gameplay` depend on the leaf `KMA.Gameplay.UI` assembly for
`MinigameHudState`, but Task 4's required UI signatures consume `MinigameBase`
and `MinigameResult`. Directly adding the reverse reference would create a Unity
asmdef cycle. The pure HUD DTO was therefore moved into the gameplay assembly
while preserving its namespace, public shape, filename GUID, and all consumers.
The asmdef edge now points UI → gameplay. `MinigameBase` exposes an additive
read-only phase-event relay so `PhaseOverlay` subscribes without exposing or
mutating the lifecycle object. Rules timing and session ownership are unchanged.

## TDD and focused verification

- RED: focused EditMode compilation exited non-zero with the expected missing
  `TutorialStep`, `TutorialOverlay`, `PhaseOverlay`, and `ResultPanel` types.
- GREEN EditMode:
  `rtk /home/duongduy/.local/bin/unity test . --editor-version 6000.3.23f1 --mode EditMode --filter KMA.Tests.Presentation.TutorialOverlayTests --output /tmp/s2-tutorial-green.xml --timeout 240 -- -nographics`
  — 4 passed, 0 failed, exit 0.
- GREEN PlayMode:
  `rtk /home/duongduy/.local/bin/unity test . --editor-version 6000.3.23f1 --mode PlayMode --filter KMA.Tests.Presentation.PhaseFlowTests --output /tmp/s2-phase-play-graphics.xml --timeout 240`
  — 2 passed, 0 failed, exit 0. This covers prefab/font/effect/result bindings
  and the real Sprint default Tutorial → Countdown → Play lifecycle without UI
  mutation of the Sprint snapshot at bind time.
- `rtk git diff --check` passed. Static scans found no missing script/font/theme
  references in either prefab and no session/routing/discovery calls in Task 4 UI.

## Limitations and scope controls

- Per the user's latest instruction, no broad EditMode or PlayMode suite was run.
- The first focused PlayMode attempt with `-nographics` recorded one passing
  prefab test and one environmental failure when TMP's delayed package importer
  tried to open a window (`No graphic device is available`). The same two-test
  filter passed with graphics enabled; no gameplay fix was made for that editor
  environment issue.
- Verification used installed Unity `6000.3.23f1`; Unity's incidental rewrite of
  tracked `ProjectVersion.txt` was restored to `6000.3.22f1` before commit.
- S2 Tasks 5/6, S3, S4, scenes, camera, full Sprint gate, and README were not
  changed. Pre-existing README and unrelated generated/untracked ProjectSettings
  files were preserved and excluded from staging.
