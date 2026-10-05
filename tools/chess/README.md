# Chess puzzle authoring

The final exam grades moves only against verified JSON in
`Assets/_Project/Resources/Chess/Puzzles/`. Never hand-edit those files.

## Setup (Windows, once)

    python -m pip install -r tools/chess/requirements.txt
    curl -L -o tools/chess/stockfish.zip https://github.com/official-stockfish/Stockfish/releases/latest/download/stockfish-windows-x86-64-universal.zip
    unzip tools/chess/stockfish.zip -d tools/chess/stockfish

## Add a puzzle

1. `python tools/chess/fetch_candidates.py --theme mateIn2 --count 15` (or `mateIn1`).
2. Pick a candidate. Add `{ id, lichessId, difficulty, ideaHint }` to `puzzle_sources.json`.
   `difficulty` is `Easy` (mate in 1) or `Normal` (mate in 2).
   `ideaHint` is one short Vietnamese sentence that does not name the move.
3. `python tools/chess/verify_puzzles.py` writes `<id>.json`. Exit code 1 means rejected.
4. Run the Unity EditMode filter `KMA.Tests.Gameplay.Chess` to re-check every file in C#.

The journey uses the first `Normal` puzzle by file name. Lichess puzzles are CC0.
