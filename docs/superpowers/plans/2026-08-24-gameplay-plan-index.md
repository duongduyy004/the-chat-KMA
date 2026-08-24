# KMA Gameplay Plan Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the seven normalized-score minigames, shared gameplay contracts, recovery challenges, and final boss described by `PLAN.md`.

**Architecture:** Build pure, deterministic C# domain models first, then attach thin Unity `MonoBehaviour` presentation adapters. Split execution into four independently reviewable plans ordered by dependency; each plan leaves a playable or testable vertical slice.

**Tech Stack:** Unity 6.3 LTS, C#/.NET Standard 2.1, Input System + EnhancedTouch, NUnit EditMode tests, Unity PlayMode tests, Physics2D.

**Spec:** `PLAN.md`

## Global Constraints

- Pin one Unity `6.3.x` patch in `ProjectSettings/ProjectVersion.txt`; all workers use that exact patch.
- Android landscape only; Canvas reference resolution is `1920x1080`.
- Every subject has one `PrimaryObjective`; bonus mechanics never set `Pass = true`.
- Every subject returns `Score` in `0..10`, rounded to one decimal: objective `6`, accuracy `0..2`, efficiency `0..1`, mastery `0..1`.
- Rank thresholds are `S >= 9`, `A >= 8`, `B >= 7`, `C >= 6`, `D >= 5`, otherwise `F`.
- Any adverse gameplay event must expose a visual/audio cue and a deterministic counterplay window.
- Gameplay models do not call `UnityEngine.Random`; variation comes from authored ScriptableObject patterns selected before play.
- Run shell commands through `rtk`; set `KMA_UNITY_EDITOR` to the exact pinned Unity executable before CLI test steps.

---

## Execution order

| Order | Plan | Produces | Depends on |
|---|---|---|---|
| 1 | `2026-08-24-gameplay-foundation.md` | Result/score contracts, timing evaluators, minigame lifecycle, shared test helpers | Unity project only |
| 2 | `2026-08-24-running-minigames.md` | Playable Sprint and Endurance models/controllers | Foundation |
| 3 | `2026-08-24-ball-minigames.md` | BallRig boundary and Volleyball, Basketball, PingPong, Badminton, Football rules | Foundation |
| 4 | `2026-08-24-progression-boss.md` | Lives/attempts, recovery challenge, save-facing records, boss phases, end-to-end flow | All gameplay plans |

### Task 1: Establish execution baseline

**Files:**
- Create through Unity Hub: `Assets/`, `Packages/`, `ProjectSettings/`
- Create: `.gitignore`
- Create: `.gitattributes`
- Verify: `ProjectSettings/ProjectVersion.txt`

**Interfaces:**
- Consumes: installed Unity 6.3 LTS editor with Android Build Support.
- Produces: a version-pinned Universal 2D project accepted by every child plan.

- [ ] **Step 1: Create the Universal 2D project in this directory**

In Unity Hub select `Universal 2D`, location `/home/duongduy/data/project/the-chat-KMA`, and the chosen `6.3.x` patch. Do not create a second nested project directory.

- [ ] **Step 2: Verify the generated project root**

Run: `rtk test -f ProjectSettings/ProjectVersion.txt && rtk test -f Packages/manifest.json`

Expected: exit `0`; both files exist directly under the repository root.

- [ ] **Step 3: Pin editor and serialization settings**

Set `ProjectSettings/EditorSettings.asset` to `m_SerializationMode: 2` (Force Text) and keep the exact generated editor version in `ProjectSettings/ProjectVersion.txt`.

- [ ] **Step 4: Initialize version control**

Run: `rtk git init && rtk git add PLAN.md Assets Packages ProjectSettings .gitignore .gitattributes docs && rtk git commit -m "chore: bootstrap Unity gameplay project"`

Expected: one root commit containing the pinned project and all plan documents.

### Task 2: Execute child plans in dependency order

**Files:**
- Read: `docs/superpowers/plans/2026-08-24-gameplay-foundation.md`
- Read: `docs/superpowers/plans/2026-08-24-running-minigames.md`
- Read: `docs/superpowers/plans/2026-08-24-ball-minigames.md`
- Read: `docs/superpowers/plans/2026-08-24-progression-boss.md`

**Interfaces:**
- Consumes: the baseline from Task 1.
- Produces: complete gameplay loop with seven normalized-score subjects and boss.

- [ ] **Step 1: Execute foundation plan and run its full EditMode suite**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults-foundation.xml -quit`

Expected: exit `0`, no failed test cases.

- [ ] **Step 2: Execute running plan and verify both scene smoke tests**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter KMA.Tests.Running -testResults TestResults-running.xml -quit`

Expected: Sprint and Endurance PlayMode fixtures pass.

- [ ] **Step 3: Execute ball plan and verify five rule engines**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter KMA.Tests.Ball -testResults TestResults-ball.xml -quit`

Expected: Volleyball, Basketball, PingPong, Badminton, and Football fixtures pass.

- [ ] **Step 4: Execute progression plan and verify the complete loop**

Run: `rtk proxy "$KMA_UNITY_EDITOR" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter KMA.Tests.Progression -testResults TestResults-progression.xml -quit`

Expected: first failure, recovery, second attempt, life loss, seven passes, boss unlock, and boss completion all pass.

- [ ] **Step 5: Commit the integrated gameplay suite**

Run: `rtk git add Assets Packages ProjectSettings && rtk git commit -m "feat: complete normalized KMA gameplay suite"`

