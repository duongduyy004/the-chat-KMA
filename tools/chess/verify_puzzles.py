"""Build exhaustive, Stockfish-ordered solution trees for the puzzles in puzzle_sources.json.

For each source: apply the opponent's opening move from Lichess `Moves` to get startFen,
check White is to move and Stockfish sees mate within N, then search every White move.
A White move enters the tree only if it forces mate in the moves left against *every*
defence, and every legal defence is listed (longest resistance first, so the boss plays
the toughest one). The Lichess main line must be in the tree. Any failure exits 1.

Usage:
  python tools/chess/verify_puzzles.py [--engine PATH] [--depth 18]
"""
import argparse
import csv
import glob
import json
import os
import sys

import chess
import chess.engine

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Chess", "Puzzles")
# difficulty -> (player moves, think seconds, recoverable mistakes); spec section 2 table
DIFFICULTY = {"Easy": (1, 90, 3), "Normal": (2, 90, 2), "Hard": (3, 120, 1)}


def fail(message):
    print("FAIL: " + message, file=sys.stderr)
    sys.exit(1)


def load_candidates():
    rows = {}
    for path in glob.glob(os.path.join(HERE, "cache", "candidates-*.csv")):
        with open(path, encoding="utf-8", newline="") as handle:
            for row in csv.DictReader(handle):
                rows[row["PuzzleId"]] = row
    return rows


def forces(board, moves_left):
    """Defender to move. True if the attacker mates within moves_left attacker moves
    (the move that produced this position counts as one)."""
    if board.is_checkmate():
        return True
    if moves_left <= 1 or board.is_game_over():
        return False
    for defence in list(board.legal_moves):
        board.push(defence)
        ok = any(attack_forces(board, attack, moves_left - 1) for attack in list(board.legal_moves))
        board.pop()
        if not ok:
            return False
    return True


def attack_forces(board, move, moves_left):
    board.push(move)
    ok = forces(board, moves_left)
    board.pop()
    return ok


class TreeBuilder:
    def __init__(self, engine, depth):
        self.engine = engine
        self.depth = depth
        self.nodes = []

    def mate_distance(self, board):
        info = self.engine.analyse(board, chess.engine.Limit(depth=self.depth))
        return info["score"].white().mate()

    def node(self, board, moves_left):
        index = len(self.nodes)
        self.nodes.append(None)
        candidates = [m for m in board.legal_moves if attack_forces(board, m, moves_left)]
        if not candidates:
            fail(f"no forcing move at {board.fen()}")

        def player_key(move):
            board.push(move)
            mate = 0 if board.is_checkmate() else (self.mate_distance(board) or 99)
            board.pop()
            return (mate, move.uci())  # fastest mate first, so hints show the cleanest line

        entries = []
        for move in sorted(candidates, key=player_key):
            board.push(move)
            if board.is_checkmate():
                entries.append({"uci": move.uci(), "mate": True, "replies": []})
            else:
                replies = []
                for defence in self.order_defences(board):
                    board.push(defence)
                    replies.append({"uci": defence.uci(), "next": self.node(board, moves_left - 1)})
                    board.pop()
                entries.append({"uci": move.uci(), "mate": False, "replies": replies})
            board.pop()
        self.nodes[index] = {"moves": entries}
        return index

    def order_defences(self, board):
        scored = []
        for defence in board.legal_moves:
            board.push(defence)
            mate = self.mate_distance(board)
            board.pop()
            scored.append((-(mate if mate is not None else 99), defence.uci(), defence))
        scored.sort(key=lambda item: (item[0], item[1]))
        return [defence for _, _, defence in scored]


def main_line_in_tree(nodes, solution):
    node = 0
    entry = None
    for ply, uci in enumerate(solution):
        if ply % 2 == 0:
            entry = next((m for m in nodes[node]["moves"] if m["uci"] == uci), None)
            if entry is None:
                return False
        else:
            reply = next((r for r in entry["replies"] if r["uci"] == uci), None)
            if reply is None:
                return False
            node = reply["next"]
    return True


def main():
    parser = argparse.ArgumentParser()
    found = glob.glob(os.path.join(HERE, "stockfish", "**", "stockfish*.exe"), recursive=True)
    parser.add_argument("--engine", default=found[0] if found else None)
    parser.add_argument("--depth", type=int, default=18)
    args = parser.parse_args()
    if not args.engine:
        fail("Stockfish not found; see tools/chess/README.md")

    with open(os.path.join(HERE, "puzzle_sources.json"), encoding="utf-8") as handle:
        sources = json.load(handle)
    rows = load_candidates()
    os.makedirs(OUT, exist_ok=True)

    with chess.engine.SimpleEngine.popen_uci(args.engine) as engine:
        engine_name = engine.id.get("name", "Stockfish")
        for source in sources:
            row = rows.get(source["lichessId"])
            if row is None:
                fail(f"{source['lichessId']} is not in tools/chess/cache")
            if source["difficulty"] not in DIFFICULTY:
                fail(f"{source['id']}: unknown difficulty {source['difficulty']}")
            moves_needed, seconds, mistakes = DIFFICULTY[source["difficulty"]]
            board = chess.Board(row["FEN"])
            line = row["Moves"].split()
            opener = chess.Move.from_uci(line[0])
            if opener not in board.legal_moves:
                fail(f"{source['id']}: opener {line[0]} is illegal")
            board.push(opener)
            if board.turn != chess.WHITE:
                fail(f"{source['id']}: White must be to move after the opener")
            solution = line[1:]
            if len(solution) != 2 * moves_needed - 1:
                fail(f"{source['id']}: solution has {len(solution)} plies, "
                     f"{source['difficulty']} needs {2 * moves_needed - 1}")
            builder = TreeBuilder(engine, args.depth)
            mate = builder.mate_distance(board)
            if mate is None or not 0 < mate <= moves_needed:
                fail(f"{source['id']}: Stockfish sees mate {mate}, expected within {moves_needed}")

            builder.node(board.copy(), moves_needed)
            if not main_line_in_tree(builder.nodes, solution):
                fail(f"{source['id']}: the Lichess main line is not in the tree")

            data = {
                "id": source["id"],
                "sourcePuzzleId": "lichess:" + source["lichessId"],
                "sourceFen": row["FEN"],
                "sourceMoves": row["Moves"],
                "startFen": board.fen(),
                "playerColor": "w",
                "objective": "mate",
                "maxPlayerMoves": moves_needed,
                "timeLimitSeconds": seconds,
                "maxRecoverableMistakes": mistakes,
                "difficulty": source["difficulty"],
                "ideaHint": source["ideaHint"],
                "verifiedBy": f"python-chess {chess.__version__} + {engine_name} depth {args.depth}",
                "nodes": builder.nodes,
            }
            path = os.path.join(OUT, source["id"] + ".json")
            with open(path, "w", encoding="utf-8", newline="\n") as handle:
                json.dump(data, handle, ensure_ascii=False, indent=2)
                handle.write("\n")
            print(f"ok {source['id']}: {len(builder.nodes)} nodes -> {os.path.relpath(path, ROOT)}")


if __name__ == "__main__":
    main()
