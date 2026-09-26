# Football Goal Flight Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans. Execute in the current Unity workspace requested by the user; approved HTML preview is the behavioral reference.

**Goal:** Replace Football with the approved goal-facing, slider-controlled ballistic penalty game.
**Architecture:** Pure fixed-step flight simulation feeds rules, presentation and charge-only prediction. Existing campaign integration remains the result owner.
**Tech Stack:** Unity 6, C#, SpriteRenderer, UGUI Slider, NUnit, existing screenshot helper.
**Spec:** docs/superpowers/specs/2026-09-26-football-goal-flight-design.md

## Global Constraints
- Preview only during Charging; cancellation never releases a shot.
- Five kicks, three goals to pass; consistent physics at different frame rates.
- Scope is Football, its scene/art/tests/docs; preserve existing unrelated edits.

## Review Focus
- Pause/lost focus during charge hides trajectory immediately (Task 2).
- Secondary pointer cannot shoot or release another pointer (Task 2).
- Long frame cannot skip goal/keeper or commit a shot twice (Task 1).
- Actual ball uses the same prediction integrator (Tasks 1 and 3).
- Wide/tall displays keep goal, striker and controls visible (Task 3).

### Task 1: Flight and match rules
- [x] Replace obsolete Football solver tests with ballistic, collision, determinism and scoring contracts; first demonstrate charging from Aiming fails in old rules.
- [x] Add FootballFlightSimulation, migrate FootballShotSolver and FootballRules to direct SetAim(float), charge and fixed-step flight. Remove AimLocked and random drift.
- [x] Run focused EditMode checks, then full EditMode suite after scene migration.

### Task 2: Slider, hold and lifecycle
- [x] Update FootballInputBridge/Hud/Controller to Slider AimChanged; gating and cancellation tests use actual pointer events.
- [x] Keep existing result routing; show concise Vietnamese outcomes.
- [x] Validate controller completion once, pause/freezing and match restart.

### Task 3: Saved scene and presentation
- [x] Create original preview-derived SVG/PNG art with reproducible export and provenance.
- [x] Rebuild FootballSceneConfigurator with frontal art, slider left, shoot right, physics projection and preview dots in saved scene.
- [x] Run scene/presentation checks and full PlayMode suite.
- [x] Capture and inspect idle, charging and released-flight screenshots using project workflow; verify preview visibility.

### Task 4: Delivery
- [x] Review diff and completion evidence; update QA record and current docs.
- [x] Leave implementation in workspace and report precise validation/limitations.
