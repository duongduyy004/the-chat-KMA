using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay.Chess
{
    /// Checks that a solution tree is exhaustive: every listed move forces mate in the moves
    /// left against every defence, every defence is listed, and no unlisted move also forces it.
    public static class PuzzleVerifier
    {
        public static bool Verify(PuzzleDefinition puzzle, out string error)
        {
            if (puzzle == null) { error = "No puzzle."; return false; }
            if (!puzzle.TryValidateShape(out error)) return false;
            return VerifyNode(puzzle, 0, ChessPosition.FromFen(puzzle.startFen), puzzle.maxPlayerMoves, out error);
        }

        static bool VerifyNode(PuzzleDefinition puzzle, int index, ChessPosition position, int movesLeft,
            out string error)
        {
            PuzzleNode node = puzzle.nodes[index];
            List<ChessMove> legal = MoveGenerator.LegalMoves(position);
            var listed = new HashSet<string>();
            foreach (PuzzleMove entry in node.moves)
            {
                ChessMove.TryParseUci(entry.uci, out ChessMove move);
                if (!listed.Add(entry.uci)) { error = $"node {index}: {entry.uci} is listed twice."; return false; }
                if (!legal.Contains(move)) { error = $"node {index}: {entry.uci} is not legal."; return false; }
                ChessPosition after = position.Apply(move);
                if (entry.mate)
                {
                    if (!MoveGenerator.IsCheckmate(after))
                    {
                        error = $"node {index}: {entry.uci} is not checkmate.";
                        return false;
                    }
                    continue;
                }
                if (movesLeft <= 1) { error = $"node {index}: {entry.uci} is not checkmate on the last move."; return false; }

                List<ChessMove> defences = MoveGenerator.LegalMoves(after);
                string[] expected = defences.Select(m => m.ToUci()).OrderBy(u => u).ToArray();
                string[] given = entry.replies.Select(r => r.uci).OrderBy(u => u).ToArray();
                if (expected.Length == 0 || !expected.SequenceEqual(given))
                {
                    error = $"node {index}: replies to {entry.uci} must list exactly the {expected.Length} legal defences.";
                    return false;
                }
                foreach (PuzzleReply reply in entry.replies)
                {
                    ChessMove.TryParseUci(reply.uci, out ChessMove defence);
                    if (!VerifyNode(puzzle, reply.next, after.Apply(defence), movesLeft - 1, out error)) return false;
                }
            }

            foreach (ChessMove move in legal)
            {
                if (listed.Contains(move.ToUci())) continue;
                if (ForcesMate(position.Apply(move), movesLeft))
                {
                    error = $"node {index}: {move.ToUci()} also forces mate but is not listed.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// The defender is to move in `after`; true if the attacker mates within `movesLeft` attacker moves
        /// (the move that produced `after` counts as one).
        static bool ForcesMate(ChessPosition after, int movesLeft)
        {
            if (MoveGenerator.IsCheckmate(after)) return true;
            if (movesLeft <= 1) return false;
            List<ChessMove> defences = MoveGenerator.LegalMoves(after);
            if (defences.Count == 0) return false; // stalemate
            foreach (ChessMove defence in defences)
            {
                ChessPosition reply = after.Apply(defence);
                bool mated = MoveGenerator.LegalMoves(reply).Any(attack => ForcesMate(reply.Apply(attack), movesLeft - 1));
                if (!mated) return false;
            }
            return true;
        }
    }
}
