# The Chat KMA — Gameplay

Unity gameplay prototype for KMA: three sports subjects, normalized scoring, and campaign progression.

## Project status

- Unity `6000.3.23f1`
- Input System `1.20.0`
- NUnit/Unity Test Framework `1.6.0`
- Android targets landscape, ARM64, IL2CPP, API 25/35, and `com.kma.thechat`. A separate x86_64 APK supports Genymotion; those results are in [demo QA](docs/qa/android-report-demo.md).
- Sprint and Volleyball are selectable; Football remains disabled on the map.
- Historical test snapshots predate the current scope and have not been rerun after recent removals.

The subjects are Sprint, Volleyball and Football. Sprint and Volleyball are selectable on the map.

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
5. Failure costs one life and returns to Map, or to GameOver when no lives remain.

`SceneRouter` keeps the live `GameSession` across scene loads and guards against duplicate transitions.

## Scenes

| Scene | Purpose |
| --- | --- |
| `MG_Sprint` | Sprint subject with rival pace, stamina, wind cue, and counterplay |
| `MG_Volleyball` | 1v1 beach volleyball against an authored AI; first to 5 points within 120 s |
| `MG_Football` | Football subject scene |
| `Punishment` | Recovery challenge for a failed first attempt |
| `Map` | Return route after subject resolution |
| `GameOver` | Route after lives are exhausted |

## Default keyboard controls

| Gameplay | Controls |
| --- | --- |
| Sprint | Left/Right arrows |
| Volleyball | WASD/arrow keys move, Space for the action button (touch: left-thumb joystick, right action button) |
| Punishment | `Space` tap-mash, `H` rhythm hold, Left/Right arrows alternate tap |

Touch input is supported by the shared gameplay input router and Punishment input bridge where required.

## Open the project

1. Install Unity `6000.3.23f1` with the required 2D and Input System packages.
2. Open this repository as the Unity project root.
3. For the report demo, open `Assets/_Project/Scenes/Bootstrap.unity` and press Play: Splash → Menu → Map → Sprint. Choose New Game, then Sprint, acknowledge the tutorial, and alternate the left/right touch buttons or arrow keys.
4. For isolated gameplay development, open `MG_Sprint.unity`.

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

## Build APK

`tools/build-apk.sh` (Linux) and `tools/build-apk.ps1` (Windows) wrap
`KMA.EditorTools.AndroidBuildMatrix.Build`, which builds every requested ABI inside one headless
Editor session and restores the project's ARM64 default before quitting. Close the Editor first —
batchmode cannot open a locked project.

```bash
tools/build-apk.sh                 # arm64 -> Builds/Android/kma-arm64.apk
tools/build-apk.sh --arm64         # phones only
tools/build-apk.sh --x86_64        # emulators only
tools/build-apk.sh --abi all       # arm64 + x86_64
tools/build-apk.sh --abi all --output-dir Builds/Release --name kma-1.0
```

```powershell
.\tools\build-apk.cmd
.\tools\build-apk.cmd -Abi x86_64
.\tools\build-apk.cmd -Abi all -OutputDir Builds/Release -Name kma-1.0
```

On a default Windows install the execution policy is `Restricted`, so `.\tools\build-apk.ps1`
fails with `running scripts is disabled on this system`. `build-apk.cmd` is a thin wrapper that
forwards its arguments through `powershell -ExecutionPolicy Bypass -File`, so it works without
changing machine settings. To call the `.ps1` directly instead, allow local scripts once with
`Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` (no admin rights needed).

Both resolve Unity from `KMA_UNITY_EDITOR`, then from the Unity Hub install matching
`ProjectSettings/ProjectVersion.txt`; override with `--unity` / `-Unity`. The default ARM64 build
keeps the common phone build to one Unity build; request `--x86_64` or `--abi all` when an emulator
APK is also needed. Each run streams
Unity's build output to the terminal while writing the same output to `<output-dir>/build-apk.log`,
then exits when Unity finishes and reports each APK's size and SHA-256. A non-zero exit dumps the
last 40 log lines.

The APKs are signed with Unity's debug keystore (`androidUseCustomKeystore: 0`); set a release
keystore in Player Settings before distributing. `BuildScript.BuildAndroid` stays the ARM64-only
shipping path and `DemoEmulatorBuild.Build` the fixed-path emulator demo build; neither changes.

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

The last recorded suite counts predate the current scope; they have not been rerun after the removals.

Test runs regenerate `Assets/_Project/Fonts/Nunito-Bold.asset` — the dynamic TextMeshPro atlas caching newly rendered glyphs. Revert it rather than committing it.

### Historical verification snapshots

Superseded by the counts above, kept for provenance: Task 1 verified `209/209` EditMode and `125/128` PlayMode; the report demo's counts are in [Android Report Demo QA](docs/qa/android-report-demo.md); an earlier S2 snapshot verified `SprintSceneShowsTutorialCountdownHudAndInputResponse` against the real `MG_Sprint` scene.

## Design documents

- [`PLAN.md`](PLAN.md) — original gameplay specification
- [`docs/superpowers/plans/2026-08-24-gameplay-foundation.md`](docs/superpowers/plans/2026-08-24-gameplay-foundation.md) — score, timing, and lifecycle contracts
- [`docs/superpowers/plans/2026-08-24-running-minigames.md`](docs/superpowers/plans/2026-08-24-running-minigames.md) — Sprint
- [`docs/superpowers/plans/2026-08-24-ball-minigames.md`](docs/superpowers/plans/2026-08-24-ball-minigames.md) — shared ball gameplay systems
