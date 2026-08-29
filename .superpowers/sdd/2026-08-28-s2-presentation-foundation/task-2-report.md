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
- The `TestMinigameBase` fixture now calls `Initialize()` before phase assertions so EditMode coverage does not rely on implicit editor-time `Awake()` behavior.
- The missing `KMA.Gameplay.UI` namespace import was restored so the fixture resolves `MinigameHudState` against its referenced assembly.
- The existing focused PlayMode test continues to exercise null-safe `RefreshFrom(MinigameHudState.Empty)`.
- Final static checks found no old `primary01`/`secondary01` constructor consumers and no temporary `.orig` artifacts.

## Final re-review correction

- The PlayMode HUD test asmdef had a stale `KMA.Gameplay.Presentation` reference, but no project asmdef declares that assembly. The invalid reference was removed; all other existing test references and production architecture remain unchanged.
- Static validation confirms every remaining project assembly reference in the test asmdef exists. Unity was not launched per the requested no-broad-Unity verification scope.

## Verification limitation

Per the final instruction, Unity was not launched. No standalone `dotnet`, `csc`, or `mcs` compiler is installed in this environment, so the final review diff could not be compiled or executed here. The focused Unity results recorded by the previous implementation predate this contract correction and are not presented as current green evidence.

## Scope cleanup and verification

- Re-review found that commit `3cd03e1` accidentally tracked a Codex resume marker in `README.md`, a Unity editor version upgrade, and three unrelated generated paths. The resume marker (and its blank separator) was removed, and `ProjectSettings/ProjectVersion.txt` was restored to its `3cd03e1^` parent version, Unity `6000.3.22f1`.
- `Assets/_Project/Settings/UI/Resources.meta`, `ProjectSettings/PackageManagerSettings.asset`, and `ProjectSettings/SceneTemplateSettings.json` were removed from the Git index. They remain as local untracked files so no working-copy content is discarded.
- No Task 1 or Task 2 source or test file was changed by this cleanup.
- Verification consists of an inspected scoped diff, `git diff --check`, and final Git status inspection. Unity was not run.
