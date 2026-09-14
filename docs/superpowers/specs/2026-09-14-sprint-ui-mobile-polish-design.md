# Sprint Mobile UI Polish Design

**Date:** 2026-09-14
**Status:** Approved
**Supersedes the UI layer of:** `docs/superpowers/specs/2026-09-13-sprint-mobile-ui-redesign-design.md`

## Goal

Make the Sprint minigame read as a finished Android landscape mobile game. A player
glancing at the screen must identify their own runner, how far they have run, what
place they are in, and which button to press next, without reading instructions.

Gameplay logic, characters, lanes, pacing, rules, lifecycle, scoring, and result
routing do not change. This is a presentation-layer change only.

## Constraints

- Android landscape first; no layout may depend on a fixed resolution.
- `SprintRules`, `SprintController` pacing/stamina/wind/time limit, `GameplayInputRouter`,
  `MinigameLifecycle`, and `ResultPanel` behavior stay authoritative and unmodified.
- `ScreenTapArea` remains the only component that forwards Sprint input.
- Vietnamese copy throughout, consistent with the rest of the game.
- No new package. No new bitmap asset — rounded shapes are generated at runtime.
- Changes apply to Sprint only; other minigames keep their current chrome.

## Decisions

| Decision | Choice | Why |
|---|---|---|
| UI ownership | Runtime C# builder is the single owner | Existing PlayMode tests resolve objects by name from the builder; sizes derive from the safe-area rect so responsiveness is structural. The alternative (prefab) would require rewriting most presentation tests. |
| Control model | Visible button *is* the hit area | Today the visual (20%x16%) and hit area (28%x24%) are different rects reconciled every frame. One rect removes the see/press mismatch and ~70 lines of screen-space math. |
| Rounded shapes | Generated procedurally, cached, 9-sliced | Satisfies the rounded-corner requirement with no asset files and no package. |
| Copy language | Vietnamese (`TRÁI` / `PHẢI`) | The rest of the game is Vietnamese; existing copy assertions stay meaningful. |
| Acceptance | Editor Play-mode screenshots at multiple aspects | The acceptance criteria are visual; automated tests alone already signed off a broken screen once. |

## Architecture

Three new pure, stateless units plus a thinner assembler:

| Unit | Responsibility | Depends on |
|---|---|---|
| `SprintUiTheme` | Color, typography, radius, spacing tokens. No Unity scene access. | — |
| `SprintUiShapes` | Generates and caches antialiased rounded-rect `Sprite`s with 9-slice borders. | `SprintUiTheme` |
| `SprintUiLayout` | Every on-screen rect as a pure function of the safe-area rect. | — |
| `SprintFestivalPresentation` | Assembles the hierarchy from theme + shapes + layout, wires presenters. | all three |
| `SprintControlPresenter` | Control visual state and press feedback. Owns no input. | `SprintUiTheme` |
| `SprintStartPresentation` | Tutorial gate, countdown, instruction. | `SprintUiTheme` |

Each of the first three can be understood and tested without a scene. The assembler
holds no layout constants of its own.

## Layout

**Sizing rule.** Element *sizes* are fractions of the safe-area **height** (`H`).
Element *positions* are fractions of width (`W`) and height. The canvas scaler matches
on height, so height-relative sizes keep every panel and button the same physical size
on a 16:9 tablet and a 20:9 phone — only the gaps between them stretch. Sizing anything
off `W` would shrink the HUD on narrow aspects and inflate the buttons on wide ones.

Origin is bottom-left of the safe area.

