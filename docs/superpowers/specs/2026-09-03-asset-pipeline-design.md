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
- A one-time character-sourcing workflow: download Kenney Toon Characters (CC0), pick one frame (a running/athletic pose) as RivalRunner's single sprite, recolor it to a neutral skin matching `UITheme`, and import it — see §4.3 for why only one sprite is needed (not a pose set).
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
- `Assets/_Project/Animations/RivalRunner*.anim` + `RivalRunner.controller` exist and reference 6 states (`Idle`, `Run`, `Burst`, `Celebrate`, `Fail`, `Stumble`). **Verified by reading the clips directly: every clip's `m_PPtrCurves` is empty** — none of them swap sprites. Each only animates `Visual.m_LocalPosition.y` (a bounce) plus one more float curve. The actual image is a single `SpriteRenderer` on `RivalRunner.prefab`'s `Visual` child, currently pointing at Unity's built-in placeholder sprite (`fileID: 10913`, a built-in primitive) at `m_Size: {x: 0.2, y: 0.2}`. **RivalRunner needs exactly one static character sprite, not a pose set** — the 6 animator states differentiate by transform/scale curves already authored, not by frame swapping.
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

- Reads `Assets/_Project/Settings/UI/UITheme.asset` (via `KMA.Gameplay.UI.UITheme`) for `CornerRadius` (24) and `BorderWidth` (4) — **not** hardcoded, since these are already author-tunable fields on the asset.
- Bakes **one neutral sprite**, not one per color: a square `Texture2D` (128×128, scaled up from the 24px radius/4px border for clean 9-slice stretching) with a **white fill** and a **black border** (`UITheme.Border`), rounded corners at `CornerRadius` (alpha-cutout, no anti-aliasing, to match the flat neo-brutalist look). Confirmed against `Assets/_Project/Prefabs/UI/Btn_Brutal.prefab`: its `visual` Image already carries the theme color via `m_Color` (Image tint) with `m_Sprite: {fileID: 0}` — a white-fill sprite multiplies correctly with any tint, while the black border stays black under any tint (black × anything = black). One sprite serves every button color and the shadow (tinted black) without per-color rebakes.
- Saves as `Assets/_Project/Art/UI/Generated/ButtonBrutal.png`, then configures it as `Sprite (2D and UI)`, `Sprite Mode: Single`, 9-slice border set from `CornerRadius + BorderWidth`, PPU 100.
- The shadow layer stays a plain child `Image`, now also using this sprite (tinted black) instead of Unity's built-in flat-rect sprite, so the shadow's corners match the visual's rounded corners — fixing a corner mismatch visible in the current prefab.
- **Wiring this sprite into `Btn_Brutal.prefab` (or any other UI prefab) is out of scope for this plan** — see §2 Out of scope. This branch's deliverable is the generator + the generated, correctly-imported asset.

### 4.2 Branch 2 — Procedural prop icons (no external asset)

`Assets/Editor/GeneratePropIcons.cs` (same pattern):

- Badminton shuttlecock → cone-ish shape (triangle + small circle base).
- Table-tennis → circle (ball) + a small paddle (rounded rect + short handle).
- Swimming → a simple wave glyph (stacked sine-like arcs).
- Push-ups → a horizontal bar/figure abstraction.
- All flat-filled from `UITheme`, saved to `Assets/_Project/Art/Props/Generated/`, imported as single sprites (no 9-slice needed — these are icons, not stretchable containers).

### 4.3 Branch 3 — Character sourcing (the one external source)

`RivalRunner.prefab`'s `Visual` child has exactly one `SpriteRenderer`, currently assigned Unity's built-in placeholder sprite. None of the 6 animation clips swap sprites (confirmed empty `m_PPtrCurves` in every clip, §3) — they only animate transform/scale. So this branch needs **one** sprite, not a pose set:

1. Download `kenney_toon-characters.zip` (CC0) into scratch space; verify `License.txt` says CC0 before anything is copied into the repo.
2. Pick one frame — `Male person/PNG/Poses/character_malePerson_run1.png` (a mid-stride running pose, appropriate for a runner rival) — as the single source image.
3. Python/PIL script recolors the outfit region to a `UITheme` accent color and simplifies the face so the character reads as a neutral generic runner rather than the pack's named mascot. Script is checked into `Assets/Editor/Tooling/` for repeatability, not run ad hoc.
4. Copy the result to `Assets/_Project/Art/Characters/RivalRunner/RivalRunner.png`.
5. Batch import (shared step, §4.4) applied — plain single sprite, PPU chosen so the imported sprite's world size matches the prefab's existing `m_Size: {x: 0.2, y: 0.2}` (verify the exact PPU/pixel-dimension math during implementation against the source frame's pixel size).
6. Assign the imported sprite to `RivalRunner.prefab`'s `Visual` → `SpriteRenderer.m_Sprite`, replacing the built-in placeholder. This is the one piece of "wiring into an existing prefab" this plan does perform — unlike the UI branch, there is only one consumer and one field, not a fan-out across many prefabs, so it stays in scope.
7. `Assets/_Project/CREDITS.md` gets a new entry: source URL, pack name, license (CC0), date, and a one-line note on what was modified (recolor only), matching the existing font-credit entry's format.

A multi-pose sprite-swap upgrade (celebrate/stumble as distinct frames) is explicitly not built here — there is no animation mechanism in the repo today that would consume it. If that's wanted later, it's a new, separately-scoped change to the animation clips themselves, not an asset-pipeline task.

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
  - Branch 3: `RivalRunner.png` exists under `Art/Characters/RivalRunner/`, is configured as a single sprite at the correct PPU, and `RivalRunner.prefab`'s `Visual/SpriteRenderer.m_Sprite` no longer points at the built-in placeholder (`fileID: 10913`).
- Visual correctness (does the button actually look neo-brutalist, does the character read as intended) **cannot be verified in this environment** and must be confirmed by the user opening the Editor locally. This limitation will be stated explicitly when reporting completion — it is not being silently skipped.

## 7. Open Items Deferred to Sub-project B

- Background/environment art for all 7 minigame scenes — apply the same "procedural/minimal-draw first" default already stated in `PLAN.md` §7 (dòng 186) unless a specific scene proves that's insufficient.
- Any subject-specific prop/character beyond RivalRunner and the 4 generated icons above.
- Audio sourcing.

These are intentionally not designed here — each should get its own short design (likely "bounded" scope, not architectural, since the pipeline this spec builds already exists for them to use) when work on them starts.
