# Sprint UI Polish — Verification Gate

**Branch:** `feature/sprint-ui-polish` (14 commits, `d706ccb..eacdac7`)
**Spec:** `docs/superpowers/specs/2026-09-14-sprint-ui-mobile-polish-design.md`
**Plan:** `docs/superpowers/plans/2026-09-14-sprint-ui-mobile-polish.md`
**Unity:** `6000.3.23f1`
**Date:** 2026-09-14

## Automated verification — PASS

Full suites, unfiltered, run from a clean tracked status:

| Suite | total | passed | failed | inconclusive |
|---|---|---|---|---|
| EditMode | 369 | 369 | 0 | 0 |
| PlayMode | 270 | 270 | 0 | 0 |

Repository baselines before this work were 258 EditMode and 243 PlayMode, so the branch adds
111 EditMode and 27 PlayMode tests, all green.

Command form used (note: `rtk` and `KMA_UNITY_EDITOR` are not available in this environment):

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:\project\the-chat-KMA" -runTests -testPlatform EditMode \
  -testResults <out>.xml -logFile <out>.log
```

Unity exits `0` even when tests fail. Every result in this document was read from the
`<test-run>` element's `total`/`passed`/`failed`/`inconclusive` attributes, never from an exit code.

### What the automated suites prove

- **Layout geometry**: `SprintUiLayout` rects are pure functions of the safe-area rect, with a
  pairwise overlap matrix, safe-area containment, control symmetry, the never-cross-midpoint
  guarantee, and height-only sizing — all asserted at 16:9, 16:10, 18:9, 19.5:9, 20:9 and a notch inset.
- **Shape generation**: corner transparency, centre opacity, antialiasing along the corner arc,
  9-slice border equal to the radius, and sprite caching by radius.
- **Design tokens**: the type scale's 24 floor, and WCAG contrast >= 4.5:1 for every text colour on Surface.
- **Scene hierarchy**: the approved `SprintBroadcastChrome` tree, legacy chrome absent, controls
  structured as real buttons owned by their tap areas, tap areas still outside the HUD chrome.
- **Behaviour**: start-flow timings, countdown sequence, instruction persistence and fade, button
  highlight following `ExpectedSide`, press feedback and decay, player identity layers, rivals never
  using the reserved player accent.
- **No gameplay regression**: `SprintRuntimeInputTests` and `SprintControllerTests` green; one tap
  still reaches gameplay exactly once.

## Visual verification — NOT COMPLETED

**The visual acceptance criteria in the spec were not verified, and this gate does not claim they were.**

Plan Task 10 called for nine Play-mode screenshots (seven states at 16:9, two at 20:9) inspected for
overlap, clipping, player recognisability and overall finish. That was attempted and abandoned as
unsound, for a concrete reason:

In the Unity Editor, the Sprint HUD canvas has a **zero-sized RectTransform at runtime**, so every
anchor-based element collapses to the canvas origin. Captures taken through
`tools/qa-screenshot.sh` therefore show a broken layout that does not reflect the code under test.
Measured in a human-initiated Play session with the Game View foreground and rendering at 1920x1080:

```
canvas.enabled=True  renderMode=ScreenSpaceCamera  worldCamera=GameCamera  isActiveAndEnabled=True
cam.enabled=True     pixelRect=(0,0,1920,1080)     targetTexture=none
scaler: ScaleWithScreenSize  refRes=(1920,1080)  match=1
canvasRect=(0,0)             <- and still (0,0) after Canvas.ForceUpdateCanvases()
lossyScale=(0.01,0.01,0.01)  <- correct for ortho size 5.4 against a 1080 reference
```

> **Superseded 2026-09-15.** The root cause below was fixed; see
> [Safe-area root fix](#safe-area-root-fix-2026-09-15) at the end of this document. The Editor now
> renders the Sprint HUD at real geometry, so Play-mode screenshots are usable again.

### Root cause (identified, NOT fixed — out of scope for this branch)

`Assets/_Project/Prefabs/UI/HUD_Minigame.prefab` contains **two** `SafeAreaFitter` components, enabled
backwards:

| Component on | GameObject fileID | `m_Enabled` | Should be |
|---|---|---|---|
| `HUD_Minigame` (the Canvas root) | `7330655373654108319` | **1** | 0 |
| `SafeAreaRoot` | `8863690910283851387` | **0** | 1 |

`SafeAreaFitter.Apply()` writes `offsetMin`/`offsetMax` on its own RectTransform. The canvas root has
`anchorMin == anchorMax == (0,0)` — coincident anchors — and for those, `offsetMax - offsetMin` *is*
`sizeDelta`. Writing zeros forces `sizeDelta = (0,0)`. It re-applies from
`OnRectTransformDimensionsChange`, so it re-zeroes the canvas every time Unity sizes it; this is why
changing Render Mode and calling `ForceUpdateCanvases()` both had no effect. Meanwhile the fitter on
`SafeAreaRoot` — which has proper `(0,0)-(1,1)` stretch anchors, where offsets correctly mean insets —
is switched off.

**This was deliberately left unfixed.** The project owner reports the game running correctly on a real
Android device over adb at true resolution, and elected not to pursue it. Device behaviour is what
ships; the finding is recorded here so it is not lost.

### Consequence for this gate

The following spec acceptance criteria remain **unverified**:

1. The player identifies their runner in under a second.
2. Which button to press next is visible without reading text.
3. Distance, rank and progress are legible at a glance during play.
7. The screen reads as a finished mobile game rather than a debug screen.

Criteria 4 (no overlap at any tested landscape aspect), 6 (no legacy HUD element visible) and 8 (all
existing tests green) ARE covered by the automated suites. Criterion 5 (no clipped text) is only
partially covered — see below.

### Specifically unverified details

- **Vietnamese diacritic clipping.** PlayMode batchmode has no laid-out canvas, so `isTextOverflowing`
  was removed from the suite and replaced with an EditMode width budget on the scoreboard's widest row
  (`100 / 100 m` beside the rank pill: 497.9 required against 540 available, a 7.8% margin). That proves
  the *rect* is wide enough; it does not prove TextMeshPro renders the diacritics unclipped.
- **The head-marker chevron** is a rounded square rotated 45 degrees — a symmetric diamond with no
  directionality. It likely reads as a dot rather than a downward pointer. The spec's "diamond" for the
  rail pip also shipped as an unrotated rounded bar.
- **The control button border** is a filled rounded rect with the translucent background drawn over it,
  so the Accent bleeds across the whole button face instead of forming a 3-unit ring. Computed label
  contrast on the highlighted face is ~6.8:1, so it is not a legibility failure, but it is not the
  appearance the spec describes.

## Explicitly out of scope for this gate

- No Android APK was built, installed or inspected as part of this work.
- No emulator run was performed. A pre-existing crash log sits at `Builds/Screenshots/logcat_crash.txt`
  from an earlier session and was not investigated.
- Other minigames' HUDs were not inspected, visually or otherwise.

## Recommended next step

Capture the nine states on a real device (`adb exec-out screencap -p`), where the canvas sizes
correctly, and inspect them against Task 10's checklist. That is the only remaining way to close the
four unverified acceptance criteria.

---

## Safe-area root fix (2026-09-15)

The root cause recorded above under "Root cause (identified, NOT fixed)" has been fixed. The
`SafeAreaFitter` inversion was real and reproduced exactly as described.

### What was wrong

`SafeAreaFitter.Apply()` writes `offsetMin`/`offsetMax`. Those are *insets* only while the anchors
are apart on that axis; where the anchors are coincident, `offsetMax - offsetMin` **is** `sizeDelta`,
so the write is a resize. The fitter was enabled on the Canvas root, whose anchors are
`(0,0)-(0,0)` — so it resized the canvas to `0x0` and every anchored child collapsed onto the origin.

Three layers carried the same mistake:

| Layer | Was | Now |
|---|---|---|
| `HUD_Minigame.prefab` | fitter on the Canvas root, enabled; `SafeAreaRoot`'s fitter disabled | Canvas-root fitter **removed**; `SafeAreaRoot`'s fitter **enabled** |
| `SprintFestivalPresentation.PrepareSafeArea` | disabled `SafeAreaRoot`'s fitter and zeroed its offsets | enables it and applies it |
| `MinigameUIAssembler.ConfigureCanvas` | `root.AddComponent<SafeAreaFitter>()` on the Canvas root | `EnsureSafeAreaFitter` puts it on `SafeAreaRoot`, never the root |

`SafeAreaFitter.Apply()` additionally now refuses to write on an axis whose anchors are coincident,
so this class of bug cannot be reintroduced by any future wiring.

Commit `0d56cef` ("keep sprint safe area single rooted") is what inverted it: the intent — exactly
one fitter applying — was right, but it kept the Canvas-root fitter and switched off the correctly
stretch-anchored one. The intent still holds: there is exactly one fitter, on `SafeAreaRoot`.

### Consequence: two workarounds were load-bearing on the bug

`SprintFestivalPresentation` had two "degenerate safe rect (batchmode's headless canvas)" branches —
in `EnsurePause` and `BuildControl` — that skipped the real anchor math when `safe` was zero-sized.
In batchmode that branch was always taken, so two PlayMode assertions had been pinned to the fallback
geometry rather than to `SprintUiLayout`:

- the pause button's corner-pin anchors `(1,1)`, now the spread anchors `SprintUiLayout.PauseRect`
  produces;
- the tap areas' authored `.01` inset, now `SprintUiLayout`'s `EdgeX` of `.02`.

Both assertions were corrected to derive from `SprintUiLayout`. The fallback branches were left in
place as genuine first-frame guards; they are simply no longer the path batchmode takes.

`UIComponentTests.SafeAreaFitterMapsLandscapeInsetsToBothHorizontalEdges` applied the fitter to a
default `RectTransform` — whose anchors are coincident — and asserted the resize. It now uses a
stretched rect, the only configuration where that assertion is meaningful.

### A defect the fix exposed

`WindCueHost` was parented directly to the Canvas root, outside `SafeAreaRoot`. It only satisfied the
existing "must be inside the safe-area hierarchy" assertion because the fitter was (wrongly) on the
Canvas root, an ancestor of everything. It is now parented under `SafeAreaRoot` in `MG_Sprint.unity`,
so the wind cue actually respects the safe area.

### Verification

| Suite | total | passed | failed | skipped |
|---|---|---|---|---|
| EditMode | 375 | 372 | 0 | 3 |
| PlayMode | 268 | 268 | 0 | 0 |

Against the loss-route gate's baseline (EditMode 369/366/0/3, PlayMode 268/268/0/0): EditMode gains
the 6 new `SafeAreaContractTests`, PlayMode is unchanged. The 3 skips are the same `[Ignore]`d parked
challenge tests. Counts read from the root `<test-run>` element, never from an exit code.

`Assets/Tests/EditMode/Presentation/SafeAreaContractTests.cs` pins the contract: the fitter leaves a
coincident-anchor rect unresized, still insets a stretched one, the prefab keeps no fitter on the
Canvas root and an enabled one on `SafeAreaRoot`, and neither the Sprint build nor the assembler may
undo that. All five failed before the fix, for the expected reasons.

### Visual verification — NOW POSSIBLE

The four acceptance criteria this gate had to leave open were blocked only by the collapsed canvas.
Editor Play-mode capture via `tools/qa-screenshot.sh` now returns a correctly laid-out HUD:

- `Builds/Screenshots/sprint-fixed.png` — start/countdown state
- `Builds/Screenshots/sprint-running.png` — race state

Scoreboard, rank pill, progress rail, mode chip, pause, countdown, instruction plate and both
controls all render in their intended positions, with Vietnamese diacritics unclipped.

**Still open, now visible for the first time** (pre-existing presentation issues, not regressions):

1. The `PlayerMarker` plate is far wider than the "PLAYER" label it backs and sits well above the
   runner's head, so it reads as a stray bar rather than a marker.
2. The mode chip "CHẠY NƯỚC RÚT · 100M" is low-contrast caption text over the blue sky.
3. Lane 0 of the four runner lanes sits above the red track art, in the sky region.

These were not investigated and are not addressed here.

---

## Lane alignment, small controls, and finish-line approach (2026-09-15)

Two of the three issues left open by the section above are fixed. Both were only visible once the
canvas rendered at real geometry.

### Runners now stand on the painted lanes

`Track.png` carries five painted lane lines — four lanes. Runner Y was a set of hand-tuned constants
in `SprintRivalMapping` (`2.1 / 0.7 / -0.7 / -2.1`) that had drifted off the artwork: the top runner
stood in the sky above the track, and the fourth painted lane sat empty behind the control buttons.

The line rows were measured off the asset itself and are identical at x = 10%, 50% and 90%:

| Painted line | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Row in `Track.png` (875 px tall) | 471.5 | 553.5 | 639 | 722 | 798 |

`SprintTrackLayout` is now the single source of truth. The backdrop keeps its authored size — bottom
edge on the viewport floor, so the track covers the screen with no filler needed — and every runner's
Y is derived from the painted rows, so the two cannot drift apart again:

| | Lane 1 | Lane 2 (player) | Lane 3 | Lane 4 |
|---|---|---|---|---|
| Before | 2.100 (in the sky) | 0.700 | -0.700 | -2.100 |
| After | 1.000 | -0.479 | -1.966 | -3.370 |

`KMA/Sprint/Align Track Lanes` applies this and is re-runnable.

### The controls sit on the lanes, drawn small

Lane 4 runs underneath the TRÁI/PHẢI buttons, which is the accepted arrangement rather than a problem
to design around: the buttons lie on the running lanes and the *drawn* button was shrunk so it stops
hiding them. The hit box is untouched.

`ScreenTapArea`'s own RectTransform is still the full `SprintUiLayout.ControlRect`
(`.43 x .26` of safe height) and still the raycast target. Only its `Visual` child shrank — it used to
stretch to fill the tap area and is now placed by `SprintUiLayout.ControlVisualRect01`
(`.52` wide, `.50` tall, sitting `.04` up from the tap area's floor, centred horizontally). That puts
the artwork where a thumb rests in landscape and clear of the runner in the lane above it.

Measured on the rendered frame at 1101x534 — drawn button against the touch target it answers for:

| | x | y | size |
|---|---|---|---|
| Hit box (unchanged) | 22 – 251.6 | 373.8 – 512.6 | 229.6 x 138.8 |
| Drawn button, predicted | 77.1 – 196.5 | 437.6 – 507.0 | 119.4 x 69.4 |
| Drawn button, measured | 77 – 196 | 438 – 506 | 119 x 68 |

The drawn button is about a quarter of the area a tap still lands in.

### The finish line approaches instead of appearing

`SprintFinishLinePresenter` used to `SetActive(true)` the ribbon the instant the runner passed 70 m,
so a full-height checkerboard appeared on the track out of nothing. It now enters from beyond the
right edge at 70 m and slides to its resting place as the runner reaches the line, at full opacity
throughout (`SprintUiLayout.FinishReveal01` / `FinishAnchorMinX`). The reveal distance is unchanged.

### Verification

| Suite | total | passed | failed | skipped |
|---|---|---|---|---|
| EditMode | 406 | 403 | 0 | 3 |
| PlayMode | 268 | 268 | 0 | 0 |

EditMode gains 31 tests over the previous section's 375 (`SprintTrackLayoutTests`, the finish-reveal
cases and the control-visual cases in `SprintUiLayoutTests`); PlayMode holds at 268. The 3 skips
remain the parked challenge tests.

Six PlayMode tests asserted geometry this work deliberately moved — four the old hand-tuned lane
constants, one the player's lane Y, and `SprintControls_AreRealButtonsMatchingTheirHitAreas`, which
asserted the visual filled its tap area exactly. That last one is now
`SprintControls_DrawASmallVisualInsideTheirFullSizeHitAreas` and asserts the new contract in both
directions: the visual is under half the tap area's area, and the tap area still measures the full
`ControlRect`. The rest were repointed at `SprintTrackLayout` rather than renumbered, so the scene and
the geometry stay locked together.

Measured from the rendered frames rather than from the code:

- `Builds/Screenshots/small-buttons.png` — the painted lines land at 182, 253, 400 and 467 px against
  a predicted 181.8, 253.4, 400.4, 466.8 (the fourth is behind the instruction plate); each runner
  stands on the band centre between them. The frame's bottom row is the track artwork's own bottom
  edge, so no sky shows beneath it.
- `finish-69m.png` — no ribbon on screen. `finish-75m.png` — left edge at 1072 px against a predicted
  1071.6. `finish-88m.png` — 995 px against 995.3. `finish-97m.png` — at rest as the runner arrives.

### Still open

- The `PlayerMarker` plate is far wider than the "PLAYER" label it backs.
- The mode chip's low contrast over the sky.
- The `←` / `→` glyphs on the control buttons do not exist in `Baloo2-ExtraBold` and render as blank
  space; the buttons read on their "TRÁI" / "PHẢI" labels alone. Pre-existing, and visible in every
  screenshot in this document.
