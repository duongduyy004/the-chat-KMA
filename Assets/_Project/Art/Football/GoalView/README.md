# Goal-facing Football artwork

Project-authored vector layers in the shared flat cartoon style (dark navy outlines, few flat colours). No third-party character, stadium or goal images are included.

Regenerate the SVG and 2x PNG layers with `node tools/render-football-goalview-art.js Assets/_Project/Art/Football/GoalView` (needs `npm i @resvg/resvg-js`). The goal mouth, field lines and penalty spot keep the 1200 × 675 preview geometry that `FootballShotSolver` projects onto.

The kicker (the hero, `MaleAdventurer`) and the keeper (`MalePerson`) are project character poses loaded by `CharacterArt`; see `Assets/_Project/CREDITS.md`. The keeper's collision capsules in `FootballFlightSimulation` trace the `cheer1` pose drawn 108x144 preview px with its feet 21 px below the hip; `KeeperSilhouetteTests` fails if the sprite and the capsules drift apart.
