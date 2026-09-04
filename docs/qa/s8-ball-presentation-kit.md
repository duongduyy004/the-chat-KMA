# S8 Ball Presentation Kit QA

Verified on 2026-09-04 with Unity `6000.3.23f1` in the stabilized recovery worktree.

## Contract and ownership

`BallRig` and `Ballistics` remain the physics authority. The only `BallRig` change is an additive, read-only prospective `PredictLandingPoint(direction, force, curvature)` overload. `TrajectoryPreview.Refresh` uses that seam for its endpoint and only uses `Ballistics.AdvanceVelocity` for intermediate display samples. `BallShadow` reads target height and changes only its cached shadow transform and renderer.

The refresh paths contain no `Instantiate`, `Destroy`, material creation, or component lookup. `TrajectoryPreview` allocates its 16-point array during configuration/awake and reuses it. The focused repeated-refresh test verifies that the same line object and child count survive repeated calls, while the immutability test verifies unchanged ball position, velocity, and snapshot.

## Authored profiles

| Profile | Gravity | Drag | Ground | Bounce |
|---|---:|---:|---:|---:|
| Volleyball | `1.00` | `0.05` | `0.00` | `0.75` |
| Basketball | `1.00` | `0.02` | `0.00` | `0.80` |
| PingPong | `0.85` | `0.08` | `0.00` | `0.65` |
| Shuttle | `0.90` | `4.00` | `0.00` | `0.00` |
| Football | `1.10` | `0.03` | `0.00` | `0.60` |

`FlightProfileTests` loads all five assets by production path, checks these exact values and finite/range invariants, and separately verifies Shuttle drag exceeds Volleyball drag with zero bounce.

## Prefab inspection

`Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab` was authored through Unity APIs. It is inactive for later subject-controller wiring and contains exactly one each of:

- `BallRig` with a serialized `Rigidbody2D` reference;
- `TrajectoryPreview` with its serialized source and `LineRenderer` references;
- `BallShadow` with serialized target, shadow transform, and `SpriteRenderer` references;
- dashed unlit preview material/texture and soft shadow sprite.

The prefab test recursively reports zero missing scripts, rejects any subject-specific `MonoBehaviour`, checks every reference above, and verifies the line material/texture and shadow sprite load through Unity.

## TDD and automated evidence

All commands used `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` and the absolute stabilized worktree path. Unity test runs omit a trailing `-quit` because this repository's test-runner wrapper lets the Unity Test Framework write XML and terminate the process itself.

- Initial RED: focused EditMode compile with `-testFilter TrajectoryPreviewTests`; exit `1`, `CS0246` because `TrajectoryPreview` did not exist. Log: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-red-edit.log`.
- Prefab integrity RED: `-runTests -testPlatform EditMode -testFilter TrajectoryPreviewTests`; `8/9` passed and the prefab contract failed on the null serialized `BallRig.body` reference. XML: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-prefab-body-red.xml`.
- Focused preview/shadow/prefab GREEN: same focused command; `9/9` passed. XML: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-focused-preview-final.xml`.
- Focused profiles GREEN: `-runTests -testPlatform EditMode -testFilter FlightProfileTests`; `6/6` passed. XML: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-focused-profiles.xml`.
- Focused BallRig regression: `-runTests -testPlatform PlayMode -testFilter BallRigTests`; `7/7` passed. XML: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-focused-ballrig.xml`.
- Full EditMode: `-runTests -testPlatform EditMode`; `253/253` passed. XML: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-full-editmode.xml`.
- Full PlayMode: `-runTests -testPlatform PlayMode`; `147/147` passed. XML: `.superpowers/sdd/2026-09-02-s1-s9-stabilization/task-7-full-playmode.xml`.

The XML/log files above existed and were inspected for the recorded totals. Disposable copies created at the worktree root were cleaned after inspection; they are not part of the product commit.

The endpoint, two drag directions, release/launch hiding, shadow ground/midpoint/clamped-high mapping, and prefab integrity are covered in headless Unity tests. No device or manual visual-performance claim is made by this S8 gate.

## Handoff

Task 8 and later subject integrations own controller/input/rules wiring: configure the shared preview and shadow against the subject's ball, refresh during drag/height changes, and disable the preview on release. This S8 evidence does not claim subject-scene completion, device completion, S9-S16 completion, or whole-game Definition of Done.
