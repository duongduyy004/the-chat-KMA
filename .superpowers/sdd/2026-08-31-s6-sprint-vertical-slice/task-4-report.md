# Task 4 - Sprint rivals and parallax presentation

## Fix round 3

- Implementation commit: 5ccdf38fec927e74939289a7f32bbc174fcce433
- Subject: fix: complete sprint parallax and rival motions

### Resolved review findings

- `SprintParallax.Layer.Scroll` still moves both tiles by the same distance delta, but now recycles only the leftmost tile after it has crossed the authored `-loopWidth` offscreen threshold. The paired tile remains in place until it independently becomes offscreen, preserving one-width spacing and coverage.
- `SprintControllerTests` adds a regression with a 10m small scroll that leaves tiles at `-1.5` and `24.1`, followed by a 200m refresh that recycles only the offscreen tile to `21.2` while its partner remains at `-4.4`.
- Every RivalRunner Animator state now references a distinct, non-empty `.anim` clip. Each clip animates the root transform's local Y position for an observable idle, run, burst, stumble, celebrate, or fail visual response.
- The scene contract now verifies all three rivals remain instances of `RivalRunner.prefab`, and uses `UnityEditor.Animations.AnimatorController` plus `AnimationUtility` to require exactly the six named states, an `AnimationClip` motion per state, positive clip duration, and a local-transform curve.

### Static verification

- `rtk git diff --cached --check` passed before committing the implementation.
- The staged implementation contained only the Task 4 parallax source, RivalRunner controller and six clip assets, focused PlayMode tests, and their test assembly reference.
- Controller GUIDs were cross-checked against the six committed clip meta assets; every required state has a non-zero Motion reference and every clip contains an `m_LocalPosition.y` curve.

### Verification limit

- No Unity, network, or device command was run in this static-only pass. Unity import, C# compilation, PlayMode execution, and visual playback remain unverified.
- Unrelated dirty files remain preserved: `README.md` and the untracked S5/S6 plan files.

## Implementation

- Commit: 5da94ab634fce12fa692592dbff9b0daf2e0330e
- Subject: feat: add sprint rivals and parallax presentation

## Static evidence

- rtk git diff --check passed before staging.
- rtk git diff --cached --check passed before the implementation commit.
- Static scene/source inspection found three prefab-backed rival instances in lanes 1, 3 and 4, each inheriting a SpriteRenderer and six-state RivalRunner Animator Controller.
- Parallax has three serialized layers, six renderer-backed tiles, valid first/second bindings, 2560 x 1080 coverage, and one-tile recycling driven by distance deltas without object creation.
- The focused tests assert rival visuals/controllers, player x=35% placement, parallax bindings/renderers/recycling spacing, 70 percent burst state, unchanged rival distances, unchanged rank and unchanged pass/score result.

## Scope recorded

- Authored lane 1, lane 3, and lane 4 pace profiles.
- RivalRunnerAI state and visual-position adapter.
- SprintParallax declaration with three layers and 2560 x 1080 authored coverage.
- RivalRunner and SprintLane prefabs.
- MG_Sprint lane/player placement and focused scene/controller assertions.

## Verification limits

- No Unity test was run in this finalization pass, per user instruction.
- Runtime Unity compilation, Animator asset import, scene loading, and visual validation remain unverified.
- Unrelated dirty files were preserved: README.md and the untracked S5/S6 plan files.