```
┌───────────────────────────────────────────────────────────────┐
│ ▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂ PROGRESS RAIL ▂▂▂▂▂◆▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂  │ .955–.985
│ ┌────────────────────┐   CHẠY NƯỚC RÚT · 100M        ┌─────┐  │
│ │  42 / 100 m  ⟨1st⟩ │                               │  ‖  │  │
│ │  COMBO ×5          │                               └─────┘  │
│ └────────────────────┘                                        │ .79–.94
│    ▼ PLAYER                                                   │
│   🏃 ─────────────────── lane 1 ────────────────────────────  │ ~.74
│   🏃 ─────────────────── lane 2 ────────────────────────────  │ ~.61
│   🏃 ─────────────────── lane 3 ────────────────────────────  │ ~.48
│   🏃 ─────────────────── lane 4 ────────────────────────────  │ ~.36
│ ┌──────────┐                                   ┌──────────┐   │
│ │    ←     │                                   │    →     │   │
│ │   TRÁI   │                                   │   PHẢI   │   │ .04–.30
│ └──────────┘                                   └──────────┘   │
└───────────────────────────────────────────────────────────────┘
  .02W                                                     .98W
```

| Element | Anchor | Size | Notes |
|---|---|---|---|
| Progress rail | `x .03W–.97W`, top edge at `.985H` | height `.03H` (32 units) | Pill track, gold fill, cyan player pip. Width is the one element that *should* span, since it maps 0-100 m onto the run axis. |
| Scoreboard | top-left, inset `.02W` / `.06H` | `.50H × .15H` (540×162) | One rounded `Surface` panel, two rows. |
| Mode chip | top-centre, at `.895H–.945H` | `.36H × .05H` (389×54) | Text only, `TextPrimary` at 60% alpha, smallest step. |
| Pause | top-right, inset `.02W` / `.06H` | `.089H × .089H` (96×96) | Rounded square, `‖` from two Images, no label. `PausePanel` behavior unchanged. |
| Left control | bottom-left, inset `.02W` / `.04H` | `.43H × .26H` (464×281) | |
| Right control | bottom-right, inset `.02W` / `.04H` | `.43H × .26H` | Mirrors the left exactly. |
| Countdown |  centred at `x .5W`, `y .62H` | `.53H × .32H` | Empty at race start. |
| Instruction |  centred at `x .5W`, `y .38H` | `1.07H × .10H` | Below countdown. |

**Scoreboard rows.** Two rows, not three: `42 / 100 m` (Title, left) and `1st`
(Headline, gold pill, right) share row 1; `COMBO ×5` (Body) is row 2. Three stacked
rows would need `54 + 48 + 32` plus padding — about 182 units against the 162 available
— and would clip. Two rows need `54 + 8 + 32 + 32` padding = 126, leaving margin.

Worst-case width at row 1 is `100 / 100 m` (~330) + gap 24 + rank pill (~113) + padding
32 = 499 against 540. Tight, which is exactly what the text-overflow assertion is for.

Three layout choices worth recording:

**The progress bar is a full-width top rail rather than a bar inside the scoreboard.**
It maps 0-100 m onto the same left-to-right axis the player is running along, which
reads faster than a short bar in a panel, and it frees vertical room so distance and
rank can both be large. The rail sits directly above the scoreboard so the two still
read as one cluster.

**The scoreboard's bottom edge is `.79` and the top lane sits near `.74`.** That ~5%
gutter exists so the panel never covers a runner, including at the start when all four
are bunched at the left edge beneath it. Verify the real lane positions in the scene
and adjust the panel, not the lanes.

**The controls do not grow on wider screens.** At `.43H` each they are 464 units wide
regardless of aspect, so on a 20:9 phone the gap between them widens rather than the
buttons stretching toward the middle. A button wider than a thumb's reach is not a
better button, and keeping them fixed guarantees they never approach `x = .5`. At the
narrowest aspect tested (16:10, `W = 1728`) the two still leave a 730-unit gap.

## Visual system

### Color

| Token | Value | Used for | Contrast on `Surface` |
|---|---|---|---|
| `Surface` | `#08233D` | Panels, buttons, pause, marker plate, at .30-.92 alpha | — |
| `TextPrimary` | `#FFF9E7` | Distance, control labels, instruction | ~15:1 |
| `Accent` | `#FFCA3A` | Rank, progress fill, countdown, button border | ~10.4:1 |
| `Player` | `#3AE6FF` | Player only: sprite outline, head marker, rail pip | ~10.6:1 |
| `Energy` | `#FF595E` | Combo, press flash | ~5.2:1 |

