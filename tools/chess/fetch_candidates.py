"""Stream the Lichess puzzle database and keep a few puzzles where White delivers mate.

The database FEN is the position *before* the opponent's opening move, so side 'b'
in the FEN means White solves. Stops reading once enough candidates are kept.

Usage:
  python tools/chess/fetch_candidates.py --theme mateIn2 --count 15
Writes tools/chess/cache/candidates-<theme>.csv (gitignored).
"""
import argparse
import csv
import io
import itertools
import os
import urllib.request

import zstandard

URL = "https://database.lichess.org/lichess_db_puzzle.csv.zst"
HERE = os.path.dirname(os.path.abspath(__file__))
COLUMNS = ["PuzzleId", "FEN", "Moves", "Rating", "RatingDeviation", "Popularity",
           "NbPlays", "Themes", "GameUrl", "OpeningTags"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--theme", default="mateIn2")
    parser.add_argument("--count", type=int, default=15)
    parser.add_argument("--min-rating", type=int, default=1000)
    parser.add_argument("--max-rating", type=int, default=1700)
    parser.add_argument("--max-pieces", type=int, default=16)
    parser.add_argument("--scan-limit", type=int, default=500000)
    args = parser.parse_args()

    os.makedirs(os.path.join(HERE, "cache"), exist_ok=True)
    out_path = os.path.join(HERE, "cache", f"candidates-{args.theme}.csv")
    request = urllib.request.Request(URL, headers={"User-Agent": "kma-chess-final-tools"})
    kept = []
    with urllib.request.urlopen(request) as response:
        reader = zstandard.ZstdDecompressor().stream_reader(response)
        rows = csv.reader(io.TextIOWrapper(reader, encoding="utf-8", newline=""))
        first = next(rows)
        has_header = bool(first) and first[0] == "PuzzleId"
        col = {name: i for i, name in enumerate(COLUMNS)}
        source = rows if has_header else itertools.chain([first], rows)
        for scanned, row in enumerate(source):
            if scanned >= args.scan_limit or len(kept) >= args.count:
                break
            fen = row[col["FEN"]].split()
            if args.theme not in row[col["Themes"]].split() or fen[1] != "b":
                continue
            if not args.min_rating <= int(row[col["Rating"]]) <= args.max_rating:
                continue
            if int(row[col["Popularity"]]) < 85 or int(row[col["NbPlays"]]) < 500:
                continue
            if sum(ch.isalpha() for ch in fen[0]) > args.max_pieces:
                continue
            kept.append(row)

    with open(out_path, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(COLUMNS)
        writer.writerows(kept)
    print(f"kept {len(kept)} -> {out_path}")


if __name__ == "__main__":
    main()
