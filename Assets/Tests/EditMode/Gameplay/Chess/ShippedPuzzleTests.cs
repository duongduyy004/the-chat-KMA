using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ShippedPuzzleTests
    {
        [Test]
        public void TheJourneyPuzzleIsANormalWhiteMateInTwo()
        {
            PuzzleDefinition puzzle = ChessPuzzleLibrary.ForDifficulty(ChallengeDifficulty.Normal);
            Assert.That(puzzle.playerColor, Is.EqualTo("w"));
            Assert.That(puzzle.maxPlayerMoves, Is.EqualTo(2));
            Assert.That(puzzle.timeLimitSeconds, Is.EqualTo(90f));
            Assert.That(puzzle.maxRecoverableMistakes, Is.EqualTo(2));
        }

        [Test, Timeout(180000)]
        public void EveryShippedPuzzleIsExhaustiveAndSourced()
        {
            var puzzles = ChessPuzzleLibrary.LoadAll();
            Assert.That(puzzles, Is.Not.Empty);
            Assert.That(puzzles.Select(p => p.id).Distinct().Count(), Is.EqualTo(puzzles.Count));
            foreach (PuzzleDefinition puzzle in puzzles)
            {
                Assert.That(PuzzleVerifier.Verify(puzzle, out string error), Is.True, $"{puzzle.id}: {error}");
                Assert.That(puzzle.sourcePuzzleId, Does.StartWith("lichess:"), puzzle.id);
                Assert.That(puzzle.ideaHint, Is.Not.Null.And.Not.Empty, puzzle.id);
                Assert.That(puzzle.ideaHint, Does.Not.Contain("—"), puzzle.id);
            }
        }
    }
}
