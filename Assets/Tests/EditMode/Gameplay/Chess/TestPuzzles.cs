using KMA.Gameplay.Chess;

namespace KMA.Tests.Gameplay.Chess
{
    internal static class TestPuzzles
    {
        // After 1.e4 e5 2.Bc4 Nc6. The tree pretends 3.Qh5 Nf6 4.Qxf7# is forced; that
        // is enough to exercise the grader, which never searches.
        public const string ScholarStart = "r1bqkbnr/pppp1ppp/2n5/4p3/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 2 3";
        // After 3.Qh5 Nf6: Qxf7 is the only mate in one.
        public const string ScholarMateInOne = "r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4";

        public static PuzzleDefinition ScholarTwoMover() => new PuzzleDefinition
        {
            id = "test_m2", startFen = ScholarStart, playerColor = "w", objective = "mate",
            maxPlayerMoves = 2, timeLimitSeconds = 90f, maxRecoverableMistakes = 2, difficulty = "Normal",
            ideaHint = "Nhắm vào ô f7.",
            nodes = new[]
            {
                Node(Move("d1h5", false, Reply("g8f6", 1))),
                Node(Move("h5f7", true))
            }
        };

        public static PuzzleDefinition ScholarOneMover() => new PuzzleDefinition
        {
            id = "test_m1", startFen = ScholarMateInOne, playerColor = "w", objective = "mate",
            maxPlayerMoves = 1, timeLimitSeconds = 90f, maxRecoverableMistakes = 3, difficulty = "Easy",
            ideaHint = "Ô f7 chỉ có vua giữ.",
            nodes = new[] { Node(Move("h5f7", true)) }
        };

        public static PuzzleNode Node(params PuzzleMove[] moves) => new PuzzleNode { moves = moves };

        public static PuzzleMove Move(string uci, bool mate, params PuzzleReply[] replies) =>
            new PuzzleMove { uci = uci, mate = mate, replies = replies };

        public static PuzzleReply Reply(string uci, int next) => new PuzzleReply { uci = uci, next = next };

        public static ChessMove Uci(string text)
        {
            ChessMove.TryParseUci(text, out ChessMove move);
            return move;
        }
    }
}
