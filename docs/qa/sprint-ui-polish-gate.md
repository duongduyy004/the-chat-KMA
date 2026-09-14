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

`HUD_Minigame.prefab` is shared by all four playable minigames, so **Endurance, Volleyball and
Basketball are likely affected identically**. Not investigated.

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
