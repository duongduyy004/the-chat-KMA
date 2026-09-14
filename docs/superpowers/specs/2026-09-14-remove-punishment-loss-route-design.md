# Remove the Punishment Leg from the Loss Route

**Date:** 2026-09-14
**Status:** Approved

## Goal

Losing a minigame returns the player to the subject-select screen, minus one life.
The Punishment stop between the two attempts is removed from the route.

## Why

Losing a subject for the first time currently routes to `SessionRoute.Punishment`,
which loads `Assets/_Project/Scenes/Punishment.unity`. That scene renders nothing
real. `MinigameUIAssembler` gives it the shared `HUD_Minigame` and `PhaseOverlay`
prefabs, but `PunishmentController` is a plain C# class, not a `MinigameBase`, so
`FindInScene<MinigameBase>` returns null, `minigameSource` stays null, and neither
`MinigameHUD.Update()` nor `PhaseOverlay.ApplyPhase()` ever runs. Every label keeps
the placeholder text baked into the prefab and every phase root stays active at once:

| On screen | Actual source |
|---|---|
| `TUTORIAL` | `PhaseOverlay.prefab:653` |
| `TAP LEFT / RIGHT` | `HUD_Minigame.prefab:219` |
| `PLAY` | `PhaseOverlay.prefab:1337` |
| `14` | `HUD_Minigame.prefab:507` |

Behind that frozen screen `PunishmentSceneController` is live and polling input for a
three-step sequence (`TapMash` ×3 → `RhythmHold` ×1 → `AlternateTap` ×2), giving the
player no feedback about any of it. Captured as `Builds/Screenshots/diag_punishment.png`.

Rather than build a UI for a challenge the game does not want, the leg comes out of
the route.

## Constraints

- Punishment code is retired in place, not deleted: the scene, `PunishmentController`,
  `PunishmentSceneController`, and the `Punishment` / `RetrySubject` enum members all stay.
- No new UI. `ResultPanel`, the subject-select screen, and all Sprint chrome are untouched.
- Existing saves must not strand a player in the unreachable Punishment scene.
- Vietnamese copy unchanged — no copy is added or edited.

## Decisions

| Decision | Choice | Why |
|---|---|---|
| Where to cut the route | `GameSession.SubmitResult` | `SubmitResult` and `PreviewRoute` both derive from `RouteForResult`, so one edit keeps the result panel's preview and the actual transition in agreement. Cutting in `SceneRouter` instead would leave `awaitingPunishment` stuck true and `active` still set, and the next `StartSubject` would throw `"A subject attempt is already active."` |
| Life cost | One life per loss | Chosen by the product owner. The old two-attempt rule existed only to give the Punishment gate something to sit between. |
| Punishment code | Kept, unreachable | Chosen by the product owner. Recorded here as dormant: it needs a real UI before it can be re-enabled. |
| Legacy saves | Flags ignored on restore | An unreachable scene plus a save flag that routes to it is a soft-lock, since nothing can complete the punishment any more. |

## Behaviour

**Before.** First loss: no life lost, `visitAttempt` → `FinalVisit`, `awaitingPunishment`
→ true, route `Punishment`. Punishment completes → `RetrySubject`. Second loss:
`Lives--`, route `Map`, or `GameOver` when `Lives <= 1`.

**After.** Every loss: `Lives--`, `RecordFailedVisit()`, `ClearActiveSubject()`, route
`Map` — or `GameOver` when the loss empties the last life.

`RecordFailedVisit()` only increments a counter; it does not lock the subject. A player
who loses may pick the same subject again from the select screen, spending another life
per attempt. No change is needed for that — it already works that way.

## Changes

### `GameSession.RouteForResult`

```csharp
SessionRoute RouteForResult(MinigameResult result) => result.Pass
    ? SessionRoute.Map
    : Lives <= 1 ? SessionRoute.GameOver : SessionRoute.Map;
```

`RouteForResult` reads `Lives` *before* `SubmitResult` decrements it, so `Lives <= 1`
means "this loss empties the last life". That reading is already correct today and is
preserved deliberately.

### `GameSession.SubmitResult`

Delete the `visitAttempt == FirstVisit` branch. Every failure now falls through to the
existing tail: `Lives--`, `records[id].RecordFailedVisit()`, `ClearActiveSubject()`.

`visitAttempt` therefore never leaves `FirstVisit` and `awaitingPunishment` is never set
true. `PendingPunishmentSubject` is always null, `ResumeRoute()` can no longer return
`Punishment`, and `CompletePunishment()` throws if called. The fields stay so the save
format does not change.

### `GameSession.RestoreActiveAttempt`

A save written by the current build can carry `awaitingPunishment = true`. Restoring it
would make `ResumeRoute()` return `Punishment` and drop the player into the dead scene
with no way out. On restore, ignore both flags: force `awaitingPunishment = false` and
`visitAttempt = FirstVisit`. A player mid-punishment resumes at that same subject instead.

## Testing

Ten test files hold 68 references to the punishment and visit machinery, concentrated in
`GameSessionPersistenceTests` (24) and `FullGameplayFlowTests` (15). Which of them break
has not been measured yet — the implementation plan starts by running the suite and
enumerating the actual failures rather than predicting them. Every test asserting that a
first loss routes to `Punishment`, or that a first loss costs no life, is expected to fail
by design and must be rewritten to the new rule.

`PunishmentRouteTests` now covers an unreachable route. Invert it: assert that a loss
does *not* route to `Punishment`.

New tests:

- A first loss costs exactly one life and routes to `Map`.
- A loss at `Lives == 1` routes to `GameOver`.
- A save carrying `awaitingPunishment = true` restores without routing to `Punishment`.

`ChallengeSequenceTests` is unaffected — it tests a pure class that still behaves the same.

## Out of scope

The Sprint screen has a separate, real layout defect: the scoreboard, mode chip, pause
button, and countdown all collapse toward screen centre and overlap the instruction line
(`Builds/Screenshots/diag_sprint.png`). One confirmed contributor is
`SprintFestivalPresentation.cs:443`, where `EnsurePause` returns early because
`MinigameUIAssembler.EnsurePausePanel()` has already put a `PausePanel` in `MG_Sprint`,
so the Sprint pause button is never built. That points at a wider ownership collision
between the legacy shared chrome and `SprintFestivalPresentation`, whose root cause is
not yet established. It gets its own debugging pass and its own spec; no fix is specified
here, because none has been justified yet.
