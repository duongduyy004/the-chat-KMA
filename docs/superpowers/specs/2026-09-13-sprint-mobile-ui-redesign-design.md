# Sprint Mobile UI Redesign

Date: 2026-09-13
Status: Approved design
Scene: `Assets/_Project/Scenes/MG_Sprint.unity`

## Goal

Redesign the Sprint minigame presentation for Android landscape so players can immediately identify their runner, understand alternating LEFT/RIGHT input, read distance and rank, see race completion, and understand the result. Preserve all gameplay rules, scoring, game state, lifecycle, routing, and progression behavior.

## Non-goals

- Do not change `SprintRules`, rival pacing, stamina, wind timing, the 14-second limit, pass/fail conditions, score calculation, rank calculation, or result routing.
- Do not add a new gameplay mechanic.
- Do not make haptics a required dependency.
- Do not redesign other minigames or globally restyle their result screens.
- Do not replace the current runner or track art.

## Chosen direction

Use the approved **Broadcast Sports** layout:

- A compact scoreboard spans the top safe area.
- Four runners remain the focus and sit at the vertical center of their authored lanes.
- The player has a cyan outline/glow and a nearby `PLAYER` marker.
- Small translucent LEFT and RIGHT visual buttons sit in the lower corners while larger invisible hit areas remain easy to spam.
- The finish line stays outside the presentation before 70 m and becomes visible at the right edge from 70 m onward.
- Sprint gets a short, automatic tutorial banner, followed by the existing countdown and Play phase.
- The Sprint result modal uses the same typography, radius, color, shadow, and motion system as its gameplay HUD.

## Visual system

### Palette

- Surface/navy: `#071C31` at approximately 93% opacity for the scoreboard and modal.
- Primary/cyan: `#42DFFF` for player identity, distance, and progress information.
- Action/yellow: `#FFE04D` for the next required input, CTA, and rank emphasis.
- Success/green: `#55DF9D`.
- Failure/red: `#FF656B`.
- Primary text: warm white.
- Secondary text: muted blue-gray.
- Disabled state: gray with reduced opacity.

Strong colors have one semantic role at a time. Navy surfaces use rounded corners, a restrained outline, subtle shadow, and optional low-intensity glow.

### Typography

- Instruction/countdown is the largest transient text.
- Distance and rank are the most prominent persistent text.
- Combo is one level smaller.
- Supporting labels use uppercase, compact letter spacing, and remain readable against navy panels.
- Text placed directly over gameplay receives a small shadow or outline.
- Text must use the project's current TMP font asset and auto-sizing or bounded responsive sizing where appropriate.

### Spacing and responsiveness

- Keep the existing `CanvasScaler` reference resolution of `1920 × 1080`.
- Anchor all interactive HUD elements to `SafeAreaRoot`.
- Use normalized anchors and bounded size calculations, not device-specific pixel positions.
- Maintain at least 4% horizontal safe-area inset for visible controls and scoreboard.
- Visible controls target approximately 20% of safe-area width by 16% of safe-area height.
- Invisible input hit areas target approximately 28% by 24% and may extend beyond the visible button without crossing the screen midpoint.
- The layout must support common Android landscape ratios including 16:9, 18:9, 19.5:9, and 20:9 without clipping or overlap.

## Gameplay screen

### Scoreboard

Replace scattered Sprint metrics with one top scoreboard inside the safe area. It contains three columns and one shared progress indicator:

1. `QUÃNG ĐƯỜNG` with value `45 / 100 m`.
2. `THỨ HẠNG` with the existing ordinal value such as `2nd`.
3. `NHỊP CHẠY` with value `COMBO ×6`.
4. A progress bar beneath the columns, thick enough to read at phone scale.

The scoreboard reads only existing `SprintController` state. Sprint-specific presentation suppresses the shared phase, score, and status labels that currently duplicate or overlap these metrics. Pause remains in the top safe-area hierarchy without covering the scoreboard.

### Runner and lane alignment

Each runner's visual pivot is vertically centered in its lane. With four equal lane bands, the target normalized vertical centers are 12.5%, 37.5%, 62.5%, and 87.5% of the track region. Scene-authored runner roots retain their gameplay-driven horizontal movement.

