# Task 2 Report — Lifecycle events and HUD ViewModel contract

## Delivered scope

- Added `MinigameLifecycle.PhaseChanged`; it is emitted once after each Tutorial → Countdown, Countdown → Play, and Play → Resolve transition.
- Added serialized `tutorialSeconds = 2f` and `countdownSeconds = 3f` defaults to `MinigameBase`, preserving the former lifecycle timing.
- Added the protected `BuildHudState()` contract, `ReadHudState()` public bridge, and a readonly immutable `MinigameHudState` with exactly six declared data fields: `phase`, `timeRemaining`, `progress01`, `stamina01`, `score`, and `statusText`. `Empty` is a static property, not a seventh field.
- Added a pull-only, null-safe `MinigameHUD.RefreshFrom(MinigameHudState)` that tolerates missing source, theme, labels, and fill images.
- Added Sprint and Endurance HUD adapters using current public rules/controller state.
- Added EditMode lifecycle/default-state coverage and PlayMode empty-HUD coverage.

## Assembly boundary

The existing `KMA.Gameplay.UI` assembly referenced `KMA.Gameplay`, while `MinigameBase.BuildHudState()` needs the ViewModel type from UI. Directly adding the reverse reference would create a Unity asmdef cycle. The UI assembly is therefore a leaf (uGUI/TMP only); gameplay and the two controller assemblies reference it. The `phase` snapshot field is a string so the leaf UI contract does not depend on the gameplay enum. `MinigameHUD` serializes an explicit `MonoBehaviour` source and refreshes it only when it implements `IMinigameHudStateSource`, which `MinigameBase` implements. Sprint and Endurance construct snapshots from current state without holding or pushing HUD references.

## Test coverage and verification evidence

- `MinigameLifecyclePresentationTests` now covers Tutorial → Countdown, Countdown → Play, and Play → Resolve notifications, asserting the first `BeginResolve()` is true, the duplicate is false, and Resolve is emitted exactly once.
- The same EditMode fixture asserts the exact readonly six-field HUD contract, `Empty` property shape, and serialized `MinigameBase` defaults of `2f` and `3f`.
- The missing `KMA.Gameplay.UI` namespace import was restored so the fixture resolves `MinigameHudState` against its referenced assembly.
- The existing focused PlayMode test continues to exercise null-safe `RefreshFrom(MinigameHudState.Empty)`.
- Final static checks found no old `primary01`/`secondary01` constructor consumers and no temporary `.orig` artifacts.

## Verification limitation

Per the final instruction, Unity was not launched. No standalone `dotnet`, `csc`, or `mcs` compiler is installed in this environment, so the final review diff could not be compiled or executed here. The focused Unity results recorded by the previous implementation predate this contract correction and are not presented as current green evidence.

## Workspace hygiene

No `.orig` or `.orig.meta` files remain. Pre-existing `README.md`, `ProjectSettings`, Task 1 resource changes, and unrelated generated files were preserved without modification by this review fix.
