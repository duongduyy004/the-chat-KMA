using System;
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ChessPositionTests
    {
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        [TestCase(ChessPosition.StartFen)]
        [TestCase(Kiwipete)]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("rnbqkbnr/pp1ppppp/8/2pP4/8/8/PPP1PPPP/RNBQKBNR b KQkq c6 0 2")]
        public void FenRoundTrips(string fen) =>
            Assert.That(ChessPosition.FromFen(fen).ToFen(), Is.EqualTo(fen));

        [TestCase("")]
        [TestCase("8/8/8/8/8/8/8/8 w - - 0 1")]                       // no kings
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP w KQkq - 0 1")]   // 7 ranks
        [TestCase("rnbqkbnr/pppppppp/9/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR x KQkq - 0 1")]
        public void InvalidFenThrows(string fen) =>
            Assert.Throws<FormatException>(() => ChessPosition.FromFen(fen));

        [Test]
        public void SquaresAndUciRoundTrip()
        {
            Assert.That(Square.Parse("a1"), Is.EqualTo(0));
            Assert.That(Square.Parse("h8"), Is.EqualTo(63));
            Assert.That(Square.Parse("e9"), Is.EqualTo(-1));
            Assert.That(Square.Name(Square.Parse("e4")), Is.EqualTo("e4"));
            Assert.That(ChessMove.TryParseUci("a7a8n", out ChessMove promo), Is.True);
            Assert.That(promo.Promotion, Is.EqualTo(Piece.Knight));
            Assert.That(promo.ToUci(), Is.EqualTo("a7a8n"));
            Assert.That(ChessMove.TryParseUci("e2e4", out ChessMove plain), Is.True);
            Assert.That(plain.Promotion, Is.EqualTo(0));
            Assert.That(ChessMove.TryParseUci("e2e4k", out _), Is.False);
            Assert.That(ChessMove.TryParseUci("z2e4", out _), Is.False);
        }

        [Test]
        public void ApplyDoublePushSetsEnPassantAndKeepsOriginalUnchanged()
        {
            ChessPosition start = ChessPosition.FromFen(ChessPosition.StartFen);
            ChessMove.TryParseUci("e2e4", out ChessMove move);
            ChessPosition next = start.Apply(move);
            Assert.That(next.ToFen(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"));
            Assert.That(start.ToFen(), Is.EqualTo(ChessPosition.StartFen));
        }

        [Test]
        public void ApplyCastlingMovesTheRookAndDropsRights()
        {
            ChessPosition p = ChessPosition.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
            ChessMove.TryParseUci("e1g1", out ChessMove castle);
            Assert.That(p.Apply(castle).ToFen(), Is.EqualTo("r3k2r/8/8/8/8/8/8/R4RK1 b kq - 1 1"));
            ChessMove.TryParseUci("e1c1", out ChessMove longCastle);
            Assert.That(p.Apply(longCastle).ToFen(), Is.EqualTo("r3k2r/8/8/8/8/8/8/2KR3R b kq - 1 1"));
        }

        [Test]
        public void ApplyEnPassantRemovesTheCapturedPawn()
        {
            ChessPosition p = ChessPosition.FromFen("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3");
            ChessMove.TryParseUci("e5f6", out ChessMove ep);
            Assert.That(p.Apply(ep).ToFen(), Is.EqualTo("rnbqkbnr/ppp1p1pp/5P2/3p4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3"));
        }

        [Test]
        public void ApplyPromotionAndRookCaptureClearsRights()
        {
            ChessPosition p = ChessPosition.FromFen("r3k3/1P6/8/8/8/8/8/4K3 w q - 0 1");
            ChessMove.TryParseUci("b7a8n", out ChessMove promo);
            Assert.That(p.Apply(promo).ToFen(), Is.EqualTo("N3k3/8/8/8/8/8/8/4K3 b - - 0 1"));
        }

        [Test]
        public void ApplyRejectsMovingTheWrongSide()
        {
            ChessPosition start = ChessPosition.FromFen(ChessPosition.StartFen);
            ChessMove.TryParseUci("e7e5", out ChessMove move);
            Assert.Throws<InvalidOperationException>(() => start.Apply(move));
        }
    }
}
