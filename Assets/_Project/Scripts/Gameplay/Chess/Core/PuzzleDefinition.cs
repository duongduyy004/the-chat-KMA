using System;

namespace KMA.Gameplay.Chess
{
    [Serializable]
    public sealed class PuzzleDefinition
    {
        public string id;
        public string sourcePuzzleId;
        public string sourceFen;
        public string sourceMoves;
        public string startFen;
        public string playerColor = "w";
        public string objective = "mate";
        public int maxPlayerMoves;
        public float timeLimitSeconds;
        public int maxRecoverableMistakes;
        public string difficulty;
        public string ideaHint;
        public string verifiedBy;
        public PuzzleNode[] nodes;

        public PieceColor PlayerColor => playerColor == "b" ? PieceColor.Black : PieceColor.White;

        /// Structural checks only; PuzzleVerifier checks the chess.
        public bool TryValidateShape(out string error)
        {
            error = null;
            if (playerColor != "w" && playerColor != "b") error = "playerColor must be w or b.";
            else if (objective != "mate") error = "Only the mate objective is supported.";
            else if (maxPlayerMoves < 1) error = "maxPlayerMoves must be at least 1.";
            else if (!(timeLimitSeconds > 0f)) error = "timeLimitSeconds must be positive.";
            else if (maxRecoverableMistakes < 0) error = "maxRecoverableMistakes cannot be negative.";
            else if (nodes == null || nodes.Length == 0) error = "The puzzle has no nodes.";
            if (error != null) return false;

            ChessPosition start;
            try { start = ChessPosition.FromFen(startFen); }
            catch (FormatException exception) { error = "startFen: " + exception.Message; return false; }
            if (start.SideToMove != PlayerColor)
            {
                error = "startFen must have the player to move.";
                return false;
            }

            for (int n = 0; n < nodes.Length; n++)
            {
                PuzzleNode node = nodes[n];
                if (node?.moves == null || node.moves.Length == 0)
                {
                    error = $"node {n} has no moves.";
                    return false;
                }
                foreach (PuzzleMove move in node.moves)
                {
                    if (move == null || !ChessMove.TryParseUci(move.uci, out _))
                    {
                        error = $"node {n} has an unreadable move.";
                        return false;
                    }
                    if (move.mate != (move.replies == null || move.replies.Length == 0))
                    {
                        error = $"node {n} move {move.uci}: a mate has no replies and a non-mate has some.";
                        return false;
                    }
                    if (move.replies == null) continue;
                    foreach (PuzzleReply reply in move.replies)
                    {
                        if (reply == null || !ChessMove.TryParseUci(reply.uci, out _) ||
                            reply.next < 0 || reply.next >= nodes.Length)
                        {
                            error = $"node {n} move {move.uci} has a reply with a bad node index or move.";
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [Serializable]
    public sealed class PuzzleNode
    {
        public PuzzleMove[] moves;
    }

    [Serializable]
    public sealed class PuzzleMove
    {
        public string uci;
        public bool mate;
        public PuzzleReply[] replies;
    }

    [Serializable]
    public sealed class PuzzleReply
    {
        public string uci;
        public int next;
    }
}
