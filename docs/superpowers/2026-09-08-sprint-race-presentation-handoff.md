# Sprint race presentation handoff

Date: 2026-09-08 (Asia/Ho_Chi_Minh)  
Scope: Minigame 1 (`MG_Sprint`) only.

## Requested outcome

Fix the Android Sprint presentation shown in the supplied emulator screenshots:

- remove the generic HUD layer covering the race;
- place the main player in lane 2;
- make the main player and all three rivals advance independently at their own speeds;
- keep left/right tapping as alternating cadence input.

The approved behavior supersedes the previous `PLAN.md` rule that locked the
main player at 35% of the viewport.

## Root cause

Input was not the cause. The scene routes the two transparent tap areas through
`GameplayInputRouter` into `SprintController`, and `SprintRules.Tap` applies an
18-point impulse for the expected side or a 7.2-point impulse for the wrong
side. `SprintRules.Tick` then converts speed into distance.

The visual break was downstream:

1. `RunnerVisualPresenter` selected animation states but never mapped the main
   player's `Snapshot.Distance` to a transform position. Rivals already mapped
   their own `rivalDistance` values across the track.
2. The `HUD_Minigame` prefab's `SafeAreaRoot` remained active alongside the
   custom `SprintMetrics`, producing the large generic layer and duplicate HUD.
3. The track artwork scrolled with player distance, which would double the
   motion once all runners physically advance.

## Implemented changes

- `RunnerVisualPresenter` now maps the main player's own race distance from
  track X `-9.6` to `9.6`, clamped over the 0–100 distance range. It moves the
  `Player` root and preserves its authored Y value (`0.7`, lane 2).
- `MG_Sprint.unity` disables only the generic `SafeAreaRoot`; the shared canvas,
  custom `SprintMetrics`, phase overlay, result panel, pause control, and input
  surfaces remain intact.
- The third parallax layer (track) now has a zero scroll multiplier. Sky and
  campus retain their authored parallax values.
- `PLAN.md` now records that the main player and three rivals advance from start
  to finish according to their own distances.

## Regression coverage and TDD evidence

New tests in `RunnerVisualTests` cover:

- main-player distance-to-track mapping and lane preservation;
- generic HUD content disabled while Sprint metrics remain visible;
- fixed track layer while runners advance.

Observed RED evidence:

- half-distance player expected X `0`, actual X `-2.88`;
- expected no active `SafeAreaRoot`, actual object was active;
- expected track tile X `0`, actual X `-16.25` after refresh.

Observed GREEN evidence:

- `RunnerVisualTests`: 6/6 passed;
- Sprint gameplay tests: 17/17 passed (`SprintControllerTests` 14/14 and
  `SprintRuntimeInputTests` 3/3);
- complete PlayMode suite: 200/200 passed, 0 failed, duration 127.90 seconds.

Evidence files are local and untracked:

- `/tmp/kma-sprint-red.xml`
- `/tmp/kma-sprint-ui-red.xml`
- `/tmp/kma-sprint-ui-green.xml`
- `/tmp/kma-sprint-gameplay-green.xml`
- `/tmp/kma-playmode-final.xml`

## Device verification still required

Rebuild/install the Android artifact and repeat the original scenario at
570×1230:

1. Enter Minigame 1 and confirm there is no generic translucent HUD panel.
2. Confirm the main player begins in lane 2 and all four runners begin at the
   same track X.
3. Alternate left/right taps and confirm the main player advances.
4. Confirm wrong/repeated-side taps advance more slowly than correct cadence.
5. Confirm rivals separate according to their profiles and the track stays
   fixed while sky/campus parallax remains subtle.
6. Exercise countdown, wind cue, pause/resume, resolve, and result navigation.

No fresh APK installation or emulator screenshot was produced in this task.

## Worktree hygiene

This is a shared dirty worktree. Do not commit or revert unrelated UI work or
the existing deleted `.worktrees/*` entries. Unity PlayMode runs regenerated
`Assets/_Project/Fonts/Nunito-Bold.asset`; README instructs restoring that
artifact before a task-scoped commit. Re-check `git status` immediately before
staging because other UI/test files changed concurrently during this task.
