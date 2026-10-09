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
        public void LongCombo_RaisesTopSpeedWithinTheBoostLimit()
        {
            SprintBalanceParameters tuning = SprintBalanceParameters.Default;
            var plain = new SprintRules(60f, null, null, tuning);
            var combo = new SprintRules(60f, null, null, tuning);
            for (int tap = 0; tap < 60; tap++)
            {
                plain.Tap(tap % 3 == 0 ? Side.Right : Side.Left);
                combo.Tap(tap % 2 == 0 ? Side.Left : Side.Right);
                plain.Tick(.01f);
                combo.Tick(.01f);
            }

            Assert.That(combo.IsComboBoosting, Is.True);
            Assert.That(plain.IsComboBoosting, Is.False);
            Assert.That(combo.Speed, Is.GreaterThan(tuning.SpeedCap));
            Assert.That(combo.Speed, Is.LessThanOrEqualTo(tuning.SpeedCap * (1f + tuning.ComboBoostMax)));
        }

        [Test]
        public void WrongTap_EndsTheComboBoost()
        {
            var rules = new SprintRules(60f, null, null, SprintBalanceParameters.Default);
            for (int tap = 0; tap < 20; tap++) rules.Tap(tap % 2 == 0 ? Side.Left : Side.Right);
            Assert.That(rules.IsComboBoosting, Is.True);

            rules.Tap(Side.Right);

            Assert.That(rules.IsComboBoosting, Is.False);
        }

        [Test]
        public void StoppingTaps_BrakesTheRunnerToAHaltWithinASecond()
        {
            var rules = new SprintRules(60f, null, null, SprintBalanceParameters.Default);
            for (int tap = 0; tap < 12; tap++)
            {
                rules.Tap(tap % 2 == 0 ? Side.Left : Side.Right);
                rules.Tick(1f / 6f);
            }
            float top = rules.Speed;

            rules.Tick(.2f);
            Assert.That(rules.Speed, Is.GreaterThan(top * .9f), "a short pause between taps keeps momentum");
            for (int i = 0; i < 20; i++) rules.Tick(1f / 60f);
            Assert.That(rules.Speed, Is.GreaterThan(0f), "the runner still slides instead of freezing");
            for (int i = 0; i < 30; i++) rules.Tick(1f / 60f);
            Assert.That(rules.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void IdleBrake_DoesNotDependOnFrameRate()
        {
            var smooth = new SprintRules(60f, null, null, SprintBalanceParameters.Default);
            var hitch = new SprintRules(60f, null, null, SprintBalanceParameters.Default);
            smooth.Tap(Side.Left);
            hitch.Tap(Side.Left);

            for (int i = 0; i < 27; i++) smooth.Tick(1f / 60f);
            hitch.Tick(.45f);

            Assert.That(hitch.Speed, Is.EqualTo(smooth.Speed).Within(.01f));
            Assert.That(hitch.Speed, Is.GreaterThan(0f));
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
                new RivalPaceProfile("RunnerB", 5f, 6f)
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
        public void EmptyStamina_DoesNotCreateAnotherPassGate()
        {
            var rules = SprintRules.ForTest(distance: 100f, elapsed: 13.9f, rank: 4, stamina: 0f);

            Assert.That(rules.BuildResult().Pass, Is.True);
        }

        [Test]
        public void FinishingFirst_ScoresAboveFinishingLast_ForTheSameRun()
        {
            RivalPaceProfile[] Rivals(float speed) => new[]
            {
                new RivalPaceProfile("A", speed, speed),
                new RivalPaceProfile("B", speed, speed),
                new RivalPaceProfile("C", speed, speed)
            };
            var first = SprintRules.ForTest(distance: 100f, elapsed: 11f, rank: 1, rivalProfiles: Rivals(0f));
            var last = SprintRules.ForTest(distance: 100f, elapsed: 11f, rank: 4, rivalProfiles: Rivals(200f));
            first.Tick(1f);
            last.Tick(1f);

            Assert.That(first.Rank, Is.EqualTo(1));
            Assert.That(last.Rank, Is.EqualTo(4));
            Assert.That(first.BuildResult().Score, Is.EqualTo(7.7f).Within(.001f));
            Assert.That(last.BuildResult().Score, Is.EqualTo(6.7f).Within(.001f));
        }

        [Test]
        public void LeftoverStamina_DoesNotChangeTheScore()
        {
            var spent = SprintRules.ForTest(distance: 100f, elapsed: 12f, rank: 1, stamina: 0f);
            var saved = SprintRules.ForTest(distance: 100f, elapsed: 12f, rank: 1, stamina: 100f);

            Assert.That(spent.BuildResult().Score, Is.EqualTo(saved.BuildResult().Score));
        }

        [Test]
        public void CompletionIsTheOnlyPrimaryObjective()
        {
            var rules = SprintRules.ForTest(distance: 99.9f, elapsed: 1f, rank: 1);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }
    }
}
