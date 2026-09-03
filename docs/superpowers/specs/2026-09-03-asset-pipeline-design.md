# Asset Pipeline Infrastructure Design

**Date:** 2026-09-03
**Status:** Approved in chat
**Product source of truth:** `PLAN.md` (mục 7 — Asset pipeline)
**Target:** Sub-project A of the asset-sourcing initiative — infrastructure only, no per-minigame content

## 1. Purpose

The project currently has zero art (`Assets/_Project/Art/` does not exist). Fonts, animation clips, prefabs, and scenes exist, but every visual surface — UI, the RivalRunner character, and sport props — has no source imagery. This spec defines the **infrastructure** that will produce those assets: how UI is generated with no external download, how the RivalRunner character is sourced and completed, how prop icons are generated, and how everything is imported into Unity with correct settings and license attribution.

This spec does not cover per-minigame background/environment art or the remaining six subjects' bespoke needs — that is Sub-project B, executed after this infrastructure exists, using the same tools this spec builds.

## 2. Product Boundary

### In scope

- A procedural generator that bakes neo-brutalist UI sprites (9-slice rounded-rect, radius 16, 4px black border) with no downloaded asset, for every `UITheme` color.
- A procedural generator that bakes the four missing sport prop icons (badminton shuttlecock, table-tennis paddle/ball, swimming, push-ups) as simple geometric shapes colored from `UITheme`.
- A one-time character-remix workflow: download Kenney Toon Characters (CC0), filter to the frames RivalRunner needs, recolor to a neutral skin matching `UITheme`, hand-paint the two missing poses (celebrate, a clearer stumble), and import as sprites bound to the existing `RivalRunner.controller`.
- A batch Editor import step (Unity CLI, batchmode) that applies correct `TextureImporter` settings (Sprite mode, PPU 100, filter Bilinear, ASTC compression, 9-slice border where relevant) to every new file this pipeline produces.
- An `EditMode` test that asserts the generated/imported assets exist with the correct import settings.
- A manual license-check gate before any externally sourced file is copied into the repo, with a mandatory `CREDITS.md` entry for that file.

### Out of scope

- Background/environment art for any of the 7 minigame scenes (Sub-project B).
- Sourcing or building prop/character art for the other 6 subjects beyond RivalRunner.
- Audio (music, additional SFX).
- Wiring the generated assets into gameplay prefabs/scenes beyond what already references them (`RivalRunner.controller` already expects these clip names; no controller changes here).
- Any hand-drawn art tool or pipeline beyond what's needed for the two RivalRunner poses.

## 3. Current Baseline

Verified by direct inspection (2026-09-03):

- No `Assets/_Project/Art/` directory exists.
- No `.png`/`.jpg`/`.svg`/`.fbx` files exist anywhere under `Assets/_Project/`.
- `Assets/_Project/Animations/RivalRunner*.anim` + `RivalRunner.controller` exist and reference 6 states (`Idle`, `Run`, `Burst`, `Celebrate`, `Fail`, `Stumble`) with no backing sprites.
- `Assets/_Project/CREDITS.md` currently documents only the TMP font fallback substitution — no art entries.
- Kenney.nl assets were downloaded and inspected directly (not just page descriptions):
  - **Sports Pack** (380 assets, CC0): top-down chibi characters, generic ball/bat/racket icons. No badminton shuttlecock, no table-tennis paddle/ball, no swimming, no push-ups content. Confirmed insufficient alone.
  - **UI Pack** (430 assets, CC0): thin rounded borders with gradient/gloss — not neo-brutalist. Confirmed a mismatch; procedural generation replaces it entirely (see §2 in-scope).
  - **Toon Characters** (CC0): `Male person` has `idle`, `walk0-7`, `run0-2`, `jump`, `climb0-1`, `hit` PNG poses in a usable side/three-quarter view. No dedicated celebrate pose; `hit` is a usable stumble base. Confirmed as the character source.
- Repo has an existing precedent for Editor-script-driven, batchmode-generated assets: `Assets/Editor/GenerateTask1Fonts.cs`, invoked via `unity -batchmode -quit -projectPath . -executeMethod GenerateTask1Fonts.Run` (same pattern `BuildScript.cs` uses for CI builds).
- Local tooling confirmed available: `unity` CLI (`/home/duydt/.local/bin/unity`), `python3` with pip (Pillow availability to be confirmed before implementation), network egress to `kenney.nl` confirmed reachable.

## 4. Architecture

Three independent branches. None blocks another; each can be implemented and tested in isolation.

```text
Branch 1: Procedural UI          Branch 2: Procedural props        Branch 3: Character remix
--------------------------       --------------------------        --------------------------
GenerateBrutalUISprites.cs       GeneratePropIcons.cs               download (curl, CC0 zip)
  -> Texture2D draw                -> Texture2D draw                  -> filter frames
  -> Sprite (9-slice)              -> Sprite                          -> recolor (Python/PIL)
  -> Art/UI/Generated/*.png        -> Art/Props/Generated/*.png       -> hand-paint 2 poses
                                                                       -> Art/Characters/RivalRunner/*.png
        \                                 |                                   /
         \                                |                                  /
          `------------------  Batch Editor import (shared)  ----------------`
                         TextureImporter settings pass
                         (Sprite mode, PPU 100, Bilinear, ASTC, 9-slice border)
                                        |
                              AssetDatabase.Refresh + Save
                                        |
                         CREDITS.md entry (Branch 3 only — Branches 1/2 are
                         code-generated, no external source, nothing to credit)
```

