# Campus scenery, buttons and UI fixes — QA (2026-10-06)

Spec: docs/superpowers/specs/2026-10-06-campus-ui-unification-design.md
Plan: docs/superpowers/plans/2026-10-06-campus-ui-unification.md

## Tests
- EditMode: 839/839 passed (baseline 798/801 with 3 skipped ChallengeSequenceTests; the retired Punishment tests were removed in Task 16; pre-existing failures: none)
- PlayMode: 287/287 passed (baseline 285/285)
- After the final fix wave: EditMode 854/854, PlayMode 289/289 passed

## Before / after (16:9, captured at HEAD)
| Scene | Before | After | Notes |
|---|---|---|---|
| Menu | images/campus-before-menu.png | images/campus-after-menu.png | slanted kit buttons; settings: images/campus-after-menu-settings.png |
| Map | images/campus-before-map.png | images/campus-after-map.png | all-complete state incl. "Bài kiểm tra cuối": images/campus-after-map-complete.png |
| Sprint | images/campus-before-sprint.png | images/campus-after-sprint.png | campus above track, no ghost copy; pause: images/campus-after-sprint-pause.png |
| Sprint result (fail) | images/campus-before-sprint-result.png | images/campus-after-sprint-result.png | single headline "THẤT BẠI" above the score caption (now "ĐIỂM", see final fix wave) |
| Volleyball | images/campus-before-volleyball.png | images/campus-after-volleyball.png | campus band above court, skyline repeats |
| Football | images/campus-before-football.png | images/campus-after-football.png | compact opaque start card (850×400) between the score chip and the kicker; kicker fully visible (final fix wave) |
| Volleyball result (pass) | — | images/campus-after-volleyball-result-pass.png | "CHIẾN THẮNG", caption "ĐIỂM", one headline |
| Volleyball result (fail) | — | images/campus-after-volleyball-result-fail.png | "THẤT BẠI", caption "ĐIỂM", one headline |
| Football result (pass) | — | images/campus-after-football-result-pass.png | "CHIẾN THẮNG"; the forced QA result is drawn over the start card, whose text shows faintly through the 92% card (harness artifact: in play the start card is gone before the result) |
| Football result (fail) | — | images/campus-after-football-result-fail.png | "THẤT BẠI"; same harness note as the pass capture |
| FrogJump | images/campus-before-frogjump.png | images/campus-after-frogjump.png | campus + kit HUD; fall: images/campus-after-frogjump-fall.png |
| Chess final | images/campus-before-chess.png | images/campus-after-chess.png | opaque intro card; hint enabled: images/campus-after-chess-hint.png |
| Celebration | images/campus-before-celebration.png | images/campus-after-celebration.png | campus backdrop; cheer: images/campus-after-celebration-cheer.png |
| GameOver | images/campus-before-gameover.png | images/campus-after-gameover.png | no leftover minigame UI |

## Audit items
| # | Item | Result |
|---|---|---|
| 1 | Sprint shadowing | fixed (Task 7) |
| 2 | Football start card | fixed (Task 10, final fix wave): card is opaque, 850×400 and raised (y +200) so it no longer covers the kicker; the disabled "GIỮ ĐỂ SÚT" button and the direction panel sit beneath the start-card scrim (dimmed but visible) |
| 3 | Chess intro card / repeated objective | fixed (Task 11) |
| 4 | Sprint chevrons under controls, clouds on HUD | fixed (Task 7, Task 1 sky) |
| 5 | FrogJump plain HUD text | fixed (Task 9); remaining nit: ProgressCard timer text is right-aligned and the progress bar is thin |
| 6 | GameOver leftover minigame UI | fixed (Task 12) |
| 7 | Stacked result headline | not reproduced (Task 15); fail result shows the title "THẤT BẠI" above SCORE, pass shows "HOÀN THÀNH!" — one headline in both |
| 8 | Volleyball "player outside the line" | not a bug: serve position behind the baseline |
| 9 | Punishment placeholder scene | removed (Task 16) |
| 10 | English text on screen | fixed (final fix wave): Sprint "HẠNG 1..4", "CHUỖI ×n", "BẠN" tag, countdown "CHẠY!"; Volleyball "BẠN : ĐỐI THỦ", timing "HOÀN HẢO/TỐT/SỚM/MUỘN"; result caption "ĐIỂM". Left as is: "GAME OVER" logo art (by design) |
| 11 | Disabled "TIẾP TỤC BÀI HỌC" unreadable | fixed (final fix wave): DisabledText on DisabledSurface at full opacity (no 45% fade) |

### Remaining visual issues (not fixed in this task)
- Volleyball: the skyline sprite tiles about 3 times across the narrow band above the court, so the main building appears several times.
- English text: only the "GAME OVER" logo art remains (intentional).
- Football: with the start card up, the disabled "GIỮ ĐỂ SÚT" button is sitting under the scrim (expected, but low contrast).
- Chess: the lower band is a teal ground rather than grass (consistent, not dark).

## 20:9 (2400×1080)
Captured with `tools/qa-screenshot.sh <out> <scene> 5 false -1 "" false "" 0 false "" 2400x1080` (new `gameViewSize` option). Every capture was checked for a dark band at either edge and for clipped HUD.

| Scene | Capture | Verdict |
|---|---|---|
| Menu | images/campus-after-20x9-menu.png | full-bleed illustration, no band; buttons and logo intact |
| Map | images/campus-after-20x9-map.png | four stops and lesson panel fit; header and lives chip unclipped |
| Sprint | images/campus-after-20x9-sprint.png | skyline spans the width; scoreboard "HẠNG 1 / CHUỖI ×0", "BẠN" tag; controls in corners |
| Volleyball | images/campus-after-20x9-volleyball.png | skyline tiles to both edges; "BẠN 0 : 0 ĐỐI THỦ" scoreboard |
| Football | images/campus-after-20x9-football.png | goal-view art covers the width; compact start card, kicker visible |
| FrogJump | images/campus-after-20x9-frogjump.png | sky/skyline/ground reach both edges (the red track strip is narrower than the view, as authored) |
| Chess final | images/campus-after-20x9-chessfinal.png | campus backdrop to both edges; board, side cards and intro card unclipped |
| Celebration | images/campus-after-20x9-celebration.png | campus backdrop to both edges; "Bỏ qua" unclipped |
| GameOver | images/campus-after-20x9-gameover.png | illustration envelopes the screen (AspectRatioFitter), no band |
