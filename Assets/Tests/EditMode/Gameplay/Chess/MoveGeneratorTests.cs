using System.Linq;
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class MoveGeneratorTests
    {
        // Reference counts from the Chess Programming Wiki "Perft Results" page.
        [TestCase(ChessPosition.StartFen, 1, 20L)]
        [TestCase(ChessPosition.StartFen, 2, 400L)]
        [TestCase(ChessPosition.StartFen, 3, 8902L)]
        [TestCase(ChessPosition.StartFen, 4, 197281L)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 1, 48L)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 2, 2039L)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 3, 97862L)]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 4, 43238L)]
        [TestCase("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 3, 9467L)]
        [TestCase("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62379L)]
        public void PerftMatchesReference(string fen, int depth, long expected) =>
            Assert.That(MoveGenerator.Perft(ChessPosition.FromFen(fen), depth), Is.EqualTo(expected));

        [Test]
        public void CastlingThroughAnAttackedSquareIsIllegal()
        {
            // The black rook on f8 covers f1, so White may castle queen side only.
            ChessPosition p = ChessPosition.FromFen("5r1k/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            string[] moves = MoveGenerator.LegalMoves(p).Select(m => m.ToUci()).ToArray();
            Assert.That(moves, Does.Not.Contain("e1g1"));
            Assert.That(moves, Does.Contain("e1c1"));
        }

        [Test]
        public void EnPassantThatExposesTheKingAlongTheRankIsIllegal()
        {
            ChessPosition p = ChessPosition.FromFen("8/8/8/K2pP2r/8/8/8/7k w - d6 0 1");
            Assert.That(MoveGenerator.LegalMoves(p).Select(m => m.ToUci()), Does.Not.Contain("e5d6"));
        }

        [Test]
        public void PromotionOffersAllFourPieces()
        {
            ChessPosition p = ChessPosition.FromFen("7k/P7/8/8/8/8/8/K7 w - - 0 1");
            string[] promotions = MoveGenerator.LegalMoves(p).Select(m => m.ToUci())
                .Where(u => u.StartsWith("a7a8")).OrderBy(u => u).ToArray();
            Assert.That(promotions, Is.EqualTo(new[] { "a7a8b", "a7a8n", "a7a8q", "a7a8r" }));
        }

        [Test]
        public void DetectsCheckmateAndStalemate()
        {
            ChessPosition mate = ChessPosition.FromFen("R5k1/5ppp/8/8/8/8/8/6K1 b - - 1 1");
            Assert.That(MoveGenerator.IsCheckmate(mate), Is.True);
            Assert.That(MoveGenerator.IsStalemate(mate), Is.False);

            ChessPosition stalemate = ChessPosition.FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");
            Assert.That(MoveGenerator.IsStalemate(stalemate), Is.True);
            Assert.That(MoveGenerator.IsCheckmate(stalemate), Is.False);
        }
    }
}
