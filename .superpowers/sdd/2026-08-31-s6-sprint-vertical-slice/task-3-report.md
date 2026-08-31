# Task 3 fix round 2 report: Sprint HUD ownership and wind presentation

Status: DONE_WITH_CONCERNS

Implementation commit: `93a95f37c5bf46d97b9781580e2dbf01cecfee48`

## Findings addressed

### 1. Sprint-specific HUD ownership

- `SprintHud` no longer writes the shared `Timer`, `Stamina`, `Score`, `Phase`, `Status`, or shared fills owned by `MinigameHUD`.
- Sprint-only values are bound to the scene-local `SprintMetrics` hierarchy: `SprintDistance`, `SprintRank`, `SprintCadence`, and `SprintDistanceFill`.
- `SprintPresentationGateTests` checks that the dedicated labels are distinct from `Score`, `Phase`, and `Status`, then drives a 42 m state and asserts the rendered values `42 m`, `1st`, `COMBO x0`, and distance fill `0.42`. It also refreshes `MinigameHUD` and asserts its shared score remains `0`.
- `KMA.Gameplay.Sprint.asmdef` now explicitly references `Unity.TextMeshPro`, which is required by the Sprint presentation scripts.

### 2. Canvas-safe wind visuals

- `WindCueHost` is now a scene-local child of the HUD Canvas root (`692924607`), which owns the effective `SafeAreaFitter`; it is no longer under the world-space `FX` transform.
- The host remains active as the `SprintWindCue` component owner. Only the authored visual child is toggled, so `Update` continues to process later wind transitions.
- `SprintWindCue` accepts the serialized external host and binds its Image/TMP children from that host.
- The presentation test requires the host Image/TMP bindings, Canvas parent, SafeAreaFitter parent, active component host, and visual state transitions: `WIND INCOMING`/white, `COUNTER THE WIND NOW`/yellow, `WIND COUNTERED`/green, and `WIND MISSED`/red.

### 3. Existing presentation fixes preserved

- Pause remains top-right inside the effective safe-area hierarchy.
- Left and right input zones retain the center-bottom gap and 140 px minimum width at the 1920x1080 reference resolution.
- Tutorial copy, skip behavior, and Sprint-specific PlayerPrefs persistence remain covered by the existing presentation gate.

## Verification

- `git diff --cached --check`: PASS before the implementation commit.
- `git diff --check`: PASS before the implementation commit.
- Static scene checks: PASS. The scene contains unique serialized file IDs, one `SprintMetrics` hierarchy, one `WindCueHost`, HUD prefab additions for metric and wind roots, and no legacy SprintHud shared-label bindings.
- Unity Test Runner: NOT RUN by request. Runtime compilation and PlayMode execution are therefore not claimed as passing in this report.

Report-only commit follows this implementation commit and records the exact implementation hash above.