`Player` cyan is reserved. Nothing else in the Sprint scene may use it. Identity is
never carried by hue alone — the marker also has a chevron shape and the word PLAYER.

Cyan is weak against the bright blue sky the player runs against in the top lane, so
the head marker sits on a `Surface` plate at 85% alpha.

### Typography

One font (the TMP asset already on the canvas), one scale, in canvas units at the
1920x1080 reference. The canvas scaler matches on height
(`m_MatchWidthOrHeight: 1`, `HUD_Minigame.prefab:1083`), so these sizes scale with
screen height on any landscape aspect.

| Step | Size | Applied to |
|---|---|---|
| Display | 160 | Countdown `3 2 1 GO!` |
| Title | 54 | `42 / 100 m` |
| Headline | 48 | `1st` |
| Body-L | 40 | `TRÁI` / `PHẢI`, instruction |
| Body | 32 | `COMBO ×5` |
| Caption | 24 | Mode chip. Hard floor: nothing smaller ships. |

Size encodes the information priority: distance and rank largest, combo one step
down, minigame name smallest.

### Shape and depth

`SprintUiShapes.RoundedRect(radius)` generates an antialiased rounded-rect texture
once per radius, returns a `Sprite` with a 9-slice border equal to the radius, and
caches it in a static dictionary. Radii: panel 24, control 36, pause 20, pill
(rail, rank badge) = height / 2. Spacing scale 8 / 16 / 24 / 32; panel inner padding 24.

Depth comes from a single `Shadow` component, `rgba(0,0,0,.35)` at offset `(0,-4)`,
replacing the current `Outline` usage at `SprintFestivalPresentation.cs:51, 162, 264, 350`.
`Outline` emits four extra copies of every glyph; `Shadow` emits one, and this HUD's
text rebuilds every frame.

## Controls

`SprintRules.cs:104` reads `ExpectedSide` from an authored sequence, not hardcoded
alternation. The default is `{Left, Right}` (`SprintRules.cs:76`), so "luân phiên" is
accurate today, but the highlight generalizes to any authored pattern. A wrong tap is
not punished — it still grants 40% impulse (`SprintRules.cs:128`) — so the highlight is
a nudge and must never look like an error.

### Structure

```
LeftTap                 ScreenTapArea + Image(alpha 0, raycastTarget) — rect = SprintUiLayout.ControlRect
└ Visual                stretched to fill, raycast off; this is what scales
  ├ Background          rounded 9-slice, Surface
  ├ Border              rounded ring, Accent
  ├ Arrow               ← / → glyph, Body-L x1.6
  └ Label               "TRÁI" / "PHẢI", Body-L
```

`Visual` is a stretched child of the tap area, so the button *is* the hit area by
construction. `SyncLayout`, `SetVisualLayout`, `HasSafeAreaInsets` and `ScreenRect`
(`SprintControlPresenter.cs:119-186`) are deleted.

Scaling `Visual` rather than `LeftTap` keeps the hit area fixed during the press
animation; a 6% shrink mid-spam could otherwise drop an edge tap.

`SprintUiLayout.ControlRect` becomes authoritative and repositions the authored
`LeftTap` / `RightTap` rects in `MG_Sprint.unity:333,352`. `ScreenTapArea` remains the
only input forwarder and no router or controller code changes, but the touchable
region does move and grow. This is intended.

### States

All applied to `Visual`:

| State | Background | Border | Scale | Timing |
|---|---|---|---|---|
| Idle | `Surface` @ .42 | `Accent` @ .25 | 1.0 | — |
| Next expected | `Surface` @ .55 | `Accent` @ .75 | 1.0 ↔ 1.03 | 1.2 Hz breathe |
| Pressed | `Energy` @ .55 | `Accent` @ .75 | 0.94 | instant in, 90 ms ease out |
| Finished | `Surface` @ .30 | `Accent` @ .15 | 1.0 | after the race ends |

