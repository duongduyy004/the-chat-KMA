# The Chat KMA Functional MVP Plan Suite

> **For agentic workers:** Execute the plans below in order. Every plan requires `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans`; do not start a later plan until the previous plan's full-suite gate passes.

**Goal:** Deliver the Functional Android MVP approved in `docs/superpowers/specs/2026-08-25-functional-mvp-design.md` through nine working checkpoints.

**Spec:** `docs/superpowers/specs/2026-08-25-functional-mvp-design.md`

## Ordered plans

| Order | Plan | Working checkpoint |
| --- | --- | --- |
| 1 | `2026-08-25-product-shell-sprint.md` | New Game/Continue -> Map -> Sprint -> Result/Punishment with save |
| 2 | `2026-08-25-endurance-punishment-integration.md` | Endurance and Punishment use production touch/HUD/route flow |
| 3 | `2026-08-25-pingpong-vertical-slice.md` | First complete ball-sport route |
| 4 | `2026-08-25-basketball-vertical-slice.md` | Pass/alley-oop/apex loop |
| 5 | `2026-08-25-volleyball-vertical-slice.md` | Dig/set/spike rally loop |
| 6 | `2026-08-25-badminton-vertical-slice.md` | Hold/release shuttle loop |
| 7 | `2026-08-25-football-vertical-slice.md` | Five-kick goalkeeper loop |
| 8 | `2026-08-25-boss-endgame-integration.md` | Boss, Victory, and GameOver complete the campaign |
| 9 | `2026-08-25-android-hardening.md` | Validated APK, install/launch/device report |

## Suite-wide gates

- Use Unity `6000.3.22f1`; run shell commands through `rtk`.
- Preserve `PLAN.md` mechanics, deterministic rules, normalized scoring, and one completion event.
- Touch is production input; keyboard mirrors it for Editor/test.
- Keep all earlier EditMode and PlayMode tests green at every checkpoint.
- Commit only the files named by the current task; preserve unrelated and untracked user files.
- A rule-only test is not evidence that a minigame is playable.

