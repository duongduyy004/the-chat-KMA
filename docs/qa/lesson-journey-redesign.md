# Chapter lesson journey QA — 2026-10-05

The authored Map scene presents Học → Luyện → Thi as three connected cards. Chapter accents are blue/orange/green. Cards show objectives, prerequisites, completed/current badges and actions. The chapter header shows completion progress and the existing continue action. Course results use a compact strip above the lesson panel, leaving replay cards visible and available.

Presentation uses the shared UITheme, Vietnamese typography and UiKit sprites. Book/trophy details use native Image rectangles. The three chapter glyphs are baked in the scene, including inactive glyphs, and refresh only switches their visibility. Journey, Review, Supplementary and FreePlay selection, unlocks, attempts and gameplay remain owned by the existing journey services.

## Automated evidence

| Check | Passed | Failed | Ignored | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full EditMode | 619 | 0 | 3 | `Builds/TestResults/lessons-edit-verified.xml` |
| Full PlayMode | 239 | 0 | 0 | `Builds/TestResults/lessons-play-final.xml` |
| Chapter geometry, progress and completed-course summary | 11 | 0 | 0 | `Builds/TestResults/lessons-ui-final2.xml` |
| Final UIComponentTests after icon cleanup and probe removal | 27 | 0 | 0 | `Builds/TestResults/lessons-final-ui.xml` |

The full EditMode XML root reports `Skipped:Ignored` because of three existing ignored ChallengeSequenceTests. The runner wrapper exits 1 for this root status; individual cases were parsed and show 619 passed and zero failures. PlayMode reports `Passed`.

Geometry checks cover all three chapters at 1440×1080, 1920×1080 and 2400×1080. Authored-scene PlayMode layout checks also cover 1280×720 and 1728×1080, including lesson text height. The legacy Festival UI check now measures actual rendered chapter cards; the disabled GridLayoutGroup's cell size is not the route's geometry.

Reopen validation confirmed no duplicated UI and no transient sprite references. Review found and resolved a compact-screen Football objective truncation, the completed-course summary overlap and a chapter glyph refresh that replaced baked sprites with transient ones.

## Visual evidence

All seven final PNGs were opened and inspected. Captures use the active Android GameView group's named `FixedResolution` setting; PNG dimensions were checked directly. A temporary Editor probe supplies in-memory progress only and has been removed.

| PNG under `Builds/Screenshots/lesson-journey/` | Dimensions | State |
| --- | --- | --- |
| `sprint-start-16x9.png` | 1920×1080 | Learn current; Practice/Exam locked |
| `sprint-practice-16x9.png` | 1920×1080 | Learn completed; Practice current |
| `volleyball-16x9.png` | 1920×1080 | Orange chapter; full objectives visible |
| `football-16x9.png` | 1920×1080 | Green chapter; full objectives visible |
| `football-4x3.png` | 1440×1080 | Long Football objectives fit |
| `sprint-wide.png` | 2400×1080 | Three stages retain equal size and sequence |
| `completed-course.png` | 1920×1080 | Score strip clears all lesson cards; continue hidden |

These are static Unity renders. Physical touch, Android safe areas/GPU behavior and APK export were not verified in this task. Unrelated test-generated scene/prefab/font changes were restored; the pre-existing modified `student-journey/soccer-learn.png` was preserved.
