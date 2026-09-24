# SDD ledger — plan: docs/superpowers/plans/2026-08-24-progression-retired-route.md

## Preflight

| Scope | Producer | Consumer | Finding | Ruling |
|---|---|---|---|---|
| Result/score | Foundation + Running/Ball | Session records and retired-route gate | All subject results use finite normalized `MinigameResult` | Progression stores canonical results; combo/mastery never replaces pass. |
| Lifecycle | Foundation | Controllers/session flow | Shared exactly-once lifecycle is reviewed PASS | Session flow must react to one result event only. |
| Subject records | Running/Ball plans | retired-route unlock | Seven subject records are authored across plans | retired-route gate derives from records, not UI/combo state. |
| Workspace | User ruling | All Progression tasks | User explicitly declined worktree | Work directly on current `master`; do not create a worktree. |
| Engine | Index plan | All Unity tasks | Project pinned to Unity `6000.3.22f1` | Keep project version unchanged. |

## Task self-check

| Task | Internal consistency check |
|---|---|
| Progression 1 | First failure routes authored punishment, active starts are guarded, second failure loses life, and retired-route unlock derives from seven canonical records; final review PASS. |
| Progression 2 | Punishment order/completion is authored, finite-safe, subject-bound, and one-shot; final review PASS. |
| Progression 3 | retired-route gate, runtime session handoff, real keyboard input, and fixed TapMash → RhythmHold → AlternateTap sequence are explicit; final review PASS. |
| Progression 4 | End-to-end retry, life loss, seven passes, and retired-route flow are covered; production routes now use persistent scene-loaded controller binding, authored build-enabled route scenes, and a live punishment-input retry bridge. |

## Current status

- Foundation, Running, and Ball plans are reviewed PASS through `93ccf58`.
- Progression Task 1: complete and reviewed PASS through commit `50ea483`.
- Progression Task 2: complete and reviewed PASS through commit `68ee3d6`.
- Progression Task 3: complete and reviewed PASS through commit `53456ab`.
- Progression Task 4: corrected on master; persistent SceneRouter binding, authored Map/Punishment/GameOver scenes, explicit Sprint/Endurance-only subject routes, live keyboard/touch punishment completion to retry, and the live retired-route-to-map completion path are covered by `FullGameplayFlowTests` (5/5), `PunishmentRouteTests` (1/1), `retired-routePhaseControllerTests` (10/10), `GameSessionTests` (9/9), and full EditMode (121/121).
- Next: no remaining tasks in the 2026-08-24 progression/retired-route plan.
