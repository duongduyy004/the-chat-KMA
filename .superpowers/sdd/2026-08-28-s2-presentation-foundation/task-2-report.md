# Task 2 Report — Lifecycle events and HUD ViewModel contract

## Delivered scope

- Added `MinigameLifecycle.PhaseChanged`; it is emitted once after each Tutorial → Countdown, Countdown → Play, and Play → Resolve transition.
- Added serialized `tutorialSeconds = 2f` and `countdownSeconds = 3f` defaults to `MinigameBase`, preserving the former lifecycle timing.
- Added the protected `BuildHudState()` contract, `ReadHudState()` public bridge, and an immutable `MinigameHudState` with the six planned fields.
- Added a pull-only, null-safe `MinigameHUD.RefreshFrom(MinigameHudState)` that tolerates missing source, theme, labels, and fill images.
- Added Sprint and Endurance HUD adapters using current public rules/controller state.
- Added EditMode lifecycle/default-state coverage and PlayMode empty-HUD coverage.

## Assembly boundary

The existing `KMA.Gameplay.UI` assembly referenced `KMA.Gameplay`, while `MinigameBase.BuildHudState()` needs the ViewModel type from UI. Directly adding the reverse reference would create a Unity asmdef cycle. The UI assembly is therefore now a leaf (uGUI/TMP only); gameplay and the two controller assemblies reference it. `MinigameHUD` serializes an explicit `MonoBehaviour` source and refreshes it only when it implements `IMinigameHudStateSource`, which `MinigameBase` implements. This preserves the intended pull-only boundary without controller HUD references.

## TDD and verification evidence

- Red: focused EditMode compilation failed first because `MinigameHudState` and the HUD contract were absent.
- Green EditMode: `PhaseChangedFiresOncePerTransition` and `DefaultHudStateIsEmptyAndSafe` passed, 2/2, report `/tmp/s2-hud-green-edit.xml`.
- Green PlayMode: `RefreshFrom_HandlesEmptyStateWithoutOptionalUiFields` passed, 1/1, report `/tmp/s2-hud-green-play.xml`.
- `git diff --check` completed with no whitespace errors.

## Verification limitation

Per the final instruction to avoid long-running Unity, the requested full EditMode and PlayMode suites were not launched. Both focused Unity invocations exited 0; Unity also logged a pre-existing TMP font-asset teardown `UnassignedReferenceException` after test completion, without affecting either exit code.

## Workspace hygiene

The fallback patch utility created `.orig` files and Unity created matching `.orig.meta` files. All sixteen generated artifacts were removed. Pre-existing `README.md`, `ProjectSettings`, and Task 1 resource changes were preserved and will not be staged.