The player receives:

- A thin cyan outline/glow that remains visible when runners overlap horizontally.
- A compact `PLAYER` marker directly above the player, with a short downward pointer.
- No label offset that can drift independently from the runner.

These treatments are presentation-only and do not change runner transforms used by race rules.

### LEFT and RIGHT controls

The visible buttons are symmetric rounded rectangles in the lower left and lower right safe area. Their navy background uses approximately 50% opacity, a soft border, and restrained shadow. Labels are `← TRÁI` and `PHẢI →`.

The existing `LeftTap` and `RightTap` input surfaces remain the authoritative hit areas. Visual children can be smaller than those hit areas. The input areas must not overlap each other or the screen midpoint.

Feedback behavior:

- Pointer-down immediately scales the visible button to 0.94.
- A short flash/ripple lasts about 90 ms, then returns to idle.
- The side matching `SprintController.ExpectedSide` receives a mild yellow border/glow and `!` emphasis.
- The most recently accepted side may show a short check mark.
- Feedback reads expected-side state and does not determine or modify the expected side.
- Optional haptic feedback is allowed only through an already-supported platform API and must fail silently when unavailable.

### Instructions and countdown

Sprint replaces its current multi-page blocking tutorial presentation with one automatic banner:

`← TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI →`

The banner remains for 1.5 seconds, fades out, and releases only the existing Sprint tutorial gate. It does not require a close, skip, or start button. The current lifecycle then runs the countdown as `3`, `2`, `1`, `GO!`; input remains ignored until the existing `Play` phase.

`TĂNG TỐC!` appears briefly on entry to Play or for an applicable authored cue and fades out in approximately 250 ms. It must not remain as permanent text over the track. Existing wind-cue meaning remains intact, but its presentation must occupy a single transient instruction region instead of creating another overlapping text block.

### Finish line

The checkered finish line is a presentation element spanning all four lanes at the authored 100 m track end.

- At distances below 70 m, it is hidden or positioned outside the camera presentation.
- At 70 m, it enters from the right edge.
- From 70 m to 100 m, the visual relationship between the player and finish line closes consistently with existing distance progress.
- At 100 m, the existing controller resolves the race; the finish-line presenter never decides completion.

The reveal threshold is exactly 70 m and must be tested at 69.9 m and 70 m.

## Result screen

The redesign applies to the Sprint scene's result instance, not other minigames.

- A dark scrim covers the gameplay background with a light blur when supported.
- The centered modal uses the shared navy surface, radius, border, shadow, and typography.
- Failure title: `THẤT BẠI` in failure red.
- Success title: `HOÀN THÀNH!` in success green.
- Score and rank use two compact adjacent stat cards so each label stays near its value.
- Rank appears as a large colored badge.
- `TIẾP TỤC` is a centered, high-contrast yellow CTA with a large hit target.

Motion sequence:

1. Scrim fades in over approximately 120 ms.
2. Modal fades and scales in over approximately 180 ms.
3. Title pops over approximately 140 ms.
4. Score counts up for no more than 350 ms.
5. Rank badge pops over approximately 160 ms.

The CTA becomes interactive as soon as the modal appears. Animation must never delay or duplicate the existing `Continue()` action or route emission.

## Component responsibilities

### Existing gameplay components

- `SprintController` remains the source of distance, rank, combo, expected side, phase, wind state, and result.
- `SprintRules` remains untouched.
- `ScreenTapArea` / `GameplayInputRouter` remain authoritative for LEFT/RIGHT input.
- `ResultPanel.Continue()` and progression routing remain untouched in behavior.

### Presentation components

- `SprintHud` binds and updates the consolidated scoreboard.
- `SprintControlPresenter` owns visible button state, press animation, and expected-side highlight. It forwards no new rule decisions.
- `SprintFinishLinePresenter` maps existing distance to visibility/presentation from 70 m to 100 m.
- `SprintStartPresentation` owns the 1.5-second banner, countdown visuals, and transient Play instruction while releasing the existing tutorial gate at the correct time.
- `SprintResultPresentation` styles and animates the Sprint result instance while leaving its action event intact.
- `SprintFestivalPresentation` may create or configure missing visual-only objects, but permanent hierarchy and serialized references should live in the scene/prefab where practical.

