# S6 Sprint integration and device gate

Date: 2026-08-31 (Asia/Ho_Chi_Minh)  
Scope: S6 only. This records the Sprint minigame as one routed subject inside
the existing S5 session/result flow. It is not evidence for S7-S16 or the
final Definition of Done.

## Build identity

| Field | Value |
| --- | --- |
| Project Unity version | `6000.3.23f1` (`ProjectSettings/ProjectVersion.txt`) |
| Product/package | `Thể Chất KMA` / `com.kma.thechat` |
| Configured Android version | `1.0` (version code `1`) |
| Target display configuration | Landscape auto-rotation; 1920x1080 reference resolution |
| APK artifact | Not produced; `Builds/Android/kma-s6.apk` does not exist because the Unity executable is absent. |

## Automated checks

The required pinned executable is absent. The attempted EditMode command was:

```text
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-editmode.xml -logFile /tmp/kma-s6-editmode.log -quit
```

Outcome: exit `127` with the exact error:

```text
[rtk: No such file or directory (os error 2)]
```

The confirming availability check also reported:

```text
/usr/bin/ls: cannot access '/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor': No such file or directory
/usr/bin/ls: cannot access '/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity': No such file or directory
```

Therefore the following commands were not run: they require the same missing
Unity executable. No XML files, test totals, compiler output, or Unity log can
be claimed for this gate.

```text
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-playmode.xml -logFile /tmp/kma-s6-playmode.log -quit
rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -executeMethod KMA.EditorTools.BuildScript.BuildAndroid -buildOutput Builds/Android/kma-s6.apk -logFile /tmp/kma-s6-build.log -quit
```

## Safe static evidence performed

The following local, no-network inspections were completed:

```text
rtk sed -n '1,320p' Assets/Tests/PlayMode/Presentation/SprintPresentationGateTests.cs
rtk sed -n '1,340p' Assets/_Project/Scripts/Gameplay/Sprint/SprintController.cs
rtk sed -n '120,290p' Assets/_Project/Scripts/Input/GameplayInputRouter.cs
rtk rg -n -C 24 '<Sprint script GUIDs>' Assets/_Project/Scenes/MG_Sprint.unity
rtk sed -n '1,520p' Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs
rtk sed -n '1,260p' Assets/Tests/PlayMode/Gameplay/Running/SprintRuntimeInputTests.cs
```

Static findings (not runtime validation):

- `MG_Sprint` references the shared `KMA.inputactions`, enables Sprint routing
  on `GameplayInputRouter`, and sets `SprintController.directInputEnabled` to
  `0`; the controller's direct action path is disabled in the scene.
- The scene contains the Sprint controller, HUD, wind cue, router, three
  authored rival profiles (lanes 1, 3, and 4), and a three-layer 2560x1080
  parallax configuration. The player remains outside rival lane 2.
- The authored wind cue lead is `0.8` seconds. Existing tests cover cue timing,
  correct/wrong wind outcomes, one terminal completion, late-input rejection,
  dedicated HUD state, tutorial persistence, and one touch impulse through the
  two screen tap areas.
- `MinigameBase.Finish` only invokes `Completed` when lifecycle resolution
  begins. `ResultPanel.Continue` is separately guarded by `HasContinued`, and
  `SceneRouter` previews then submits the Sprint subject result through the S5
  session flow.

No regression test was added. The existing S6 tests already cover the
device-observed input and terminal-result contracts, and this environment
cannot compile or execute an added Unity test.

## Device scenario availability

`/usr/bin/adb` is installed, but `rtk adb devices -l` could not start its
daemon in this environment:

```text
could not install *smartsocket* listener: Operation not permitted
adb: failed to check server version: cannot connect to daemon
```

No Android device was enumerated. Device model, API level, aspect ratio,
screenshots, and the hands-on scenario (tutorial, countdown, alternating taps,
HUD/rank, wind counter/failure, pause/resume/restart/exit, one result panel,
and return route) are therefore **unavailable and unverified**.

## Performance gate availability

Unavailable and unverified: there is no APK, connected device, or Unity
Profiler session. Consequently this gate does not establish the 60 FPS target,
frame time, draw-call count, absence of sustained sub-30 FPS during
parallax/wind, or the absence of duplicate input events on a physical device.

## Explicit S16 balance scope

S6 preserves the authored Sprint behavior and documents its integration gate.
Final balance criteria, including any tuning or acceptance thresholds beyond
the S6 implementation contracts, remain the responsibility of **S16**.
