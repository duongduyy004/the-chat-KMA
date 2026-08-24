using System.Collections.Generic;
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
        public void AuthoredReturnPattern_SelectsDeterministicExchangeAndLaunchesBallRig()
        {
            var pattern = ReturnPattern.AuthoredDefault();
            var gameObject = new GameObject("ping-pong-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            Assert.That(pattern.Exchanges.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(pattern.IsPlacementValid(pattern.Exchanges[0]), Is.True);
            Assert.That(pattern.TryLaunch(ball, 0), Is.True);
            Assert.That(ball.Snapshot.IsInFlight, Is.True);
            Assert.That(ball.Body.linearVelocity, Is.EqualTo(pattern.LaunchVelocity(0)).Using(Vector2Comparer.Instance));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void ScoringRequiresSuccessfulAuthoredReturns_AndOnlyPrimaryObjectivePasses()
        {
            var rules = new PingPongRules(10, 25);
            for (var i = 0; i < 4; i++)
            {
                rules.SuccessfulReturn(1f);
                rules.AwardPlayerPoint();
            }

            Assert.That(rules.PlayerScore, Is.EqualTo(4));
            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.BuildResult().Pass, Is.False);

            rules.SuccessfulReturn(1f);
            rules.AwardPlayerPoint();

            Assert.That(rules.PlayerScore, Is.EqualTo(5));
            Assert.That(rules.PrimaryObjectiveComplete, Is.True);
            Assert.That(rules.BuildResult().Pass, Is.True);
            Assert.That(rules.BuildResult().Score, Is.InRange(0f, 10f));
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

            for (var point = 0; point < 5; point++)
            {
                Assert.That(rules.TryReturn(ball, 1f, rules.AuthoredPattern.Exchanges[point % rules.AuthoredPattern.Exchanges.Count]), Is.True);
                rules.AwardPlayerPoint();
            }

            Assert.That(rules.PrimaryObjectiveComplete, Is.True);
            Assert.That(rules.BeginResolve(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(rules.BeginResolve(), Is.False);

            Object.DestroyImmediate(gameObject);
        }

        sealed class Vector2Comparer : IEqualityComparer<Vector2>
        {
            public static readonly Vector2Comparer Instance = new Vector2Comparer();
            public bool Equals(Vector2 left, Vector2 right) => Vector2.Distance(left, right) < .0001f;
            public int GetHashCode(Vector2 value) => value.GetHashCode();
        }
    }
}
