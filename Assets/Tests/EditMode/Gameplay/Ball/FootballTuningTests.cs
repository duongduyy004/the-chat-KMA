using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballTuningTests
    {
        [TestCase(FootballDifficulty.Easy, 2.4f, .38f, 1.9f)]
        [TestCase(FootballDifficulty.Normal, 2.042035f, .23f, 2.5f)]
        [TestCase(FootballDifficulty.Hard, 1.7f, .15f, 3.2f)]
        public void PresetsMatchApprovedDifficultyValues(FootballDifficulty difficulty,
            float power, float reaction, float speed)
        {
            var tuning = FootballTuning.For(difficulty);

            Assert.That(tuning.PowerRiseSeconds, Is.EqualTo(power));
            Assert.That(tuning.KeeperReactionSeconds, Is.EqualTo(reaction));
            Assert.That(tuning.KeeperSpeed, Is.EqualTo(speed));
        }

        [TestCase(0f, 1f, 1f)]
        [TestCase(float.NaN, 1f, 1f)]
        [TestCase(1f, float.PositiveInfinity, 1f)]
        [TestCase(1f, 1f, -1f)]
        public void NonPositiveOrNonFiniteTuningIsRejected(float power,
            float reaction, float keeperSpeed)
        {
            Assert.That(() => new FootballTuning(power, reaction, keeperSpeed),
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
