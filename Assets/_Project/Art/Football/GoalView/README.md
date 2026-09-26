# Goal-facing Football artwork

Original project-authored vector artwork extracted from the user-approved `docs/proposals/football-goal-view.html`. No third-party character, stadium or goal images are included in these layers.

Regenerate SVG and 2x PNG layers with `/usr/bin/python3 tools/export-football-preview-art.py` (PyGObject/librsvg/pycairo).

The keeper collider capsules use the same authored pixel-space body/limb geometry as keeper.svg. Its sprite pivot is (0.5, 27/130); player pivot is (65/140, 0.5). Keep those pivots and shapes synchronized when editing character art. UI sprites outside GoalView retain their existing source and licensing.
