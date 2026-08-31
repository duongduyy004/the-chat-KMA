# Task 1 Report: S6 runtime input contract

## Status

DONE_WITH_CONCERNS

## Files changed

- `Assets/Tests/EditMode/Input/AlternateTapInputDetectorTests.cs`
- `Assets/Tests/EditMode/Input/AlternateTapInputDetectorTests.cs.meta`
- `Assets/Tests/PlayMode/Gameplay/Running/SprintRuntimeInputTests.cs`
- `Assets/Tests/PlayMode/Gameplay/Running/SprintRuntimeInputTests.cs.meta`
- `Assets/Tests/PlayMode/Gameplay/Running/SprintControllerTests.cs`
- `Assets/Tests/PlayMode/Gameplay/Running/KMA.Gameplay.Running.PlayMode.Tests.asmdef`
- `.superpowers/sdd/2026-08-31-s6-sprint-vertical-slice/task-1-report.md`

Production Sprint controller, input detector, and SprintRules files were inspected only. Existing unrelated dirty files were not modified.

The PlayMode test assembly gained its required `KMA.Input` reference so the requested router contract test can compile. No SprintRules behavior or runtime production code was changed.

## Implemented contract coverage

- Detector: first expected side, repeated-side `OnWrongSide`, valid alternation exactly once per tap, finite supplied timestamps, and no dependency on `Time.time`.
- Runtime mapping: in-memory `InputActionAsset` with `SprintLeft`/`SprintRight`, keyboard actions routed through `GameplayInputRouter` into `AlternateTapInputDetector`, then into the controller seams; valid taps advance controller state once and a wrong-side action does not advance it.
- Authored gameplay: existing distance cue and 0.8-second activation assertions retained; existing correct/wrong counterplay result assertions retained; later taps after resolve are now asserted not to create another completion.

## Tests and exact evidence

1. `rtk git diff --check`

   Output: empty; exit code 0.

2. Brief command with its literal Unity path:

   `rtk /home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-task1-edit.xml -logFile /tmp/kma-s6-task1-edit.log -testFilter "AlternateTapInputDetectorTests" -quit`

   Output: `[rtk: No such file or directory (os error 2)]`; the brief path is unavailable.

3. Correct installed-editor command:

   `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-task1-edit.xml -logFile /tmp/kma-s6-task1-edit.log -testFilter "AlternateTapInputDetectorTests" -quit`

   Unity initially reported compiler errors because the new PlayMode test referenced `UnityEngine.InputSystem.TestFramework`, which is not referenced by the Sprint PlayMode test assembly. The test was changed to use `InputSystem.QueueDeltaStateEvent` and `InputSystem.Update`; the assembly was given the required `KMA.Input` reference.

4. Correct installed-editor PlayMode command:

   `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task1-play.xml -logFile /tmp/kma-s6-task1-play.log -testFilter "SprintRuntimeInputTests|SprintControllerTests" -quit`

   Final Unity process output was empty and exited code 0. `/tmp/kma-s6-task1-edit.xml` and `/tmp/kma-s6-task1-play.xml` were not created. The logs contain `Batchmode quit successfully invoked - shutting down!`, but no Test Runner `test-suite`, `test-case`, pass, or fail records. Both logs also contain `Licensing::Module Error: Access token is unavailable; failed to update` and the environment reports no .NET SDK for the Unity build-server helper.

5. RTK proxy fallback:

   `rtk proxy /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-task1-edit.xml -logFile /tmp/kma-s6-task1-edit-proxy.log -testFilter "AlternateTapInputDetectorTests" -quit`

   Same limitation: Unity exited batchmode successfully, `/tmp/kma-s6-task1-edit.xml` was not created, and `/tmp/kma-s6-task1-edit-proxy.log` has no Test Runner records.

## Commit

`177baf5006850280ae076d5ead13be70937f2214` (`test: define sprint runtime input contract`)

## Concerns

- The installed Unity CLI can refresh/import/compile and exit successfully, but this environment does not provide authoritative test-result XML or Test Runner execution evidence for the requested focused suites.
- Unity logs show a licensing token update error and missing .NET SDK for the build-server helper; these are environment limitations, not confirmed test failures.

## Review fix round 1

### Changes

- `AlternateTapInputDetectorTests` now proves finite timestamps are accepted while a non-finite timestamp emits neither valid nor wrong-side events. The detector API exposes no timestamp callback, so the test asserts the observable timestamp-validation boundary rather than claiming to inspect an unexposed value.
- `SprintRuntimeInputTests` now counts detector-valid and controller-forwarded events, snapshots speed before the repeated-left action, and asserts the wrong-side action leaves both counts and speed unchanged.
- This report section records the covering reruns and final fix commit.

### Rerun commands and output

1. `rtk git diff --check`

   Output: empty; exit code 0.

2. `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-task1-edit-fix.xml -logFile /tmp/kma-s6-task1-edit-fix.log -testFilter "AlternateTapInputDetectorTests" -quit`

   Unity exited with code 0 after project refresh/compile. No test-result XML was created; the log contains `Batchmode quit successfully invoked - shutting down!` but no Test Runner `test-suite`, `test-case`, pass, or fail records. It also contains `Licensing::Module Error: Access token is unavailable; failed to update` and the missing .NET SDK build-server message.

3. `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task1-play-fix.xml -logFile /tmp/kma-s6-task1-play-fix.log -testFilter "SprintRuntimeInputTests|SprintControllerTests" -quit`

   Unity exited with code 0 after project refresh/compile. No test-result XML was created and the log contains no Test Runner `test-suite`, `test-case`, pass, or fail records; the same licensing and missing .NET SDK messages are present.

### Fix commit

`16238fd1047876e2b7155e06ec3360831b31d76c` (`test: tighten sprint input contract review fixes`)

## Review fix round 2

### Changes

- Removed the non-independent `controllerInputEvents` counter from `SprintRuntimeInputTests`.
- The test now keeps `detectorValidEvents` as the detector observation and independently observes controller invocation through `SprintSnapshot.Speed`: the repeated-left action must leave speed unchanged, and the valid right action must add exactly one 18-point impulse. A duplicate controller forwarding fails the exact speed assertion.
- The timestamp fix from round 1 is retained.

### Rerun commands and exact output

1. `rtk git diff --check`

   Output: empty; exit code 0.

2. `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/kma-s6-task1-edit-fix.xml -logFile /tmp/kma-s6-task1-edit-fix.log -testFilter "AlternateTapInputDetectorTests" -quit`

   Output: empty; exit code 0. The XML file was not created. Log evidence: `Batchmode quit successfully invoked - shutting down!`; no Test Runner `test-suite`, `test-case`, pass, or fail records. The log also reports `Licensing::Module Error: Access token is unavailable; failed to update` and `No .NET SDKs were found.`

3. `rtk /home/duongduy/Unity/Hub/Editor/6000.3.23f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults /tmp/kma-s6-task1-play-fix.xml -logFile /tmp/kma-s6-task1-play-fix.log -testFilter "SprintRuntimeInputTests|SprintControllerTests" -quit`

   Output: empty; exit code 0. The XML file was not created. Log evidence: `Batchmode quit successfully invoked - shutting down!`; no Test Runner `test-suite`, `test-case`, pass, or fail records. The log contains the same licensing-token and missing-.NET-SDK messages.

### Fix commit

`62fbce83bcef65d591c2c7bf9ed7825eb8f77ccd` (`test: independently verify sprint input routing`)
