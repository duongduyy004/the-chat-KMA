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

The pose frames under `Art/Characters/` come from Kenney's Toon Characters pack.
Each of the four Sprint runners draws from a different pack character so the
player and the three rivals are told apart at a glance; Unity animation clips
control frame timing and presentation.

- Source: `https://kenney.nl/assets/toon-characters`
- Pack: Kenney Toon Characters, version 1.0 (2019)
- License: Creative Commons Zero (CC0 1.0)
- Retrieved: 2026-09-05 (`Male person`), 2026-09-15 (remaining characters and poses)
- Changes: filenames normalized for the Unity project; pixels unchanged

| Project folder | Pack character | Role |
| --- | --- | --- |
| `Characters/MaleAdventurer/` | `Male adventurer` | Player |
| `Characters/MalePerson/` | `Male person` | Rival, lane 1 |
| `Characters/FemalePerson/` | `Female person` | Rival, lane 3 |
| `Characters/FemaleAdventurer/` | `Female adventurer` | Rival, lane 4 |

Each folder holds the same eight poses, taken from that character's
`PNG/Poses/character_<name>_{idle,run0,run1,run2,hit,cheer0,cheer1,fallDown}.png`.
`cheer0`/`cheer1` drive the Celebrate state and `fallDown` drives Fail; before
this the two states reused the idle and hit poses.

## Project-generated report demo art

The game logo mark, Android icon, home illustration, and Sprint environment
layers were generated specifically for this project with OpenAI image
generation on 2026-09-05. The logo intentionally contains no baked text;
Unity renders the exact Vietnamese product title with TextMeshPro.


## Audio (2026-09-26)

KMA - Audio credits

Music by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0:
https://creativecommons.org/licenses/by/4.0/
Move Forward (menu/map): https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1300018
Cipher (sprint): https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1100844
Beachfront Celebration (volleyball): https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1200022
Winner Winner! (football): https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1400036
Original music recordings unchanged; encoded by Unity for playback and repeated in-game.

Volleyball Hit by Nicholas Judy / designerschoice, licensed under CC BY 4.0:
https://freesound.org/people/designerschoice/sounds/845535/
https://creativecommons.org/licenses/by/4.0/
Edits: converted to mono PCM, peak-normalized, edge fades added.

Kenney Interface Sounds, Music Jingles, Impact Sounds (CC0):
https://kenney.nl/assets/interface-sounds
https://kenney.nl/assets/music-jingles
https://kenney.nl/assets/impact-sounds
https://creativecommons.org/publicdomain/zero/1.0/

Additional CC0 sound recordings (trimmed, converted to mono, normalized and faded):
Referee whistle - Rosa-Orenes256: https://freesound.org/people/Rosa-Orenes256/sounds/538422/
Crowd Cheer - FoolBoyMedia: https://freesound.org/people/FoolBoyMedia/sounds/397434/
Running Footsteps - ralph.whitehead: https://freesound.org/people/ralph.whitehead/sounds/565708/
Footsteps on sand and gravel - fthgurdy: https://freesound.org/people/fthgurdy/sounds/528948/
SoccerBallKick - purchasing102: https://freesound.org/people/purchasing102/sounds/521825/

Source files and licenses are retained in Assets/_Project/Audio/ThirdParty.
Prepared cue offsets and hashes: Assets/_Project/Audio/Prepared/provenance.json.
