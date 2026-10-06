# Campus scenery, buttons and UI fixes — QA (2026-10-06)

Spec: docs/superpowers/specs/2026-10-06-campus-ui-unification-design.md
Plan: docs/superpowers/plans/2026-10-06-campus-ui-unification.md

## Tests
- EditMode: 839/839 passed (baseline 798/801 with 3 skipped ChallengeSequenceTests; the retired Punishment tests were removed in Task 16; pre-existing failures: none)
- PlayMode: 287/287 passed (baseline 285/285)

## Before / after (16:9, captured at HEAD)
| Scene | Before | After | Notes |
|---|---|---|---|
| Menu | images/campus-before-menu.png | images/campus-after-menu.png | slanted kit buttons; settings: images/campus-after-menu-settings.png |
| Map | images/campus-before-map.png | images/campus-after-map.png | all-complete state incl. "Bài kiểm tra cuối": images/campus-after-map-complete.png |
| Sprint | images/campus-before-sprint.png | images/campus-after-sprint.png | campus above track, no ghost copy; pause: images/campus-after-sprint-pause.png |
| Sprint result (fail) | images/campus-before-sprint-result.png | images/campus-after-sprint-result.png | single headline "THẤT BẠI" above SCORE |
| Volleyball | images/campus-before-volleyball.png | images/campus-after-volleyball.png | campus band above court, skyline repeats |
| Football | images/campus-before-football.png | images/campus-after-football.png | opaque start card |
| FrogJump | images/campus-before-frogjump.png | images/campus-after-frogjump.png | campus + kit HUD; fall: images/campus-after-frogjump-fall.png |
| Chess final | images/campus-before-chess.png | images/campus-after-chess.png | opaque intro card; hint enabled: images/campus-after-chess-hint.png |
| Celebration | images/campus-before-celebration.png | images/campus-after-celebration.png | campus backdrop; cheer: images/campus-after-celebration-cheer.png |
| GameOver | images/campus-before-gameover.png | images/campus-after-gameover.png | no leftover minigame UI |

## Audit items
| # | Item | Result |
|---|---|---|
| 1 | Sprint shadowing | fixed (Task 7) |
| 2 | Football start card | fixed (Task 10): card is opaque; note the disabled "GIỮ ĐỂ SÚT" button and the direction panel sit beneath the start-card scrim (dimmed but visible) |
| 3 | Chess intro card / repeated objective | fixed (Task 11) |
| 4 | Sprint chevrons under controls, clouds on HUD | fixed (Task 7, Task 1 sky) |
| 5 | FrogJump plain HUD text | fixed (Task 9); remaining nit: ProgressCard timer text is right-aligned and the progress bar is thin |
| 6 | GameOver leftover minigame UI | fixed (Task 12) |
| 7 | Stacked result headline | not reproduced (Task 15); fail result shows the title "THẤT BẠI" above SCORE, pass shows "HOÀN THÀNH!" — one headline in both |
| 8 | Volleyball "player outside the line" | not a bug: serve position behind the baseline |
| 9 | Punishment placeholder scene | removed (Task 16) |

### Remaining visual issues (not fixed in this task)
- Volleyball: the skyline sprite tiles about 3 times across the narrow band above the court, so the main building appears several times.
- English text still visible: "1st", "COMBO", "PLAYER" tag in Sprint; "PLAYER : ENEMY" on the Volleyball scoreboard; "SCORE" label on the result panel; "GAME OVER" in the GameOver logo art.
- Football: with the start card up, the disabled "GIỮ ĐỂ SÚT" button is sitting under the scrim (expected, but low contrast).
- Chess: the lower band is a teal ground rather than grass (consistent, not dark).

## 20:9
Not captured: tooling has no resolution option (PlayModeScreenshot.cs captures the Game view as-is and cannot set 2400x1080 headlessly). Remaining check for a device or a manually sized Game view.
