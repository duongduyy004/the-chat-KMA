# Football Penalty Art Sources

Every Football asset is authored for this project; no third-party art remains.

| Output | Source | Notes |
|---|---|---|
| `GoalView/*.svg`, `GoalView/*.png` | `tools/render-football-goalview-art.js` | Flat cartoon field, goal, net, ball, crosshair and shadow drawn as SVG in the shared character style and rendered at 2x |
| `GoalView/knob.*` | Project-authored | Plain white circle tinted at runtime |
| `Characters/player.png` | Project image generation, 2026-10-05 | Main hero standing pose, 255 × 320 RGBA; not referenced by the scene (the kicker uses `Art/Characters/MaleAdventurer`) |
| `Characters/keeper.png` | Project image generation, 2026-10-05 | Tân Thủ keeper stance, 255 × 320 RGBA; not referenced by the scene (the keeper uses `Art/Characters/MalePerson`) |
