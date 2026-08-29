# SDD ledger — plan: docs/superpowers/plans/2026-08-28-s2-presentation-foundation.md

## Preflight conflict scan

| Scope | Shared file/interface | Finding | Ruling |
|---|---|---|---|
| Task 1 → Task 2 | `KMA.Gameplay.UI.asmdef`, `UITheme`, `ITutorialSeenStore` | Task 2 consumes the assembly/theme/store created by Task 1; no conflicting ownership. | Keep Task 1 as the provider and Task 2 as consumer. |
| Task 2 → Task 3 | `MinigameHudState`, `MinigameHUD` | Task 2 defines the HUD data contract; Task 3 renders it and must not redefine fields. | Keep the struct in the gameplay/common-facing contract and make UI pull-only. |
| Task 2 → Task 4 | `MinigameLifecycle.PhaseChanged`, `MinigameBase` | Task 4 subscribes to the event and uses serialized lifecycle durations from Task 2. | Preserve existing transition timing and emit exactly once per transition. |
| Task 3 → Task 4 | `BrutalButton`, `SafeAreaFitter`, UI prefabs | Task 4 composes Task 3 components; no source file needs dual ownership. | Task 4 may bind prefabs but must not fork component behavior. |
| Task 4 → Task 5 | `PhaseOverlay`, `ResultPanel`, `HUD_Minigame` | Task 5 places completed prefabs in scenes and supplies serialized controller references. | Scene-owned UI is authoritative because `SceneRouter` uses `LoadSceneMode.Single`. |
| Task 5 → Task 6 | six scenes, `GameCamera`, Sprint bindings | Task 6 depends on the scene shell and tests the real Sprint scene. | Do not start Task 6 until the six-scene contract test is green. |
| Task 1 | theme/font/store files | Tests require assets and types that do not exist initially; the red phase is valid. | Implement exact palette/glyph contract before downstream UI. |
| Task 2 | Sprint HUD example | The plan's code example uses actual fields `SprintRules.Elapsed`, `Stamina`, and `Snapshot.Distance`; `TimeRemaining` is derived as `14f - Elapsed`. | Use additive controller adapters; do not change rules signatures. |
| Task 3 | `OnGUI` compatibility | Existing `GameplayPresentation` is a legacy compatibility shim; S2 must not leave duplicate production rendering. | Keep it only where existing tests require it, and disable its drawing when S2 Canvas exists. |
| Task 4 | tutorial persistence versus S4 | S2 needs per-subject seen state before S4's full SaveSystem exists. | Use the plan's narrow `ITutorialSeenStore` plus PlayerPrefs adapter; S4 may replace the backend without changing overlay API. |
| Task 5/6 | generated Unity assets | Unity may generate unrelated `.meta`, `.utmp`, ShaderGraph, or crash files. | Stage only deliberate S2 files; preserve or move generated noise, never wholesale-stage it. |

## Rulings

- Ruling: execute directly on `master` because the user explicitly declined creating a worktree — cost if wrong: S2 commits share the current branch and must be reverted/cherry-picked manually.
- Ruling: use the existing Unity 6000.3.23f1 and S1 API25/ABI constraints as environmental facts — cost if wrong: the S2 gate may differ on another editor/device.
- Ruling: keep one canonical UITheme asset under Settings/UI and use AssetDatabase in tests — the plan's Resources.Load example conflicts with its canonical asset path; cost if wrong: runtime resource loading would need a second duplicate asset.

## Task status
- Task 1 round 1 fix: corrected exact Color32 palette assertions and canonical `MutedForeground` RGBA; added deterministic 4096-atlas generation plus serialized `VietnameseFallback` coverage for the required Vietnamese glyphs. Focused UITheme tests 3/3 passed; full EditMode suite passed.
- Task 1: fix round 1/5 (3 addressed, 0 open): dynamic Vietnamese fallback preserved, full requested Unicode ranges tested through the serialized chain, and deterministic sorted stable-text scanning enforced.
- Task 1: fix round 1/5 (3 addressed, 0 open; commits c38bd80..676114d; scoped re-review 01a0492f-ac13-7dc2-921a-5ea6cedb3fe1 PASS).
- Task 1: complete (commits 7e77489..676114d, review clean).
- Task 2: complete (commits c505098..6a3214b, review clean after fixes, static re-review PASS, Unity not run in final review).
- Task 3: BASE 6a3214b (`.superpowers/sdd/2026-08-28-s2-presentation-foundation/task-3-brief.md`).
