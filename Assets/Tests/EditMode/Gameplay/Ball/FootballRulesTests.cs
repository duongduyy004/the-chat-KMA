using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballRulesTests
    {
        [Test]
        public void AimLocksAndPowerOscillatesWhileHeld()
        {
            var rules = NewRules();

            Assert.That(rules.BeginCharge(), Is.False);
            Assert.That(rules.Start(), Is.True);
            rules.Tick(.85f);
            Assert.That(rules.AimX, Is.EqualTo(.9f).Within(.0001f));
            Assert.That(rules.LockAim(), Is.True);
            rules.Tick(60f);
            Assert.That(rules.AimX, Is.EqualTo(.9f).Within(.0001f));
            Assert.That(rules.BeginCharge(), Is.True);
            rules.Tick(1.4f);
            Assert.That(rules.Power, Is.EqualTo(1f).Within(.0001f));
            rules.Tick(1.4f);
            Assert.That(rules.Power, Is.EqualTo(0f).Within(.0001f));
            Assert.That(rules.ReleaseShot(), Is.True);
            Assert.That(rules.ReleaseShot(), Is.False);
        }

        [TestCase(2, false, 0f)]
        [TestCase(3, true, 6f)]
        [TestCase(4, true, 8f)]
        [TestCase(5, true, 10f)]
        public void FiveKicksRequireThreeGoals(int goalsExpected, bool passed, float expectedScore)
        {
            var rules = NewRules();
            Assert.That(rules.Start(), Is.True);

            for (int kick = 0; kick < 5; kick++)
            {
                AdvanceAimToRightEdge(rules);

                Assert.That(rules.LockAim(), Is.True, $"lock aim for kick {kick + 1}");
                Assert.That(rules.BeginCharge(), Is.True, $"begin kick {kick + 1}");
                if (kick < goalsExpected)
                    rules.Tick(1.19f); // Normal force reaches exactly .85 on its rising half.
                else
                    rules.Tick(2.8f); // One full power cycle returns to zero for a short MISS.
                Assert.That(rules.ReleaseShot(), Is.True);
                Assert.That(rules.Kicks, Is.EqualTo(kick)); // Count commits after shot outcome.
                rules.Tick(.18f);
                rules.Tick(1.91f);

                if (kick < 4)
                    Assert.That(rules.State, Is.EqualTo(FootballState.Aiming));
                else
                    Assert.That(rules.State, Is.EqualTo(FootballState.MatchResult));
            }

            Assert.That(rules.Kicks, Is.EqualTo(5));
            Assert.That(rules.Goals, Is.EqualTo(goalsExpected));
            Assert.That(rules.BuildResult().Pass, Is.EqualTo(passed));
            Assert.That(rules.BuildResult().Score, Is.EqualTo(expectedScore));
            Assert.That(rules.LockAim(), Is.False);
        }

        [Test]
        public void ThreeGoalsDoNotEndTheMatchBeforeFifthKick()
        {
            var rules = NewRules();
            rules.Start();

            for (int i = 0; i < 4; i++)
            {
                AdvanceAimToRightEdge(rules);
                rules.LockAim();
                rules.BeginCharge();
                rules.Tick(i < 3 ? 1.19f : 2.8f);
                rules.ReleaseShot();
                rules.Tick(.18f);
                rules.Tick(1.91f);
            }

            Assert.That(rules.Goals, Is.EqualTo(3));
            Assert.That(rules.State, Is.EqualTo(FootballState.Aiming));
            Assert.That(rules.Kicks, Is.EqualTo(4));
        }

        [Test]
        public void InvalidDeltaDoesNotMutateState()
        {
            var rules = NewRules();
            rules.Start();
            rules.Tick(.3f);
            float before = rules.AimX;

            rules.Tick(float.NaN);
            rules.Tick(float.PositiveInfinity);
            rules.Tick(-1f);

            Assert.That(rules.AimX, Is.EqualTo(before));
            Assert.That(rules.State, Is.EqualTo(FootballState.Aiming));
        }

        [Test]
        public void LargeAndSmallTicksProduceTheSameAimAndPower()
        {
            var large = NewRules();
            var small = NewRules();
            large.Start();
            small.Start();

            large.Tick(5.3f);
            for (int i = 0; i < 53; i++) small.Tick(.1f);

            Assert.That(large.AimX, Is.EqualTo(small.AimX).Within(.0001f));
            large.LockAim();
            small.LockAim();
            large.BeginCharge();
            small.BeginCharge();
            large.Tick(3.7f);
            for (int i = 0; i < 37; i++) small.Tick(.1f);

            Assert.That(large.Power, Is.EqualTo(small.Power).Within(.0001f));
            Assert.That(large.State, Is.EqualTo(small.State));
        }

        [Test]
        public void DefaultNoiseIsSampledOnlyForPowerAboveAccuracyLimit()
        {
            int calls = 0;
            var rules = new FootballRules(FootballTuning.For(FootballDifficulty.Normal), () =>
            {
                calls++;
                return 0f;
            });
            rules.Start();
            rules.LockAim();
            rules.BeginCharge();
            rules.Tick(1.19f);
            rules.ReleaseShot();
            Assert.That(calls, Is.Zero);

            var highPower = new FootballRules(FootballTuning.For(FootballDifficulty.Normal), () =>
            {
                calls++;
                return .5f;
            });
            highPower.Start();
            highPower.LockAim();
            highPower.BeginCharge();
                highPower.Tick(1.204f);
            highPower.ReleaseShot();
            Assert.That(calls, Is.EqualTo(1));
        }

        static FootballRules NewRules() =>
            new FootballRules(FootballTuning.For(FootballDifficulty.Normal), () => 0f);

        static void AdvanceAimToRightEdge(FootballRules rules)
        {
            for (int i = 0; i < 20000 && rules.AimX < .8998f; i++)
                rules.Tick(.0001f);
            Assert.That(rules.State, Is.EqualTo(FootballState.Aiming));
            Assert.That(rules.AimX, Is.EqualTo(.9f).Within(.0002f));
        }
    }
}
