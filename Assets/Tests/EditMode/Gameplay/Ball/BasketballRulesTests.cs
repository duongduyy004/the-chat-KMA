using System.Collections.Generic;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class BasketballRulesTests
    {
        [Test]
        public void HoldAlone_DoesNotCreateOrAdvanceApex()
        {
            var rules = new BasketballRules(5, 30);

            rules.Hold(15f);

            Assert.That(rules.State, Is.EqualTo(BasketballState.Holding));
            Assert.That(rules.ApexProgress, Is.Zero);
            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
        }

        [Test]
        public void PassRequiresAuthoredVector_AndLaunchUsesAuthoredToss()
        {
            var rules = new BasketballRules(5, 30);
            var pattern = AlleyOopPattern.AuthoredDefault();
            var gameObject = new GameObject("basketball-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            Assert.That(rules.TryPass(ball, Vector2.zero), Is.False);
            Assert.That(rules.TryPass(ball, new Vector2(1f, .5f)), Is.True);
            Assert.That(rules.State, Is.EqualTo(BasketballState.Passing));
            Assert.That(rules.TryLaunchAuthoredAlleyOop(ball, pattern), Is.True);
            Assert.That(rules.State, Is.EqualTo(BasketballState.AlleyOopFlight));
            Assert.That(ball.Snapshot.IsInFlight, Is.True);
            Assert.That(ball.Body.linearVelocity, Is.EqualTo(pattern.LaunchVelocity).Using(Vector2Comparer.Instance));

            Object.DestroyImmediate(gameObject);
        }

        [TestCase(2.8f, .1f, FinishJudge.Perfect)]
        [TestCase(3.2f, -.1f, FinishJudge.Perfect)]
        [TestCase(2.79f, 0f, FinishJudge.Early)]
        [TestCase(3.21f, 0f, FinishJudge.Late)]
        [TestCase(3f, .11f, FinishJudge.Early)]
        [TestCase(3f, -.11f, FinishJudge.Late)]
        public void AlleyOopTap_JudgesAuthoredApexBoundary(float y, float vy, FinishJudge expected)
        {
            var rules = BasketballRules.InFlight(2.8f, 3.2f, .1f);

            Assert.That(rules.TapFinish(y, vy), Is.EqualTo(expected));
        }

        [Test]
        public void FinalTapIsRequiredForPrimaryObjective()
        {
            var rules = BasketballRules.InFlight(2.8f, 3.2f, .1f);

            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.TapFinish(3f, 0f), Is.EqualTo(FinishJudge.Perfect));
            Assert.That(rules.PrimaryObjectiveComplete, Is.False);
            Assert.That(rules.Baskets, Is.EqualTo(1));
        }

        [Test]
        public void ObjectiveAndScoring_RequireFiveAuthoredBaskets_NotComboShortcut()
        {
            var rules = BasketballRules.ForTest(4, 8, 20);

            Assert.That(rules.BuildResult().Pass, Is.False);

            rules = BasketballRules.ForTest(5, 5, 20);
            var result = rules.BuildResult();

            Assert.That(result.Pass, Is.True);
            Assert.That(result.Score, Is.GreaterThan(0f));
        }

        [Test]
        public void Lifecycle_RejectsInputOutsidePlay_AndResolvesOnceObjectiveIsComplete()
        {
            var lifecycle = new MinigameLifecycle(2f, 3f);
            var rules = new BasketballRules(1, 30, lifecycle);

            Assert.That(rules.TryPass(null, Vector2.right), Is.False);
            lifecycle.Tick(2f);
            lifecycle.Tick(3f);
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Play));

            var gameObject = new GameObject("basketball-lifecycle-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();
            Assert.That(rules.TryPass(ball, Vector2.right), Is.True);
            Assert.That(rules.TryLaunchAuthoredAlleyOop(ball, AlleyOopPattern.AuthoredDefault()), Is.True);
            Assert.That(rules.TapFinish(3f, 0f), Is.EqualTo(FinishJudge.Perfect));
            Assert.That(rules.State, Is.EqualTo(BasketballState.Resolved));
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
