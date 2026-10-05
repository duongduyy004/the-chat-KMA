# Frog jump penalty and life regen - QA evidence

Captured with `tools/qa-screenshot.sh` against a background GUI Editor (Unity 6000.3.23f1). The states that need a
session setup use the editor-only `qaState` argument (`Assets/Editor/PlayModeScreenshotQaStates.cs`), which calls the
same public seams production uses (`ResultPanel.ShowFrogJump/ShowChallenge`, `FrogJumpRules.Stop`, a throwaway
`GameSession` for the Map). Nothing is persisted. Game view is the Editor default, about 1082x504 landscape.

## Test runs (batch mode, Editor closed)

- EditMode: 713 total, 710 passed, 0 failed, 3 skipped (pre-existing ignores).
- PlayMode: 265 total, 265 passed, 0 failed.
- Leftover grep (`AwaitingSupplementary|PracticeCurrentSubject|ChallengeAttemptMode.Supplementary`): only
  `SupplementaryModeIsAlwaysRejected` (FrogJumpPenaltyTests.cs:138) and the migration test (JourneyProgressTests.cs:119).
  The enum member itself is declared as `Supplementary`, not matched by the pattern.

## Screenshots

| Image | Checked | Result |
|---|---|---|
| frog-jump-1-tutorial.png | hint chip `Chạm khi kim ở giữa để bật xa. Sát mép là ngã!` above the power bar, diacritics | pass; chip text intact. The shared tutorial card no longer opens (see defect 1) |
| frog-jump-2-midplay.png | needle, coloured bar with red ends, timer, distance, hero on track | pass; needle visible, bar does not cover hero. `Còn 40,0 m` is crossed by the needle (defect 4) |
| frog-jump-3-fall.png | `NGÃ!`, fall pose, needle hidden | pass |
| frog-jump-4a-result-win.png | `VỀ ĐÍCH!`, `Giữ được lượt thi`, `LƯỢT THI: 3/5`, `THI LẠI`, no stray score | pass; large empty gap where the score was (defect 5) |
| frog-jump-4b-result-zero.png | `HẾT GIỜ!`, `-1 lượt thi`, `LƯỢT THI: 0/5`, `VỀ BẢN ĐỒ` | pass; same gap |
| frog-jump-5a-exam-fail-1.png | `BẬT CÓC`, `Về đích trong 60 s để giữ lượt thi` | pass |
| frog-jump-5b-exam-fail-2.png | `BẬT CÓC`, `−1 lượt thi. Bật cóc xong mới được thi lại` | pass |
| frog-jump-pause.png | pause menu in the frog scene shows only `TIẾP TỤC` | pass; panel keeps a tall empty area (defect 6) |
| frog-jump-6a-map-3.png | `Lượt thi: 3/5`, `m:ss` countdown above the hearts | countdown present (`5:00`) but tiny and touching the header top edge (defect 3) |
| frog-jump-6b-map-0.png | 0 lives, exam card `Hết lượt thi`, `CHỜ HỒI LƯỢT`, hint `Hết lượt thi · Chờ hồi lượt để thi tiếp` | pass; red countdown has the same placement problem |

Notes on the captures: the result cards in 4a/4b are forced during the pre-play countdown, so a very faint countdown
digit shows through the scrim near the card centre; it is a capture artefact, not part of the real flow. In 6a only
two lesson cards are visible because the hook rebinds the list and it scrolls to the checkpoint.

## Defects found and fixed

1. Generic English tutorial card opened (fixed earlier: `FrogJumpController.UsesSharedTutorial => false`).
2. Frog HUD: the stray `PLAY` caption, stamina and score are hidden; the progress bar stays (real distance). Re-captured in images 1, 2, 3.
3. `Còn x m` now sits under the progress bar at the top, clear of the power bar and needle (image 2).
4. Map countdown: the lives panel has room above the hearts and the label is 22 px, centred, inside the panel (6a, 6b).
5. Frog result card uses a shorter card, so there is no empty score gap (4a, 4b). The normal result layout is restored on every other `Show`.
6. Single-button pause menu shrinks to fit (pause).
7. At 0 lives the `TIẾP TỤC BÀI HỌC` button is dimmed with the kit's disabled alpha (6b).
8. Volleyball shows an `m:ss` clock under the scoreboard whenever the match has a time limit (`volleyball-timer.png`, 1:57 of 2:00); `BuildHudState` reports the remaining time for practice too.
9. Frog scene Sky and Grass quads shared a sorting order, so a rebuild could draw the sky over the grass; Sky is now -31.

Capture note: a background Editor only renders the Game view reliably when its window is activated; activate it
before each capture and restart it after editing scripts.
