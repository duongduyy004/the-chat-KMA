using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class BadmintonRulesTests
    {
        [TestCase(.8f, .2f, BadmintonShot.Lift)]
        [TestCase(.8f, .5f, BadmintonShot.Drive)]
        [TestCase(.8f, .85f, BadmintonShot.Smash)]
        [TestCase(1.01f, .85f, BadmintonShot.Overcharge)]
        public void Release_UsesChargeAndContactHeight(float charge, float height, BadmintonShot expected)
        {
            Assert.That(new BadmintonRules().Release(charge, height), Is.EqualTo(expected));
        }

        [Test]
        public void Release_UsesAuthoredHeightBands()
        {
            var rules = new BadmintonRules();

            Assert.That(rules.Release(.8f, .35f), Is.EqualTo(BadmintonShot.Drive));
            Assert.That(rules.Release(.8f, .7f), Is.EqualTo(BadmintonShot.Smash));
        }

        [Test]
        public void AuthoredExchange_IsDeterministicAndScoresOnlyAfterValidRelease()
        {
            var first = new BadmintonRules();
            var second = new BadmintonRules();

            Assert.That(first.AuthoredPattern.Exchanges.Count, Is.EqualTo(5));
            for (var i = 0; i < first.AuthoredPattern.Exchanges.Count; i++)
            {
                Assert.That(first.AuthoredPattern.Exchanges[i].WindCue,
                    Is.EqualTo(second.AuthoredPattern.Exchanges[i].WindCue));
                Assert.That(first.AuthoredPattern.Exchanges[i].Timing,
                    Is.EqualTo(second.AuthoredPattern.Exchanges[i].Timing));
                Assert.That(first.AuthoredPattern.Exchanges[i].Trajectory,
                    Is.EqualTo(second.AuthoredPattern.Exchanges[i].Trajectory));
                Assert.That(first.TryExchange(.8f, .85f), Is.True);
                Assert.That(first.AwardPlayerPoint(), Is.True);
            }

            Assert.That(first.PlayerPoints, Is.EqualTo(5));
            Assert.That(first.LongestRally, Is.EqualTo(1));
            Assert.That(first.PrimaryObjectiveComplete, Is.True);
        }

        [Test]
        public void RallyTargetWithoutFivePoints_DoesNotPass()
        {
            Assert.That(BadmintonRules.ForTest(4, 0, 50).BuildResult().Pass, Is.False);
        }

        [Test]
        public void PrimaryObjectiveRequiresFivePointLead()
        {
            var rules = BadmintonRules.ForTest(5, 5, 50);

            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void Lifecycle_GatesExchangeAndResolvesOnce()
        {
            var lifecycle = new MinigameLifecycle(2f, 3f);
            var rules = new BadmintonRules(lifecycle);

            Assert.That(rules.TryExchange(.8f, .85f), Is.False);
            lifecycle.Tick(2f);
            lifecycle.Tick(3f);
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Play));

            for (var point = 0; point < 5; point++)
            {
                Assert.That(rules.TryExchange(.8f, .85f), Is.True);
                Assert.That(rules.AwardPlayerPoint(), Is.True);
            }

            Assert.That(rules.BeginResolve(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(rules.BeginResolve(), Is.False);
        }
    }
}
