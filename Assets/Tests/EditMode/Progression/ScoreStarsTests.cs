using NUnit.Framework;
using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ScoreStarsTests
    {
        [TestCase(Rank.F, 0)]
        [TestCase(Rank.D, 1)]
        [TestCase(Rank.C, 2)]
        [TestCase(Rank.B, 2)]
        [TestCase(Rank.A, 3)]
        [TestCase(Rank.S, 3)]
        public void ToStars_UsesRankBands(Rank rank, int expected) =>
            Assert.That(ScoreUtil.ToStars(rank), Is.EqualTo(expected));
    }
}
