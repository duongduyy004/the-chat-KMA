using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class ScoreUtilTests
    {
        [TestCase(9f, Rank.S)]
        [TestCase(8f, Rank.A)]
        [TestCase(7f, Rank.B)]
        [TestCase(6f, Rank.C)]
        [TestCase(5f, Rank.D)]
        [TestCase(4.9f, Rank.F)]
        public void ToRank_UsesTenPointBoundaries(float score, Rank expected) =>
            Assert.That(ScoreUtil.ToRank(score), Is.EqualTo(expected));

        [Test]
        public void Build_PassedResult_ComposesAndRounds()
        {
            var result = ScoreUtil.Build(true, 1.64f, .72f, .56f);
            Assert.That(result.Score, Is.EqualTo(8.9f));
            Assert.That(result.Rank, Is.EqualTo(Rank.A));
            Assert.That(result.Pass, Is.True);
        }

        [Test]
        public void Build_FailedResult_IgnoresBonuses() =>
            Assert.That(ScoreUtil.Build(false, 2, 1, 1).Score, Is.Zero);
    }
}
