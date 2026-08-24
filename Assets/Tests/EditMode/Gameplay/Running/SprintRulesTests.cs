using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintRulesTests
    {
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

        [Test]
        public void CompletionIsTheOnlyPrimaryObjective()
        {
            var rules = SprintRules.ForTest(distance: 99.9f, elapsed: 1f, rank: 1);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }
    }
}
