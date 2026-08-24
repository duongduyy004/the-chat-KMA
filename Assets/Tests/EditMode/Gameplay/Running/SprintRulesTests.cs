using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintRulesTests
    {
        [Test]
        public void AuthoredSequence_StartsLeftAndInvalidInputDoesNotAdvanceIt()
        {
            var rules = SprintRules.Default();

            Assert.That(rules.ExpectedSide, Is.EqualTo(Side.Left));
            rules.Tap(Side.Right);
            Assert.That(rules.ExpectedSide, Is.EqualTo(Side.Left));
            Assert.That(rules.ValidTapRatio, Is.EqualTo(0f));

            rules.Tap(Side.Left);
            Assert.That(rules.ExpectedSide, Is.EqualTo(Side.Right));
            rules.Tap(Side.Left);
            Assert.That(rules.ExpectedSide, Is.EqualTo(Side.Right));
            rules.Tap(Side.Right);
            Assert.That(rules.ExpectedSide, Is.EqualTo(Side.Left));
            Assert.That(rules.ValidTapRatio, Is.EqualTo(.5f).Within(.001f));
        }

        [Test]
        public void SameSideTap_GivesFortyPercentImpulse()
        {
            var rules = SprintRules.Default();

            rules.Tap(Side.Left);
            float first = rules.Speed;
            rules.Tap(Side.Left);

            Assert.That(rules.Speed - first, Is.EqualTo(7.2f).Within(.001f));
        }

        [Test]
        public void TopTwoAfterTimeout_DoesNotPass()
        {
            var rules = SprintRules.ForTest(distance: 100f, elapsed: 14.1f, rank: 1);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void EqualInputs_ProduceEqualSnapshots()
        {
            var a = SprintRules.Default();
            var b = SprintRules.Default();

            foreach (var side in new[] { Side.Left, Side.Right, Side.Left })
            {
                a.Tap(side);
                b.Tap(side);
                a.Tick(.1f);
                b.Tick(.1f);
            }

            Assert.That(a.Snapshot, Is.EqualTo(b.Snapshot));
        }

        [Test]
        public void FixedRivalProfiles_AreConsumedWithoutChangingPlayerSnapshot()
        {
            var profiles = new[]
            {
                new RivalPaceProfile("FastStart", 8f, 4f),
                new RivalPaceProfile("Endurance", 5f, 6f)
            };
            var rules = new SprintRules(rivalProfiles: profiles);

            rules.Tick(.5f);

            Assert.That(rules.RivalProfiles, Is.EqualTo(profiles));
            Assert.That(rules.Snapshot.Distance, Is.EqualTo(0f));
        }

        [TestCase(0f, StaminaBand.Low)]
        [TestCase(29.999f, StaminaBand.Low)]
        [TestCase(30f, StaminaBand.Mid)]
        [TestCase(69.999f, StaminaBand.Mid)]
        [TestCase(70f, StaminaBand.High)]
        [TestCase(100f, StaminaBand.High)]
        public void StaminaBand_UsesExplicitDeterministicBoundaries(float stamina, StaminaBand expected)
        {
            Assert.That(SprintRules.ClassifyStamina(stamina), Is.EqualTo(expected));
        }

        [Test]
        public void RivalProfiles_DetermineDeterministicRankAndDistance()
        {
            var fast = new SprintRules(rivalProfiles: new[]
            {
                new RivalPaceProfile("Fast", 100f, 100f)
            });
            var slow = new SprintRules(rivalProfiles: new[]
            {
                new RivalPaceProfile("Slow", 0f, 0f)
            });
            var repeat = new SprintRules(rivalProfiles: new[]
            {
                new RivalPaceProfile("Fast", 100f, 100f)
            });

            fast.Tap(Side.Left);
            slow.Tap(Side.Left);
            repeat.Tap(Side.Left);
            fast.Tick(1f);
            slow.Tick(1f);
            repeat.Tick(1f);

            Assert.That(fast.RivalDistances, Is.EqualTo(repeat.RivalDistances));
            Assert.That(fast.Rank, Is.EqualTo(repeat.Rank));
            Assert.That(fast.Rank, Is.GreaterThan(slow.Rank));
            Assert.That(fast.RivalDistances[0], Is.GreaterThan(slow.RivalDistances[0]));
        }

        [Test]
        public void StaminaIsEfficiencyOnly_AndDoesNotCreateAnotherPassGate()
        {
            var rules = SprintRules.ForTest(distance: 100f, elapsed: 13.9f, rank: 4, stamina: 0f);

            Assert.That(rules.BuildResult().Pass, Is.True);
        }

        [Test]
        public void CompletionIsTheOnlyPrimaryObjective()
        {
            var rules = SprintRules.ForTest(distance: 99.9f, elapsed: 1f, rank: 1);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }
    }
}
