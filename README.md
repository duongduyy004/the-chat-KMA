# The Chat KMA — Gameplay

Unity gameplay prototype for KMA: seven sports subjects, normalized scoring, recovery challenges, progression, and a final boss.

## Project status

- Unity `6000.3.23f1`
- Input System `1.20.0`
- NUnit/Unity Test Framework `1.6.0`
- Android targets landscape, ARM64, IL2CPP, API 25/35, and `com.kma.thechat`. A separate x86_64 APK supports Genymotion; those results are in [demo QA](docs/qa/android-report-demo.md).
- Current playable subject routes are Sprint, Endurance, Volleyball and Basketball.
- S1–S10 is a **checkpoint, not a release**. S11–S16 are outside the current plan, and the physical-device gate is still open for both sections — see [S1–S9 Stabilization Gate](docs/qa/s1-s9-stabilization-gate.md) and [S10 Basketball Gate](docs/qa/s10-basketball-device-gate.md).

The seven subject rule engines are present: Sprint, Endurance, Volleyball, Basketball, PingPong, Badminton, and Football. The scene router exposes Sprint, Endurance, Volleyball and Basketball as playable subject scenes; the other three are implemented as deterministic gameplay models and ball-rule contracts.

Volleyball plays as a three-touch `Dig → Set → Spike` possession driven by swipes on the shared gameplay surface, with the rally point awarded when the flight resolves. Its known gaps — the authored net and court colliders are not yet enforced as rules, and the ground plane sits above the drawn floor — are recorded in the gate document above.

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
| `MG_Volleyball` | Three-touch Dig-Set-Spike possession against an authored opponent return |
| `MG_Basketball` | Charged alley-oop lob finished by a tap inside the authored apex window |
| `MG_Boss` | Three-phase final boss sequence |
| `Punishment` | Recovery challenge for a failed first attempt |
| `Map` | Return route after subject/boss resolution |
| `GameOver` | Route after lives are exhausted |

## Default keyboard controls

| Gameplay | Controls |
| --- | --- |
| Sprint | Left/Right arrows |
| Endurance | `T` tap, `H` hold, Up/Down arrows swipe |
| Volleyball | No keyboard fallback - swipe on the gameplay surface with touch or a mouse drag |
| Basketball | `Space` begins the charge, Left/Right arrows release the pass, `Space` finishes at the apex |
| Boss | `Space` tap-mash, `H` rhythm hold, Left/Right arrows alternate tap |
| Punishment | `Space` tap-mash, `H` rhythm hold, Left/Right arrows alternate tap |

Touch input is supported by Endurance, Boss, and Punishment input bridges where the scene requires it. Volleyball and Basketball have no bridge: each controller owns its detectors on the scene's shared `GameplayInputRouter`, fed by the one full-screen `ScreenTapArea`. Volleyball is gesture-only; Basketball also has a keyboard path through the `Basketball` action map.

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

Current verification is `258/258` EditMode and `243/243` PlayMode, each reproduced twice from a clean tracked status. Evidence, commands and the open device gates are in [S1–S9 Stabilization Gate](docs/qa/s1-s9-stabilization-gate.md) and [S10 Basketball Gate](docs/qa/s10-basketball-device-gate.md).

Test runs regenerate `Assets/_Project/Fonts/Nunito-Bold.asset` — the dynamic TextMeshPro atlas caching newly rendered glyphs. Revert it rather than committing it.

### Historical verification snapshots

Superseded by the counts above, kept for provenance: Task 1 verified `209/209` EditMode and `125/128` PlayMode; the report demo's counts are in [Android Report Demo QA](docs/qa/android-report-demo.md); an earlier S2 snapshot verified `SprintSceneShowsTutorialCountdownHudAndInputResponse` against the real `MG_Sprint` scene.

## Design documents

- [`PLAN.md`](PLAN.md) — original gameplay specification
- [`docs/superpowers/plans/2026-08-24-gameplay-foundation.md`](docs/superpowers/plans/2026-08-24-gameplay-foundation.md) — score, timing, and lifecycle contracts
- [`docs/superpowers/plans/2026-08-24-running-minigames.md`](docs/superpowers/plans/2026-08-24-running-minigames.md) — Sprint and Endurance
- [`docs/superpowers/plans/2026-08-24-ball-minigames.md`](docs/superpowers/plans/2026-08-24-ball-minigames.md) — five ball minigames
- [`docs/superpowers/plans/2026-08-24-progression-boss.md`](docs/superpowers/plans/2026-08-24-progression-boss.md) — progression, Punishment, routing, and Boss
