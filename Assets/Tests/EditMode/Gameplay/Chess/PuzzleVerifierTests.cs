using KMA.Gameplay.Chess;
using NUnit.Framework;
using static KMA.Tests.Gameplay.Chess.TestPuzzles;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class PuzzleVerifierTests
    {
        [Test]
        public void CompleteMateInOneTreePasses()
        {
            Assert.That(PuzzleVerifier.Verify(ScholarOneMover(), out string error), Is.True, error);
        }

        [Test]
        public void MissingDefenceFails()
        {
            // Black has many answers to Qh5; the tree lists only Nf6.
            Assert.That(PuzzleVerifier.Verify(ScholarTwoMover(), out string error), Is.False);
            Assert.That(error, Does.Contain("replies"));
        }

        [Test]
        public void LeafThatIsNotMateFails()
        {
            PuzzleDefinition puzzle = ScholarOneMover();
            puzzle.nodes[0] = Node(Move("c4f7", true));
            Assert.That(PuzzleVerifier.Verify(puzzle, out string error), Is.False);
            Assert.That(error, Does.Contain("not checkmate"));
        }

        [Test]
        public void UnlistedMatingMoveFails()
        {
            // Two mates in one exist; the tree lists only one.
            var puzzle = new PuzzleDefinition
            {
                startFen = "6k1/5ppp/8/8/8/8/5PPP/RR4K1 w - - 0 1", playerColor = "w", maxPlayerMoves = 1,
                timeLimitSeconds = 90f, maxRecoverableMistakes = 2,
                nodes = new[] { Node(Move("a1a8", true)) }
            };
            Assert.That(PuzzleVerifier.Verify(puzzle, out string error), Is.False);
            Assert.That(error, Does.Contain("b1b8"));
        }

        [Test]
        public void ShapeErrorsAreReported()
        {
            PuzzleDefinition wrongSide = ScholarOneMover();
            wrongSide.startFen = wrongSide.startFen.Replace(" w ", " b ");
            Assert.That(wrongSide.TryValidateShape(out _), Is.False);

            PuzzleDefinition badIndex = ScholarTwoMover();
            badIndex.nodes[0].moves[0].replies[0].next = 7;
            Assert.That(badIndex.TryValidateShape(out string error), Is.False);
            Assert.That(error, Does.Contain("node"));
        }
    }
}
