# The Chat KMA — Gameplay

Unity gameplay prototype for KMA: seven sports subjects, normalized scoring, recovery challenges, progression, and a final boss.

## Project status

- Unity `6000.3.23f1`
- Input System `1.20.0`
- NUnit/Unity Test Framework `1.6.0`
- Android project configuration targets landscape, ARM64, IL2CPP, API 25/35, and `com.kma.thechat`; build/device verification is pending
- Current playable subject routes are Sprint and Endurance; Volleyball is not yet a playable route

The seven subject rule engines are present: Sprint, Endurance, Volleyball, Basketball, PingPong, Badminton, and Football. The current scene router exposes Sprint and Endurance as playable subject scenes; the other five are implemented as deterministic gameplay models and ball-rule contracts.

## Core gameplay

Every minigame returns a normalized result from `0` to `10`, rounded to one decimal place:

```text
Score = objective 6 + accuracy 0..2 + efficiency 0..1 + mastery 0..1
```

Ranks are `S >= 9`, `A >= 8`, `B >= 7`, `C >= 6`, `D >= 5`, otherwise `F`.

The shared lifecycle is `Tutorial → Countdown → Play → Resolve`, with exactly one completion event. Gameplay variation comes from authored patterns; rule models do not use random number generation.

## Progression loop

1. Start a subject with five lives available.
2. A first failure opens the authored Punishment scene.
3. Completing Punishment routes back to the same subject for its second attempt.
4. A second failure costs one life.
5. Passing all seven subject records unlocks the Boss.
6. The Boss uses the authored sequence `TapMash → RhythmHold → AlternateTap` and returns to Map once resolved.

`SceneRouter` keeps the live `GameSession` across scene loads and guards against duplicate transitions.

## Scenes

| Scene | Purpose |
| --- | --- |
| `MG_Sprint` | Sprint subject with rival pace, stamina, wind cue, and counterplay |
| `MG_Endurance` | Phased rhythm subject with tap, hold, and swipe modes |
| `MG_Boss` | Three-phase final boss sequence |
| `Punishment` | Recovery challenge for a failed first attempt |
| `Map` | Return route after subject/boss resolution |
| `GameOver` | Route after lives are exhausted |

## Default keyboard controls

| Gameplay | Controls |
| --- | --- |
| Sprint | Left/Right arrows |
| Endurance | `T` tap, `H` hold, Up/Down arrows swipe |
| Boss | `Space` tap-mash, `H` rhythm hold, Left/Right arrows alternate tap |
| Punishment | `Space` tap-mash, `H` rhythm hold, Left/Right arrows alternate tap |

Touch input is supported by Endurance, Boss, and Punishment input bridges where the scene requires it.

## Open the project

1. Install Unity `6000.3.23f1` with the required 2D and Input System packages.
2. Open this repository as the Unity project root.
3. Start from `Assets/_Project/Scenes/MG_Sprint.unity`, `MG_Endurance.unity`, or `MG_Boss.unity`.

## Run tests

Set the Unity executable path for your machine, then run Unity Test Framework without `-quit`:

```bash
KMA_UNITY_EDITOR=/path/to/Unity
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults /tmp/kma-editmode.xml -logFile /tmp/kma-editmode.log

rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/kma-playmode.xml -logFile /tmp/kma-playmode.log
```

The latest Task 1 verification passed `17/17` focused configuration tests and `209/209` full EditMode tests on Unity `6000.3.23f1`. Full PlayMode remained at the planned stabilization baseline of `125/128`: two known rhythm/input failures and one known Sprint prefab-instance failure, with no additional Task 1 failure.

### Historical S2 presentation evidence

A prior S2 snapshot verified `SprintSceneShowsTutorialCountdownHudAndInputResponse` against the real `MG_Sprint` scene, covering tutorial, countdown, Play HUD refresh, and expected-side input response. That snapshot is historical evidence only; the Task 1 counts above are the current verification source of truth.

## Design documents

- [`PLAN.md`](PLAN.md) — original gameplay specification
- [`docs/superpowers/plans/2026-08-24-gameplay-foundation.md`](docs/superpowers/plans/2026-08-24-gameplay-foundation.md) — score, timing, and lifecycle contracts
- [`docs/superpowers/plans/2026-08-24-running-minigames.md`](docs/superpowers/plans/2026-08-24-running-minigames.md) — Sprint and Endurance
- [`docs/superpowers/plans/2026-08-24-ball-minigames.md`](docs/superpowers/plans/2026-08-24-ball-minigames.md) — five ball minigames
- [`docs/superpowers/plans/2026-08-24-progression-boss.md`](docs/superpowers/plans/2026-08-24-progression-boss.md) — progression, Punishment, routing, and Boss
