using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class VolleyballControllerTests : InputTestFixture
    {
        readonly List<Object> temporaryObjects = new List<Object>();

        public override void TearDown()
        {
            for (int index = temporaryObjects.Count - 1; index >= 0; index--)
                Object.DestroyImmediate(temporaryObjects[index]);
            temporaryObjects.Clear();
            base.TearDown();
        }

        [Test]
        public void ConfigureForTest_ExposesConfiguredRulesBallAndLiveStateInPlay()
        {
            var fixture = CreateFixture();

            AdvanceRulesToPlay(fixture.Rules);

            Assert.That(fixture.Controller.Rules, Is.SameAs(fixture.Rules));
            Assert.That(fixture.Controller.Ball, Is.SameAs(fixture.Ball));
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.LongestCombo, Is.Zero);
        }

        [TestCase(BallContext.Low, 0f, -1f, VolleyAction.Dig, 5f)]
        [TestCase(BallContext.Rising, 0f, 1f, VolleyAction.Set, 5f)]
        [TestCase(BallContext.ApexNearNet, 1f, -1f, VolleyAction.Spike, 8f)]
        public void SubmitSwipe_ResolvesEachAuthoredActionThroughRulesAndLaunchesOnce(
            BallContext expectedContext,
            float swipeX,
            float swipeY,
            VolleyAction expectedAction,
            float expectedLaunchForce)
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);
            SetBallForContext(fixture, expectedContext);

            fixture.Controller.SubmitSwipe(new Vector2(swipeX, swipeY), inReachZone: true, timingAccuracy: 1f);

            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(expectedContext));
            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
            Assert.That(fixture.Ball.Body.velocity.magnitude, Is.EqualTo(expectedLaunchForce).Within(.001f));
            Assert.That(ResolveExpectedAction(expectedContext, new Vector2(swipeX, swipeY)), Is.EqualTo(expectedAction));
        }

        [Test]
        public void SubmitSwipe_RejectsOutOfReachAndBelowThresholdWithoutChangingTouchOrScore()
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: false, timingAccuracy: 1f);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: .74f);

            Assert.That(fixture.Rules.TotalTouches, Is.Zero);
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.False);
        }

        [TestCase(SwipeDirection.Left)]
        [TestCase(SwipeDirection.Right)]
        [TestCase(SwipeDirection.Up)]
        [TestCase(SwipeDirection.Down)]
        public void GameplayInputRouter_OnSwipeRoutesEveryCardinalDirectionToController(SwipeDirection direction)
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);
            SetBallForContext(fixture, BallContext.Low);

            RouteSwipe(fixture.SwipeDetector, direction);

            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
        }

        [Test]
        public void ContextAndReach_AreCalculatedFromBallVelocityNetWindowAndReachBounds()
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);

            SetBallForContext(fixture, BallContext.Low);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.InReachZone, Is.True);

            SetBallForContext(fixture, BallContext.Rising);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Rising));
            Assert.That(fixture.Controller.InReachZone, Is.True);

            SetBallForContext(fixture, BallContext.ApexNearNet);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.ApexNearNet));
            Assert.That(fixture.Controller.InReachZone, Is.True);

            fixture.Ball.Body.position = new Vector2(4f, 2f);
            fixture.Ball.Body.velocity = Vector2.down;
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.InReachZone, Is.False);
        }

        [Test]
        public void AutoPositioning_UsesBallPredictionWithoutMutatingBallPhysics()
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);
            fixture.Ball.Launch(new Vector2(1f, 1f), 4f, .25f);
            fixture.Ball.Body.position = new Vector2(.25f, 2f);
            fixture.Ball.Body.velocity = new Vector2(3f, 2f);
            Vector2 prediction = fixture.Ball.PredictLandingPoint();
            Vector2 position = fixture.Ball.Body.position;
            Vector2 velocity = fixture.Ball.Body.velocity;

            fixture.Controller.SimulateForTest(Time.fixedDeltaTime);

            Assert.That(fixture.Controller.PredictedLandingPoint.x, Is.EqualTo(prediction.x).Within(.001f));
            Assert.That(fixture.Controller.PredictedLandingPoint.y, Is.EqualTo(prediction.y).Within(.001f));
            Assert.That(fixture.Ball.Body.position, Is.EqualTo(position));
            Assert.That(fixture.Ball.Body.velocity, Is.EqualTo(velocity));
        }

        [Test]
        public void ThirdValidTouch_ShowsCounterplayCuesWithoutChangingPointsOrPrediction()
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);

            for (int touch = 0; touch < 3; touch++)
            {
                SetBallForContext(fixture, BallContext.Low);
                fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            }
            Vector2 predictionBeforeCueFrame = fixture.Ball.PredictLandingPoint();
            fixture.Controller.SimulateForTest(0f);

            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(3));
            Assert.That(fixture.Controller.OpponentCounterCueVisible, Is.True);
            Assert.That(fixture.Controller.OpponentFakeCueVisible, Is.True);
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.PredictedLandingPoint.x, Is.EqualTo(predictionBeforeCueFrame.x).Within(.001f));
            Assert.That(fixture.Controller.PredictedLandingPoint.y, Is.EqualTo(predictionBeforeCueFrame.y).Within(.001f));
        }

        [Test]
        public void HudAndCompletion_FollowRulesResultAndCompleteOnlyOnce()
        {
            var fixture = CreateFixture();
            AdvanceRulesToPlay(fixture.Rules);

            Assert.That(fixture.Controller.BuildHudState().statusText, Is.EqualTo("TOUCH 1/2/3"));
            fixture.Rules.AwardRallyPoint();
            Assert.That(fixture.Controller.BuildHudState().score, Is.EqualTo(fixture.Rules.BuildResult().Score));

            int completions = 0;
            fixture.Controller.Completed += _ => completions++;
            fixture.Rules.AwardRallyPoint();
            fixture.Controller.SimulateForTest(0f);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            fixture.Controller.SimulateForTest(1f);

            Assert.That(completions, Is.EqualTo(1));
        }

        ControllerFixture CreateFixture()
        {
            var controllerObject = new GameObject("VolleyballControllerTest");
            temporaryObjects.Add(controllerObject);
            controllerObject.AddComponent<BoxCollider2D>().size = new Vector2(2f, 4f);
            var router = controllerObject.AddComponent<GameplayInputRouter>();
            var controller = controllerObject.AddComponent<VolleyballController>();

            var ballObject = new GameObject("VolleyballBallTest");
            temporaryObjects.Add(ballObject);
            ballObject.AddComponent<Rigidbody2D>();
            var ball = ballObject.AddComponent<BallRig>();
            var profile = FlightProfile.Create(1f, 0f, -10f, 1f);
            temporaryObjects.Add(profile);
            ball.SetProfile(profile);

            var detector = new SwipeInputDetector();
            router.SetDetectors(null, null, null, null, detector);
            var rules = new VolleyballRules(targetScore: 2, timeLimit: 60f);
            controller.ConfigureForTest(rules, ball);
            return new ControllerFixture(controller, rules, ball, detector);
        }

        static void AdvanceRulesToPlay(VolleyballRules rules)
        {
            rules.Tick(2f);
            rules.Tick(3f);
        }

        static void SetBallForContext(ControllerFixture fixture, BallContext context)
        {
            fixture.Ball.Body.position = context == BallContext.ApexNearNet
                ? new Vector2(0f, 2f)
                : new Vector2(0f, 1f);
            fixture.Ball.Body.velocity = context switch
            {
                BallContext.Low => new Vector2(0f, -2f),
                BallContext.Rising => new Vector2(0f, 2f),
                BallContext.ApexNearNet => Vector2.zero,
                _ => throw new System.ArgumentOutOfRangeException(nameof(context), context, null)
            };
        }

        static VolleyAction ResolveExpectedAction(BallContext context, Vector2 swipe) =>
            VolleyballRules.ResolveGesture(context, swipe);

        static void RouteSwipe(SwipeInputDetector detector, SwipeDirection direction)
        {
            Vector2 end = direction switch
            {
                SwipeDirection.Left => Vector2.left,
                SwipeDirection.Right => Vector2.right,
                SwipeDirection.Up => Vector2.up,
                SwipeDirection.Down => Vector2.down,
                _ => throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
            detector.FeedSample(Vector2.zero, 0d);
            detector.FeedSample(end * 100f, 1d);
            detector.FeedEnd();
        }

        readonly struct ControllerFixture
        {
            public ControllerFixture(VolleyballController controller, VolleyballRules rules, BallRig ball, SwipeInputDetector swipeDetector)
            {
                Controller = controller;
                Rules = rules;
                Ball = ball;
                SwipeDetector = swipeDetector;
            }

            public VolleyballController Controller { get; }
            public VolleyballRules Rules { get; }
            public BallRig Ball { get; }
            public SwipeInputDetector SwipeDetector { get; }
        }
    }
}
