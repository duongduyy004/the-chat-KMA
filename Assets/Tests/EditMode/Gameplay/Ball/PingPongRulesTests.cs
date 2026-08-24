using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class PingPongRulesTests
    {
        [Test]
        public void SpeedNeverExceedsConfiguredCap()
        {
            var rules = new PingPongRules(10, 25);
            for (var i = 0; i < 100; i++) rules.SuccessfulReturn(1f);
            Assert.That(rules.BallSpeed, Is.EqualTo(25f));
        }

        [Test]
        public void LongRallyWithoutFivePoints_DoesNotPass()
        {
            var rules = PingPongRules.ForTest(4, 0, 100);
            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void PrimaryObjectiveRequiresFivePointLead_NotRallyLength()
        {
            var rules = PingPongRules.ForTest(5, 5, 100);
            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void TryReturn_LaunchesPhysicalBallAtCappedSpeed()
        {
            var rules = new PingPongRules(10f, 12f);
            var gameObject = new GameObject("ping-pong-capped-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            for (var i = 0; i < 100; i++)
            {
                Assert.That(rules.TryReturn(ball, 1f, rules.AuthoredPattern.Exchanges[i % rules.AuthoredPattern.Exchanges.Count]), Is.True);
                Assert.That(ball.Body.linearVelocity.magnitude, Is.LessThanOrEqualTo(12f));
            }

            Assert.That(ball.Body.linearVelocity.magnitude, Is.EqualTo(12f).Within(.0001f));
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void ScoringRequiresSuccessfulAuthoredReturns_AndOnlyPrimaryObjectivePasses()
        {
            var rules = new PingPongRules(10, 25);
            var gameObject = new GameObject("ping-pong-scoring-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            Assert.That(rules.AwardPlayerPoint(), Is.False);
            for (var i = 0; i < 4; i++)
            {
                Assert.That(rules.TryReturn(ball, 1f, rules.AuthoredPattern.Exchanges[i]), Is.True);
                Assert.That(rules.AwardPlayerPoint(), Is.True);
            }

            Assert.That(rules.PlayerScore, Is.EqualTo(4));
            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.BuildResult().Pass, Is.False);

            Assert.That(rules.TryReturn(ball, 1f, rules.AuthoredPattern.Exchanges[0]), Is.True);
            Assert.That(rules.AwardPlayerPoint(), Is.True);

            Assert.That(rules.PlayerScore, Is.EqualTo(5));
            Assert.That(rules.PrimaryObjectiveComplete, Is.True);
            Assert.That(rules.BuildResult().Pass, Is.True);
            Assert.That(rules.BuildResult().Score, Is.InRange(0f, 10f));
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Lifecycle_RejectsReturnsOutsidePlay_AndResolvesAfterObjective()
        {
            var lifecycle = new MinigameLifecycle(2f, 3f);
            var rules = new PingPongRules(10, 25, lifecycle);
            var gameObject = new GameObject("ping-pong-lifecycle-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            Assert.That(rules.TryReturn(ball, 1f, rules.AuthoredPattern.Exchanges[0]), Is.False);
            lifecycle.Tick(2f);
            lifecycle.Tick(3f);
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(rules.TryReturn(ball, .74f, rules.AuthoredPattern.Exchanges[0]), Is.False);
            Assert.That(rules.TryReturn(ball, 1f, Vector2.zero), Is.False);

            for (var point = 0; point < 5; point++)
            {
                Assert.That(rules.TryReturn(ball, 1f, rules.AuthoredPattern.Exchanges[point % rules.AuthoredPattern.Exchanges.Count]), Is.True);
                Assert.That(rules.AwardPlayerPoint(), Is.True);
            }

            Assert.That(rules.PrimaryObjectiveComplete, Is.True);
            Assert.That(rules.BeginResolve(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(rules.BeginResolve(), Is.False);

            Object.DestroyImmediate(gameObject);
        }

    }
}
