# Design — Interactive Unity UI QA

Date: 2026-09-08  
Status: approved in conversation

## Problem

The existing `PlayModeScreenshot` bridge can open a scene, wait, and capture one
static frame. It cannot operate menus or exercise gameplay input, so visual QA
cannot cover states reached through buttons, taps, holds, swipes, or keyboard
controls. Directly invoking callbacks would increase apparent coverage while
bypassing the actual EventSystem and Input System paths that need verification.

## Goals and boundaries

- Drive an ordered interaction scenario through the warm GUI Unity Editor, then
  inspect screenshots at meaningful checkpoints.
- Support both stable named UI controls and normalized screen coordinates for
  gameplay surfaces.
- Exercise Unity's EventSystem/Input System paths rather than calling application
  callbacks directly.
- Preserve the existing `tools/qa-screenshot.sh` request format and behavior.
- Do not add OCR, arbitrary C# method invocation, game-state mutation, visual
  assertions, APK automation, or Android-specific rendering guarantees.

## Architecture

`Assets/Editor/PlayModeScreenshot.cs` remains the single warm-Editor request
consumer. Its request model gains an optional ordered `actions` array. Requests
without actions follow the existing wait-and-capture flow unchanged.

Interactive requests run as a small Editor-driven state machine that survives
Play Mode domain transitions through `SessionState`. Each action starts only
after the previous action completes. Scene loads and frame-dependent operations
wait for observable readiness rather than assuming they completed synchronously.

Pointer actions use one of two locators:

- `target`: the unique name or hierarchy path of an active UI GameObject. The
  bridge resolves its active `RectTransform` and uses its screen-space center.
- `position`: normalized `[x, y]` screen coordinates, each within `0..1`, suited
  to full-screen gameplay input areas.

Named UI actions are translated to pointer input; they do not call `Button.onClick`
or other gameplay methods. Pointer and keyboard operations are injected through
Unity's supported EventSystem/Input System mechanisms so normal routing,
interactability, raycasting, and gesture detection remain under test.

## Scenario contract

An interactive request uses this shape:

```json
{
  "id": "run-123",
  "scene": "Assets/_Project/Scenes/Menu.unity",
  "actions": [
    { "type": "wait", "seconds": 2 },
    { "type": "tap", "target": "PLAYButton" },
    { "type": "wait", "seconds": 1 },
    {
      "type": "swipe",
      "from": [0.5, 0.7],
      "to": [0.5, 0.2],
      "duration": 0.4
    },
    {
      "type": "capture",
      "output": "Builds/Screenshots/after-swipe.png"
    }
  ]
}
```

Supported actions:

| Action | Required data | Behavior |
|---|---|---|
| `wait` | `seconds` | Wait using unscaled Editor time. |
| `tap` | `target` or `position` | Pointer down, one rendered frame, pointer up. |
| `hold` | locator and `duration` | Pointer down, hold for the duration, pointer up. |
| `swipe` | `from`, `to`, `duration` | Pointer down, interpolate movement across rendered frames, pointer up. |
| `key` | `key`, optional `duration` | Press and release a supported Input System key. |
| `capture` | `output` | Capture and flush a checkpoint PNG before continuing. |

Exactly one locator is allowed where a locator is required. Initial key support
is limited to keys Unity's Input System can resolve by name; an unknown key is an
error. Durations must be finite and non-negative. Pointer coordinates must be
finite and inside `0..1`.

The request-level `output` and `waitSeconds` fields retain their current meaning.
After all actions complete, the bridge waits `waitSeconds` and captures `output`
when supplied. This permits interactive scenarios to retain a conventional final
screenshot while also producing checkpoint captures.

## CLI and scenario files

`tools/qa-screenshot.sh` remains the simple static-frame interface.

A new command accepts a complete scenario document:

```bash
tools/qa-unity-ui.sh <scenario.json>
```

The script validates that the input file exists, assigns a unique request ID,
writes `Builds/Screenshots/request.json` atomically, removes stale completion
state, waits within a bounded timeout derived from the scenario durations, and
prints `done.json`. It must not overwrite the source scenario. Reusable project
scenarios may live under `tools/qa-scenarios/`, but no sample is required until a
real flow benefits from one.

## Results and errors

`done.json` remains backward compatible with `id`, `status`, and `message`, and
adds fields useful to interactive runs:

```json
{
  "id": "run-123",
  "status": "error",
  "message": "Target is missing or ambiguous: PLAYButton",
  "failedActionIndex": 1,
  "elapsedSeconds": 2.1,
  "screenshots": []
}
```

`failedActionIndex` is `-1` when failure is not tied to an action. The bridge
stops on the first failure, releases any held input, exits Play Mode, and writes
the result once. Errors include malformed or unsupported actions, invalid
coordinates/durations, missing or ambiguous targets, inactive/non-interactable
UI controls, unsupported keys, scene/open failures, action timeout, capture
failure, and unexpected exit from Play Mode. Diagnostic messages identify the
action and locator without dumping unrelated editor state.

## Testing strategy

Implementation follows test-driven development.

- EditMode tests cover request/action validation, normalized coordinate
  conversion, unique active target resolution, hierarchy paths, ambiguous or
  inactive targets, and legacy request compatibility.
- PlayMode tests use real EventSystem/Input System paths to prove a named tap
  activates exactly one interactable button, a coordinate tap reaches a gameplay
  input handler, hold/swipe sequences have correct down/move/up ordering, and
  input is released after failure.
- Shell-level checks cover missing scenario files, atomic request generation,
  propagation of Unity errors, and timeout calculation where practical.
- A warm GUI Editor run exercises at least one menu transition plus a checkpoint
  capture when Unity is available. The produced PNG must be visually inspected;
  file creation alone is not a visual pass.
- If the required Unity GUI environment is unavailable, automated tests and
  static validation still run, and the unverified runtime check is reported.

## Skill update

`.claude/skills/testing-unity-ui-with-screenshots/SKILL.md` will be reframed as
scenario-driven Unity UI and gameplay QA. It will explain when to choose the
static helper versus the interactive helper, document the action contract with
one representative example, require inspection of every requested checkpoint,
and retain the limitations around static evidence and Android rendering.

## Completion criteria

1. Existing static screenshot requests behave as before.
2. A scenario can tap a named UI control and reach a later screen before capture.
3. Normalized taps, holds, and swipes traverse the gameplay input path.
4. Keyboard actions traverse the Input System and always release held state.
5. Checkpoint and final screenshots are reported and can be visually inspected.
6. Invalid scenarios fail deterministically with the action index and useful
   message, without leaving Play Mode or input devices stuck.
7. Focused automated tests pass, the skill validates, and `git diff --check` is
   clean; any unavailable real-Editor verification is stated explicitly.
