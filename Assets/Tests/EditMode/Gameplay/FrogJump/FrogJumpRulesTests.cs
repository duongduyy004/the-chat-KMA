using KMA.Gameplay.FrogJump;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.FrogJump
{
    public sealed class FrogJumpRulesTests
    {
        static readonly FrogJumpTuning Tuning = new FrogJumpTuning();
        const float Eps = 1e-3f;

        [TestCase(.5f, 3f)]
        [TestCase(.1f, 1f)]
        [TestCase(.9f, 1f)]
        [TestCase(.3f, 2f)]
        public void JumpShrinksLinearlyFromCentreToTheSafeEdge(float needle, float metres)
        {
            Assert.That(FrogJumpRules.JumpMetres(needle, Tuning), Is.EqualTo(metres).Within(Eps));
        }

        [TestCase(.05f)]
        [TestCase(.97f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void OutsideTheSafeZoneIsAFall(float needle)
        {
            Assert.That(FrogJumpRules.IsFall(needle, Tuning), Is.True);
            Assert.That(FrogJumpRules.JumpMetres(needle, Tuning), Is.Zero);
        }

        [Test]
        public void NeedleSweepsEdgeToEdgeAndBack()
        {
            var rules = new FrogJumpRules(Tuning);
            Assert.That(rules.Needle01, Is.Zero);
            rules.Tick(.6f);
            Assert.That(rules.Needle01, Is.EqualTo(.5f).Within(Eps));
            rules.Tick(.6f);
            Assert.That(rules.Needle01, Is.EqualTo(1f).Within(Eps));
            rules.Tick(.6f);
            Assert.That(rules.Needle01, Is.EqualTo(.5f).Within(Eps));
        }

        [Test]
        public void CentreStopJumpsThreeMetresAfterTheJumpTime()
        {
            var rules = new FrogJumpRules(Tuning);
            rules.Tick(.6f);
            Assert.That(rules.Stop(), Is.True);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Jumping));
            Assert.That(rules.Stop(), Is.False);
            rules.Tick(.59f);
            Assert.That(rules.Distance, Is.Zero);
            rules.Tick(.02f);
            Assert.That(rules.Distance, Is.EqualTo(3f).Within(Eps));
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Aiming));
            Assert.That(rules.Needle01, Is.Zero);
            Assert.That(rules.Jumps, Is.EqualTo(1));
        }

        [Test]
        public void EdgeStopFallsAndBlocksInputWhileRecovering()
        {
            var rules = new FrogJumpRules(Tuning);
            Assert.That(rules.Stop(), Is.True);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Fallen));
            Assert.That(rules.LastJumpMetres, Is.Zero);
            rules.Tick(2.4f);
            Assert.That(rules.Stop(), Is.False);
            rules.Tick(.2f);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Aiming));
            Assert.That(rules.Distance, Is.Zero);
            Assert.That(rules.Falls, Is.EqualTo(1));
        }

        [Test]
        public void ReachingFortyMetresWinsAndClampsDistance()
        {
            var rules = new FrogJumpRules(Tuning);
            for (int i = 0; i < 14 && !rules.IsOver; i++)
            {
                rules.Tick(.6f);
                rules.Stop();
                rules.Tick(.6f);
            }
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Finished));
            Assert.That(rules.ReachedFinish, Is.True);
            Assert.That(rules.Distance, Is.EqualTo(40f));
            Assert.That(rules.Progress01, Is.EqualTo(1f));
        }

        [Test]
        public void RunningOutOfTimeLoses()
        {
            var rules = new FrogJumpRules(Tuning);
            rules.Tick(59.9f);
            Assert.That(rules.IsOver, Is.False);
            rules.Tick(.2f);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.TimedOut));
            Assert.That(rules.ReachedFinish, Is.False);
            Assert.That(rules.Elapsed, Is.EqualTo(60f).Within(Eps));
            Assert.That(rules.TimeRemaining, Is.Zero);
            Assert.That(rules.Stop(), Is.False);
        }

        [Test]
        public void LandingOnTheFinishExactlyAtTheLimitStillWins()
        {
            var tuning = new FrogJumpTuning { trackMetres = 3f, timeLimitSeconds = 1.2f };
            var rules = new FrogJumpRules(tuning);
            rules.Tick(.6f);
            rules.Stop();
            rules.Tick(.6f);
            Assert.That(rules.State, Is.EqualTo(FrogJumpState.Finished));
        }
    }
}
