# Task 3 fix round 1 report: Sprint HUD, wind cue, and tutorial

Status: DONE_WITH_CONCERNS

Implementation commit: `3643d99f66d159544711671ade40c8c658445284`

## Findings addressed

- `SprintHud` now caches the authored `S2_HUD_Minigame/SafeAreaRoot` TMP labels and fill Images once in `Awake`; `HasBoundVisuals` is asserted by the presentation gate.
- `SprintWindCue` now uses a separate scene-local `WindCueHost` with authored Image and TMP children, binds them in `Awake`, and never disables its own component host. Cue, active-window, countered, and missed states remain visual-only.
- The HUD prefab root now owns `SafeAreaFitter`; its nested fitter is disabled so the added top-right `PausePanel` is inside the effective safe-area hierarchy without applying insets twice.
- `LeftTap` and `RightTap` leave a center-bottom strip and each span at least 140 reference pixels at 1920x1080.
- `SprintPresentationGateTests` now requires exactly one scene-local controller, Sprint HUD, wind cue, tutorial overlay, and PausePanel; checks all bindings, cue host visuals, Canvas/CanvasScaler, Pause safe-area/top-right placement, zone widths/gap, tutorial copy, and tutorial skip persistence after scene reload.

## Verification

- `git diff --check`: PASS.
- Static scene checks: PASS (unique scene file IDs; exactly one cue host/state; expected safe-area fitter declarations; expected input anchors).
- Unity Test Runner: NOT RUN by request. Unity/.NET availability and PlayMode execution remain unverified.

Report-only commit follows the implementation commit and records its exact hash above.
