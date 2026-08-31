# Task 1 report: Endurance input, mode exclusivity, and DSP contracts

## Scope completed

- Preserved the pre-existing Task 1 test-contract commit `691aba0` (`test: define endurance input and dsp contract`).
- Kept `EnduranceInputBridgeTests` in `Assets/Tests/PlayMode/Gameplay/Running/`, whose asmdef references `KMA.Gameplay.Endurance` and has the required `InternalsVisibleTo` access.
- Added the minimum production seam, `EnduranceInputBridge.ConfigureDetectorsForTest`, to attach a supplied controller to rhythm, hold, and swipe detectors without Android input actions.
- The seam routes rhythm detector deltas through `EnduranceController.Tap`; this preserves the controller as the sole place that applies `RhythmOffsetMs`, so the offset is applied once.
- Hold completion forwards the detector's clamped `ChargeRatio`; swipe results route only vertical directions to the Endurance `Up` and `Down` directions.
- Detector subscriptions are removed on bridge disable and destruction to prevent stale or duplicate event delivery.
- Tightened the mode-exclusivity bridge test so its hold assertion starts from a `Good` tap at 98 stamina and observes stamina recovery, rather than attempting to raise already-full stamina.

## Contracts covered

- Controller rules remain the mode gate: taps mutate judgment only in `RhythmTap`, holds mutate stamina only in `BreathHold`, and vertical swipes mutate obstacle state only in `ObstacleSwipe`.
- Existing detector tests retain inclusive `Perfect` at plus/minus 80 ms, inclusive `Good` at plus/minus 160 ms, and `Miss` outside that window.
- Existing controller tests cover DSP pause idempotence, stable paused beat time, no replacement metronome, and continuation from the paused beat.
- Existing detector tests cover clamped hold charge, one end event per press, and vertical swipe direction detection/reset behavior.

## Verification

`rtk git diff --check` completed with exit code 0.

The required Unity batch commands could not start because `/home/duydt/Unity/Hub/Editor/6000.3.23f1/Editor/Unity` is absent in this environment:

```text
EditMode: exit 127 -- [rtk: No such file or directory (os error 2)]
PlayMode: exit 127 -- [rtk: No such file or directory (os error 2)]
```

No Android device was required or used.

## Concern

Run the two focused Unity commands from the task brief on a machine with Unity 6000.3.23f1 installed before relying on runtime test results.
