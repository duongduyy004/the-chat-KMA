# Asset credits

## Vietnamese UI font source

The requested `Baloo2-ExtraBold` and `Nunito-Bold` TMP assets use the installed
Noto Sans ExtraBold and Noto Sans Bold font files as project-compatible local
fallback sources because Baloo 2 and Nunito were not installed in the build
environment. Noto Sans is distributed under the SIL Open Font License 1.1.
The asset filenames describe their intended UI roles; they do not claim that
the embedded font face is Baloo 2 or Nunito. The role assets link to a
project-owned `VietnameseFallback` TMP asset carrying the required UI glyph
set because some broad-range codepoints are not covered directly by the role
assets.

- Source: `/usr/share/fonts/truetype/noto/NotoSans-ExtraBold.ttf` (available locally)
- Source: `/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf` (available locally)
- License: SIL Open Font License 1.1

## Runner character sprites

The idle, run, and hit frames in `Art/Characters/Runner/` come from Kenney's
Toon Characters pack. They are used as the visual frames for the player and
Sprint rivals; Unity animation clips control frame timing and presentation.

- Source: `https://kenney.nl/assets/toon-characters`
- Pack: Kenney Toon Characters, version 1.0 (2019)
- Source files: `Male person/PNG/Poses/character_malePerson_{idle,run0,run1,run2,hit}.png`
- License: Creative Commons Zero (CC0 1.0)
- Retrieved: 2026-09-05
- Changes: filenames normalized for the Unity project; pixels unchanged

## Project-generated report demo art

The game logo mark, Android icon, home illustration, and Sprint environment
layers were generated specifically for this project with OpenAI image
generation on 2026-09-05. The logo intentionally contains no baked text;
Unity renders the exact Vietnamese product title with TextMeshPro.
