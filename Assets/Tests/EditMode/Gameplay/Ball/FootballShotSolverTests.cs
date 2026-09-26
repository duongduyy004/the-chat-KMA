using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballShotSolverTests
    {
        FootballTuning slowKeeper;

        [SetUp]
        public void SetUp() => slowKeeper = new FootballTuning(1.7f, 1.4f, 3f, .75f);

        [TestCase(.119f, true, .9f)]
        [TestCase(.12f, false, .9f)]
        [TestCase(1f, false, .4f)]
        public void FlightTimeUsesPowerAndShortShotThreshold(float power, bool isShort, float flight)
        {
            var shot = FootballShotSolver.Create(.9f, power, 0f);

            Assert.That(shot.IsShort, Is.EqualTo(isShort));
            Assert.That(shot.FlightSeconds, Is.EqualTo(flight).Within(.0001f));
            Assert.That(FootballShotSolver.Resolve(shot, slowKeeper),
                Is.EqualTo(isShort ? FootballOutcome.Miss : FootballOutcome.Goal));
        }

        [Test]
        public void AccurateShotDoesNotDriftAndHighPowerDriftsExactlyOnce()
        {
            Assert.That(FootballShotSolver.Create(.9f, .85f, 1f).TargetX, Is.EqualTo(.9f));
            Assert.That(FootballShotSolver.Create(.9f, 1f, 1f).TargetX, Is.EqualTo(1.2f).Within(.0001f));
            Assert.That(FootballShotSolver.Resolve(
                FootballShotSolver.Create(.9f, 1f, 1f), slowKeeper), Is.EqualTo(FootballOutcome.Miss));
        }

        [Test]
        public void KeeperRespectsReactionDelayAndReachBoundary()
        {
            var shot = FootballShotSolver.Create(.3f, .85f, 0f);
            Assert.That(FootballShotSolver.KeeperX(shot, slowKeeper, .2f), Is.Zero);
            var responsiveKeeper = new FootballTuning(1.7f, 1.4f, .2f, .75f);
            Assert.That(FootballShotSolver.KeeperX(shot, responsiveKeeper, 1f), Is.EqualTo(.3f).Within(.0001f));
            Assert.That(FootballShotSolver.Resolve(
                FootballShotSolver.Create(.22f, .85f, 0f), slowKeeper), Is.EqualTo(FootballOutcome.Saved));
            Assert.That(FootballShotSolver.Resolve(
                FootballShotSolver.Create(.2201f, .85f, 0f), slowKeeper), Is.EqualTo(FootballOutcome.Goal));
        }

        [TestCase(.96f, FootballOutcome.Goal)]
        [TestCase(.9601f, FootballOutcome.Miss)]
        public void BallMustFitInsideTheGoal(float targetX, FootballOutcome expected)
        {
            var shot = FootballShotSolver.Create(.9f, 1f, (targetX - .9f) / .3f);

            Assert.That(FootballShotSolver.Resolve(shot, slowKeeper), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidShotArgumentsAreRejected()
        {
            Assert.That(() => FootballShotSolver.Create(float.NaN, .5f, 0f),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => FootballShotSolver.Create(0f, .5f, 1.01f),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => FootballShotSolver.KeeperX(
                FootballShotSolver.Create(0f, .5f, 0f), slowKeeper, float.NaN),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