Press feedback fires on `PointerDown`, never on up or click — anything else feels
laggy at spam speed.

The current `ApplyHighlight` (`SprintControlPresenter.cs:105`) forces alpha to `.5`
unconditionally and only moves the outline between `.16` and `.45` alpha, which is why
the highlight is invisible today. It is replaced by the table above.

### Haptics

One `HapticsService.Light()` (`Core/HapticsService.cs:18`) per **correct** tap. It is a
20 ms / amplitude-64 Android one-shot that already respects the user's vibration
setting and no-ops on unsupported devices. Nothing fires on a wrong tap — a buzz on
every tap through a 14-second spam race is unpleasant, and a wrong tap is not an error.

## Start flow

Today the instruction appears *after* GO for `PlayInstructionDuration = .25f`
(`SprintStartPresentation.cs:10`) and reads `"TĂNG TỐC!"` (line 169) — too brief to read
and it does not say what to press. The how-to-play copy lives on the tutorial banner,
which has already gone by then.

```
t=0.0  ┌ gate ON ──────────────────────────────────────┐
       │  [ instruction plate ]                        │
       │  ← BẤM TRÁI VÀ PHẢI LUÂN PHIÊN ĐỂ CHẠY →      │  visible
t=1.5  ├ gate OFF, countdown starts ───────────────────┤  continuously
       │       3        2        1                     │  from t=0
t=4.5  │            GO!                                │
t=5.0  └ fade 0.4s ────────────────────────────────────┘
```

One instruction line, anchored at `y .28–.38`, persisting across Tutorial and Countdown
so there is no flicker between phases. It is readable for ~5 seconds instead of 0.25,
auto-fades, and needs no popup or confirmation.

This deviates from the requested "instruction after GO": it starts *before* GO, because
that is when the player needs it.

`3` `2` `1` each enter at scale 1.25 → 1.0 over 150 ms. `GO!` pops 1.4 → 1.0 over
200 ms. Raise `GoDuration` from `.25f` to `.5f` so GO! registers. The countdown sits at
`x .35–.65`, empty at race start since all four runners are still at the left edge.

Fix the latent inconsistency while here: `TutorialText` returns `TutorialCopy` with
arrows (line 37) while the label is set from `TutorialMessage` without them (line 209).

## Player identity

Three independent layers so no single one has to carry recognition:

| Layer | Current | Change |
|---|---|---|
| Sprite outline | `SprintPlayerIdentityOutline` duplicates the sprite at `1.16` scale in cyan behind the original | Keep the mechanism, soften to `1.12` |
| Head marker | Bare cyan `TextMesh` at `localPosition.y = 1.65`, `characterSize .065`, overlapping the sprite | Raise to `~2.15`, add a downward cyan chevron, add a `Surface` @ .85 plate behind it |
| Rail pip | none | Cyan diamond on the progress rail at `distance / 100` |

The marker is world-space (`TextMesh` + `MeshRenderer`,
`SprintFestivalPresentation.cs:319-327`), not uGUI, so the plate is a `SpriteRenderer`
quad using the same generated rounded sprite: plate sorting order 19, text and chevron 20.

**Explicitly out of scope:** rival pips on the progress rail. `SprintRules.GetRivalDistance(i)`
would make it easy and it would show rank spatially, but rank is already a large gold
number and four extra pips on a 32-unit rail would dilute the one thing the rail is
for. Cheap to add later if rank turns out to be hard to feel mid-race.

## Cleanup

`DisableSharedMetrics` (`SprintFestivalPresentation.cs:270-279`) calls
`safeArea.Find(name)` over `{ "Time", "Phase", "Score", "Status", "Progress", "Stamina",
"SprintMetrics" }`. `SafeAreaRoot`'s children are exactly
`Timer, Phase, Score, Status, Progress, Stamina, HeartBar`
(`HUD_Minigame.prefab:1152-1159`). Three concrete misses:

