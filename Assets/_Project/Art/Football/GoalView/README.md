# Goal-facing Football artwork

Original project-authored vector artwork extracted from the user-approved `docs/proposals/football-goal-view.html`. No third-party character, stadium or goal images are included in these layers.

Regenerate SVG and 2x PNG layers with `/usr/bin/python3 tools/export-football-preview-art.py` (PyGObject/librsvg/pycairo).

The kicker (the hero, `MaleAdventurer`) and the keeper (`MalePerson`) are Kenney Toon Characters poses loaded by `ToonCharacterArt`; see `Assets/_Project/CREDITS.md`. The keeper's collision capsules in `FootballFlightSimulation` trace the `fall` pose drawn 108x144 preview px with its feet 21 px below the hip; `KeeperSilhouetteTests` fails if the sprite and the capsules drift apart. UI sprites outside GoalView retain their existing source and licensing.
