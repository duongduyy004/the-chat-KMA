# S3 Shared Input Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a deterministic shared input layer for Sprint, Endurance, Boss and Punishment without rewiring existing controllers or changing the four legacy punishment detector stubs.

**Architecture:** Plain C# detectors receive explicit timestamps and emit mechanic-level events; they do not read Unity clocks, devices or scenes. One `GameplayInputRouter` owns Input System/EnhancedTouch reads and feeds detectors, while `ScreenTapArea` is the only gameplay tap boundary so EventSystem UI taps cannot double-fire.

**Tech Stack:** Unity `6000.3.23f1`, C#/.NET Standard 2.1, Input System `1.20.0`, EnhancedTouch, NUnit Unity EditMode tests.

**Spec:** `docs/superpowers/specs/2026-08-27-kma-game-completion-design.md` §5 S3, decisions S3-1 and S3-2.

## Global Constraints

- Keep `TapMashDetector`, `RhythmBeatDetector`, `HoldDetector` and `AlternateTapDetector` in `KMA.Gameplay` unchanged; `ChallengeSequenceTests` asserts their exact types.
- Put real detectors in a new `KMA.Input` assembly and use the `Input` suffix in every real detector name.
- Detectors must be deterministic, timestamp-injected plain C# types; no `Time`, `Input`, `Touchscreen`, `EventSystem` or `MonoBehaviour` references in detector files.
- Apply `rhythmOffsetMs` in the router boundary, not inside `RhythmBeatInputDetector`.
- S3 creates `Assets/_Project/Settings/Input/KMA.inputactions` with maps `Sprint`, `Endurance`, `Boss`, `Punishment`, `UI`; it does not rewire Sprint or Endurance serialized actions.
- Every task must run its focused tests, then full EditMode and PlayMode suites before its own commit.
- Preserve unrelated dirty worktree changes; stage only S3 files.

## Task 1: Establish the KMA.Input assembly and action asset

**Files:**
- Create: `Assets/_Project/Scripts/Input/KMA.Input.asmdef`
- Create: `Assets/_Project/Settings/Input/KMA.inputactions`
- Create: `Assets/Tests/EditMode/Input/KMA.Input.EditMode.Tests.asmdef`
- Create: `Assets/Tests/EditMode/Input/InputAssetContractTests.cs`

**Interfaces:** Produces the `KMA.Input` assembly and a stable action-map asset with five named maps. The asset is additive; existing `SprintInputActions.inputactions` and `EnduranceInputActions.inputactions` remain untouched.

- [ ] **Step 1: Write the failing action contract tests.** Load the asset through `AssetDatabase`, assert it exists, assert exactly the five required maps, and assert the assembly does not reference gameplay stubs.
- [ ] **Step 2: Run the focused EditMode test and verify the expected red failure.** Run `rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Input.InputAssetContractTests' --output /tmp/kma-s3-asset-red.xml --timeout 600 -- -nographics`; expected failure is missing asset/assembly.
- [ ] **Step 3: Add the assembly and action asset.** Define maps `Sprint`, `Endurance`, `Boss`, `Punishment`, `UI`; include keyboard mirrors for Editor and touch-capable actions for runtime, but do not bind existing scene fields yet.
- [ ] **Step 4: Run the focused test green and commit.** Run the same filter with `/tmp/kma-s3-asset-green.xml`, then full EditMode and PlayMode. Commit `feat: add shared KMA input action asset` with only Task 1 files.

## Task 2: Implement the five deterministic detectors

**Files:**
- Create: `Assets/_Project/Scripts/Input/{TapMashInputDetector,RhythmBeatInputDetector,HoldInputDetector,AlternateTapInputDetector,SwipeInputDetector}.cs`
- Create: `Assets/Tests/EditMode/Input/DetectorContractTests.cs`

**Interfaces:**
- `TapMashInputDetector.FeedTap(double t)` updates `TapsPerSecond` and emits `OnTap`.
- `RhythmBeatInputDetector.FeedTap(double inputDsp, double beatDsp)` emits `OnJudge(TimingJudge judge, double deltaMs)` using perfect `±80ms`, good `±160ms`, miss beyond good.
- `HoldInputDetector.FeedDown(double t)` and `FeedUp(double t)` expose `ChargeRatio` clamped to `0..1`, `OnHoldStart`, and `OnHoldEnd(duration)`.
- `AlternateTapInputDetector.FeedTap(Side side, double t)` emits `OnValidTap(Side)` or `OnWrongSide`; `Side` has `Left` and `Right`.
- `SwipeInputDetector.FeedSample(Vector2 position, double t)` and `FeedEnd()` emit direction, length, duration and curvature.

