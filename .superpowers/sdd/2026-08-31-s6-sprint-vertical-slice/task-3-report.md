# Task 3 report: Sprint HUD, wind cue and tutorial

Status: DONE_WITH_CONCERNS

Implementation commit: `3c73dcf630beff0e70fab411a24727ffbb7edc8f`

## Implemented

- Added `SprintHud`, a read-only presentation binder that caches the Sprint controller in `Awake`, polls the controller state, and renders timer, stamina, distance, rank (`1st`–`4th`), cadence combo, and progress values without mutating rules.
- Added `SprintWindCue` with visible, active-window, countered, and missed states. It only reads the controller wind flags and does not alter collision or timing state.
- Updated Sprint tutorial copy to the required two-step content: `Tap the shown side` and `Counter the wind before the window closes`.
- Added presentation assertions for scene-local HUD/cue objects, both tutorial steps, skip behavior, and Sprint-specific remembered state.
- Wired HUD and cue components into `MG_Sprint`. Existing landscape CanvasScaler remains at 1920x1080; existing input zones are bottom anchored and substantially wider than the 140px minimum. Existing PausePanel remains top-right on the SafeAreaFitter-backed UI hierarchy.

## Verification

- `git diff --check`: PASS.
- Static scene component-list check: PASS.
- Unity Test Runner: NOT RUN — no `unity-editor`, `Unity`, or `dotnet` executable is available in this environment.
- The presentation test is therefore not independently green-verified in this environment. The test loads `MG_Sprint` and uses Unity PlayMode APIs when run under the project’s Unity test setup.

## Concerns

- Because Unity is unavailable, serialized scene loading, C# compilation, and PlayMode execution remain unverified. Run `SprintPresentationGateTests` in Unity before merging further work.
- HUD text/Image references are intentionally left null in the scene because the existing scene HUD objects contain no authored TMP/Image subfields to bind; the components still expose deterministic readouts and can be connected to authored labels in the editor.