1. The list says `"Time"`; the object is named **`Timer`**. The timer label never hides.
2. **`HeartBar`** is not in the list at all and stays visible.
3. **`SprintMetrics`** is a *sibling* of `SafeAreaRoot`, added directly under the canvas
   root (`MG_Sprint.unity:2088`), so `safeArea.Find` can never reach it. Its four labels
   — `SprintDistance`, `SprintRank`, `SprintCadence`, `SprintDistanceFill`, anchored
   top-left at `(96, -104)`, size `520x150` — render alongside the new chrome forever.

Fix: search the whole canvas by name rather than one transform's direct children, add
`Timer` and `HeartBar`, and delete `SprintMetrics` from `MG_Sprint.unity` outright
rather than hiding it. `SprintHud`'s serialized references to those labels
(`MG_Sprint.unity:277`) go with it; `CacheVisuals` already rebinds from the built chrome.

## Verification

### EditMode — pure geometry

Because `SprintUiLayout` owns every rect, the hardest requirement becomes a loop:

```csharp
[TestCaseSource(nameof(LandscapeSafeAreas))]  // 16:9, 16:10, 18:9, 19.5:9, 20:9, + notch inset
public void NoTwoElementsOverlap(Rect safe)
{
    foreach (var (a, b) in Pairs(SprintUiLayout.StartStateRects(safe)))
        Assert.That(a.rect.Overlaps(b.rect), Is.False, $"{a.name} overlaps {b.name}");
}
```

Two groups, since not everything is on screen at once: `RaceRects` (rail, scoreboard,
mode chip, pause, both controls) and `StartStateRects` (those plus countdown and
instruction). The same source asserts:

- every rect lies inside the safe area with at least 2% edge padding
- the two controls are symmetric, equal-sized, and never cross `x = .5`
- `SprintUiTheme`'s smallest step is >= 24
- every text/surface color pair is at least 4.5:1

`SprintUiShapes` tests: corners transparent, centre opaque, 9-slice border equals the
radius, repeated requests for the same radius return the cached instance.

### PlayMode — real scene contracts

Extends `SprintPresentationGateTests`:

- `GameObject.Find("SprintMetrics")` is null; `Timer` and `HeartBar` are inactive
- each control's `Visual` rect equals its `ScreenTapArea` rect
- the highlight follows `controller.ExpectedSide`; a tap does not double-increment `CadenceCombo`
- the start sequence hits the timings above
- the finish line reveals at exactly 70 m
- for every TMP label, `isTextOverflowing` is false — this is what catches Vietnamese
  diacritics clipping

Regression: `SprintRuntimeInputTests`, `SprintControllerTests`, `CoreLoopTests`,
`PhaseFlowTests` stay green, and one tap still reaches gameplay exactly once.

### Screenshots

Seven states at 16:9 — instruction, countdown, GO, early race, late race with finish
line, result pass, result fail — plus race and result repeated at 20:9. Inspect each
PNG for what no assertion can check: whether it reads as a finished game, whether
PLAYER is obvious at a glance, whether the marker survives both sky and track.

**Staleness guard.** `task6_countdown.png` and `task6_gameplay.png` from the previous
round are visually identical despite being different forced states: the Editor was
serving stale captures and the work was signed off against them. After capturing the
set, hash the PNGs and assert that states which must differ actually do. Two identical
hashes mean the capture is stale, not that the UI is fine — retake the set before
inspecting any of it.

## Acceptance criteria

1. The player identifies their runner in under a second (cyan outline + head marker + rail pip).
2. Which button to press next is visible without reading text (border highlight + breathe).
3. Distance, rank, and progress are each legible at a glance during play.
4. No two UI elements overlap at any tested landscape aspect, proven by EditMode test.
5. No text is clipped, proven by the overflow assertion.
6. No legacy HUD element (`SprintMetrics`, `Timer`, `HeartBar`) is visible.
7. Screenshots read as a finished mobile game, not a debug screen.
8. All existing gameplay, input, lifecycle, and result tests stay green.