- [ ] **Step 1: Write focused failing tests.** Cover rhythm exact boundaries at `80ms` and `160ms`, alternate expected-side transitions, hold duration/ratio clamp, tap-window rate and swipe metrics including a curved sample sequence.
- [ ] **Step 2: Run the detector tests red.** Run `rtk ~/.local/bin/unity test . --mode EditMode --testFilter 'KMA.Tests.Input.DetectorContractTests' --output /tmp/kma-s3-detectors-red.xml --timeout 600 -- -nographics`; expected failure is missing detector types.
- [ ] **Step 3: Implement the minimal plain classes.** Use `System.Action` events and immutable result structs where values have multiple fields; use injected `double` seconds, clamp invalid negative hold durations to zero, reject non-finite timestamps, and reset swipe samples after `FeedEnd`.
- [ ] **Step 4: Run detector tests green and refactor only under green.** Confirm event payloads and boundary behavior, then run all existing EditMode tests to prove legacy detector types still pass.
- [ ] **Step 5: Commit** `feat: add deterministic shared input detectors`.

## Task 3: Implement router and ScreenTapArea ownership

**Files:**
- Create: `Assets/_Project/Scripts/Input/GameplayInputRouter.cs`
- Create: `Assets/_Project/Scripts/Input/ScreenTapArea.cs`
- Create: `Assets/Tests/PlayMode/Input/GameplayInputRouterTests.cs`
- Create: `Assets/Tests/PlayMode/Input/KMA.Input.PlayMode.Tests.asmdef`

**Interfaces:** `GameplayInputRouter` is the single Unity-facing input reader. It exposes serialized action references/map names, `RhythmOffsetMs`, and methods for feeding detector instances in tests. `ScreenTapArea` accepts a pointer only when its gameplay region owns the event and marks the event handled before forwarding it; UI EventSystem controls never enter the gameplay feed path.

- [ ] **Step 1: Write failing PlayMode ownership tests.** Use `InputTestFixture` to prove a gameplay tap is delivered once, a tap over a UI control is not delivered to a detector, and a rhythm input is shifted by exactly `RhythmOffsetMs` before the detector receives it.
- [ ] **Step 2: Run the focused PlayMode test red.** Run `rtk ~/.local/bin/unity test . --mode PlayMode --testFilter 'KMA.Tests.Input.GameplayInputRouterTests' --output /tmp/kma-s3-router-red.xml --timeout 600 -- -nographics`; expected failure is missing router/area.
- [ ] **Step 3: Implement router ownership.** Enable EnhancedTouch only while the router is enabled, subscribe/unsubscribe exactly once, map pointer down/up/move to detector feeds, use one monotonic timestamp source at the router boundary, and apply `inputDsp + rhythmOffsetMs/1000d` only for rhythm events.
- [ ] **Step 4: Implement ScreenTapArea event gating.** Use `IPointerDownHandler`/`IPointerUpHandler`, reject events already used by EventSystem/UI, and forward only taps inside the configured gameplay area; do not read raw `Input.touches` in other gameplay components.
- [ ] **Step 5: Run focused and full suites, then commit** `feat: route gameplay input through one boundary`.

## Task 4: S3 integration verification and handoff

**Files:**
- Modify only if needed: `Assets/_Project/Scripts/Input/*.cs`, input asmdefs/tests, `README.md` input test note
- Do not modify: `Assets/_Project/Scripts/Progression/PunishmentController.cs`, `SprintController.cs`, `EnduranceInputBridge.cs`, existing `.inputactions`

**Interfaces:** S3 leaves real detector APIs ready for S6/S7/S14 adapters while all current consumers continue using their existing APIs.

- [ ] **Step 1: Run the complete focused S3 suite.** Run all `KMA.Tests.Input` EditMode and PlayMode tests and inspect XML for zero failures.
- [ ] **Step 2: Run the full project suites.** Use the pinned Unity editor for complete EditMode and PlayMode runs; expected result is no compile errors and no regression in legacy progression/input tests.
- [ ] **Step 3: Inspect the diff and contract.** Run `rtk git diff --check -- Assets/_Project/Scripts/Input Assets/Tests/EditMode/Input Assets/Tests/PlayMode/Input Assets/_Project/Settings/Input`, verify no existing controller/input asset was rewired, and confirm only the five required action maps exist.
- [ ] **Step 4: Record S3 evidence.** Add the test command/result and the deliberate non-rewire boundary to the relevant project documentation, then commit `test: verify S3 shared input layer`.

## Final S3 Gate

S3 is complete only when the five detector contracts pass their deterministic boundary tests, router ownership prevents double-fire, `rhythmOffsetMs` is applied at the router, legacy `ChallengeSequenceTests` remains green, and full EditMode/PlayMode suites pass. Android device input is deferred to S6/S7/S14 because S3 intentionally does not rewire gameplay scenes.
