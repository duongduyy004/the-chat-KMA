# The Chat KMA — Gameplay

Unity gameplay prototype for KMA: seven sports subjects, normalized scoring, recovery challenges, progression, and a final boss.

## Project status

- Unity `6000.3.23f1`
- Input System `1.20.0`
- NUnit/Unity Test Framework `1.6.0`
- Android targets landscape, ARM64, IL2CPP, API 25/35, and `com.kma.thechat`. A separate x86_64 APK supports Genymotion; current results are in [demo QA](docs/qa/android-report-demo.md).
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
3. For the report demo, open `Assets/_Project/Scenes/Bootstrap.unity` and press Play: Splash → Menu → Map → Sprint. Choose New Game, then Sprint, acknowledge the tutorial, and alternate the left/right touch buttons or arrow keys.
4. For isolated gameplay development, open `MG_Sprint.unity`, `MG_Endurance.unity`, or `MG_Boss.unity`.

## Android report demo

Splash/home share a project logo and stadium illustration. Sprint has three parallax artwork layers, an animated player labelled `PLAYER`, and three animated rivals. Loading bars observe asynchronous scene progress; the initial intro remains visible for at least 1.5 seconds.

Close the Editor before batch commands. Build and run on the current x86_64 emulator:

```bash
KMA_UNITY_EDITOR=/home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity
rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . \
  -executeMethod KMA.EditorTools.DemoEmulatorBuild.Build \
  -logFile /tmp/kma-report-emulator-build.log -quit
rtk adb -s 127.0.0.1:6555 install -r Builds/Android/kma-report-emulator.apk
rtk adb -s 127.0.0.1:6555 shell am start \
  -n com.kma.thechat/com.unity3d.player.UnityPlayerGameActivity
```

For an ARM64 Android phone, use `KMA.EditorTools.BuildScript.BuildAndroid` with `-buildOutput Builds/Android/kma-report.apk`. The emulator method restores the project architecture afterward. If installation reports a signature mismatch, use the matching signing key; preserve the existing save before considering an uninstall.

Rebuild authored demo layouts through **KMA → Demo → Configure Splash and Home** and **Configure Sprint Artwork**. Run these before manual layout adjustments. Sources/licenses are in [CREDITS](Assets/_Project/CREDITS.md); verified tests, device checks and remaining limits are in [Android Report Demo QA](docs/qa/android-report-demo.md).

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

Historical Task 1 verification was `209/209` EditMode and `125/128` PlayMode. Current report-demo verification supersedes those counts in [Android Report Demo QA](docs/qa/android-report-demo.md).

### Historical S2 presentation evidence

A prior S2 snapshot verified `SprintSceneShowsTutorialCountdownHudAndInputResponse` against the real `MG_Sprint` scene, covering tutorial, countdown, Play HUD refresh, and expected-side input response. That snapshot is historical evidence only; the Task 1 counts above are the current verification source of truth.

## Design documents

- [`PLAN.md`](PLAN.md) — original gameplay specification
- [`docs/superpowers/plans/2026-08-24-gameplay-foundation.md`](docs/superpowers/plans/2026-08-24-gameplay-foundation.md) — score, timing, and lifecycle contracts
- [`docs/superpowers/plans/2026-08-24-running-minigames.md`](docs/superpowers/plans/2026-08-24-running-minigames.md) — Sprint and Endurance
- [`docs/superpowers/plans/2026-08-24-ball-minigames.md`](docs/superpowers/plans/2026-08-24-ball-minigames.md) — five ball minigames
- [`docs/superpowers/plans/2026-08-24-progression-boss.md`](docs/superpowers/plans/2026-08-24-progression-boss.md) — progression, Punishment, routing, and Boss