### 4.1 Branch 1 — Procedural UI (no external asset)

`Assets/Editor/GenerateBrutalUISprites.cs` (new, sibling to `GenerateTask1Fonts.cs`, same static-class-with-`Run()`-entry-point pattern):

- Reads color entries from `Assets/_Project/Settings/UI/UITheme.asset`.
- For each color, draws a square `Texture2D` at a fixed size (e.g. 64×64) with: flat fill of the theme color, a 4px solid black border, and rounded corners at radius 16 (alpha-cutout, not anti-aliased blend, to match the flat neo-brutalist look).
- Saves each as a `.png` under `Assets/_Project/Art/UI/Generated/`, then configures it as `Sprite (2D and UI)`, `Sprite Mode: Single`, border set for 9-slice (border = corner radius + outline width), PPU 100.
- The shadow layer stays a plain child `Image` with a solid dark color and a fixed offset, as already specified in `PLAN.md` §Nút — no separate shadow sprite needed.

### 4.2 Branch 2 — Procedural prop icons (no external asset)

`Assets/Editor/GeneratePropIcons.cs` (same pattern):

- Badminton shuttlecock → cone-ish shape (triangle + small circle base).
- Table-tennis → circle (ball) + a small paddle (rounded rect + short handle).
- Swimming → a simple wave glyph (stacked sine-like arcs).
- Push-ups → a horizontal bar/figure abstraction.
- All flat-filled from `UITheme`, saved to `Assets/_Project/Art/Props/Generated/`, imported as single sprites (no 9-slice needed — these are icons, not stretchable containers).

### 4.3 Branch 3 — Character remix (the one external source)

Manual/scripted steps, run once, output committed:

1. Download `kenney_toon-characters.zip` (CC0) into scratch space; verify `License.txt` says CC0 before anything is copied into the repo.
2. Extract only `Male person/PNG/Poses/character_malePerson_{idle,walk0..7,run0..2,jump,hit}.png`.
3. Python/PIL script recolors the outfit region to a `UITheme` accent color and desaturates/simplifies the face so the character reads as a neutral generic runner rather than the pack's named mascot. Script is checked into `Assets/Editor/Tooling/` (or `Tools/`) for repeatability, not run ad hoc.
4. Two additional frames — `Celebrate` and a clearer `Stumble` — are hand-painted (Aseprite/Figma, outside this repo's tooling) to match the recolored proportions, then dropped into the same input folder before import.
5. All resulting frames copied to `Assets/_Project/Art/Characters/RivalRunner/`.
6. Batch import (shared step, §4.4) applied — no 9-slice border here, plain sprites at a resolution matching the existing `.anim` clip's expected sprite size (verify against `RivalRunner_Idle.anim` etc. during implementation).
7. `Assets/_Project/CREDITS.md` gets a new entry: source URL, pack name, license (CC0), date, and a one-line note on what was modified (recolor + 2 added poses), matching the existing font-credit entry's format.

### 4.4 Shared batch import step

A single Editor method, `AssetImportPipeline.ApplyImportSettings(string folder)`, walks a given `Art/` subfolder and applies `TextureImporter` settings uniformly (texture type, PPU, filter mode, compression, and border for the UI folder only). Invoked once per branch via:

```
unity -batchmode -quit -projectPath . -executeMethod GenerateBrutalUISprites.Run
unity -batchmode -quit -projectPath . -executeMethod GeneratePropIcons.Run
unity -batchmode -quit -projectPath . -executeMethod AssetImportPipeline.ApplyImportSettings -importPath "Assets/_Project/Art/Characters/RivalRunner"
```

## 5. License & Credits Gate

Only Branch 3 touches externally authored content. Before any file from the Kenney zip is copied into `Assets/`:

- `License.txt` inside the zip must be read and confirmed CC0 (already verified for Toon Characters at design time — re-verified at implementation time in case the pack changed).
- `CREDITS.md` is updated in the same commit that adds the files — not after. This mirrors the existing font-credit entry, which was written before/with the asset it documents.

Branches 1 and 2 produce no entry — code-generated content baked by a script this repo owns has no external source to disclose.

## 6. Testing / Verification

No Editor GUI is available in this environment, so verification is config-level, not visual:

- `unity -batchmode -quit` run of each generator must exit 0 (compile + execution success).
- One new `EditMode` test (in `Assets/Tests/EditMode/`, following the existing suite's structure) per branch, asserting:
  - Branch 1/2: expected sprite files exist at the expected paths, `TextureImporter.spriteImportMode` and border values match spec.
  - Branch 3: all 8 required pose files exist under `Art/Characters/RivalRunner/`, each configured as a sprite at PPU 100.
- Visual correctness (does the button actually look neo-brutalist, does the character read as intended) **cannot be verified in this environment** and must be confirmed by the user opening the Editor locally. This limitation will be stated explicitly when reporting completion — it is not being silently skipped.

## 7. Open Items Deferred to Sub-project B

- Background/environment art for all 7 minigame scenes — apply the same "procedural/minimal-draw first" default already stated in `PLAN.md` §7 (dòng 186) unless a specific scene proves that's insufficient.
- Any subject-specific prop/character beyond RivalRunner and the 4 generated icons above.
- Audio sourcing.

These are intentionally not designed here — each should get its own short design (likely "bounded" scope, not architectural, since the pipeline this spec builds already exists for them to use) when work on them starts.