Presentation components must tolerate missing optional visual references without throwing. Required bindings are asserted by scene tests.

## Data flow

```text
SprintController/SprintRules
    ├── SprintHud ───────────────> scoreboard and progress
    ├── SprintControlPresenter ──> next-side highlight
    ├── SprintFinishLinePresenter > finish visibility/position
    ├── runner presenters ───────> horizontal runner visuals
    └── ResultPanel + SprintResultPresentation

LeftTap / RightTap hit areas
    ├── immediate visual press feedback
    └── existing GameplayInputRouter ──> SprintController
```

No presentation component writes distance, rank, combo, score, expected side, result, lifecycle phase, or progression state.

## Planned file scope

Modify:

- `Assets/_Project/Scenes/MG_Sprint.unity`
- `Assets/_Project/Scripts/Gameplay/Sprint/SprintHud.cs`
- `Assets/_Project/Scripts/Gameplay/Sprint/SprintFestivalPresentation.cs`
- `Assets/_Project/Scripts/UI/PhaseOverlay.cs` only where a Sprint-specific handoff is required
- Sprint presentation PlayMode tests

Add as focused presentation components:

- `SprintControlPresenter.cs`
- `SprintFinishLinePresenter.cs`
- `SprintStartPresentation.cs`
- `SprintResultPresentation.cs`
- Corresponding `.meta` files and focused tests

Do not alter the user's existing unrelated changes in map/progression files. If implementation reveals that the shared `TutorialOverlay` or `ResultPanel` must change globally, stop and revise this design before proceeding.

## Verification

### Automated tests

Add or update tests for:

- Complete scoreboard bindings and exact distance format `N / 100 m`.
- Shared Sprint labels suppressed to prevent duplication.
- Four runner roots aligned to their lane centers within a small tolerance.
- Player marker follows the player visual and the player highlight is present.
- Visible controls are symmetric, smaller than their hit areas, inside safe area, and do not cross the midpoint.
- Expected-side highlight mirrors controller state and changes after a valid alternating tap.
- Press feedback returns to idle and does not create duplicate input.
- Sprint tutorial auto-releases after 1.5 seconds, countdown remains ordered, and input remains gated before Play.
- Finish line is absent at 69.9 m, visible at 70 m, and correctly presented at 100 m.
- Result status, score, rank, CTA, and single-action continuation behavior.
- Existing Sprint gameplay/rules/controller suites continue to pass unchanged.

### Visual QA

Use the repository screenshot workflow and inspect the actual PNG at these checkpoints:

- Tutorial banner.
- Countdown.
- Early gameplay before 70 m.
- Late gameplay with finish line visible.
- Failure result.
- Success result.

Capture at the normal 16:9 reference and at least one wider landscape ratio. Screenshot generation alone is not acceptance; inspect each image for readability, runner/lane alignment, overlap, clipping, safe-area placement, and control visibility.

### Android runtime QA

After static Play Mode QA is clean:

1. Build the x86_64 Android APK using the established project workflow.
2. Install and launch it on the available emulator.
3. Verify package/process state and filtered logcat separately from visual checks.
4. Capture and inspect gameplay and result screenshots on Android.
5. Manually confirm rapid alternating touches, press feedback, next-side highlighting, countdown gating, finish reveal, and immediate Continue behavior.

Build success, automated tests, screenshot capture, visual acceptance, and Android interaction acceptance are separate verification gates.

## Acceptance criteria

- A new player can immediately identify the player runner and the LEFT/RIGHT alternating action.
- Distance, progress, rank, and combo are readable at a glance in one HUD cluster.
- No Sprint gameplay text or controls overlap each other, runners, or the safe-area edges.
- Visible controls are unobtrusive while their hit targets remain large enough for rapid two-thumb play.
- The finish line is not visible before 70 m and is clearly visible afterward.
- Result status, score, rank, and Continue action are understood immediately.
- The redesign changes presentation only; all existing gameplay, scoring, state, lifecycle, and progression contracts remain valid.
