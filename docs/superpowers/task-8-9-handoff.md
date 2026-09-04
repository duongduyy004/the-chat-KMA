# Tasks 8–9 Handoff

- Plan: `docs/superpowers/plans/2026-09-02-s1-s9-stabilization.md`
- Recovery ledger: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/progress.md`
- Recovery worktree: `D:\the-chat-KMA\.worktrees\s1-s9-stabilization-recovery`
- Branch: `codex/s1-s9-stabilization-recovery`
- Accepted implementation through Task 7: `b60b2afe8aca4a272cf909d883928ec59d68b5ab`; start from the latest pushed tip of this branch (or the identical `master` tip if the final fast-forward succeeds).
- Status: Tasks 1–7 complete; Tasks 8–9 not started.

Task 7 fix round 1 added automatic preview invalidation after launch/source destruction. Genuine RED was PlayMode `0/2`; GREEN was `2/2`, preview EditMode `9/9`, BallRig PlayMode `7/7`; full EditMode `253/253` and PlayMode `149/149` passed. Scoped independent re-review: **Approved**, with no findings.

Next action: start Task 8 only. Read its relevant plan section, dispatch a fresh implementer, use genuine TDD for new behavior, run focused RED/GREEN before required broader verification, complete each required independent review gate, fix blocking findings, and update the ledger immediately. Then follow the same process for Task 9 and the plan's final whole-branch verification/review.

Known rulings and pitfalls: preserve the warmed recovery `Library/`; do not create another worktree or reconstruct Tasks 1–7. Long Unity compiles are not necessarily hung. A prior NUnit failure was a transient Unity Licensing/Package Manager registration failure, not missing imports; inspect licensing/package logs before changing code. Keep durable XML/log evidence. The historical S8 branch ref is absent, so the approved Task 7 implementation and current `BallRig` interfaces are authoritative. Retain the recorded Unity Android API-25 floor ruling and leave final README count reconciliation to Task 9.
