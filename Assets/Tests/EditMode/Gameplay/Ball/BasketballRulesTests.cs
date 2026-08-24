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
        public void AuthoredPass_GeneratesLaunchPattern_AndRejectsReplacement()
        {
            var rules = new BasketballRules(5, 30);
            var gameObject = new GameObject("basketball-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            Assert.That(rules.TryPass(ball, Vector2.zero), Is.False);
            var leftwardPass = new Vector2(-1f, .5f);
            Assert.That(rules.TryPass(ball, leftwardPass), Is.True);
            Assert.That(rules.State, Is.EqualTo(BasketballState.Passing));
            var generated = rules.AuthoredPattern;
            Assert.That(generated, Is.Not.Null);
            Assert.That(generated.PassVector, Is.EqualTo(leftwardPass));
            var unrelatedReplacement = AlleyOopPattern.AuthoredDefault(Vector2.right);
            Assert.That(rules.TryLaunchAlleyOop(ball, unrelatedReplacement), Is.False);
            Assert.That(rules.State, Is.EqualTo(BasketballState.Passing));
            Assert.That(rules.TryLaunchAlleyOop(ball), Is.True);
            Assert.That(rules.State, Is.EqualTo(BasketballState.AlleyOopFlight));
            Assert.That(ball.Snapshot.IsInFlight, Is.True);
            Assert.That(ball.Body.linearVelocity, Is.EqualTo(generated.LaunchVelocity).Using(Vector2Comparer.Instance));
            Assert.That(ball.Body.linearVelocity.x, Is.LessThan(0f));

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
            var rules = new BasketballRules(5, 30);
            var gameObject = new GameObject("basketball-objective-ball-test");
            gameObject.AddComponent<Rigidbody2D>();
            var ball = gameObject.AddComponent<BallRig>();

            for (var basket = 0; basket < 4; basket++)
                CompleteAuthoredBasket(rules, ball, new Vector2(-1f, .75f));

            Assert.That(rules.Baskets, Is.EqualTo(4));
            Assert.That(rules.Combo, Is.EqualTo(4));
            Assert.That(rules.BuildResult().Pass, Is.False);

            CompleteAuthoredBasket(rules, ball, new Vector2(-1f, .75f));
            var result = rules.BuildResult();

            Assert.That(rules.Baskets, Is.EqualTo(5));
            Assert.That(result.Pass, Is.True);
            Assert.That(result.Score, Is.GreaterThan(0f));

            Object.DestroyImmediate(gameObject);
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
            Assert.That(rules.TryLaunchAlleyOop(ball), Is.True);
            Assert.That(rules.TapFinish(3f, 0f), Is.EqualTo(FinishJudge.Perfect));
            Assert.That(rules.State, Is.EqualTo(BasketballState.Resolved));
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(rules.BeginResolve(), Is.False);

            Object.DestroyImmediate(gameObject);
        }

        static void CompleteAuthoredBasket(BasketballRules rules, BallRig ball, Vector2 passVector)
        {
            Assert.That(rules.TryPass(ball, passVector), Is.True);
            var authoredPattern = rules.AuthoredPattern;
            Assert.That(rules.TryLaunchAlleyOop(ball), Is.True);
            Assert.That(authoredPattern.IsApexWindow(3f, 0f), Is.True);
            Assert.That(rules.TapFinish(3f, 0f), Is.EqualTo(FinishJudge.Perfect));
        }

        sealed class Vector2Comparer : IEqualityComparer<Vector2>
        {
            public static readonly Vector2Comparer Instance = new Vector2Comparer();
            public bool Equals(Vector2 left, Vector2 right) => Vector2.Distance(left, right) < .0001f;
            public int GetHashCode(Vector2 value) => value.GetHashCode();
        }
    }
}
