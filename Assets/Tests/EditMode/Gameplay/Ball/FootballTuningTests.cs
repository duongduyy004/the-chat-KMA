using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballTuningTests
    {
        [TestCase(FootballDifficulty.Easy, 2.4f, 1.4f, .40f, .75f)]
        [TestCase(FootballDifficulty.Normal, 1.7f, 1.4f, .27f, 1.20f)]
        [TestCase(FootballDifficulty.Hard, 1.1f, .9f, .15f, 1.70f)]
        public void PresetsMatchApprovedDifficultyValues(FootballDifficulty difficulty,
            float aim, float power, float reaction, float speed)
        {
            var tuning = FootballTuning.For(difficulty);

            Assert.That(tuning.AimTraverseSeconds, Is.EqualTo(aim));
            Assert.That(tuning.PowerRiseSeconds, Is.EqualTo(power));
            Assert.That(tuning.KeeperReactionSeconds, Is.EqualTo(reaction));
            Assert.That(tuning.KeeperSpeed, Is.EqualTo(speed));
        }

        [TestCase(0f, 1f, 1f, 1f)]
        [TestCase(1f, float.NaN, 1f, 1f)]
        [TestCase(1f, 1f, float.PositiveInfinity, 1f)]
        [TestCase(1f, 1f, 1f, -1f)]
        public void NonPositiveOrNonFiniteTuningIsRejected(float aim, float power,
            float reaction, float keeperSpeed)
        {
            Assert.That(() => new FootballTuning(aim, power, reaction, keeperSpeed),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void UnknownDifficultyIsRejected()
        {
            Assert.That(() => FootballTuning.For((FootballDifficulty)99),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void DefaultTuningCannotStartARulesSimulation()
        {
            Assert.That(() => new FootballRules(default),
                Throws.TypeOf<System.ArgumentException>());
        }
    }
}
