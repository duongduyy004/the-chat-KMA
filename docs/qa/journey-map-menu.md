# Journey-map subject menu QA — 2026-10-05

The `Map` scene now presents the three subjects as a winding journey: circular stops (Chạy nước rút → Bóng chuyền → Bóng đá) on a curved road that ends in a chequered flag, above a shorter lesson panel. Header title stays `CHỌN MÔN THI` (no subtitle). The panel heading is the subject name only. Unlock rules, routes, saves and the lesson/attempt flow are unchanged; Volleyball and Football still unlock only after the previous subject's exam.

Each stop always shows its number, sport icon, name, three stars (from `MapNodeView.Stars`) and a status pill (`SẴN SÀNG`, `HOÀN THÀNH`, `CHƯA MỞ KHÓA`). The current stop (first unlocked, not yet passed) is larger, ringed in gold and tagged `ĐANG Ở ĐÂY`; locked stops are grey with a lock icon. The road is dotted until a subject is passed, then solid gold up to the next stop.

Spec: `docs/superpowers/specs/2026-10-05-journey-map-subject-menu-design.md`. Plan: `docs/superpowers/plans/2026-10-05-journey-map-subject-menu.md`.

## Automated evidence

| Check | Passed | Failed | Ignored | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full EditMode | 662 | 0 | 3 | `Builds/TestResults/rev-full.xml` |
| Full PlayMode | 259 | 0 | 0 | `Builds/TestResults/rev-play.xml` |

The EditMode XML root reports `Skipped:Ignored` because of the three existing ignored `ChallengeSequenceTests`; `tools/run-unity-tests.sh` therefore exits 1 even though every case passed or was ignored. `ShellSceneAuthoring.Validate` was run after the final re-author (`Validated Map: 386 objects, no transient sprites`).

New geometry coverage: stops stay inside the map zone and clear of the lesson panel at 1280, 1440, 1728, 1920 and 2400 px wide (EditMode, canvas 1080 high — this is the real aspect-ratio coverage). The PlayMode scene tests call `Screen.SetResolution`, which does not resize the Editor Game view, so they check the authored scene only at the Editor's default Game-view size, not at five sizes. No stop label is truncated; stars never overlap the status text; status text and sport icons keep readable contrast; the road is built once, is dotted at zero progress and gold only on passed legs.

## Visual evidence

Captured from the Editor's Game view with a temporary in-memory progress probe (removed, never committed). Every PNG was opened and inspected.

| PNG under `docs/qa/images/` | Dimensions | State |
| --- | --- | --- |
| `journey-map-fresh.png` | 1082×533 | Sprint current; Volleyball and Football locked |
| `journey-map-sprint-done.png` | 1082×533 | Sprint passed (2 stars); Volleyball current |
| `journey-map-volleyball-done.png` | 1082×533 | Two subjects passed; Football current with the flag |
| `journey-map-complete.png` | 1082×533 | Course complete; no current stop; summary strip under the header |

Two states were captured mid-way through the lesson-card reveal animation, so one lesson card may look faded.

## Not verified

- 1920×1080 and 4:3 screenshots: the Game view used for capture was 1082×533, so the 1280-wide and other EditMode cases have no image. Layout at the other sizes is covered by the EditMode geometry tests above, not by images.
- Physical touch, Android safe areas and GPU behaviour, and APK export.
- The preview HTML showed all three subjects open at the start; the game really starts with two locked, so the real fresh state differs from that preview by design.
- Locked lesson-card colours were left as they were (`LockedLessonCardsStayReadable` passes at contrast ≥ 4.5).
